using DeviceStreamsUtilities;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                {
                    WebSocketManager manager = new WebSocketManager(webSocket);

                    manager.RegisterCallback(Flag.Response, async (ArraySegment<byte> data, CancellationToken _) =>
                    {
                        Console.WriteLine(Encoding.UTF8.GetString(data));
                    }, CancellationToken.None);

                    manager.RegisterCallback(Flag.SendFile, async (ArraySegment<byte> data, CancellationToken _) =>
                    {
                        byte[] file = data.ToArray();

                        try
                        {
                            File.WriteAllBytes(@"C:\Users\Lee\Documents\Test\To\testFile.txt", file);
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }, CancellationToken.None);
                    await Task.WhenAll(manager.StartRecieving(ct), TestSendAsync(manager, ct));
                }

            };

            temp().Wait();
            Console.WriteLine("Done");
        }

        static async Task TestSendAsync(WebSocketManager manager, CancellationToken ct)
        {
            /* TODO: find open source version. This is intended as a temporary hack. there should be a library that does this. */
            CommandLineParser parser = new CommandLineParser();

            RegisterCommands(manager, parser, ct);

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
            await manager.Close();

        }

        /// <summary>
        ///     Registers the "getfile" command. when getfile is used, it will parse source and destination and ask the module for the given file.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="parser"></param>
        static void RegisterCommands(WebSocketManager manager, CommandLineParser parser, CancellationToken ct)
        {
            /* Register list file command */
            parser.RegisterCommand("ls", async () =>
            {
                await manager.Send(Flag.ListFiles, ct);
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
                //if (destination == null)
                //{
                //    Console.WriteLine("Please use -d to set a destination");
                //    return;
                //}
                if (source == null)
                {
                    Console.WriteLine("Please use -s to set a source");
                    return;
                }

                await manager.Send(Flag.SendFile, Encoding.UTF8.GetBytes(source), ct);
                Console.WriteLine($"Succesfilly saved {source} to {destination}");
            }, args);
        }
    }
}