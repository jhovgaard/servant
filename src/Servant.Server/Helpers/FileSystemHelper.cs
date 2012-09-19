namespace Servant.Server.Helpers
{
    public static class FileSystemHelper
    {
        public static bool DirectoryExists(string path)
        {
            return System.IO.Directory.Exists(path);
        }
    }
}