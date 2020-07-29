using System.Threading.Tasks;

namespace SixModLoader.Launcher.Doorstop
{
    public interface IDoorstop
    {
        public Task DownloadAsync();
        public void PreLaunch();
    }
}