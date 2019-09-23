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
using System.Security.Cryptography;
using System.Linq;

namespace Websockets
{
    public class HttpForwarder : SetupWebserver
    {
        private readonly string outputDir = "./tempTest";

        [SetUp]
        public void MakeTestDirectory()
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
            Directory.CreateDirectory(outputDir);
        }

        [Test]
        public async Task TestServer()
        {
            var expected = Directory.EnumerateFiles(targetDirectory).Select(Path.GetFileName);

            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(@"http://localhost:5000/api/file/list");
                Assert.True(response.IsSuccessStatusCode, "Expected Get to succeed");

                string body = await response.Content.ReadAsStringAsync();

                foreach(string file in expected)
                {
                    Assert.That(body.Contains(file), $"Response was missing {file}.");
                }
            }
        }

        [Test]
        public async Task TestListFiles()
        {
            var expected = Directory.EnumerateFiles(targetDirectory).Select(Path.GetFileName);

            HttpResponseMessage response = await TestRequest(@"http://localhost:5000/api/file/list", CancellationToken.None);
            string body = await response.Content.ReadAsStringAsync();

            foreach (string file in expected)
            {
                Assert.That(body.Contains(file), $"Response was missing {file}.");
            }
        }

        [Test]
        public async Task TestSendSmallFile()
        {
            string outputFile = $"{outputDir}/testSmall.txt";

            HttpResponseMessage response = await TestRequest(@"http://localhost:5000/api/file?filename=smallFile.txt", CancellationToken.None);
            Stream body = await response.Content.ReadAsStreamAsync();
            using (FileStream file = File.OpenWrite(outputFile))
            {
                await body.CopyToAsync(file);
            }

            CompareFiles($"{targetDirectory}/smallFile.txt", outputFile);
        }

        [Test]
        public async Task TestSendMediumFile()
        {
            string outputFile = $"{outputDir}/testMedium.txt";
            string mediumFile = $"{targetDirectory}/mediumFile.txt";
            await TestUtilities.MakeBigFile(mediumFile, 4000);

            HttpResponseMessage response = await TestRequest(@"http://localhost:5000/api/file?filename=mediumFile.txt", CancellationToken.None);
            Stream body = await response.Content.ReadAsStreamAsync();
            using (FileStream file = File.OpenWrite(outputFile))
            {
                await body.CopyToAsync(file);
            }

            CompareFiles(mediumFile, outputFile);
        }

        private async Task<HttpResponseMessage> TestRequest(string url, CancellationToken cancellationToken)
        {
            HttpResponseMessage result = null;
            CancellationTokenSource callbackCancler = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            int callbackTimes = 0;

            Func<Task> sendMessages = async () =>
            {
                await Task.Delay(100);
                using (ClientWebSocket webSocket = await serviceClient.ConnectToDevice(deviceId, cancellationToken))
                using (HttpClient httpClient = new HttpClient(new WebsocketHttpMessageHandler(webSocket)))
                {
                    result = await httpClient.GetAsync(url);
                    Assert.True(result.IsSuccessStatusCode, "Expected Get to succeed");

                    await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Normal close", cancellationToken);
                }

                callbackCancler.CancelAfter(50);
            };

            Func<Task> forwardMessages = async () =>
            {
                try
                {
                    await deviceClient.RegisterDeviceStreamCallback(async (webSocket, ct) =>
                        {
                            Interlocked.Increment(ref callbackTimes);
                            await new WebsocketHttpForwarder(webSocket).StartForwarding(ct);
                        }, callbackCancler.Token);
                }
                catch (OperationCanceledException) { }
            };

            Task forwarder = forwardMessages();
            await sendMessages();
            await forwarder;

            Assert.AreEqual(1, callbackTimes);
            Assert.IsNotNull(result, "Expected to get result from server");

            return result;
        }

        private void CompareFiles(string expected, string actual)
        {
            using (var md5 = MD5.Create())
            using (var f1 = File.OpenRead(expected))
            using (var f2 = File.OpenRead(actual))
            {
                Assert.AreEqual(md5.ComputeHash(f1), md5.ComputeHash(f2), $"Expected files to be the same.");
            }
        }
    }
}