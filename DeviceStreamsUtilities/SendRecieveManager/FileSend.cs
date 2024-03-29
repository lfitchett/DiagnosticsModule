﻿using System;
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
        public async Task SendFile(string source, string destination, CancellationToken ct)
        {
            Console.WriteLine($"Sending file: {source} to destination {destination}");
            await Send(Flag.FileStart, Encoding.UTF8.GetBytes(destination), ct);

            using (var file = File.OpenRead(source))
            {
                while (file.Position < file.Length)
                {
                    Console.WriteLine($"Sending bytes index: {file.Position}");
                    sendBuffer[0] = (byte)Flag.FilePart;
                    int numToSend = file.Read(sendBuffer, 1, BUFFERSIZE - 1);
                    await webSocket.SendAsync(new ArraySegment<byte>(sendBuffer, 0, numToSend + 1), WebSocketMessageType.Binary, true, ct);
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
                    if ((Flag)recieveBuffer[0] == Flag.MultiPartEnd)
                    {
                        break;
                    }

                    /* Recieved unexpected flag, cancel file recieve and process it. */
                    if((Flag)recieveBuffer[0] != Flag.FilePart)
                    {
                        Console.WriteLine("File Recieve interupted");
                        await HandleRecieve(receiveResult, ct);
                        return;
                    }

                    file.Write(recieveBuffer, 0, receiveResult.Count);
                }

                Console.WriteLine("File Recieve Done");
            }
        }
    }
}