using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Mono.Unix;
using NuGet.Versioning;
using Octokit;
using SixModLoader.Launcher.EasyMetadata;

namespace SixModLoader.Launcher
{
    public class AutoUpdate
    {
        public GitHubClient GitHubClient { get; } = new GitHubClient(new ProductHeaderValue("SixModLoader.Launcher", Program.Version.ToString()));

        public async Task UpdateAsync()
        {
            var rateLimit = await GitHubClient.Miscellaneous.GetRateLimits();
            if (rateLimit.Resources.Core.Remaining <= 0)
            {
                Console.WriteLine($"GitHub API rate limit reached, skipping auto update. (try again in {rateLimit.Resources.Core.Reset})");
                return;
            }

            await UpdateLauncherAsync();
            Console.WriteLine();
            await UpdateSixModLoaderAsync();
        }

        public async Task UpdateLauncherAsync()
        {
            var assemblyLocation = Process.GetCurrentProcess()!.MainModule!.FileName;
            if (Path.GetFileName(assemblyLocation) == "dotnet.exe")
            {
                Console.WriteLine("Update skipped (dev environment?)");
                return;
            }
            if (Path.GetFileName(assemblyLocation) == "mono-sgen")
            {
                assemblyLocation = Assembly.GetExecutingAssembly().Location;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                foreach (var file in Directory.GetFiles(Path.GetDirectoryName(assemblyLocation)).Where(x => x.EndsWith(".delete")))
                {
                    Console.WriteLine("Deleting " + file);
                    File.Delete(file);
                }
            }

            var version = Program.Version;

            var releases = await GitHubClient.Repository.Release.GetAll("SixModLoader", "SixModLoader.Launcher");

            var newerRelease = releases
                .Where(x => version.IsPrerelease || !x.Prerelease)
                .Select(x => (Release: x, Version: SemanticVersion.Parse(x.TagName)))
                .FirstOrDefault(x => x.Version.CompareTo(version) > 0);

            if (newerRelease == default)
            {
                Console.WriteLine("You are on newest launcher version :)");
            }
            else
            {
                Console.WriteLine("Newest launcher version: " + newerRelease.Version);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using var stream = await Program.HttpClient.GetStreamAsync(
                        newerRelease.Release.Assets
#if NET472
                            .Single(x => x.Name == "net472.zip")
#elif NETCOREAPP
                            .Single(x => x.Name == $"netcore-{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" : "linux")}-x64.zip")
#endif
                            .BrowserDownloadUrl
                    );

                    var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);

#if NET472
                    var entry = zipArchive.GetEntry("SixModLoader.Launcher.exe");
#elif NETCOREAPP
                    var entry = zipArchive.GetEntry(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "SixModLoader.Launcher.exe" : "SixModLoader.Launcher");
#endif

                    Console.WriteLine("Updating " + assemblyLocation);

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        File.Move(assemblyLocation, assemblyLocation + ".delete");
                    }

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && File.Exists(assemblyLocation))
                    {
                        File.Delete(assemblyLocation);
                    }

                    entry.ExtractToFile(assemblyLocation, true);

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        new UnixFileInfo(assemblyLocation).FileAccessPermissions
                            |= FileAccessPermissions.OtherExecute | FileAccessPermissions.GroupExecute | FileAccessPermissions.UserExecute;
                    }

                    Console.WriteLine("Updated launcher, please restart");
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine("Your OS doesn't support auto update, please update manually.");
                }
            }
        }

        public async Task UpdateSixModLoaderAsync()
        {
            SemanticVersion version = null;

            if (File.Exists(Program.SixModLoaderPath))
            {
                var assemblyInfo = new AssemblyInfo(Program.SixModLoaderPath);
                version = SemanticVersion.Parse(assemblyInfo.Version);

                Console.WriteLine("SixModLoader " + version);
            }

            var releases = await GitHubClient.Repository.Release.GetAll("SixModLoader", "SixModLoader");

            var newerRelease = releases
                .Where(x => version == null || x.Prerelease == version?.IsPrerelease)
                .Select(x => (Release: x, Version: SemanticVersion.Parse(x.TagName)))
                .FirstOrDefault(x => x.Version.CompareTo(version) > 0);

            if (newerRelease == default)
            {
                Console.WriteLine("You are on newest SixModLoader version :)");
            }
            else
            {
                Console.WriteLine("Updating SixModLoader to version: " + newerRelease.Version);

                using var stream = await Program.HttpClient.GetStreamAsync(
                    newerRelease.Release.Assets
                        .Single(x => x.Name == "SixModLoader.zip").BrowserDownloadUrl
                );

                var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);

                foreach (var entry in zipArchive.Entries)
                {
                    var fullPath = Path.Combine("SixModLoader", entry.FullName);

                    if (string.IsNullOrEmpty(Path.GetFileName(fullPath)))
                    {
                        Directory.CreateDirectory(fullPath);
                    }
                    else
                    {
                        entry.ExtractToFile(fullPath, true);
                    }
                }
            }
        }
    }
}