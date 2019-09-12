using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceStreamsUtilities
{
    public partial class WebSocketManager
    {
        private async Task SendFile(string source, string destination, CancellationToken ct)
        {
            Console.WriteLine("Filesend");
            await Send(Flag.FileStart, Encoding.UTF8.GetBytes(destination), ct);

            using (var file = File.OpenRead(source))
            {
                while (file.Position < file.Length)
                {
                    Console.WriteLine($"Sending bytes index: {file.Position}");
                    int numToSend = file.Read(sendBuffer, 0, BUFFERSIZE);
                    await webSocket.SendAsync(new ArraySegment<byte>(sendBuffer, 0, numToSend), WebSocketMessageType.Binary, true, ct);
                }
            }
            await Send(Flag.FileEnd, ct);
            Console.WriteLine("Filesend done");
        }

        private async Task RecieveFile(string filename, CancellationToken ct)
        {
            Console.WriteLine("FileRecieve");

            using (var file = File.OpenWrite(filename))
            {
                int i = 0;
                while (!ct.IsCancellationRequested & webSocket.State == WebSocketState.Open)
                {
                    Console.WriteLine($"Recieved packet {i++}");
                    WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(recieveBuffer), ct);
                    if (receiveResult.Count == 1)
                    {
                        if ((Flag)recieveBuffer[0] == Flag.MultiPartEnd)
                        {
                            break;
                        }
                        else
                        {
                            Console.Write("Send interupted, canceling");
                            break;
                        }
                    }

                    file.Write(recieveBuffer, 0, receiveResult.Count);
                }
            }
            Console.WriteLine("File Recieve Done");
        }
    }
}