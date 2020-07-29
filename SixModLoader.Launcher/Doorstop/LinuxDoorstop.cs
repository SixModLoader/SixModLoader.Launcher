using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace SixModLoader.Launcher.Doorstop
{
    public class LinuxDoorstop : IDoorstop
    {
        private const string DoorstopPath = "libdoorstop_x64.so";
        
        public async Task DownloadAsync()
        {
            if (!File.Exists(DoorstopPath))
            {
                Console.WriteLine("Downloading " + DoorstopPath);

                var stream = await Program.HttpClient.GetStreamAsync("https://github.com/NeighTools/UnityDoorstop.Unix/releases/download/v1.3.0.0/doorstop_v1.3.0.0_linux.zip");
                var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);

                zipArchive.GetEntry(DoorstopPath).ExtractToFile(DoorstopPath, true);
            }
        }

        public void PreLaunch()
        {
            Console.WriteLine("Setting Doorstop environment variables");
            
            var currentDirectory = Directory.GetCurrentDirectory();
            Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", $"{Environment.GetEnvironmentVariable("LD_LIBRARY_PATH")}:{currentDirectory}");
            Environment.SetEnvironmentVariable("LD_PRELOAD", DoorstopPath);

            Environment.SetEnvironmentVariable("DOORSTOP_ENABLE", "TRUE");
            Environment.SetEnvironmentVariable("DOORSTOP_INVOKE_DLL_PATH", Path.Combine(currentDirectory, Program.SixModLoaderPath));
        }
    }
}