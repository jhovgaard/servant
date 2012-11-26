using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using DapperExtensions;
using Servant.Business.Extensions;
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
                    "SELECT * FROM LogEntries WHERE SiteIisId = @SiteIisId ORDER BY DateTime DESC, LogRow DESC LIMIT 1",
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
                "SELECT CAST(Count(ClientIpAddress) AS INTEGER) AS Count, ifnull(Agentstring, '[Empty]') as Agentstring, ClientIpAddress FROM LogEntries WHERE SiteIisId = @siteIisId AND DateTime >= @oldestDate GROUP BY  ClientIpAddress, Agentstring Order by Count(ClientIpAddress) DESC LIMIT 5",
                new { SiteIisId = iisSiteId, OldestDate = oldest.ToSqlLiteDateTime() }).ToList();

            return result;
        }

        public IEnumerable<Objects.Reporting.MostExpensiveRequest> GetMostExpensiveRequestsBySite(int iisSiteId, DateTime oldest)
        {
            var sql = "SELECT Uri, Querystring, TimeTaken, Avg(TimeTaken) AS AverageTimeTaken, Count(Uri) AS Count FROM LogEntries WHERE SiteIisId = @SiteIisId AND DateTime >= @oldestDate GROUP BY Uri, Querystring Order by AverageTimeTaken DESC LIMIT 5";
            return Connection.Query<Objects.Reporting.MostExpensiveRequest>(sql, new { SiteIisId = iisSiteId, OldestDate = oldest.ToSqlLiteDateTime() });
        }

        public IEnumerable<Objects.Reporting.MostActiveUrl> GetMostActiveUrlsBySite(int iisSiteId, DateTime oldest)
        {
            var sql = "SELECT Uri, Querystring, Count(Uri) AS Count FROM LogEntries WHERE SiteIisId = @SiteIisId AND DateTime >= @oldestDate GROUP BY Uri, Querystring Order by Count DESC LIMIT 5";
            return Connection.Query<Objects.Reporting.MostActiveUrl>(sql, new { SiteIisId = iisSiteId, OldestDate = oldest.ToSqlLiteDateTime() });
        }

        public IEnumerable<LogEntry> GetAllRelatedToException(int siteIisId, DateTime datetime)
        {
            var sql = "SELECT * FROM LogEntries WHERE SiteIisId = @SiteIisId AND HttpStatusCode = @HttpStatusCode AND DateTime == @DateTime;";
            return Connection.Query<LogEntry>(sql, new { SiteIisId = siteIisId, HttpStatusCode = 500, DateTime = datetime.ToSqlLiteDateTime()});
        }

        public IEnumerable<LogEntry> GetBySite(Site site)
        {
            var sql = "SELECT * FROM LogEntries WHERE SiteIisId = @SiteIisId";
            return Connection.Query<LogEntry>(sql, new {SiteIisId = site.IisId});
        }


        public int GetCountBySite(int siteIisId, DateTime? oldest = null)
        {
            var sql = "SELECT COUNT(*) FROM LogEntries WHERE SiteIisId = @SiteIisId";

            if (oldest != null)
                sql = sql + " AND DateTime >= @oldest";

            return (int)Connection.Query<long>(sql, new { siteIisId, Oldest = oldest.ToSqlLiteDateTime() }).Single();
        }

        public int GetTotalCount(DateTime? oldest = null)
        {
            var sql = "SELECT COUNT(*) FROM LogEntries";

            if (oldest != null)
                sql = sql + " WHERE DateTime >= @oldest";

            return (int)Connection.Query<long>(sql, oldest != null ? new { Oldest = oldest.ToSqlLiteDateTime() } : null).Single();
        }

        public double GetAverageResponseTime(DateTime? oldest = null)
        {
            var sql = "SELECT ifnull(avg(TimeTaken), 0.0) FROM LogEntries";

            if (oldest != null)
                sql = sql + " WHERE DateTime >= @oldest";

            var result = Connection.Query<double>(sql, oldest != null ? new { Oldest = oldest.ToSqlLiteDateTime() } : null);
            
            if(result == null)
                return 0;
            
            return result.Single();
        }

        public void Insert(IEnumerable<LogEntry> entities)
        {
            using (var transaction = Connection.BeginTransaction())
            {
                foreach (var entity in entities)
                    Connection.Execute(
                        "INSERT INTO LogEntries(DateTime, ServerIpAddress, HttpMethod, Uri, Querystring, Port, Username, ClientIpAddress, Agentstring, HttpStatusCode, HttpSubStatusCode, TimeTaken, SiteIisId, LogRow, LogFilename) VALUES(@DateTime, @ServerIpAddress, @HttpMethod, @Uri, @Querystring, @Port, @Username, @ClientIpAddress, @Agentstring, @HttpStatusCode, @HttpSubStatusCode, @TimeTaken, @SiteIisId, @LogRow, @LogFilename)",
                        new { entity.DateTime, entity.ServerIpAddress, entity.HttpMethod, entity.Uri, entity.Querystring, entity.Port, entity.Username, entity.ClientIpAddress, entity.Agentstring, entity.HttpStatusCode, entity.HttpSubStatusCode, entity.TimeTaken, entity.SiteIisId, entity.LogRow, entity.LogFilename });

                transaction.Commit();
            }
        }

        public IEnumerable<string> GetParsedLogfilesBySite(int iisId)
        {
            return Connection.Query<string>(
                "SELECT DISTINCT(LogFilename) FROM LogEntries WHERE SiteIisId = @SiteIisId",
                new { SiteIisId = iisId });
        }
    }
}