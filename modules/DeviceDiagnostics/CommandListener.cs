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
        private const string DIRECTORY = @"C:\Users\Lee\Documents\Test";

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
                Console.WriteLine("Getting file name.");
                string fileName = await webSocket.RecieveText(cancellationToken);
                Console.WriteLine($"Sending file: {fileName}");

                await webSocket.SendFile($"{DIRECTORY}/{fileName}", ct);
            }, CancellationToken.None);

            manager.RegisterCallback(Flag.ListFiles, async (ArraySegment<byte> data, CancellationToken ct) =>
            {
                string files = string.Join('\n', Directory.GetFiles(DIRECTORY));
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
