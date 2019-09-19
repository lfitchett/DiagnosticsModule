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

            /* recieve and reconstruct response headers */
            HttpResponseMessage response = await RecieveAndParse<HttpResponseMessage>(cancellationToken);

            /* recieve and reconstruct response content headers */
            KeyValuePair<string, string[]>[] contentHeaders = await RecieveAndParse<KeyValuePair<string, string[]>[]>(cancellationToken);

            /* apply headers to temp object */
            HttpContentHeaders tempHeaders = new StringContent("").Headers;
            ReplaceHeaders(tempHeaders, contentHeaders);

            /* recieve content */
            if (tempHeaders.Contains("Content-Length"))
            {
                /* large data case */
                int length = int.Parse(tempHeaders.GetValues("Content-Length").First());
                response.Content = new StreamContent(new WebsocketResultStream(length, websocket));
            }
            else
            {
                /* small data case */
                WebSocketReceiveResult websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), cancellationToken);
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
            ReplaceHeaders(response.Content.Headers, contentHeaders);

            return response;
        }

        private async Task<T> RecieveAndParse<T>(CancellationToken ct)
        {
            WebSocketReceiveResult websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), ct);
            string rawResponse = Encoding.UTF8.GetString(responseBuffer, 0, websocketResponse.Count);
            while (!websocketResponse.EndOfMessage)
            {
                websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), ct);
                rawResponse += Encoding.UTF8.GetString(responseBuffer, 0, websocketResponse.Count);
            }

            return JsonConvert.DeserializeObject<T>(rawResponse);
        }

        private void ReplaceHeaders(HttpContentHeaders content, IEnumerable<KeyValuePair<string, string[]>> headersToAdd)
        {
            foreach(var header in headersToAdd) 
            {
                content.Remove(header.Key);
                if (header.Value.Count() > 1)
                {
                    content.Add(header.Key, header.Value);
                }
                else
                {
                    content.Add(header.Key, header.Value.First());
                }
            }
        }
    }
}
