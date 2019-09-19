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
            AsyncMain().Wait();
            Console.WriteLine("Done");
        }

        static async Task AsyncMain()
        {
            CancellationTokenSource cancelSource = new CancellationTokenSource();
            CancellationToken ct = cancelSource.Token;

            string deviceId = "device4";
            string moduleId = "$edgeAgent";
            string connString = "HostName=lefitche-hub-3.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=s+3pkFuO8O4leS3mIFl1aW6O0/ASKEo85Cv0mjgrDUg=";



            ServiceClient client = ServiceClient.CreateFromConnectionString(connString);
            using (ClientWebSocket webSocket = await client.ConnectToDevice(deviceId, moduleId, ct))
            using (HttpClient httpClient = new HttpClient(new WebsocketHttpMessageHandler(webSocket)))
            {
                /* TODO: find open source version. This is intended to be temporary. There should be a library that does this. */
                CommandLineParser parser = new CommandLineParser();
                CommandRegister.RegisterCommands(httpClient, parser, ct);

                while (true)
                {
                    Console.Write(">");
                    string input = Console.ReadLine();
                    if (input == "exit")
                    {
                        break;
                    }

                    await parser.ParseCommand(input);
                }

                await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Normal close", CancellationToken.None);
            }
        }        
    }
}