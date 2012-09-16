using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Servant.Business.Objects;
using Servant.Business.Services;
using MSUtil;

namespace Servant.Web.Helpers
{
    public static class RequestLogHelper
    {
        private static Microsoft.Web.Administration.ServerManager _manager = new Microsoft.Web.Administration.ServerManager();
        private static readonly LogEntryService LogEntryService = new LogEntryService();

        public static void SyncDatabaseWithServer()
        {
            var sites = SiteHelper.GetSites();

            foreach (var site in sites)
            {
                var latestEntry = LogEntryService.GetLatestEntry(site);
                var entriesToAdd = GetNewBySite(site, latestEntry);
                LogEntryService.Insert(entriesToAdd);
            }
        }

        public static IEnumerable<LogEntry> GetNewBySite(Site site, LogEntry latestEntry)
        {
            FlushLog();
            System.Threading.Thread.Sleep(100); // Venter på at IIS har skrevet loggen

            var logfiles = GetLogFilesBySite(site);
            if (latestEntry != null)
                logfiles = logfiles.Where(x => x.LastWriteTime.Date >= latestEntry.DateTime.Date);

            var logEntries = new List<LogEntry>();

            var earliestTime = DateTime.UtcNow.ToShortTimeString() + ":00";
            foreach (var logfile in logfiles)
            {
                var sql = @"SELECT TO_TIMESTAMP(date, time) as date-time, s-ip, cs-method, cs-uri-stem, cs-uri-query, s-port, cs-username, c-ip, cs(User-Agent), sc-status, sc-substatus, time-taken FROM " + logfile.Path;
                if (latestEntry != null && latestEntry.DateTime.Date == logfile.LastWriteTime.Date)
                    sql += " WHERE time > '" + latestEntry.DateTime.ToLongTimeString() + "' and time < '" + earliestTime + "'";

                logEntries.AddRange(ParseQueryToLogEntries(site.IisId, sql));
            }

            return logEntries;
        }

        private static void FlushLog()
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

        private static DateTime GetLogFileDate(string path)
        {            
            // run the query against W3C log
            var parser = new LogQueryClass();
            var rows = parser.Execute("SELECT TOP 1 date FROM " + path);
            var firstRow = rows.getRecord();
            return firstRow.getValue("date");
        }

        private static IEnumerable<IisLogFile> GetLogFilesBySite(Site site)
        {
            var systemDrive = Path.GetPathRoot(Environment.SystemDirectory).Substring(0, 2);
            var pathToLogs = Path.Combine(site.LogFileDirectory.Replace("%SystemDrive%", systemDrive), "W3SVC" + site.IisId);
            var files = Directory.Exists(pathToLogs) ? Directory.GetFiles(pathToLogs) : new string[] {};

            foreach (var file in files)
            {
                yield return new IisLogFile()
                {
                    Path = file,
                    LastWriteTime = GetLogFileDate(file)
                };
            }
        }

        private static dynamic GetDbNullSafe(dynamic value)
        {
            if (value.ToString() == string.Empty)
                return string.Empty;
            return value;
        }

        private static IEnumerable<LogEntry> ParseQueryToLogEntries(int iisSiteId, string sqlQuery)
        {
            var parser = new LogQueryClass();
            var rows = parser.Execute(sqlQuery);

            while (!rows.atEnd())
            {
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
                    TimeTaken = row.getValue("time-taken")
                };
                yield return entry;

                rows.moveNext();
            }
        }

        public static IEnumerable<LogEntry> GetByRelatedException(ApplicationError exception)
        {
            return LogEntryService.GetAllRelatedToException(exception.SiteIisId, exception.DateTime);
        }
    }
}