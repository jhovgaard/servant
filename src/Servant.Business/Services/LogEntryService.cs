using System;
using System.Collections.Generic;
using Servant.Business.Objects;

namespace Servant.Business.Services
{
    public class LogEntryService : Service<LogEntry>
    {
        public LogEntryService() : base("LogEntries") {}

        public LogEntry GetLatestEntry(Site site) {
            return Table.FindAllBySiteIisId(site.IisId).OrderByDateTimeDescending().FirstOrDefault();
        }

        public IEnumerable<LogEntry> GetLatestBySite(Site site, int max = 0)
        {
            var entries = Table.FindAllBySiteIisId(site.IisId).OrderbyDateTimeDescending();
            if (max != 0)
                entries = entries.Take(max);

            return entries;
        }

        public IEnumerable<LogEntry> GetTodaysBySite(Site site)
        {
            return Table.FindAll(Table.SiteIisId == site.IisId && Table.DateTime >= DateTime.UtcNow.Date).OrderbyDateTimeDescending().ToList<LogEntry>();
        }

        public IEnumerable<LogEntry> GetLastWeekBySite(Site site)
        {
            return Table.FindAll(Table.SiteIisId == site.IisId && Table.DateTime >= DateTime.UtcNow.Date.AddDays(-7)).OrderbyDateTimeDescending().ToList<LogEntry>();
        }

        public IEnumerable<LogEntry> GetLastMonthBySite(Site site)
        {
            return Table.FindAll(Table.SiteIisId == site.IisId && Table.DateTime >= DateTime.UtcNow.Date.AddMonths(-1)).OrderbyDateTimeDescending().ToList<LogEntry>();
        }

        public IEnumerable<LogEntry> GetAllRelatedToException(int siteIisId, DateTime datetime)
        {
            return Table
                .FindAll(Table.SiteIisId == siteIisId && Table.HttpStatusCode == 500 && Table.DateTime == datetime.ToString("yyyy-MM-dd HH:mm:ss")) // string format because of bug in Simple.Data.Sqlite adapter
                .ToList<LogEntry>(); 
        }

        public IEnumerable<LogEntry> GetBySite(Site site)
        {
            return Table.FindAllBySiteIisId(site.IisId).Cast<LogEntry>();
        }

        public int GetTotalCount()
        {
            return Table.All().Count();
        }

        public double GetAverageResponseTime()
        {
            var result = Table.All().Select(Table.TimeTaken.Average()).ToScalar();
            if (result == null)
                return 0;

            return result;
        }
    }
}