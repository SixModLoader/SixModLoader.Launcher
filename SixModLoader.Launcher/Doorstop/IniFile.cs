using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SixModLoader.Launcher.Doorstop
{
    public class IniFile
    {
        public string FilePath { get; }

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string @default, StringBuilder retVal, int size, string filePath);

        public IniFile(string filePath)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException("Ini file is supported only on Windows");
            }

            FilePath = Path.GetFullPath(filePath);
        }

        public string Read(string key, string section = null)
        {
            var retVal = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", retVal, 255, FilePath);
            return retVal.ToString();
        }

        public void Write(string key, string value, string section = null)
        {
            WritePrivateProfileString(section, key, value, FilePath);
        }

        public void DeleteKey(string key, string section = null)
        {
            Write(key, null, section);
        }

        public void DeleteSection(string section = null)
        {
            Write(null, null, section);
        }

        public bool KeyExists(string key, string section = null)
        {
            return Read(key, section).Length > 0;
        }
    }
}