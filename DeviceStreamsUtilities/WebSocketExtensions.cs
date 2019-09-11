using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceStreamsUtilities
{
    public static class WebSocketExtensions
    {
        public static async Task SendFlag(this ClientWebSocket webSocket, Flag flag, CancellationToken token)
        {
            Console.WriteLine($"Sending flag {flag}");
            byte[] buffer = { (byte)flag };
            await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, token);
            Console.WriteLine($"Sent flag {flag}");
        }

        public static async Task<Flag> RecieveFlag(this ClientWebSocket webSocket, CancellationToken token)
        {
            Console.WriteLine($"Waiting for flag");
            byte[] buffer = { 0 };
            await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
            Console.WriteLine($"Got flag {(Flag)buffer[0]}");

            return (Flag)buffer[0];
        }

        public static async Task SendText(this ClientWebSocket webSocket, string text, CancellationToken token)
        {
            Console.WriteLine($"Sending text {text}");
            await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(text)), WebSocketMessageType.Binary, true, token);
            Console.WriteLine($"Sent text {text}");
        }

        public static async Task<string> RecieveText(this ClientWebSocket webSocket, CancellationToken token)
        {
            Console.WriteLine($"Waiting for text");
            byte[] buffer = new byte[1024];
            string result = "";
            while (webSocket.State == WebSocketState.Open)
            {
                var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);
                result += Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

                if (receiveResult.EndOfMessage)
                {
                    break;
                }
            }
            Console.WriteLine($"Got text {result}");

            return result;
        }


    }
}
