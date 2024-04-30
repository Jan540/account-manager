using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace AccountManagementClient
{
    internal class UdpHelper
    {
        const int udpPort = 4355;
        private static readonly IPEndPoint udpEndpoint = new IPEndPoint(IPAddress.Loopback, udpPort);

        internal static async Task<string> LoginAsync(string username, string hash)
        {
            UdpClient udpClient = new();
            
            var payload = $"login#{username}#{hash}";
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            await udpClient.SendAsync(payloadBytes, payloadBytes.Length, udpEndpoint);

            var result = await udpClient.ReceiveAsync();

            var resBytes = result.Buffer;
            var res = Encoding.UTF8.GetString(resBytes);

            return res;
        }

        internal static async Task<string> LogoutAsync(string user, string token)
        {
            UdpClient udpClient = new();

            var payload = $"logout#{user}#{token}";
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            await udpClient.SendAsync(payloadBytes, payloadBytes.Length, udpEndpoint);

            var result = await udpClient.ReceiveAsync();

            var resBytes = result.Buffer;
            var res = Encoding.UTF8.GetString(resBytes);

            return res;
        }
    }
}
