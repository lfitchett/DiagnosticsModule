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

namespace DeviceStreams1
{
    public class WebSocketReciever
    {
        private readonly ClientWebSocket webSocket;
        private readonly CancellationToken cancellationToken;

        private readonly Dictionary<Flag, Func<Task>> actions;

        public WebSocketReciever(ClientWebSocket webSocket, CancellationToken cancellationToken)
        {
            this.webSocket = webSocket;
            this.cancellationToken = cancellationToken;

            /* Define all actions. These will be called when the appropiate flag is sent. */
            actions = new Dictionary<Flag, Func<Task>>
            {
                {
                    Flag.SendFile, async () => {
                        Console.WriteLine("Getting file name.");
                        string fileName = await webSocket.RecieveText(cancellationToken);
                        Console.WriteLine($"Sending file: {fileName}");

                        await webSocket.SendFile($"/shared/{fileName}", cancellationToken);
                    }
                },
                {
                    Flag.ListFiles, async () => await webSocket.SendText(string.Join('\n', Directory.GetFiles(@"/shared")), cancellationToken)
                }
            };
        }

        public async Task StartListening()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Flag flag = await webSocket.RecieveFlag(cancellationToken);
                if (flag == Flag.Close)
                    break;

                if (actions.TryGetValue(flag, out var task))
                {
                    await task();
                }
            }

            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", cancellationToken);
            while (webSocket.State != WebSocketState.Closed) { }
        }
    }
}
