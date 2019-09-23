using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager
{
    public partial class FileWatcher
    {
        private string incomingDirectory;
        private string storageDirectory;
        private long maxDiskBytes = long.MaxValue;
        private double maxDiskPercent = 50;

        public FileWatcher()
        {
            incomingDirectory = Environment.GetEnvironmentVariable("INCOMING_DIRECTORY") ?? "/dumps";
            storageDirectory = Environment.GetEnvironmentVariable("STORAGE_DIRECTORY") ?? "/storage";

            if (!Directory.Exists(storageDirectory))
            {
                Directory.CreateDirectory(storageDirectory);
            }

            if (long.TryParse(Environment.GetEnvironmentVariable("MAX_DISK_BYTES"), out long mdb)) { maxDiskBytes = mdb; }
            if (double.TryParse(Environment.GetEnvironmentVariable("MAX_DISK_PERCENT"), out double mdp)) { maxDiskPercent = mdp; }
        }

        public async Task WatchFolder(CancellationToken cancellationToken)
        {
            Console.WriteLine("Starting watching");
            CleanSpace();
            foreach (var file in Directory.EnumerateFiles(incomingDirectory))
            {
                MoveFile(file);
            }

            CancellationTokenSource watchCanceler = new CancellationTokenSource();
            Console.WriteLine("Making filewatcher");

            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                watcher.Path = incomingDirectory;
                watcher.NotifyFilter = NotifyFilters.LastWrite;

                watcher.Error += (sender, error) =>
                {
                    Console.WriteLine("Error in file watcher");
                    Console.WriteLine(error);
                    watchCanceler.Cancel();
                };

                // Add event handlers.
                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;

                // Begin watching.
                watcher.EnableRaisingEvents = true;

                Console.WriteLine("Starting watching");
                try
                {
                    await Task.Delay(Timeout.Infinite, CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, watchCanceler.Token).Token);
                }
                catch (OperationCanceledException) { }
                Console.WriteLine("Stopped Watching");
            }
        }       
    }
}
