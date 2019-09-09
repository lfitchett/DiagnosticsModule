using DeviceStreamsUtilities;
using DotNetty.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiagnosticsCli
{
    public class WebSocketSender
    {
        private readonly ClientWebSocket webSocket;
        private readonly CancellationToken cancellationToken;

        public WebSocketSender(ClientWebSocket webSocket, CancellationToken cancellationToken)
        {
            this.webSocket = webSocket;
            this.cancellationToken = cancellationToken;
        }

        public async Task GetFile(string fileName, string destination)
        {
            await webSocket.SendFlag(Flag.SendFile, cancellationToken);
            await webSocket.SendText(fileName, cancellationToken);
            using (Stream stream = await webSocket.RecieveFile(cancellationToken))
            using (FileStream file = File.OpenWrite(destination))
            {
                stream.Position = 0;
                stream.CopyTo(file);
            }
        }

        public async Task<string> GetFileList()
        {
            await webSocket.SendFlag(Flag.ListFiles, cancellationToken);
            return await webSocket.RecieveText(cancellationToken);
        }
    }
}
