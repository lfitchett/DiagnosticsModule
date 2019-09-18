using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceStreamsTests.Setup
{
    public class SetupWebserver : SetupClients
    {
        protected CancellationTokenSource cancelServer = new CancellationTokenSource();
        private Task server;

        [OneTimeSetUp]
        public void StartServer()
        {
            string[] args = { "Test" };
            server = DeviceDiagnostics.Diagnostics.RunWebserver(args, cancelServer.Token);
        }

        [SetUp]
        [TearDown]
        public void CheckServer()
        {
            Assert.False(server.IsFaulted, $"Webserver died:\n{server.Exception}");
        }

        [OneTimeTearDown]
        public async Task EndServer()
        {
            cancelServer.Cancel();
            try
            {
                await server;
            }
            catch (OperationCanceledException) { }
        }
    }
}
