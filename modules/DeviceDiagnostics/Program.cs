
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceStreamsUtilities;
using Microsoft.Azure.Devices.Client;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starting");
        DeviceClient client = DeviceClient.CreateFromConnectionString("HostName=lefitche-hub-3.azure-devices.net;DeviceId=device4;SharedAccessKey=NRjCGhamp4JCZiZzrwwJ/QZWbAsQ8qHa8B0BZSOFBZg=");

        client.RegisterDeviceStreamCallback(OnRecieve, CancellationToken.None).Wait();
    }

    static async Task OnRecieve(ClientWebSocket webSocket, CancellationToken ct)
    {
        Console.WriteLine("Recieved connection");
        await new WebsocketHttpForwarder(webSocket).StartForwarding(ct);
        Console.WriteLine("Done forwarding");
    }
}
