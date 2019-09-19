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
        public static Task RegisterDeviceStreamCallback(this DeviceClient deviceClient, Func<ClientWebSocket, CancellationToken, Task> callback, CancellationToken ct)
        {
            Func<Task<DeviceStreamRequest>> getRequest = async () =>
            {
                DeviceStreamRequest request = await deviceClient.WaitForDeviceStreamRequestAsync(ct);
                await deviceClient.AcceptDeviceStreamRequestAsync(request, ct).ConfigureAwait(false);
                return request;
            };

            return RegisterDeviceStreamCallback(getRequest, callback, ct);
        }

        public static Task RegisterDeviceStreamCallback(this ModuleClient moduleClient, Func<ClientWebSocket, CancellationToken, Task> callback, CancellationToken ct)
        {
            Func<Task<DeviceStreamRequest>> getRequest = async () =>
            {
                DeviceStreamRequest request = await moduleClient.WaitForDeviceStreamRequestAsync(ct);
                await moduleClient.AcceptDeviceStreamRequestAsync(request, ct).ConfigureAwait(false);
                return request;
            };

            return RegisterDeviceStreamCallback(getRequest, callback, ct);
        }

        private static async Task RegisterDeviceStreamCallback(Func<Task<DeviceStreamRequest>> getRequest, Func<ClientWebSocket, CancellationToken, Task> callback, CancellationToken ct)
        {
            int sleeptime = 1000;
            // This will run until the cancelation token is canceled
            while (!ct.IsCancellationRequested)
            {
                Console.WriteLine("Waiting for connection request");
                DeviceStreamRequest request;
                try
                {
                    request = await getRequest();
                    Console.WriteLine("Got connection request");
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
                    catch (WebSocketException ex)
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