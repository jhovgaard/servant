using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace Servant.Updater
{
    class Program
    {
        static StringBuilder _log = new StringBuilder();

        static void Main(string[] args)
        {
            args = new[] {@"C:\Code\servant\src\Servant.Server\bin\Release\Servant.Server.exe"};
            string servantPath = Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), "Servant.Server.exe");
            Console.WriteLine("Path: " + servantPath);
            if(args.Length > 0)
                servantPath = args[0];

            var webClient = new WebClient();
            var servantDirectory = Path.GetDirectoryName(servantPath);
            
            var downloadLocation = Path.Combine(servantDirectory, "servant-1.1.zip");

            var versionInfo = FileVersionInfo.GetVersionInfo(servantPath);
            var currentVersion = float.Parse(versionInfo.ProductVersion.Replace(".", ""));
            WriteLogEntry("Current version is " + currentVersion);

            var releases = GetReleases();

            var latestVersion = float.Parse(releases.First().tag_name.Replace(".", "").PadRight(4, '0'));
            WriteLogEntry("Most recent version is " + latestVersion);

            if (latestVersion > currentVersion)
            {
                var servantZipUrl = releases.First().assets.First().browser_download_url;
                WriteLogEntry("Downloading " + servantZipUrl + "...");
                webClient.DownloadFile(servantZipUrl, downloadLocation);
                var fastZip = new FastZip();

                WriteLogEntry("Uninstalling current version");
                var uninstalled = Uninstall(servantPath);
                
                if (!uninstalled)
                    WriteLogEntry("Unable to uninstall Servant. It may be caused by corrupt config.json.", true);

                // Backup af nuværende installation
                var backupDirectory = Path.Combine(servantDirectory, "_temp");
                DirectoryCopy(servantDirectory, backupDirectory, true);

                try
                {
                    WriteLogEntry("Extracting new version");
                    fastZip.ExtractZip(downloadLocation, servantDirectory, FastZip.Overwrite.Always, null, null, null,
                        false);
                    var installed = Install(servantPath);
                    if (!installed)
                    {
                        WriteLogEntry("Unable to install new version of Servant. Rolling back...", true);
                    }

                    WriteLogEntry(string.Format("Successfully updated Servant from version {0} to version {1}.", currentVersion, latestVersion), true);
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry("Servant for IIS", "Failed updating Servant: " + System.Environment.NewLine + ex.Message + System.Environment.NewLine + ex.StackTrace, EventLogEntryType.Error);
                    Uninstall(servantPath);
                    CleanupServantDirectory(servantDirectory);
                    DirectoryCopy(backupDirectory, servantPath, true);
                    Install(servantPath);
                    
                    throw;
                }
                finally
                {
                    System.IO.Directory.Delete(backupDirectory, true);
                }
            }
        }

        private static void CleanupServantDirectory(string path)
        {
            string[] filePaths = Directory.GetFiles(path);
            
            foreach (var filePath in filePaths)
            {
                if(System.IO.Path.GetFileName(filePath) != "Servant.Updater.exe")
                    File.Delete(filePath);
            }

            var dir = new DirectoryInfo(path);
            var dirs = dir.GetDirectories();

            foreach (var subdir in dirs)
            {
                if (subdir.Name != "_temp")
                {
                    subdir.Delete(true);
                }
            }
        }

        /// Source: http://msdn.microsoft.com/en-us/library/bb762914(v=vs.110).aspx
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private static void WriteLogEntry(string value, bool addToEventLog = false)
        {
            Console.WriteLine(value);
            _log.Append(value);

            if (addToEventLog)
                EventLog.WriteEntry("Servant for IIS", value, EventLogEntryType.Information);
        }

        private static List<GithubReleaseResult> GetReleases()
        {
            string url = "https://api.github.com/repos/jhovgaard/servant/releases";
            var request = (HttpWebRequest)WebRequest.Create(url);

            request.UserAgent = "Servant for IIS Client";
            request.KeepAlive = false;

            var response = (HttpWebResponse)request.GetResponse();
            var responseStream =new StreamReader(response.GetResponseStream());
            string result = responseStream.ReadToEnd();
            response.Close();
            responseStream.Close();

            return Newtonsoft.Json.JsonConvert.DeserializeObject<List<GithubReleaseResult>>(result);
        } 

        static bool Uninstall(string path)
        {
            var uninstall = new Process() { StartInfo = new ProcessStartInfo(path, "uninstall") { CreateNoWindow = false, UseShellExecute = false, RedirectStandardOutput = true } };
            if (Environment.OSVersion.Version.Major >= 6)
            {
                uninstall.StartInfo.Verb = "runas";
            }
            uninstall.Start();
            uninstall.WaitForExit();
            return uninstall.ExitCode == 0;
        }

        static bool Install(string path)
        {
            var install = new Process() { StartInfo = new ProcessStartInfo(path, "install") { CreateNoWindow = false, UseShellExecute = false, RedirectStandardOutput = true } };
            if (Environment.OSVersion.Version.Major >= 6)
            {
                install.StartInfo.Verb = "runas";
            }
            install.Start();
            install.WaitForExit();
            return install.ExitCode == 0;
        }
    }
}
