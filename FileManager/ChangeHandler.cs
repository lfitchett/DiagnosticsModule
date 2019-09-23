using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileManager
{
    public partial class FileWatcher
    {
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
            if(e.ChangeType != WatcherChangeTypes.Changed)
            {
                Console.WriteLine("Unexpected event");
            }

            MoveFile(e.FullPath);
        }

        private void MoveFile(string filename)
        {
            CleanSpace(new FileInfo(filename).Length);

            string newFile = Path.Combine(storageDirectory, $"{Path.GetFileName(filename)}_{DateTime.Now.Ticks}");
            Console.WriteLine($"Moving {filename} to {newFile}");
            File.Move(filename, newFile);
        }
    }
}
