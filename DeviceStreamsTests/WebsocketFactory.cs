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

namespace Websockets
{
    public class WebsocketFactory : Setup
    {
        string deviceId;
        DeviceClient deviceClient;
        ServiceClient serviceClient;        

        [SetUp]
        public async Task Setup()
        {
            var device = await MakeNewDevice();
            deviceId = device.Id;            

            string deviceConnString = $"HostName=lefitche-hub-3.azure-devices.net;DeviceId={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}";
            deviceClient = DeviceClient.CreateFromConnectionString(deviceConnString);

            serviceClient = ServiceClient.CreateFromConnectionString("HostName=lefitche-hub-3.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=s+3pkFuO8O4leS3mIFl1aW6O0/ASKEo85Cv0mjgrDUg=");
        }

        [Test]
        public async Task InitiateConnection()
        {
            bool connected = false;
            int callbackTimes = 0;

            CancellationTokenSource callbackCancel = new CancellationTokenSource();

            await Task.WhenAll(
                deviceClient.RegisterDeviceStreamCallback((webSocket, ct) =>
                    {
                        connected = true;
                        Interlocked.Increment(ref callbackTimes);

                        Assert.AreEqual(WebSocketState.Open, webSocket.State);

                        callbackCancel.CancelAfter(100);
                        return Task.CompletedTask;
                    }, callbackCancel.Token),
                Task.Run(async () =>
                    {
                        await Task.Delay(100);
                        WebSocket webSocket = await serviceClient.ConnectToDevice(deviceId, callbackCancel.Token);
                        Assert.AreEqual(WebSocketState.Open, webSocket.State);
                    })
            );

            Assert.IsTrue(connected);
            Assert.AreEqual(1, callbackTimes);
        }

        [Test]
        public async Task SendData()
        {
            int callbackTimes = 0;
            string data = "This is a test string";
            CancellationTokenSource callbackCancel = new CancellationTokenSource();

            await Task.WhenAll(
                deviceClient.RegisterDeviceStreamCallback(async (webSocket, ct) =>
                {
                    Interlocked.Increment(ref callbackTimes);

                    byte[] buffer = new byte[1000];
                    var response = await webSocket.ReceiveAsync(buffer, ct);
                    string recievedText = Encoding.UTF8.GetString(buffer, 0, response.Count);

                    Assert.AreEqual(data, recievedText);

                    callbackCancel.CancelAfter(100);
                }, callbackCancel.Token),
                Task.Run(async () =>
                {
                    await Task.Delay(100);
                    WebSocket webSocket = await serviceClient.ConnectToDevice(deviceId, callbackCancel.Token);

                    await webSocket.SendAsync(Encoding.UTF8.GetBytes(data), WebSocketMessageType.Binary, true, callbackCancel.Token);
                })
            );

            Assert.AreEqual(1, callbackTimes);
        }
    }
}