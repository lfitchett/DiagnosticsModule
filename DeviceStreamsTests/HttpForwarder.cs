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
using System.IO;

namespace Websockets
{
    public class HttpForwarder : SetupWebserver
    {
        [Test]
        public async Task TestServer()
        {
            Directory.SetCurrentDirectory(targetDirectory);
            string expected = string.Join('\n', Directory.GetFiles("."));

            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(@"http://localhost:5000/api/file/list");
                Assert.True(response.IsSuccessStatusCode, "Expected Get to succeed");

                string body = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(expected, body);
            }
        }

        [Test]
        public async Task TestListFiles()
        {
            bool connected = false;
            int callbackTimes = 0;

            CancellationTokenSource canceler = new CancellationTokenSource();

            Directory.SetCurrentDirectory(targetDirectory);
            string expected = string.Join('\n', Directory.GetFiles("."));

            Func<Task> sendMessages = async () =>
            {
                await Task.Delay(100);
                using (ClientWebSocket webSocket = await serviceClient.ConnectToDevice(deviceId, canceler.Token))
                using (HttpClient httpClient = new HttpClient(new WebsocketHttpMessageHandler(webSocket)))
                {
                    HttpResponseMessage response = await httpClient.GetAsync(@"http://localhost:5000/api/file/list");
                    Assert.True(response.IsSuccessStatusCode, "Expected Get to succeed");

                    string body = await response.Content.ReadAsStringAsync();
                    Assert.AreEqual(expected, body);

                    await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Normal close", CancellationToken.None);
                }

                canceler.Cancel();
            };

            Func<Task> forwardMessages = () =>
            {
                return deviceClient.RegisterDeviceStreamCallback((webSocket, ct) =>
                    {
                        connected = true;
                        Interlocked.Increment(ref callbackTimes);

                        return new WebsocketHttpForwarder(webSocket).StartForwarding(ct);
                    }, canceler.Token);
            };

            try
            {
                await Task.WhenAny(
                    forwardMessages(),
                    sendMessages()
                );
            }
            catch (OperationCanceledException) { }

            Assert.IsTrue(connected);
            Assert.AreEqual(1, callbackTimes);
        }
    }
}