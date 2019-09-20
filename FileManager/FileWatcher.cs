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
        private string incomingFolder = @"C:\Users\Lee\Documents\Test\From\";
        private string dumpsFolder = @"C:\Users\Lee\Documents\Test\To\";
        private long maxDiskBytes = long.MaxValue;
        private double maxDiskPercent = 50;

        public FileWatcher()
        {
            if (!Directory.Exists(dumpsFolder))
            {
                Directory.CreateDirectory(dumpsFolder);
            }

            if (long.TryParse(Environment.GetEnvironmentVariable("MAX_DISK_BYTES"), out long mdb)) { maxDiskBytes = mdb; }
            if (double.TryParse(Environment.GetEnvironmentVariable("MAX_DISK_PERCENT"), out double mdp)) { maxDiskPercent = mdp; }
        }

        public async Task WatchFolder(CancellationToken cancellationToken)
        {
            CleanSpace();

            CancellationTokenSource watchCanceler = new CancellationTokenSource();
            Console.WriteLine("Making filewatcher");

            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                watcher.Path = incomingFolder;
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
                watcher.Deleted += OnChanged;
                watcher.Renamed += OnRenamed;

                // Begin watching.
                watcher.EnableRaisingEvents = true;

                Console.WriteLine("Starting watching");
                try
                {
                    await Task.Delay(Timeout.Infinite, CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, watchCanceler.Token).Token);
                }
                catch (OperationCanceledException) { }
            }
        }

        private static void OnRenamed(object source, RenamedEventArgs e) =>
            // Specify what is done when a file is renamed.
            Console.WriteLine($"File: {e.OldFullPath} renamed to {e.FullPath}");
    }
}
