using FileManager;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceStreamsTests
{
    public class Filewatcher
    {
        private readonly string testDir, storageDir, incomingDir;

        public Filewatcher()
        {
            testDir = Path.Combine(Directory.GetCurrentDirectory(), "runtime");
            storageDir = Path.Combine(testDir, "storage");
            incomingDir = Path.Combine(testDir, "incoming");
        }

        [SetUp]
        public void Setup()
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }

            Directory.CreateDirectory(storageDir);
            Directory.CreateDirectory(incomingDir);
        }

        [Test]
        public async Task StopWatching()
        {
            Environment.SetEnvironmentVariable("STORAGE_DIRECTORY", storageDir);
            Environment.SetEnvironmentVariable("INCOMING_DIRECTORY", incomingDir);
            CancellationTokenSource cts = new CancellationTokenSource();

            FileWatcher watcher = new FileWatcher();
            Task watchTask = watcher.WatchFolder(cts.Token);

            await Task.Delay(1000);
            cts.Cancel();

            await Task.WhenAny(
                watchTask,
                Task.Delay(1000).ContinueWith((_) => Assert.Fail("Did not cancel task"))
            );
        }
    }
}
