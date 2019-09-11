
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceDiagnostics;
using DeviceStreamsUtilities;
using Microsoft.Azure.Devices.Client;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starting");
        DeviceClient client = DeviceClient.CreateFromConnectionString("HostName=lefitche-hub-3.azure-devices.net;DeviceId=device4;SharedAccessKey=NRjCGhamp4JCZiZzrwwJ/QZWbAsQ8qHa8B0BZSOFBZg=");

        client.RegisterDeviceStreamCallback(TestRecieveAsync, CancellationToken.None).Wait();
    }

    static async Task TestRecieveAsync(ClientWebSocket webSocket, CancellationToken ct)
    {
        Console.WriteLine("Recieved connection");
        WebSocketManager manager = new WebSocketManager(webSocket);

        await new CommandListener(webSocket, ct).StartListening();
    }
}
