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
        private readonly byte[] buffer = new byte[WebsocketHttpShared.BUFFER_SIZE];

        public WebsocketHttpForwarder(WebSocket websocket)
        {
            this.websocket = websocket;
        }

        public async Task StartForwarding(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                HttpRequestMessage request = await WaitForRequest(ct);
                if (request == null)
                {
                    return;
                }

                HttpResponseMessage response = await MakeRequest(request, ct);

                await ForwardResponse(response, ct);
            }
        }

        private async Task<HttpRequestMessage> WaitForRequest(CancellationToken ct)
        {
            Console.WriteLine("Waiting for request");
            WebSocketReceiveResult websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
            if (websocketResponse.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine("Websocket closed normally");
                return null;
            }

            Console.WriteLine($"Recieved request");
            string rawRequest = Encoding.UTF8.GetString(buffer, 0, websocketResponse.Count);

            while (!websocketResponse.EndOfMessage)
            {
                websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                rawRequest += Encoding.UTF8.GetString(buffer, 0, websocketResponse.Count);
            }

            return JsonConvert.DeserializeObject<HttpRequestMessage>(rawRequest);
        }

        private async Task<HttpResponseMessage> MakeRequest(HttpRequestMessage request, CancellationToken ct)
        {
            Console.WriteLine($"Forwarding request to {request.RequestUri}");
            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.SendAsync(request, ct);
                Console.WriteLine($"Got response. Status: {response.StatusCode}");
                return response;
            }
        }

        private async Task ForwardResponse(HttpResponseMessage response, CancellationToken ct)
        {
            HttpContent content = response.Content;
            response.Content = null;

            Console.WriteLine("Sending headers");
            byte[] responseBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
            await websocket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Binary, true, ct);

            Console.WriteLine("Sending content headers");
            byte[] contentBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(content.Headers.AsEnumerable().ToArray()));
            await websocket.SendAsync(new ArraySegment<byte>(contentBytes), WebSocketMessageType.Binary, true, ct);

            Console.WriteLine("Sending content body");
            if (content.Headers.Contains("Content-Length"))
            {
                int length = int.Parse(content.Headers.GetValues("Content-Length").First());

                Stream stream = await content.ReadAsStreamAsync();
                while (stream.Position < stream.Length)
                {
                    int numToSend = stream.Read(buffer, 0, buffer.Length);
                    await websocket.SendAsync(new ArraySegment<byte>(buffer, 0, numToSend), WebSocketMessageType.Binary, true, ct);
                }
            }
            else
            {
                byte[] body = await content.ReadAsByteArrayAsync();
                if (body.Length == 0)
                {
                    body = Encoding.UTF8.GetBytes("No Content");
                }

                await websocket.SendAsync(new ArraySegment<byte>(body), WebSocketMessageType.Binary, true, ct);
            }

            Console.WriteLine("Finished forwarding");
        }
    }
}
