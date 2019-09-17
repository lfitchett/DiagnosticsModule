using DeviceStreamsUtilities;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiagnosticsCli
{
    class Program
    {
        static void Main(string[] args)
        {
            Func<Task> temp = async () =>
            {
                CancellationTokenSource cancelSource = new CancellationTokenSource();
                CancellationToken ct = cancelSource.Token;

                string deviceId = "device4";
                string connString = "HostName=lefitche-hub-3.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=s+3pkFuO8O4leS3mIFl1aW6O0/ASKEo85Cv0mjgrDUg=";



                ServiceClient client = ServiceClient.CreateFromConnectionString(connString);
                using (ClientWebSocket webSocket = await client.ConnectToDevice(deviceId, ct))
                using (HttpClient httpClient = new HttpClient(new WebsocketHttpMessageHandler(webSocket)))
                {
                    // @"http://localhost:5000/api/file?filename=C%3A%5CUsers%5CLee%5CDocuments%5CTest%5CFrom%5CNew+Text+Document.txt"
                    var response = await httpClient.GetAsync(@"http://localhost:80/api/file/list");
                    Console.WriteLine(response);
                    if (response.IsSuccessStatusCode)
                    {
                        using (FileStream file = File.OpenWrite(@"C:\Users\Lee\Documents\Test\To\test.txt"))
                        {
                            (await response.Content.ReadAsStreamAsync()).CopyTo(file);
                        }
                    }

                    await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Normal close", CancellationToken.None);
                }

            };

            temp().Wait();
            Console.WriteLine("Done");
        }
    }
}