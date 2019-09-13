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
            byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request));
            await websocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, cancellationToken);

            WebSocketReceiveResult websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), cancellationToken);
            string rawResponse = Encoding.UTF8.GetString(responseBuffer, 0, websocketResponse.Count);

            while (!websocketResponse.EndOfMessage)
            {
                websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), cancellationToken);
                rawResponse += Encoding.UTF8.GetString(responseBuffer, 0, websocketResponse.Count);
            }

            return JsonConvert.DeserializeObject<HttpResponseMessage>(rawResponse);
        }
    }
}
