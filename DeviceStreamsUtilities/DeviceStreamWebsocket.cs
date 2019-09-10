using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceStreamsUtilities
{
    public static class DeviceStreamWebsocket
    {
        public static async Task<ClientWebSocket> MakeWebSocket(Uri url, string authToken, CancellationToken ct)
        {
            Console.WriteLine("Making websocket");

            ClientWebSocket webSocket = new ClientWebSocket();
            webSocket.Options.SetRequestHeader("Authorization", "Bearer " + authToken);

            await webSocket.ConnectAsync(url, ct).ConfigureAwait(false);
            Console.WriteLine("Connected");

            return webSocket;
        }
    }
}
