using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NuGet.Versioning;
using SixModLoader.Launcher.Doorstop;

namespace SixModLoader.Launcher
{
    internal static class Program
    {
        public const string SixModLoaderPath = "SixModLoader/bin/SixModLoader.dll";

        public static HttpClient HttpClient { get; } = new HttpClient();
        public static SemanticVersion Version { get; } = SemanticVersion.Parse(Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion);

        public static async Task Main(string[] args)
        {
            HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SixModLoader.Launcher", Version.ToString()));
            Console.WriteLine($"SixModLoader.Launcher {Version}");

            await new AutoUpdate().UpdateAsync();

            string defaultProcess;
            IDoorstop doorstop;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                defaultProcess = "LocalAdmin";
                doorstop = new LinuxDoorstop();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                defaultProcess = "LocalAdmin.exe";
                doorstop = new WindowsDoorstop();
            }
            else
            {
                throw new InvalidOperationException("Unsupported OS");
            }

            await doorstop.DownloadAsync();
            
            Console.WriteLine();

            var file = (File.Exists("jumploader.txt") ? File.ReadAllText("jumploader.txt") : defaultProcess + " {args}")
                .Replace("{args}", string.Join(" ", args))
                .Split(' ');

            if (!File.Exists(file.First()))
            {
                Console.WriteLine($"{file.First()} not found!");
                return;
            }

            Console.WriteLine($"Starting \"{string.Join(" ", file).Trim()}\"");

            doorstop.PreLaunch();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = file.First(),
                    Arguments = string.Join(" ", file.Skip(1)),
                    UseShellExecute = false,
                    CreateNoWindow = false
                }
            };

            process.Start();

            process.WaitForExit();
            Console.WriteLine("Process exited");
        }
    }
}