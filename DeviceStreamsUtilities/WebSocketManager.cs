using DotNetty.Codecs.Mqtt.Packets;
using Microsoft.Azure.Amqp.Framing;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceStreamsUtilities
{
    public class WebSocketManager
    {
        private const int BUFFERSIZE = 10240;

        private readonly WebSocket webSocket;

        private readonly Dictionary<Flag, Func<ArraySegment<byte>, CancellationToken, Task>> callbacks = new Dictionary<Flag, Func<ArraySegment<byte>, CancellationToken, Task>>();
        private readonly byte[] recieveBuffer = new byte[BUFFERSIZE];
        private readonly byte[] sendBuffer = new byte[BUFFERSIZE];

        private readonly CancellationTokenSource onClose = new CancellationTokenSource();

        public WebSocketManager(WebSocket webSocket)
        {
            this.webSocket = webSocket;
        }

        /// <summary>
        ///     Will call the registered callback when the given flag is recieved. Only one callback is allowed per flag.
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="func"></param>
        /// <param name="ct"></param>
        public void RegisterCallback(Flag flag, Func<ArraySegment<byte>, CancellationToken, Task> func, CancellationToken ct)
        {
            callbacks.Add(flag, func);
            ct.Register(() => callbacks.Remove(flag));
        }

        public async Task StartRecieving(CancellationToken ct)
        {
            CancellationToken cancel = CancellationTokenSource.CreateLinkedTokenSource(onClose.Token, ct).Token;
            while (!cancel.IsCancellationRequested)
            {
                WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(recieveBuffer), cancel);

                Flag flag = (Flag)recieveBuffer[0];
                ArraySegment<byte> data = new ArraySegment<byte>(recieveBuffer, 1, receiveResult.Count - 1);

                if (flag == Flag.Close)
                {
                    Close();
                }

                if (callbacks.TryGetValue(flag, out var callback))
                {
                    await callback(data, cancel);
                }
                else
                {
                    Console.WriteLine($"Did not have callback registered for flag {flag}");
                }
            }
        }

        public async Task Send(Flag flag, byte[] dataToSend, CancellationToken ct)
        {
            CancellationToken cancel = CancellationTokenSource.CreateLinkedTokenSource(onClose.Token, ct).Token;

            if (dataToSend.Length > BUFFERSIZE - 1)
            {
                throw new Exception("Too much data");
            }

            sendBuffer[0] = (byte)flag;
            dataToSend.CopyTo(sendBuffer, 1);

            await webSocket.SendAsync(new ArraySegment<byte>(sendBuffer, 0, dataToSend.Length + 1), WebSocketMessageType.Binary, false, cancel);
        }

        public async Task Close()
        {
            onClose.Cancel();

            await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Normal close", CancellationToken.None);

            while (webSocket.State != WebSocketState.Closed)
            {
                Console.WriteLine("Pausing to let close");
                Thread.Sleep(10);
            }
        }
    }
}
