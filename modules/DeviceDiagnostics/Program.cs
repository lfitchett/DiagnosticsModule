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
                RunForwarder(args, ctSource.Token)
                //RunWebserver(args, ctSource.Token)
            ).Wait();

            Console.WriteLine("Shutting down");
            ctSource.Cancel();
            Thread.Sleep(5000);
        }

        public static async Task RunForwarder(string[] args, CancellationToken ct)
        {
            //ModuleClient client = ModuleClient.CreateFromConnectionString(@"HostName=lefitche-hub-3.azure-devices.net;DeviceId=device4;ModuleId=$edgeAgent;SharedAccessKey=TZxV7meBl+dQPHR5AGbMzON1i/efvMUJ38+BucUMou8=");

            Console.WriteLine($"Conn String: {Environment.GetEnvironmentVariable("EdgeHubConnectionString")}");

            ModuleClient client = await ModuleClient.CreateFromEnvironmentAsync();

            Console.WriteLine($"{(await client.GetTwinAsync()).ToJson()}");

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
