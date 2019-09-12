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
            int sleeptime = 1000;
            // This will run until the cancelation token is canceled
            while (!ct.IsCancellationRequested)
            {
                Console.WriteLine("Waiting for connection request");
                DeviceStreamRequest request;
                try
                {
                    request = await moduleClient.WaitForDeviceStreamRequestAsync(ct);

                    Console.WriteLine("Got connection request");
                    await moduleClient.AcceptDeviceStreamRequestAsync(request, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Request got exeption:");
                    Console.WriteLine(ex);

                    Console.WriteLine($"Trying again in {sleeptime} seconds.");
                    await Task.Delay(sleeptime);
                    /* Max sleeptime is 10 minutes */
                    if (sleeptime < 1000 * 60 * 10)
                    {
                        sleeptime *= 2;
                    }
                    continue;
                }
                sleeptime = 1000;

                using (ClientWebSocket webSocket = await DeviceStreamWebsocket.MakeWebSocket(request.Url, request.AuthorizationToken, ct))
                {
                    try
                    {
                        await callback(webSocket, ct);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Websocket threw exception.");
                        Console.WriteLine(ex);
                    }

                    Console.WriteLine("Finished Connection Request");
                }

                Console.WriteLine("Closing websocket");
            }
        }
    }
}