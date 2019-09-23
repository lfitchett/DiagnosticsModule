using DeviceStreamsTests;
using FileManager;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FilewatcherTests
{
    public class Cleanup : FileWatcherSetup
    {
        [Test]
        public async Task CleanupBigFiles()
        {
            Environment.SetEnvironmentVariable("MAX_DISK_BYTES", "4500000");

            CancellationTokenSource cts = new CancellationTokenSource();

            FileWatcher watcher = new FileWatcher();
            Task watchTask = watcher.WatchFolder(cts.Token);

            await Task.WhenAll(Enumerable.Range(0, 10).Select(i => TestUtilities.MakeBigFile(Path.Combine(incomingDir, $"test{i}.txt"), 1000)));

            await Task.Delay(50);
            cts.Cancel();
            await watchTask;

            string[] files = Directory.GetFiles(storageDir);
            Assert.AreEqual(4, files.Length, "Expected 4 files in storage directory");
        }

        [Test]
        public async Task CleanupOnStartup()
        {
            Environment.SetEnvironmentVariable("MAX_DISK_BYTES", "4500000");

            CancellationTokenSource cts = new CancellationTokenSource();

            await Task.WhenAll(Enumerable.Range(0, 10).Select(i => TestUtilities.MakeBigFile(Path.Combine(incomingDir, $"test{i}.txt"), 1000)));

            FileWatcher watcher = new FileWatcher();
            Task watchTask = watcher.WatchFolder(cts.Token);

            await Task.Delay(50);
            cts.Cancel();
            await watchTask;

            string[] files = Directory.GetFiles(storageDir);
            Assert.AreEqual(4, files.Length, "Expected 4 files in storage directory");
        }

        [Test]
        public async Task CleanupOrdering()
        {
            Environment.SetEnvironmentVariable("MAX_DISK_BYTES", "4500000");

            CancellationTokenSource cts = new CancellationTokenSource();

            FileWatcher watcher = new FileWatcher();
            Task watchTask = watcher.WatchFolder(cts.Token);

            for (int i = 10; i < 20; i++)
            {
                await TestUtilities.MakeBigFile(Path.Combine(incomingDir, $"test{i}.txt"), 1000);
            }

            await Task.Delay(50);
            cts.Cancel();
            await watchTask;

            string[] files = Directory.GetFiles(storageDir);
            Assert.AreEqual(4, files.Length, "Expected 4 files in storage directory");

            foreach (string file in files)
            {
                string temp = file.Split("_")[0];
                string temp2 = temp.Substring(temp.Length - 6, 2);
                int i = int.Parse(temp2);

                Assert.GreaterOrEqual(i, 16, "Expected most recent files to be saved");
            }
        }
    }
}
