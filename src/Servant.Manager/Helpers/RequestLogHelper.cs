using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Servant.Business.Objects;
using Servant.Business.Services;
using Servant.Manager.Infrastructure;

namespace Servant.Manager.Helpers
{
    public static class RequestLogHelper
    {
        private static readonly LogEntryService LogEntryService = new LogEntryService();

        public static void InsertNewInDbBySite(Site site, LogEntry latestEntry)
        {
            var host = TinyIoC.TinyIoCContainer.Current.Resolve<IHost>();

            var logfiles = GetLogFilesBySite(site).ToList();
            if (latestEntry != null)
            {
                var alreadyParsedLogFiles = LogEntryService.GetParsedLogfilesBySite(site.IisId).Where(x => x != latestEntry.LogFilename);
                logfiles = logfiles.Where(x => !alreadyParsedLogFiles.Contains(Path.GetFileName(x.Path))).ToList();
            }

            logfiles = logfiles.OrderByDescending(x => x.Path).ToList();

            if (!logfiles.Any())
                return;

            if (!host.LogParsingStarted) // Sørger for at vi kan afbryde parsing udefra.
                return;

            var service = new LogEntryService();

            foreach (var iisLogFile in logfiles)
            {
                if (!host.LogParsingStarted) // Sørger for at vi kan afbryde parsing udefra.
                    break;

                var logRowToSkip = 0;
                if (latestEntry != null && latestEntry.DateTime.Date == iisLogFile.LastModified.Date)
                    logRowToSkip = latestEntry.LogRow;

                var entries = Business.LogParser.ParseFile(iisLogFile.Path, site.IisId, logRowToSkip).ToList();
                service.Insert(entries);

                if (host.Debug)
                    Console.WriteLine("{0}: Wrote {1} entries to db. () {2}", site.Name, entries.Count, System.IO.Path.GetFileName(iisLogFile.Path));
            }
        }

        public static void FlushLog()
        {
            var process = new Process {
                StartInfo = new ProcessStartInfo("netsh") 
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    Arguments = "http flush logbuffer"
                }
            };
            
            process.Start();
            process.Dispose();
        }

        public static List<IisLogFile> GetLogFilesForAllSites()
        {
            var logfiles = new List<IisLogFile>();
            var siteManager = new SiteManager();

            var sites = siteManager.GetSites();
            foreach(var site in sites)
            {
                logfiles.AddRange(GetLogFilesBySite(site));
            }
            return logfiles;
        }

        private static IEnumerable<IisLogFile> GetLogFilesBySite(Site site)
        {
            var systemDrive = Path.GetPathRoot(Environment.SystemDirectory).Substring(0, 2);
            var pathToLogs = Path.Combine(site.LogFileDirectory.Replace("%SystemDrive%", systemDrive), "W3SVC" + site.IisId);
            var files = Directory.Exists(pathToLogs) ? Directory.GetFiles(pathToLogs, "*.log").Distinct() : new string[] {};

            foreach (var file in files)
            {
                yield return GetIisLogFileByPath(file);
            }
        }

        private static IisLogFile GetIisLogFileByPath(string path)
        {
            var fileInfo = new FileInfo(path);
            var lines = 0;//File.ReadLines(path).Count(x => !x.StartsWith("#"));
            
            return new IisLogFile {CreationDate = fileInfo.CreationTime.ToUniversalTime(), LastModified = fileInfo.LastWriteTimeUtc, Path = path, TotalRequests = lines};
        }

        private static dynamic GetDbNullSafe(dynamic value)
        {
            if (value.ToString() == string.Empty)
                return string.Empty;
            return value;
        }

        public static IEnumerable<LogEntry> GetByRelatedException(ApplicationError exception)
        {
            return LogEntryService.GetAllRelatedToException(exception.SiteIisId, exception.DateTime);
        }
    }
}