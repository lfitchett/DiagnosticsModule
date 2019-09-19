using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DeviceStreamsUtilities;

namespace DeviceDiagnostics
{
    public class Diagnostics
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting");
            CancellationTokenSource ctSource = new CancellationTokenSource();

            Task.WhenAny(
                RunForwarder(args, ctSource.Token),
                RunWebserver(args, ctSource.Token)
            ).Wait();

            Console.WriteLine("Shutting down");
            ctSource.Cancel();
            Thread.Sleep(5000);
        }

        public static async Task RunForwarder(string[] args, CancellationToken ct)
        {
            DeviceClient client = DeviceClient.CreateFromConnectionString("HostName=lefitche-hub-3.azure-devices.net;DeviceId=device4;SharedAccessKey=NRjCGhamp4JCZiZzrwwJ/QZWbAsQ8qHa8B0BZSOFBZg=");

            await client.RegisterDeviceStreamCallback(async (webSocket, ct_func) =>
            {
                Console.WriteLine("Recieved connection");
                await new WebsocketHttpForwarder(webSocket).StartForwarding(ct_func);
                Console.WriteLine("Done forwarding");
            }, ct);
        }

        public static Task RunWebserver(string[] args, CancellationToken ct)
        {
            return WebHost.CreateDefaultBuilder(args).UseStartup<Startup>().Build().RunAsync(ct);
        }
    }
}
