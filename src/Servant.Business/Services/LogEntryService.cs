using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using DapperExtensions;
using Servant.Business.Objects;

namespace Servant.Business.Services
{
    public class LogEntryService : SqlLiteService<LogEntry>
    {
        public LogEntry GetById(int id)
        {
            return Connection.Query<LogEntry>("SELECT * FROM LogEntries WHERE Id = @Id", new {id}).SingleOrDefault();
        }
        
        public LogEntry GetLatestEntry(Site site)
        {
            return Connection.Query<LogEntry>(
                    "SELECT * FROM LogEntries WHERE SiteIisId = @SiteIisId ORDER BY DateTime DESC LIMIT 1",
                    new {SiteIisId = site.IisId}).FirstOrDefault();
        }

        public IEnumerable<LogEntry> GetLatestBySite(Site site, int max = 0)
        {
            var sql = "SELECT * FROM LogEntries WHERE SiteIisId = @SiteIisId ORDER BY DateTime DESC";
            if(max != 0)
                sql = sql + " LIMIT " + max;

            return Connection.Query<LogEntry>(sql, new { SiteIisId = site.IisId });
        }

        public IEnumerable<Objects.Reporting.MostActiveClient> GetMostActiveClientsBySite(int iisSiteId, DateTime oldest)
        {
            var result = Connection.Query<Objects.Reporting.MostActiveClient>(
                "SELECT ClientIpAddress, ifnull(Agentstring, '[Empty]') as Agentstring, Count(ClientIpAddress) AS Count FROM LogEntries WHERE SiteIisId = @siteIisId AND DateTime >= @oldestDate GROUP BY  ClientIpAddress, Agentstring Order by Count(ClientIpAddress) DESC LIMIT 5",
                new { SiteIisId = iisSiteId, OldestDate = oldest }).ToList();

            return result;
        }

        public IEnumerable<Objects.Reporting.MostExpensiveRequest> GetMostExpensiveRequestsBySite(int iisSiteId, DateTime oldest)
        {
            var sql = "SELECT Uri, Querystring, TimeTaken, Avg(TimeTaken) AS AverageTimeTaken, Count(Uri) AS Count FROM LogEntries WHERE SiteIisId = @SiteIisId AND DateTime >= @oldestDate GROUP BY Uri, Querystring HAVING Count > 10 Order by AverageTimeTaken DESC LIMIT 5";
            return Connection.Query<Objects.Reporting.MostExpensiveRequest>(sql, new { SiteIisId = iisSiteId, OldestDate = oldest });
        }

        public IEnumerable<Objects.Reporting.MostActiveUrl> GetMostActiveUrlsBySite(int iisSiteId, DateTime oldest)
        {
            var sql = "SELECT Uri, Querystring, Count(Uri) AS Count FROM LogEntries WHERE SiteIisId = @SiteIisId AND DateTime >= @oldestDate GROUP BY Uri, Querystring Order by Count DESC LIMIT 5";
            return Connection.Query<Objects.Reporting.MostActiveUrl>(sql, new { SiteIisId = iisSiteId, OldestDate = oldest });
        }

        public IEnumerable<LogEntry> GetAllRelatedToException(int siteIisId, DateTime datetime)
        {
            var sql = "SELECT * FROM LogEntries WHERE SiteIisId = @SiteIisId AND HttpStatusCode = @HttpStatusCode AND DateTime == @DateTime;";
            return Connection.Query<LogEntry>(sql, new { SiteIisId = siteIisId, HttpStatusCode = 500, DateTime = datetime });
        }

        public IEnumerable<LogEntry> GetBySite(Site site)
        {
            var sql = "SELECT * FROM LogEntries WHERE SiteIisId = @SiteIisId";
            return Connection.Query<LogEntry>(sql, new {SiteIisId = site.IisId});
        }

        public int GetTotalCount(DateTime? oldest = null)
        {
            var sql = "SELECT COUNT(*) FROM LogEntries";

            if (oldest != null)
                sql = sql + " WHERE DateTime >= @oldest";

            return (int) Connection.Query<long>(sql, oldest != null ? new {Oldest = oldest.Value }: null).Single();
        }

        public double GetAverageResponseTime(DateTime? oldest = null)
        {
            var sql = "SELECT avg(TimeTaken) FROM LogEntries";

            if (oldest != null)
                sql = sql + " WHERE DateTime >= @oldest";

            return Connection.Query<double>(sql, oldest != null ? new { Oldest = oldest.Value } : null).Single();
        }
    }
}