using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceDiagnostics
{
    public class HttpForwarder
    {
        private readonly WebSocket websocket;
        private readonly byte[] responseBuffer = new byte[10240];
        private static readonly HttpClient httpClient = new HttpClient();

        public HttpForwarder(WebSocket websocket)
        {
            this.websocket = websocket;
        }

        public async Task StartForwarding(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                WebSocketReceiveResult websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), ct);
                string rawRequest = Encoding.UTF8.GetString(responseBuffer, 0, websocketResponse.Count);

                while (!websocketResponse.EndOfMessage)
                {
                    websocketResponse = await websocket.ReceiveAsync(new ArraySegment<byte>(responseBuffer), ct);
                    rawRequest += Encoding.UTF8.GetString(responseBuffer, 0, websocketResponse.Count);
                }

                HttpRequestMessage request = JsonConvert.DeserializeObject<HttpRequestMessage>(rawRequest);

                HttpResponseMessage response = await httpClient.SendAsync(request, ct);

                byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
                await websocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, ct);
            }
        }
    }
}
