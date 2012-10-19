using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MSUtil;
using Servant.Business.Objects;
using Servant.Business.Services;
using Servant.Manager.Infrastructure;

namespace Servant.Manager.Helpers
{
    public static class RequestLogHelper
    {
        private static readonly LogEntryService LogEntryService = new LogEntryService();
        private static List<LogEntry>[] ParsedLogEntryLists { get; set; }
        private static ManualResetEvent[] resetEvents;
        private const int FilesPerRound = 6;
        private static int _filesInRound = 0;

        public static void InsertNewInDbBySite(Site site, LogEntry latestEntry)
        {
            var host = TinyIoC.TinyIoCContainer.Current.Resolve<IHost>();

            var logfiles = GetLogFilesBySite(site).OrderByDescending(x => x.Path).ToList();
            if (latestEntry != null)
                logfiles = logfiles.Where(x => x.LastWriteTime.Date >= latestEntry.DateTime.Date).ToList();

            if (!logfiles.Any())
                return;

            ThreadPool.SetMaxThreads(1, FilesPerRound);

            var rounds = logfiles.Count / FilesPerRound;
            for (var round = 0; round < rounds; round++) // Hver runde udgør 4 logfiler
            {
                if (!host.LogParsingStarted) // Sørger for at vi kan afbryde parsing udefra.
                    return;

                var logfilesForRound = logfiles.Skip((round * FilesPerRound)).Take(FilesPerRound).ToList();
                _filesInRound = logfilesForRound.Count;
                ParsedLogEntryLists = new List<LogEntry>[logfilesForRound.Count];
                resetEvents = new ManualResetEvent[_filesInRound];

                for (int i = 0; i < logfilesForRound.Count; i++) // max 1-4
                {
                    var logfile = logfilesForRound[i];
                    resetEvents[i] = new ManualResetEvent(false);
                    ThreadPool.QueueUserWorkItem(CallBack, new { Index = i, Logfile = logfile, LatestEntry = latestEntry, Site = site, Debug = host.Debug });
                }

                WaitHandle.WaitAll(resetEvents);

                var entries = new List<LogEntry>();
                foreach (var list in ParsedLogEntryLists)
                {
                    entries.AddRange(list);
                }
                var service = new LogEntryService();
                service.Insert(entries);
                if(host.Debug)
                    Console.WriteLine("Round {0}: Wrote {1} entries to db.", round, entries.Count);

                Thread.Sleep(500);
            }
        }

        private static void CallBack(dynamic state)
        {
            var logfile = state.Logfile;
            var latestEntry = state.LatestEntry;
            var site = state.Site;
            var sw = new Stopwatch();
            sw.Start();
            var sql = @"SELECT LogRow, TO_TIMESTAMP(date, time) as date-time, s-ip, cs-method, cs-uri-stem, cs-uri-query, s-port, cs-username, c-ip, cs(User-Agent), sc-status, sc-substatus, time-taken FROM " + logfile.Path;
            if (latestEntry != null && latestEntry.DateTime.Date == logfile.LastWriteTime.Date)
                sql += " WHERE time > '" + latestEntry.DateTime.ToLongTimeString() + "' and logrow > " + latestEntry.LogRow;
            var index = (int)state.Index;
            List<LogEntry> result = ParseQueryToLogEntries(site.IisId, sql);
            ParsedLogEntryLists[index] = result;
            sw.Stop();
            if(state.Debug)
                Console.WriteLine(logfile.Path + ": parsed in " + sw.ElapsedMilliseconds + "ms");
            
            resetEvents[index].Set();
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
            
            return new IisLogFile {LastWriteTime = fileInfo.LastWriteTime, Path = path, TotalRequests = lines};
        }

        private static dynamic GetDbNullSafe(dynamic value)
        {
            if (value.ToString() == string.Empty)
                return string.Empty;
            return value;
        }

        private static List<LogEntry> ParseQueryToLogEntries(int iisSiteId, string sqlQuery)
        {
            var parser = new LogQueryClass();
            var rows = parser.Execute(sqlQuery);

            var sw = new Stopwatch();

            var result = new List<LogEntry>();
            while (!rows.atEnd())
            {
                sw.Start();
                var row = rows.getRecord();
                var entry = new LogEntry
                {
                    SiteIisId = iisSiteId,
                    DateTime = row.getValue("date-time"),
                    ServerIpAddress = row.getValue("s-ip"),
                    HttpMethod = row.getValue("cs-method"),
                    Uri = row.getValue("cs-uri-stem"),
                    Querystring = GetDbNullSafe(row.getValue("cs-uri-query")),
                    Port = row.getValue("s-port"),
                    Username = GetDbNullSafe(row.getValue("cs-username")),
                    ClientIpAddress = row.getValue("c-ip"),
                    Agentstring = GetDbNullSafe(row.getValue("cs(User-Agent)")),
                    HttpStatusCode = row.getValue("sc-status"),
                    HttpSubStatusCode = row.getValue("sc-substatus"),
                    TimeTaken = row.getValue("time-taken"),
                    LogRow = row.getValue("logrow")
                };
                sw.Stop();
                result.Add(entry);
                rows.moveNext();
            }
            rows.close();
            return result;
        }

        public static IEnumerable<LogEntry> GetByRelatedException(ApplicationError exception)
        {
            return LogEntryService.GetAllRelatedToException(exception.SiteIisId, exception.DateTime);
        }
    }
}