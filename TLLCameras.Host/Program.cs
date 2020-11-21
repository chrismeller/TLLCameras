using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TLLCameras.Client;

namespace TLLCameras.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            Run(args[0]).ConfigureAwait(false).GetAwaiter().GetResult();
            //Organize();
        }

        private static void Organize()
        {
            foreach (var file in Directory.EnumerateFiles(@"C:\Temp\TLLCameras\", "*.jpg"))
            {
                var fileTimestamp = Convert.ToDouble(Path.GetFileNameWithoutExtension(file).Split('_').Last());

                var timestamp = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(fileTimestamp);

                var directory = @"C:\Temp\TLLCameras\" + timestamp.ToString("yyyyMMdd");

                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                var destinationFilename = Path.Join(directory, Path.GetFileName(file));

                Console.WriteLine("Moving {0} to {1}", file, destinationFilename);

                File.Move(file, destinationFilename);
            }
        }

        private static async Task Run(string directory)
        {
            var cameras = new List<int>() {37, 38, 39};

            foreach (var camera in cameras)
            {
                Console.WriteLine("Starting {0}", camera);

                var thread = new Thread(async () => await RunThread(camera, directory));
                thread.Start();
            }

            Console.ReadKey();
        }

        private static async Task RunThread(int camera, string directory)
        {
            var scraper = new Scraper();

            var lastHash = "";
            do
            {
                try
                {
                    var driveInfo = new DriveInfo(directory);
                    if (driveInfo.AvailableFreeSpace < driveInfo.TotalSize * .2)
                    {
                        Console.WriteLine("Free space is low. Exiting!");
                        Environment.Exit(1);
                    }

                    var fullDirectory = Path.Join(directory, DateTimeOffset.UtcNow.ToString("yyyyMMdd"));

                    if (!Directory.Exists(fullDirectory)) Directory.CreateDirectory(fullDirectory);

                    var timestamp = (DateTimeOffset.UtcNow - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero))
                        .TotalSeconds;

                    var filename = $"{camera}_{timestamp}.jpg";
                    var fullFilename = Path.Join(fullDirectory, filename);

                    using (var imageStream = await scraper.GetImage(camera))
                    using (var fileStream = File.OpenWrite(fullFilename))
                    {
                        await imageStream.CopyToAsync(fileStream);
                    }

                    Console.WriteLine("{0} camera {1} updated.", timestamp, camera);

                    await Task.Delay(1000);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed: {0} at {1}: {2}", camera, DateTimeOffset.UtcNow, e.Message);
                }
            } while (true);
        }

        private static string Md5(string input)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = md5.ComputeHash(bytes);

                var bc = BitConverter.ToString(hashBytes);

                // lowercase it and get a standardized 8-4-4-4-12 grouping
                // in opposite order so we don't have to offset for the -'s we've already added
                bc = bc.ToLower().Replace("-", "").Insert(20, "-").Insert(16, "-").Insert(12, "-").Insert(8, "-");

                return bc;
            }
        }
    }
}
