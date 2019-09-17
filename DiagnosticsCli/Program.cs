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
                    var response = await httpClient.GetAsync(@"http://localhost:5000/api/file?filename=C%3A%5CUsers%5CLee%5CDocuments%5CTest%5CFrom%5CNew+Text+Document.txt");
                    Console.WriteLine(response);
                    if (response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        Console.WriteLine(body);
                    }

                    await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Normal close", CancellationToken.None);
                }

            };

            temp().Wait();
            Console.WriteLine("Done");
        }
    }
}