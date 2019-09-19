using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceStreamsUtilities.HttpForwarder
{
    public class WebsocketResultStream : Stream
    {
        private WebSocket websocket;

        public WebsocketResultStream(int length, WebSocket websocket)
        {
            Length = length;
            this.websocket = websocket;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            long remaining = Length - Position;
            if (remaining <= 0)
            {
                return 0;
            }

            WebSocketReceiveResult websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, (int)Math.Min(remaining, count)), cancellationToken);
            Position += websocketResponse.Count;
            return websocketResponse.Count;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count, CancellationToken.None).Result;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length { get; }

        public override long Position { get; set; }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
