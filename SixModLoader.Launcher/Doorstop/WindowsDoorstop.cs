using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace SixModLoader.Launcher.Doorstop
{
    public class WindowsDoorstop : IDoorstop
    {
        private const string DoorstopPath = "winhttp.dll";

        public async Task DownloadAsync()
        {
            if (!File.Exists(DoorstopPath))
            {
                Console.WriteLine($"Downloading Doorstop ({DoorstopPath})");

                var stream = await Program.HttpClient.GetStreamAsync("https://github.com/NeighTools/UnityDoorstop/releases/download/v3.0.2.2/Doorstop_x64_3.0.2.2.zip");
                var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);

                zipArchive.GetEntry(DoorstopPath).ExtractToFile(DoorstopPath, true);
            }
        }

        public void PreLaunch()
        {
            Console.WriteLine("Configuring Doorstop");
            
            var iniFile = new IniFile("doorstop_config.ini");

            var section = "UnityDoorstop";
            iniFile.Write("enabled", "true", section);
            iniFile.Write("targetAssembly", Program.SixModLoaderPath, section);
            iniFile.Write("redirectOutputLog", "false", section);
        }
    }
}