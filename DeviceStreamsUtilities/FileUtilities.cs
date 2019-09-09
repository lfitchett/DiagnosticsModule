using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceStreamsUtilities
{
    public static class FileUtilities
    {
        private const int BUFFER_SIZE = 10240;

        public static async Task SendFile(this ClientWebSocket webSocket, string fileName, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Opening file {fileName}");
            using (var file = File.OpenRead(fileName))
            {
                byte[] buffer = new byte[BUFFER_SIZE];
                Console.WriteLine("Sending file");

                int testCounter = 1;
                while (webSocket.State == WebSocketState.Open)
                {
                    int numBytes = file.Read(buffer, 1, BUFFER_SIZE - 1);
                    if (numBytes == 0)
                    {
                        // buffer[0] = isDone
                        buffer[0] = 1;
                        Console.WriteLine("Done sending");
                        await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, 1), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
                        break;
                    }

                    // buffer[0] = isDone
                    buffer[0] = 0;
                    Console.WriteLine($"{testCounter++}. Sending {numBytes} bytes.");
                    await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, numBytes + 1), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
                }
                Console.WriteLine("Finished sending file");
            }
        }

        public static async Task<Stream> RecieveFile(this ClientWebSocket webSocket, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[BUFFER_SIZE];

            //Console.WriteLine("Waiting for data");

            Stream result = new MemoryStream();
            while (webSocket.State == WebSocketState.Open)
            {
                var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);
                if (buffer[0] == 1)
                {
                    //Console.WriteLine($"End of message. {receiveResult.Count}");
                    break;
                }
                //Console.WriteLine($"Got {receiveResult.Count - 1} bytes");

                result.Write(buffer, 1, receiveResult.Count - 1);
            }

            //Console.WriteLine("Downloaded file");

            return result;
        }


    }
}
