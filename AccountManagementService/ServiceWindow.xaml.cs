using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using AccountManagementLibrary;

namespace AccountManagementService
{
    public partial class MainWindow : Window
    {
        const int udpPort = 4355;
        const int tcpPort = 4356;

        private static readonly object groupsLock = new();
        private static readonly object usersLock = new();
        private static readonly object loggedInUsersLock = new();
        private readonly ObservableCollection<UserWithToken> loggedInUsers;

        private readonly ChartModels chartModels;

        private UdpClient udpListener;
        private TcpListener tcpListener;

        public MainWindow()
        {
            InitializeComponent();

            loggedInUsers = new ObservableCollection<UserWithToken>();
            lbLoggedInUsers.ItemsSource = loggedInUsers;

            // Show the current data
            chartModels = new ChartModels(pie, DataManager.GroupData, bar, DataManager.UserData);

            // TODO: start the services
            _ = Task.Run(async () =>
            {
                await StartLoginService(udpPort);
            });

            _ = Task.Run(async () =>
            {
                await StartAccountManagementService(tcpPort);
            });
        }

        private async Task StartLoginService(int port)
        {
            IPEndPoint localEp = new(IPAddress.Loopback, port);

            udpListener = new(localEp);

            await Console.Out.WriteLineAsync($"UDP server started on {localEp} 🚀");

            while (true)
            {
                var result = await udpListener.ReceiveAsync();

                _ = Task.Factory.StartNew(async (state) =>
                {
                    var udpRes = (UdpReceiveResult)state;

                    var remoteEp = udpRes.RemoteEndPoint;
                    var payload = Encoding.UTF8.GetString(udpRes.Buffer);

                    await Console.Out.WriteLineAsync("Received request: " + payload);

                    var data = payload.Split('#');
                    var reqType = data[0];

                    string response = "failed";

                    switch (reqType)
                    {
                        case "login":
                            response = HandleLoginRequest(username: data[1], password: data[2]);
                            break;
                        case "logout":
                            response = HandleLogoutRequest(username: data[1], auth_token: data[2]);
                            break;
                        default:
                            break;
                    }

                    var resBytes = Encoding.UTF8.GetBytes(response);
                    await udpListener.SendAsync(resBytes, resBytes.Length, remoteEp);
                }, result);
            }
        }

        private string HandleLoginRequest(string username, string password)
        {
            var user = DataManager.UserData.Values.FirstOrDefault(u => u.Login == username);

            if (user == null)
                return "failed";

            if (!(user.PasswordHash == password))
                return "failed";

            var auth_token = Guid.NewGuid().ToString();

            lock (loggedInUsersLock)
                Dispatcher.InvokeAsync(() =>
                    loggedInUsers.Add(new UserWithToken(user, auth_token)));


            return auth_token;
        }

        private string HandleLogoutRequest(string username, string auth_token)
        {
            var loggedInUser = loggedInUsers.FirstOrDefault(u => u.User.Login == username);

            if (loggedInUser == null)
                return "failed";

            lock (loggedInUsersLock)
                Dispatcher.InvokeAsync(() =>
                    loggedInUsers.Remove(loggedInUser));

            return "ok";
        }

        private async Task StartAccountManagementService(int port)
        {
            IPEndPoint localEp = new(IPAddress.Loopback, port);

            tcpListener = new(localEp);

            tcpListener.Start();

            while (true)
            {
                var tcpClient = await tcpListener.AcceptTcpClientAsync();

                _ = Task.Factory.StartNew(async (state) =>
                {
                    var client = (TcpClient)state;

                    using NetworkStream stream = client.GetStream();

                    string payload;

                    using (var br = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
                        payload = br.ReadString();

                    try
                    {
                        switch (payload)
                        {
                            case "getusers":
                                var users = DataManager.UserData.Values.OrderBy(u => u.Lastname).ThenBy(u => u.Firstname);
                                await JsonSerializer.SerializeAsync(stream, users);
                                break;

                            case "getgroups":
                                var groups = DataManager.GroupData.OrderBy(g => g.Name);
                                await JsonSerializer.SerializeAsync(stream, groups);
                                break;

                            case "addgroup":
                                await HandleAddGroupRequest(stream);
                                break;

                            case "addtogroup":
                                await HandleAddToGroupRequest(stream);
                                break;

                            case "removegroup":
                                await HandleRemoveGroupRequest(stream);
                                break;

                            case "removefromgroup":
                                await HandleRemoveFromGroupRequest(stream);
                                break;

                            default:
                                throw new InvalidOperationException("Invalid payload type...");
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        using StreamWriter sw = new(stream);
                        sw.Write(ex.Message);
                    }

                }, tcpClient);
            }
        }

        private async Task HandleAddGroupRequest(NetworkStream stream)
        {
            string name;

            using (var br = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
                name = br.ReadString();

            if (DataManager.GroupData.Any(g => g.Name == name))
                throw new InvalidOperationException("Group name is already taken...");

            int nextId = DataManager.GroupData.Max(g => g.Gid) + 1;


            lock (groupsLock)
                DataManager.GroupData.Add(new Group(nextId, name));

            chartModels.UpdateCharts();

            await JsonSerializer.SerializeAsync(stream, nextId);
        }

        private async Task HandleAddToGroupRequest(NetworkStream stream)
        {
            int gid;
            int uid;

            using (var br = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                gid = br.ReadInt32();
                uid = br.ReadInt32();
            }

            var user = DataManager.UserData.Values.FirstOrDefault(u => u.Uid == uid) ??
                throw new InvalidOperationException("User does not exist");

            var group = DataManager.GroupData.FirstOrDefault(g => g.Gid == gid) ??
                throw new InvalidOperationException("Group does not exist...");

            lock (usersLock)
                group.Users.Add(user);

            chartModels.UpdateCharts();

            await JsonSerializer.SerializeAsync(stream, true);
        }

        private async Task HandleRemoveGroupRequest(NetworkStream stream)
        {
            int gid;

            using (var br = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
                gid = br.ReadInt32();

            var group = DataManager.GroupData.FirstOrDefault(g => g.Gid == gid);

            if (group == null)
                throw new InvalidOperationException("Group does not exist...");

            lock (groupsLock)
                DataManager.GroupData.Remove(group);

            chartModels.UpdateCharts();

            await JsonSerializer.SerializeAsync(stream, true);
        }

        private async Task HandleRemoveFromGroupRequest(NetworkStream stream)
        {
            int gid;
            int uid;

            using (var br = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            {
                gid = br.ReadInt32();
                uid = br.ReadInt32();
            }

            var user = DataManager.UserData.Values.FirstOrDefault(u => u.Uid == uid) ??
                throw new InvalidOperationException("User does not exist");

            var group = DataManager.GroupData.FirstOrDefault(g => g.Gid == gid) ??
                throw new InvalidOperationException("Group does not exist...");

            lock (usersLock)
                group.Users.Remove(user);

            chartModels.UpdateCharts();

            await JsonSerializer.SerializeAsync(stream, true);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            udpListener.Close();
            base.OnClosing(e);
        }
    }
}
