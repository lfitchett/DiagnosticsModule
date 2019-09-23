using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FilewatcherTests
{
    public class FileWatcherSetup
    {
        protected readonly string testDir, storageDir, incomingDir;

        public FileWatcherSetup()
        {
            testDir = Path.Combine(Directory.GetCurrentDirectory(), "runtime");
            storageDir = Path.Combine(testDir, "storage");
            incomingDir = Path.Combine(testDir, "incoming");
        }

        [SetUp]
        public void SetupDirectories()
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
    }
}
