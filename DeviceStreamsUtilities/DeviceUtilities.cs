using Microsoft.Azure.Devices.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceStreamsUtilities
{
    public static class DeviceUtilities
    {
        public static async Task RegisterDeviceStreamCallback(this DeviceClient moduleClient, Func<ClientWebSocket, CancellationToken, Task> callback, CancellationToken ct)
        {
            // This will run until the cancelation token is canceled
            while (!ct.IsCancellationRequested)
            {
                Console.WriteLine("Waiting for connection request");
                DeviceStreamRequest request = await moduleClient.WaitForDeviceStreamRequestAsync(ct);

                Console.WriteLine("Got connection request");
                await moduleClient.AcceptDeviceStreamRequestAsync(request, ct).ConfigureAwait(false);

                using (ClientWebSocket webSocket = new ClientWebSocket())
                {
                    Console.WriteLine("Making websocket");
                    webSocket.Options.SetRequestHeader("Authorization", "Bearer " + request.AuthorizationToken);

                    await webSocket.ConnectAsync(request.Url, ct).ConfigureAwait(false);
                    Console.WriteLine("Connected");

                    try
                    {
                        await callback(webSocket, ct);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                    Console.WriteLine("Finished Connection Request");
                }

                Console.WriteLine("Closing websocket");
            }
        }
    }
}