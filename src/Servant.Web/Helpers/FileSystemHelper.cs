using System;
using System.IO;

namespace Servant.Web.Helpers
{
    public static class FileSystemHelper
    {
        public static bool DirectoryExists(string path)
        {
            path = path.Replace("%SystemDrive%", Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 2)));
            return System.IO.Directory.Exists(path);
        }
    }
}