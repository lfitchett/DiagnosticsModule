using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeviceStreamsTests.Setup
{
    public class SetupClients : SetupBase
    {
        protected string deviceId;
        protected DeviceClient deviceClient;
        protected ServiceClient serviceClient;

        [SetUp]
        public async Task Setup()
        {
            var device = await MakeNewDevice();
            deviceId = device.Id;

            string deviceConnString = $"HostName=lefitche-hub-3.azure-devices.net;DeviceId={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}";
            deviceClient = DeviceClient.CreateFromConnectionString(deviceConnString);

            serviceClient = ServiceClient.CreateFromConnectionString("HostName=lefitche-hub-3.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=s+3pkFuO8O4leS3mIFl1aW6O0/ASKEo85Cv0mjgrDUg=");
        }
    }
}
