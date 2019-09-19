using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileManager
{
    public class FileWatcher
    {
        public async Task WatchFolder(CancellationToken cancellationToken)
        {
            string folder = @"C:\Users\Lee\Documents\Test\From\";
            CancellationTokenSource watchCanceler = new CancellationTokenSource();
            Console.WriteLine("Making filewatcher");

            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                watcher.Path = folder;

                // Watch for changes in LastAccess and LastWrite times, and
                // the renaming of files or directories.
                //watcher.NotifyFilter = NotifyFilters

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

        // Define the event handlers.
        private static void OnChanged(object source, FileSystemEventArgs e) =>
            // Specify what is done when a file is changed, created, or deleted.
            Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");

        private static void OnRenamed(object source, RenamedEventArgs e) =>
            // Specify what is done when a file is renamed.
            Console.WriteLine($"File: {e.OldFullPath} renamed to {e.FullPath}");
    }
}
