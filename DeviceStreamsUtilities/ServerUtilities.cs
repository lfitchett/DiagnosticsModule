using Microsoft.Azure.Devices;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceStreamsUtilities
{
    public static class ServerUtilities
    {
        public static async Task<ClientWebSocket> ConnectToDevice(this ServiceClient serviceClient, string deviceId, string streamName = "EdgeStream", CancellationToken? cancellationToken = null)
        {
            DeviceStreamRequest deviceStreamRequest = new DeviceStreamRequest(streamName);

            Console.WriteLine($"Connecting to {deviceId}");
            DeviceStreamResponse stream = await serviceClient.CreateStreamAsync(deviceId, deviceStreamRequest, cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
            Console.WriteLine($"Stream response received: Name={deviceStreamRequest.StreamName} IsAccepted={stream.IsAccepted}");

            ClientWebSocket webSocket = new ClientWebSocket();
            webSocket.Options.SetRequestHeader("Authorization", "Bearer " + stream.AuthorizationToken);
            await webSocket.ConnectAsync(stream.Url, cancellationToken ?? CancellationToken.None).ConfigureAwait(false);

            return webSocket;
        }
    }
}
