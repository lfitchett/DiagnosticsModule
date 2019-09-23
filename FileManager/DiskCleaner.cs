using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileManager
{
    public partial class FileWatcher
    {
        private long targetMaxBytes
        {
            get
            {
                long maxBytesFromPercent = (long)(maxDiskPercent / 100 * GetAvaliableSpace(new DirectoryInfo(storageDirectory)));
                return Math.Min(maxDiskBytes, maxBytesFromPercent);
            }
        }

        private void CleanSpace(long desiredFreeSpace = 0)
        {
            long targetBytes = targetMaxBytes - desiredFreeSpace;

            DirectoryInfo dumpsDir = new DirectoryInfo(storageDirectory);
            long currentSize = DirSize(dumpsDir);
            Console.WriteLine($"Currently using {currentSize} out of {targetMaxBytes}.");


            if (currentSize > targetBytes)
            {
                Console.WriteLine("Cleaning disk space");
                long amountToClean = currentSize - targetBytes;
                IEnumerable<FileInfo> filesToRemove = dumpsDir.EnumerateFiles().OrderBy(f => f.LastWriteTime).TakeWhile(f => (amountToClean -= f.Length) >= 0 - f.Length);

                foreach (FileInfo file in filesToRemove)
                {
                    Console.WriteLine($"Removing {file}");
                    file.Delete();
                }

                Console.WriteLine($"Finished cleaning.");
                Console.WriteLine($"Now using {DirSize(new DirectoryInfo(storageDirectory))} out of {targetMaxBytes}.");
            }
        }

        private long GetAvaliableSpace(DirectoryInfo dir)
        {
            return DriveInfo.GetDrives().Where(d => d.RootDirectory.FullName == dir.Root.FullName).First().AvailableFreeSpace;
        }

        private long DirSize(DirectoryInfo dir)
        {
            return dir.EnumerateFiles().Sum(f => f.Length) + dir.EnumerateDirectories().Sum(DirSize);
        }
    }
}
