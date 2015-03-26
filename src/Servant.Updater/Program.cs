using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using Servant.Shared;

namespace Servant.Updater
{
    public class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG
            args = new[] { Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Servant.Agent.exe") };
#endif
            
            string servantFile = null;

            if(args.Length > 0)
                servantFile = args[0];

            if (servantFile == null)
            {
                return;
            }

            var webClient = new WebClient();
            var servantDirectory = Path.GetDirectoryName(servantFile);

            if (servantDirectory == null)
            {
                return;
            }

            var release = GetLatestRelease(servantFile);

            if (release == null)
            {
                return;
            }

            var versionInfo = FileVersionInfo.GetVersionInfo(servantFile);
            var currentVersion = float.Parse(versionInfo.ProductVersion.Replace(".", ""));
            var latestVersion = float.Parse(release.Version.Replace(".", "").PadRight(4, '0'));

            WriteLogEntry("Most recent version is " + latestVersion);

            if (latestVersion > currentVersion)
            {
                var downloadFileName = Path.Combine(servantDirectory, string.Format("update-{0}.msi", latestVersion));
                WriteLogEntry("There's a new a version of Servant available. Downloading...");
                var couldDownload = false;

                try
                {
                    webClient.DownloadFile(release.Url, downloadFileName);
                    couldDownload = true;
                }
                catch (Exception e)
                {
                    WriteLogEntry("Could not download Servant agent update: " + e.Message, addToEventLog: true);
                }

                if (couldDownload)
                {
                    WriteLogEntry("File downloaded. Beginning update process...");

                    var p = new Process
                            {
                                StartInfo = { FileName = "msiexec", Arguments = string.Format("/i \"{0}\" /quiet /qn /norestart /log \"msi_update_log.txt\"", downloadFileName) }
                            };

                    p.Start();
                    p.WaitForExit();

                    if (p.ExitCode == 0)
                    {
                        WriteLogEntry("Update succeeded.");

                        try
                        {
                            File.Delete(downloadFileName);
                        }
                        catch { }
                    }
                    else
                    {
                        WriteLogEntry("Fatal error during update. Exit code: " + p.ExitCode + ". See msi-install-log.txt for more info.", addToEventLog: true);
                    }
                }
            }
            else
            {
                WriteLogEntry("No new update ready for you.");
            }
        }

        private static void WriteLogEntry(string value, bool addToEventLog = false)
        {
            Console.WriteLine(value);

            if (addToEventLog)
                EventLog.WriteEntry("Servant Agent Updater", value, EventLogEntryType.Error);
        }

        private static ReleaseResult GetLatestRelease(string servantFile)
        {
            var prelease = System.IO.File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(servantFile), "prerelease"));
#if DEBUG
            return new ReleaseResult { Url = "https://dl.dropboxusercontent.com/u/969563/Servant.Agent.1.1.0.0.msi", Version = "1.1.0" };
#endif

            const string url = "http://www.servant.io/version.json";
            var request = (HttpWebRequest)WebRequest.Create(url);

            request.UserAgent = "Servant Agent Updater";
            request.KeepAlive = false;

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    var stream = response.GetResponseStream();

                    if (stream == null)
                    {
                        throw new NullReferenceException("Stream was null.");
                    }

                    using (var responseStream = new StreamReader(stream))
                    {
                        var result = responseStream.ReadToEnd();
                        response.Close();
                        responseStream.Close();
                        return Json.DeserializeFromString<ReleaseResult>(result);
                    }
                }
            }
            catch (Exception e)
            {
                WriteLogEntry("Unable to check for updates: " + e.Message, addToEventLog: true);
            }

            return null;
        } 
    }
}
