﻿using System;
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

            string newFile = Path.Combine(dumpsFolder, $"{e.Name}_{DateTime.Now.Ticks}");
            File.Move(e.FullPath, newFile);

            CleanSpace();
        }
    }
}
