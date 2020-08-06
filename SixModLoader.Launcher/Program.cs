using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
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
using Process = System.Diagnostics.Process;

namespace SixModLoader.Launcher
{
    internal static class Program
    {
        public const string SixModLoaderPath = "SixModLoader/bin/SixModLoader.dll";

        public static SemanticVersion Version { get; } = SemanticVersion.Parse(Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion);
        public static HttpClient HttpClient { get; } = new HttpClient();
        public static IDoorstop Doorstop { get; private set; }

        public static async Task<int> Main(string[] args)
        {
            HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SixModLoader.Launcher", Version.ToString()));
            Console.WriteLine($"SixModLoader.Launcher {Version}");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Doorstop = new LinuxDoorstop();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Doorstop = new WindowsDoorstop();
            }
            else
            {
                throw new InvalidOperationException("Unsupported OS");
            }

            const string jumploaderFile = "jumploader.txt";

            if (File.Exists(jumploaderFile))
            {
                args = CommandLineStringSplitter.Instance.Split(File.ReadAllText(jumploaderFile).Replace("{args}", string.Join(" ", args))).ToArray();
            }

            var updateCommand = new Command("update", "Updates launcher and SixModLoader")
            {
                Handler = CommandHandler.Create(UpdateAsync)
            };

            var launchCommand = new Command("launch", "Runs update command and launches game")
            {
                new Argument<string>("args", () =>
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        return "LocalAdmin";
                    }

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        return "LocalAdmin.exe";
                    }

                    throw new InvalidOperationException("Unsupported OS");
                })
            };

            launchCommand.Handler = CommandHandler.Create<string>(args => LaunchAsync(CommandLineStringSplitter.Instance.Split(args).ToArray()));

            var rootCommand = new RootCommand
            {
                updateCommand,
                launchCommand
            };

            return await rootCommand.InvokeAsync(args);
        }

        public static async Task UpdateAsync()
        {
            await new AutoUpdate().UpdateAsync();
            await Doorstop.DownloadAsync();
        }

        public static async Task LaunchAsync(string[] args)
        {
            await UpdateAsync();

            Console.WriteLine();

            Console.WriteLine($"Starting \"{string.Join(" ", args).Trim()}\"");

            Doorstop.PreLaunch();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = args.First(),
                    Arguments = string.Join(" ", args.Skip(1)),
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