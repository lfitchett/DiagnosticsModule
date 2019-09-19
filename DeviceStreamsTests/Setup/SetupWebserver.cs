using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceStreamsTests.Setup
{
    public class SetupWebserver : SetupClients
    {
        protected CancellationTokenSource cancelServer = new CancellationTokenSource();
        protected string targetDirectory;
        private Task server;

        [OneTimeSetUp]
        public Task StartServer()
        {
            targetDirectory = $"{Environment.CurrentDirectory}/testFiles";
            Assert.IsTrue(Directory.Exists(targetDirectory), "Test directory not found");
            Environment.SetEnvironmentVariable("TARGET_DIRECTORY", targetDirectory);

            string[] args = { "Test" };
            server = DeviceDiagnostics.Diagnostics.RunWebserver(args, cancelServer.Token);

            return Task.Delay(200);
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
