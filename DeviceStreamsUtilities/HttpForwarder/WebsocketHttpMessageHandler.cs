using DeviceStreamsUtilities.HttpForwarder;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceStreamsUtilities
{
    public class WebsocketHttpMessageHandler : HttpMessageHandler
    {
        private readonly WebSocket websocket;
        private readonly byte[] responseBuffer = new byte[WebsocketHttpShared.BUFFER_SIZE];

        public WebsocketHttpMessageHandler(WebSocket websocket)
        {
            this.websocket = websocket;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            /* serialize and send request */
            byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request));
            await websocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, cancellationToken);

            /* recieve headers */
            WebSocketReceiveResult websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), cancellationToken);
            string rawResponse = Encoding.UTF8.GetString(responseBuffer, 0, websocketResponse.Count);

            /* reconstruct response */
            HttpResponseMessage response = JsonConvert.DeserializeObject<HttpResponseMessage>(rawResponse);

            /* reconstruct content headers */
            websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), cancellationToken);
            string rawContentHeaders = Encoding.UTF8.GetString(responseBuffer, 0, websocketResponse.Count);
            var contentHeaders = JsonConvert.DeserializeObject<KeyValuePair<string, string[]>[]>(rawContentHeaders);

            /* apply headers to temp object */
            response.Content = new StringContent("");
            Action applyHeaders = () =>
            {
                contentHeaders.ToList().ForEach((h) =>
                {
                    response.Content.Headers.Remove(h.Key);
                    if (h.Value.Length > 1)
                        response.Content.Headers.Add(h.Key, h.Value);
                    else
                        response.Content.Headers.Add(h.Key, h.Value[0]);
                });
            };
            applyHeaders();

            /* recieve content */
            if (response.Content.Headers.Contains("Content-Length"))
            {
                /* large data case */
                int length = int.Parse(response.Content.Headers.GetValues("Content-Length").First());
                response.Content = new StreamContent(new WebsocketResultStream(length, websocket));
            }
            else
            {
                /* small data case */
                websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), cancellationToken);
                List<byte> rawBody = new List<byte>();
                rawBody.AddRange(responseBuffer.Take(websocketResponse.Count));

                while (!websocketResponse.EndOfMessage)
                {
                    websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), cancellationToken);
                    rawBody.AddRange(responseBuffer.Take(websocketResponse.Count));
                }

                response.Content = new ByteArrayContent(rawBody.ToArray());
            }

            /* apply headers to final content */
            applyHeaders();

            return response;
        }
    }
}
