using Microsoft.Azure.Devices;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceStreamsUtilities
{
    public static class ServerUtilities
    {
        public static async Task<ClientWebSocket> ConnectToDevice(this ServiceClient serviceClient, string deviceId, string streamName = "EdgeStream", CancellationToken ct)
        {
            DeviceStreamRequest deviceStreamRequest = new DeviceStreamRequest(streamName);

            Console.WriteLine($"Connecting to {deviceId}");
            DeviceStreamResponse streamInfo = await serviceClient.CreateStreamAsync(deviceId, deviceStreamRequest, ct).ConfigureAwait(false);
            Console.WriteLine($"Stream response received: Name={deviceStreamRequest.StreamName} IsAccepted={streamInfo.IsAccepted}");

            return await DeviceStreamWebsocket.MakeWebSocket(streamInfo.Url, streamInfo.AuthorizationToken, ct);
        }
    }
}
