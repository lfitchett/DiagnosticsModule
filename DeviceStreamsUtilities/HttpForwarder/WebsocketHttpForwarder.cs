using DeviceStreamsUtilities.HttpForwarder;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly byte[] responseBuffer = new byte[WebsocketHttpShared.BUFFER_SIZE];
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
                if (websocketResponse.MessageType == WebSocketMessageType.Close)
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

                HttpContent content = response.Content;
                response.Content = null;

                Console.WriteLine("Sending headers");
                byte[] responseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
                await websocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Binary, true, ct);

                Console.WriteLine("Sending content");
                string contentSerialized = JsonConvert.SerializeObject(content.Headers.AsEnumerable().ToArray());
                Console.WriteLine(contentSerialized);
                byte[] contentBytes = Encoding.UTF8.GetBytes(contentSerialized);
                await websocket.SendAsync(new ArraySegment<byte>(contentBytes), WebSocketMessageType.Binary, true, ct);
                if (content.Headers.Contains("Content-Length"))
                {
                    int length = int.Parse(content.Headers.GetValues("Content-Length").First());

                    Stream stream = await content.ReadAsStreamAsync();
                    while (stream.Position < stream.Length)
                    {
                        int numToSend = stream.Read(responseBuffer, 0, responseBuffer.Length);
                        await websocket.SendAsync(new ArraySegment<byte>(responseBuffer, 0, numToSend), WebSocketMessageType.Binary, true, ct);
                    }
                }
                else
                {
                    byte[] body = await content.ReadAsByteArrayAsync();
                    if(body.Length == 0)
                    {
                        body = Encoding.UTF8.GetBytes("No Content");
                    }

                    await websocket.SendAsync(new ArraySegment<byte>(body), WebSocketMessageType.Binary, true, ct);
                }

                Console.WriteLine("Finished forwarding");
            }
        }
    }
}
