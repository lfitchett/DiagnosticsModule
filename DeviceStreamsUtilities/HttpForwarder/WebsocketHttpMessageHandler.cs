using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceStreamsUtilities
{
    public class WebsocketHttpMessageHandler : HttpMessageHandler
    {
        private readonly WebSocket websocket;
        private readonly byte[] responseBuffer = new byte[10240];

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

            while (!websocketResponse.EndOfMessage)
            {
                websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), cancellationToken);
                rawResponse += Encoding.UTF8.GetString(responseBuffer, 0, websocketResponse.Count);
            }

            /* reconstruct response */
            HttpResponseMessage response = JsonConvert.DeserializeObject<HttpResponseMessage>(rawResponse);
            if (!response.IsSuccessStatusCode)
            {
                return response;
            }

            /* recieve body */
            websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), cancellationToken);
            List<byte> rawBody = new List<byte>();
            rawBody.AddRange(responseBuffer.Take(websocketResponse.Count));

            while (!websocketResponse.EndOfMessage)
            {
                websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), cancellationToken);
                rawBody.AddRange(responseBuffer.Take(websocketResponse.Count));
            }

            
            response.Content = new ByteArrayContent(rawBody.ToArray());

            return response;
        }
    }
}
