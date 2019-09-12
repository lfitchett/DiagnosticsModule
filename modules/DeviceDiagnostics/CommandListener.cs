using DeviceStreamsUtilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceDiagnostics
{
    public class CommandListener
    {
        private const string DIRECTORY = @"C:\Users\Lee\Documents\Test\From";

        private readonly WebSocketManager manager;
        private readonly CancellationToken cancellationToken;

        public CommandListener(ClientWebSocket webSocket, CancellationToken cancellationToken)
        {
            manager = new WebSocketManager(webSocket);
            this.cancellationToken = cancellationToken;

            if (!Directory.Exists(DIRECTORY))
            {
                throw new Exception($"Directory not found: {DIRECTORY}");
            }

            manager.RegisterCallback(Flag.SendFile, async (ArraySegment<byte> data, CancellationToken ct) =>
            {
                /* | is not allowed in file paths, so it can act as a seperator */
                string[] locations = Encoding.UTF8.GetString(data).Split('|');
                if(locations.Length != 2)
                {
                    Console.WriteLine("Invalid source and destination");
                    await manager.Send(Flag.Response, Encoding.UTF8.GetBytes("Invalid source and destination"), ct);
                    return;
                }
                string source = locations[0];
                string destination = locations[1];

                try
                {
                    await manager.SendFile($"{DIRECTORY}/{source}", destination, ct);
                    await manager.Send(Flag.Response, Encoding.UTF8.GetBytes("Sent file"), ct);
                }
                catch (Exception ex)
                {
                    await manager.Send(Flag.Response, Encoding.UTF8.GetBytes(ex.Message), ct);
                }

            }, CancellationToken.None);

            manager.RegisterCallback(Flag.ListFiles, async (ArraySegment<byte> data, CancellationToken ct) =>
            {
                string files;
                try
                {
                    files = string.Join('\n', Directory.GetFiles(DIRECTORY));
                }
                catch (Exception ex)
                {
                    files = ex.Message;
                }

                if (files == "")
                {
                    files = "No files in current directory";
                }

                await manager.Send(Flag.Response, Encoding.UTF8.GetBytes(files), ct);
            }, CancellationToken.None);
        }

        public async Task StartListening()
        {
            await manager.StartRecieving(cancellationToken);
        }
    }
}
