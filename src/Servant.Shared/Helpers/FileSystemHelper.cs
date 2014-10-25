using System;
using System.IO;
using System.Linq;

namespace Servant.Shared.Helpers
{
    public static class FileSystemHelper
    {
        public static bool DirectoryExists(string path)
        {
            path = path.Replace("%SystemDrive%", Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 2)));
            return System.IO.Directory.Exists(path);
        }

        public static void CreateDirectory(string diskPath)
        {
            System.IO.Directory.CreateDirectory(diskPath);
        }

        public static bool IsPathValid(string path)
        {
            var invalidCharacters = new[] { '?', ';', '@', '&', '=', '+', '$', ',', '|', '"', '<', '>', '*' };
            if(invalidCharacters.Any(x => path.ToArray().Contains(x)))
                return false;

            return true;
        }
    }
}