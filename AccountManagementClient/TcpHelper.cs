using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AccountManagementLibrary;

namespace AccountManagementClient
{
    internal class TcpHelper
    {
        const int tcpPort = 4356;
        private static readonly IPEndPoint tcpEndpoint = new(IPAddress.Loopback, tcpPort);

        internal static async Task<List<User>> LoadUsersAsync()
        {
            TcpClient client = new TcpClient();

            await client.ConnectAsync(tcpEndpoint);
            
            using var stream = client.GetStream();

            var payload = "getusers";

            using (var bw = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                bw.Write(payload);

            List<User> users = JsonSerializer.Deserialize<List<User>>(stream);

            return users;
        }

        internal static async Task<ObservableCollection<Group>> LoadGroupsAsync()
        {
            TcpClient client = new TcpClient();

            await client.ConnectAsync(tcpEndpoint);

            using var stream = client.GetStream();

            var payload = "getgroups";

            using (var bw = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                bw.Write(payload);

            var groups = JsonSerializer.Deserialize<ObservableCollection<Group>>(stream);

            return groups;
        }

        internal static async Task<int> ProcessAddGroupAsync(string groupname)
        {
            TcpClient client = new TcpClient();

            await client.ConnectAsync(tcpEndpoint);

            using var stream = client.GetStream();

            var payload = "addgroup";

            using (var bw = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                bw.Write(payload);
                bw.Write(groupname);
            }

            return await GetResponse<int>(stream);
        }

        internal static async Task<bool> ProcessAddToGroupAsync(Group group, User user)
        {
            TcpClient client = new TcpClient();

            await client.ConnectAsync(tcpEndpoint);

            using var stream = client.GetStream();

            var payload = "addtogroup";

            using (var bw = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                bw.Write(payload);
                bw.Write(group.Gid);
                bw.Write(user.Uid);
            }

            return await GetResponse<bool>(stream);
        }

        internal static async Task<bool> ProcessRemoveGroupAsync(Group group)
        {
            TcpClient client = new TcpClient();

            await client.ConnectAsync(tcpEndpoint);

            using var stream = client.GetStream();

            var payload = "removegroup";

            using (var bw = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                bw.Write(payload);
                bw.Write(group.Gid);
            }

            return await GetResponse<bool>(stream);
        }

        internal static async Task<bool> ProcessRemoveFromGroupAsync(Group group, User user)
        {
            TcpClient client = new TcpClient();

            await client.ConnectAsync(tcpEndpoint);

            using var stream = client.GetStream();

            var payload = "removefromgroup";

            using (var bw = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                bw.Write(payload);
                bw.Write(group.Gid);
                bw.Write(user.Uid);
            }

            return await GetResponse<bool>(stream);
        }

        internal static async Task<T> GetResponse<T>(NetworkStream stream)
        {
            using StreamReader sr = new StreamReader(stream);
            var responseString = await sr.ReadToEndAsync();

            try
            {
                return JsonSerializer.Deserialize<T>(responseString);
            }
            catch (Exception)
            {
                throw new InvalidOperationException(responseString);
            }
        }
    }
}
