using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;

namespace SixModLoader.Launcher
{
    public class AutoUpdate
    {
        public async Task UpdateAsync()
        {
            await UpdateLauncherAsync();
            Console.WriteLine();
            await UpdateSixModLoaderAsync();
        }

        public async Task UpdateLauncherAsync()
        {
            var version = Program.Version;
            var releases = JArray.Parse(await Program.HttpClient.GetStringAsync("https://api.github.com/repos/SixModLoader/SixModLoader.Launcher/releases"));

            var newerRelease = releases
                .Where(x => x.Value<bool>("prerelease") == version?.IsPrerelease)
                .Select(x => (release: x, version: SemanticVersion.Parse(x.Value<string>("tag_name"))))
                .FirstOrDefault(x => x.version.CompareTo(version) > 0);

            if (newerRelease == default)
            {
                Console.WriteLine("You are on newest launcher version :)");
            }
            else
            {
                Console.WriteLine("Newest launcher version: " + newerRelease.version);
                Console.WriteLine("Please update manually (auto update coming soon)");
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

            var releases = JArray.Parse(await Program.HttpClient.GetStringAsync("https://api.github.com/repos/SixModLoader/SixModLoader/releases"));

            var newerRelease = releases
                .Where(x => version == null || x.Value<bool>("prerelease") == version?.IsPrerelease)
                .Select(x => (release: x, version: SemanticVersion.Parse(x.Value<string>("tag_name"))))
                .FirstOrDefault(x => x.version.CompareTo(version) > 0);

            if (newerRelease == default)
            {
                Console.WriteLine("You are on newest SixModLoader version :)");
            }
            else
            {
                Console.WriteLine("Updating SixModLoader to version: " + newerRelease.version);

                using var stream = await Program.HttpClient.GetStreamAsync(newerRelease.release
                    .Value<JArray>("assets")
                    .Single(x => x.Value<string>("name") == "SixModLoader.zip")
                    .Value<string>("browser_download_url")
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