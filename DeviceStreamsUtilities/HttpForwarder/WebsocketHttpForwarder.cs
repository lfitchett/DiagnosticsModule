using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceStreamsUtilities
{
    public class WebsocketHttpForwarder
    {
        private readonly WebSocket websocket;
        private readonly byte[] responseBuffer = new byte[10240];
        private static readonly HttpClient httpClient = new HttpClient();

        public WebsocketHttpForwarder(WebSocket websocket)
        {
            this.websocket = websocket;
        }

        public async Task StartForwarding(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                Console.WriteLine("Waiting for request");
                WebSocketReceiveResult websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), ct);
                if(websocketResponse.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                Console.WriteLine($"Recieved request");
                string rawRequest = Encoding.UTF8.GetString(responseBuffer, 0, websocketResponse.Count);

                while (!websocketResponse.EndOfMessage)
                {
                    websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), ct);
                    rawRequest += Encoding.UTF8.GetString(responseBuffer, 0, websocketResponse.Count);
                }

                HttpRequestMessage request = JsonConvert.DeserializeObject<HttpRequestMessage>(rawRequest);
                Console.WriteLine($"Forwarding request to {request.RequestUri}");

                HttpResponseMessage response = await httpClient.SendAsync(request, ct);
                Console.WriteLine($"Got response. Status: {response.StatusCode}");

                byte[] body = await response.Content.ReadAsByteArrayAsync();
                response.Content = null;
                byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));

                Console.WriteLine("Sending headers");
                await websocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, ct);
                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                Console.WriteLine("Sending content");
                await websocket.SendAsync(new ArraySegment<byte>(body), WebSocketMessageType.Binary, true, ct);
                Console.WriteLine("Finished forwarding");
            }
        }
    }
}
