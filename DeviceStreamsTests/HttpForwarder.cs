using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using DeviceStreamsUtilities;
using System.Net.WebSockets;
using System.Text;
using System.Net.Http.Headers;
using System;
using DeviceStreamsTests;
using DeviceStreamsTests.Setup;
using System.Net.Http;
using Microsoft.AspNetCore.Http;

namespace Websockets
{
    public class HttpForwarder : SetupWebserver
    {
        [Test]
        public async Task TestServer()
        {
            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(@"http://localhost:5000/api/file/list");
                Assert.True(response.IsSuccessStatusCode, "Expected Get to succeed");

                string body = await response.Content.ReadAsStringAsync();
                Assert.IsNotEmpty(body);
            }
        }

        //[Test]
        public async Task test1()
        {
            bool connected = false;
            int callbackTimes = 0;

            CancellationTokenSource callbackCancel = new CancellationTokenSource();

            Func<Task> sendMessages = async () =>
            {
                await Task.Delay(100);
                using (ClientWebSocket webSocket = await serviceClient.ConnectToDevice(deviceId, callbackCancel.Token))
                using (HttpClient httpClient = new HttpClient(new WebsocketHttpMessageHandler(webSocket)))
                {



                    await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Normal close", CancellationToken.None);
                }
            };

            await Task.WhenAll(
                deviceClient.RegisterDeviceStreamCallback((webSocket, ct) =>
                {
                    return new WebsocketHttpForwarder(webSocket).StartForwarding(ct);
                }, callbackCancel.Token),
                Task.Run(sendMessages)
            );

            Assert.IsTrue(connected);
            Assert.AreEqual(1, callbackTimes);
        }
    }
}