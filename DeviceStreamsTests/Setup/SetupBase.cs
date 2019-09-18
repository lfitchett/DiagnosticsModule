using Microsoft.Azure.Devices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeviceStreamsTests.Setup
{
    public class SetupBase
    {
        private RegistryManager registryManager;
        private List<Device> addedDevices = new List<Device>();

        [OneTimeSetUp]
        public void Init()
        {
            string hubConnString = "HostName=lefitche-hub-3.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=wyT/feMmLKDj8wnWxLCHkQERmOUBWaeuLMDLDjAILug=";

            registryManager = RegistryManager.CreateFromConnectionString(hubConnString);
        }

        [OneTimeTearDown]
        public async Task RemoveDevices()
        {
            await registryManager.RemoveDevices2Async(addedDevices);
            registryManager.Dispose();
        }

        protected async Task<Device> MakeNewDevice()
        {
            Device device = await registryManager.AddDeviceAsync(new Device($"TestDevice{Guid.NewGuid().ToString()}"));
            addedDevices.Add(device);
            return device;
        }
    }
}
