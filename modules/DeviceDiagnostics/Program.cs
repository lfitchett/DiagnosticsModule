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
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting");
            DeviceClient client = DeviceClient.CreateFromConnectionString("HostName=lefitche-hub-3.azure-devices.net;DeviceId=device4;SharedAccessKey=NRjCGhamp4JCZiZzrwwJ/QZWbAsQ8qHa8B0BZSOFBZg=");


            Task.WhenAny(
                client.RegisterDeviceStreamCallback(async (webSocket, ct) =>
                    {
                        Console.WriteLine("Recieved connection");
                        await new WebsocketHttpForwarder(webSocket).StartForwarding(ct);
                        Console.WriteLine("Done forwarding");
                    }, CancellationToken.None),
                Task.Run(CreateWebHostBuilder(args).Build().Run)
            ).Wait();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
