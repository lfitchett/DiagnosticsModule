using DeviceStreamsUtilities;
using Microsoft.Azure.Devices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DiagnosticsCli
{
    class Program
    {
        static void Main(string[] args)
        {
            TestSendAsync().GetAwaiter().GetResult();
            Console.WriteLine("Done");
        }

        static async Task TestSendAsync()
        {
            CancellationTokenSource ct = new CancellationTokenSource();
            string deviceId = "device4";
            string connString = "HostName=lefitche-hub-3.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=s+3pkFuO8O4leS3mIFl1aW6O0/ASKEo85Cv0mjgrDUg=";

            ServiceClient client = ServiceClient.CreateFromConnectionString(connString);
            using (ClientWebSocket webSocket = await client.ConnectToDevice(deviceId, ct.Token))
            {
                var sender = new WebSocketSender(webSocket, ct.Token);

                /* TODO: find open source version. This is intended as a temporary hack. there should be a library that does this. */
                CommandLineParser parser = new CommandLineParser();

                RegisterCommands(sender, parser);

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

                await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Done", ct.Token);
                //while (webSocket.State != WebSocketState.Closed) { }
            }
        }

        /// <summary>
        ///     Registers the "getfile" command. when getfile is used, it will parse source and destination and ask the module for the given file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="parser"></param>
        static void RegisterCommands(WebSocketSender sender, CommandLineParser parser)
        {
            /* Register list file command */
            parser.RegisterCommand("ls", async () =>
            {
                Console.WriteLine(await sender.GetFileList());
            });

            /* Register get file */
            string source = null;
            Action<string> setSource = (string val) => source = val;

            string destination = null;
            Action<string> setDest = (string val) => destination = val;

            Dictionary<string, Action<string>> args = new Dictionary<string, Action<string>>
                        {
                            {"--source", setSource },
                            {"-s", setSource },
                            {"--destination", setDest },
                            {"-d", setDest }
                        };

            parser.RegisterCommand("getfile", async () =>
            {
                if (destination == null)
                {
                    Console.WriteLine("Please use -d to set a destination");
                    return;
                }
                if (source == null)
                {
                    Console.WriteLine("Please use -s to set a source");
                    return;
                }

                await sender.GetFile(source, destination);
                Console.WriteLine($"Succesfilly saved {source} to {destination}");
            }, args);
        }
    }
}