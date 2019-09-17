using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceStreamsUtilities
{
    public partial class WebSocketManager
    {
        /// <summary>
        ///     Sends large amounts of data in multiple messages.
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="dataToSend"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task SendMultiPart(Flag flag, byte[] dataToSend, CancellationToken ct)
        {
            Console.WriteLine("Multisend");
            sendBuffer[0] = (byte)Flag.MultiPartStart;
            sendBuffer[1] = (byte)flag;
            await webSocket.SendAsync(new ArraySegment<byte>(sendBuffer, 0, 2), WebSocketMessageType.Binary, true, ct);

            int currIndex = 0;
            while (currIndex < dataToSend.Length - BUFFERSIZE)
            {
                Console.WriteLine($"Sending bytes index: {currIndex}");
                await webSocket.SendAsync(new ArraySegment<byte>(dataToSend, currIndex, BUFFERSIZE), WebSocketMessageType.Binary, true, ct);
                currIndex += BUFFERSIZE;
            }
            await webSocket.SendAsync(new ArraySegment<byte>(dataToSend, currIndex, dataToSend.Length - currIndex), WebSocketMessageType.Binary, true, ct);

            await Send(Flag.MultiPartEnd, ct);
            Console.WriteLine("Multisend done");
        }

        /// <summary>
        ///     Recieves large amounts of data in multiple messages
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<byte[]> RecieveMultiPart(CancellationToken ct)
        {
            Console.WriteLine("Multi Recieve");
            //TODO: send flag in byte 1
            List<byte> data = new List<byte>();

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

                data.AddRange(new ArraySegment<byte>(recieveBuffer, 0, receiveResult.Count));
            }

            Console.WriteLine("Multi Recieve Done");
            return data.ToArray();
        }
    }
}