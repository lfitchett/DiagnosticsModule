﻿using DotNetty.Codecs.Mqtt.Packets;
using Microsoft.Azure.Amqp.Framing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceStreamsUtilities
{
    public partial class WebSocketManager
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

        /// <summary>
        ///     Waits for messages from the websocket. When a message is recieved, calls the callback of the associated flag.
        ///     The task finishes when either the cancelation token is called or the websocket closes.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task StartRecieving(CancellationToken ct)
        {
            CancellationToken cancel = CancellationTokenSource.CreateLinkedTokenSource(onClose.Token, ct).Token;
            while (!cancel.IsCancellationRequested && webSocket.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(recieveBuffer), cancel);
                    if (receiveResult.Count == 0)
                    {
                        continue;
                    }

                    await HandleRecieve(receiveResult, cancel);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Request was canceled");
                    break;
                }
            }

            await Close();
        }

        /// <summary>
        ///     Parses recieve buffer and calls appropriate method based on recieved flag.
        /// </summary>
        /// <param name="receiveResult"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task HandleRecieve(WebSocketReceiveResult receiveResult, CancellationToken ct)
        {
            ArraySegment<byte> data;
            Flag flag = (Flag)recieveBuffer[0];
            if (flag == Flag.MultiPartStart)
            {
                flag = (Flag)recieveBuffer[1];
                data = new ArraySegment<byte>(await RecieveMultiPart(ct));
            }
            else
            {
                data = new ArraySegment<byte>(recieveBuffer, 1, receiveResult.Count - 1);
            }

            if (flag == Flag.FileStart)
            {
                await RecieveFile(Encoding.UTF8.GetString(data.ToArray()), ct);
                return;
            }
            if (flag == Flag.FilePart)
            {
                Console.WriteLine("Recieved unexpected file packet. Ignoring.");
                return;
            }

            if (callbacks.TryGetValue(flag, out var callback))
            {
                await callback(data, ct);
                return;
            }

            Console.WriteLine($"Did not have callback registered for flag {flag}");
        }

        /// <summary>
        ///     Sends the data in dataToSend with the given flag.
        ///     The task finishes early if either the cancelation token is called or the websocket closes.
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="dataToSend"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task Send(Flag flag, byte[] dataToSend, CancellationToken ct)
        {
            CancellationToken cancel = CancellationTokenSource.CreateLinkedTokenSource(onClose.Token, ct).Token;

            if (dataToSend.Length > BUFFERSIZE - 1)
            {
                await SendMultiPart(flag, dataToSend, ct);
                return;
            }

            sendBuffer[0] = (byte)flag;
            dataToSend.CopyTo(sendBuffer, 1);

            await webSocket.SendAsync(new ArraySegment<byte>(sendBuffer, 0, dataToSend.Length + 1), WebSocketMessageType.Binary, true, cancel);
        }

        public async Task Send(Flag flag, CancellationToken ct)
        {
            CancellationToken cancel = CancellationTokenSource.CreateLinkedTokenSource(onClose.Token, ct).Token;

            sendBuffer[0] = (byte)flag;
            await webSocket.SendAsync(new ArraySegment<byte>(sendBuffer, 0, 1), WebSocketMessageType.Binary, true, cancel);
        }

        /// <summary>
        ///     Stops listining to websocket and closes it.
        /// </summary>
        /// <returns></returns>
        public async Task Close()
        {
            if (webSocket.State == WebSocketState.Open)
            {
                Console.WriteLine("Closing connection");
                await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Normal close", CancellationToken.None);
            }

            onClose.Cancel();
            callbacks.Clear();
        }
    }
}
