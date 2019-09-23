using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DeviceStreamsTests
{
    public static class TestUtilities
    {
        /// <summary>
        ///     Makes a file of about size bytes
        /// </summary>
        /// <param name="path">Path of the file to be created</param>
        /// <param name="approxSize">Approximate size of the file in kBytes</param>
        public static async Task MakeBigFile(string path, int approxSize)
        {
            using (var file = File.OpenWrite(path))
            {
                for (int i = 0; i < approxSize * 50; i++)
                {
                    await file.WriteAsync(Encoding.UTF8.GetBytes($"This is line {i}!\n"));
                }
            }
        }
    }
}
