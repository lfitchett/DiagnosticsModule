using FileManager;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
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

            Environment.SetEnvironmentVariable("STORAGE_DIRECTORY", storageDir);
            Environment.SetEnvironmentVariable("INCOMING_DIRECTORY", incomingDir);
        }

        [Test]
        public async Task StopWatching()
        {
            CancellationTokenSource cts = new CancellationTokenSource();

            FileWatcher watcher = new FileWatcher();
            Task watchTask = watcher.WatchFolder(cts.Token);

            await Task.Delay(100);
            cts.Cancel();

            await Task.WhenAny(
                watchTask,
                Task.Delay(1000).ContinueWith((_) => Assert.Fail("Did not cancel task"))
            );
        }

        [Test]
        public async Task MoveFile()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            string fileName = "test.txt";

            FileWatcher watcher = new FileWatcher();
            Task watchTask = watcher.WatchFolder(cts.Token);

            File.WriteAllText(Path.Combine(incomingDir, fileName), "This is a test file!");

            await Task.Delay(50);
            cts.Cancel();
            await watchTask;

            string[] files = Directory.GetFiles(storageDir);
            Assert.AreEqual(1, files.Length, "Expected only one file in storage directory");
            string file = files[0];
            Assert.True(file.Contains(fileName), "Expected file to contain original name");
            Assert.AreEqual(File.ReadAllText(file), "This is a test file!", "Expected file contents to remain the same");
        }

        [Test]
        public async Task MoveMultiple()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            /* must be < 90 */
            int numFiles = 50;

            FileWatcher watcher = new FileWatcher();
            Task watchTask = watcher.WatchFolder(cts.Token);

            for (int i = 10; i < numFiles + 10; i++)
            {
                File.WriteAllText(Path.Combine(incomingDir, $"test{i}.txt"), $"This is test file number {i}");
            }

            await Task.Delay(50);
            cts.Cancel();
            await watchTask;

            string[] files = Directory.GetFiles(storageDir);
            Assert.AreEqual(numFiles, files.Length, $"Expected all {numFiles} files in storage directory");

            foreach(string file in files)
            {
                string temp = file.Split("_")[0];
                string temp2 = temp.Substring(temp.Length - 6, 2);
                int i = int.Parse(temp2);

                Assert.True(file.Contains($"test{i}.txt"), $"Expected {file} to contain test{i}.txt");
                Assert.AreEqual(File.ReadAllText(file), $"This is test file number {i}", "Expected file contents to remain the same");
            }
        }
    }
}
