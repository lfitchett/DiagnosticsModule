using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using DeviceStreamsUtilities;
using System.Net.WebSockets;

namespace Websockets
{
    public class WebsocketFactory
    {
        DeviceClient deviceClient;
        ServiceClient serviceClient;

        [SetUp]
        public void Setup()
        {
            deviceClient = DeviceClient.CreateFromConnectionString("HostName=lefitche-hub-3.azure-devices.net;DeviceId=TestDevice1;SharedAccessKey=JqUO2wJDKvQng358DOYapSYSmw8l4T3RKnrfl0N/p3I=");

            serviceClient = ServiceClient.CreateFromConnectionString("HostName=lefitche-hub-3.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=s+3pkFuO8O4leS3mIFl1aW6O0/ASKEo85Cv0mjgrDUg=");
        }

        [Test]
        public async Task TestInitiateConnection()
        {
            bool connected = false;
            CancellationTokenSource callbackCancel = new CancellationTokenSource();

            await Task.WhenAll(
                deviceClient.RegisterDeviceStreamCallback((webSocket, ct) =>
                    {
                        connected = true;
                        Assert.AreEqual(WebSocketState.Open, webSocket.State);

                        callbackCancel.Cancel();
                        return Task.CompletedTask;
                    }, callbackCancel.Token),
                Task.Run(async () =>
                    {
                        await Task.Delay(100);
                        WebSocket webSocket = await serviceClient.ConnectToDevice("TestDevice1", callbackCancel.Token);
                        Assert.AreEqual(WebSocketState.Open, webSocket.State);
                    })
            );

            Assert.IsTrue(connected);
        }
    }
}