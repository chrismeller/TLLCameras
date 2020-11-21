using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

namespace TLLCameras.Analysis.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            Run().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public static async Task Run()
        {
            var i = 0;
            foreach (var file in Directory.EnumerateFiles(@"C:\Users\chris\Dropbox\File requests\TLLCameras\20190120", "37_*"))
            {
                using (var image = Image.Load(file))
                {
                    // background color
                    var backgroundColor = image[1067, 112];

                    var tramColor = image[1096, 244];

                    var average = (Convert.ToDecimal(tramColor.PackedValue) /
                                   Convert.ToDecimal(backgroundColor.PackedValue));

                    if (average < Convert.ToDecimal(0.9999))
                    {
                        Console.WriteLine($"{file}\tNO TRAM\t\t{average:F5}");
                        File.Copy(file, Path.Join(@"C:\Temp\NoTram\", Path.GetFileName(file)));
                    }
                    else
                    {
                        Console.WriteLine($"{file}\tTRAM PRESENT!\t{average:F5}");
                        File.Copy(file, Path.Join(@"C:\Temp\WithTram\", Path.GetFileName(file)));
                    }
                }

                i++;
            }
        }
    }
}
