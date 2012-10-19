using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Servant.Business.Objects; 

namespace Servant.Business.Services
{
    public class ApplicationErrorService : SqlLiteService<ApplicationError>
    {
        public ApplicationError GetById(int id)
        {
            return Connection.Query<ApplicationError>("SELECT * FROM ApplicationErrors WHERE Id = @Id", new { id }).SingleOrDefault();
        }

        public IEnumerable<ApplicationError> GetByDateTimeDescending(int max = 0)
        {
            var sql = "SELECT * FROM ApplicationErrors ORDER BY DateTime DESC";

            if (max != 0)
                sql = sql + " LIMIT @max";

            return Connection.Query<ApplicationError>(sql, new {max});
        }

        public ApplicationError GetLatest()
        {
            var sql = "SELECT * FROM ApplicationErrors ORDER BY DateTime DESC LIMIT 1";
            return Connection.Query<ApplicationError>(sql).SingleOrDefault();
        }

        public IEnumerable<ApplicationError> GetBySite(int siteIisId, DateTime oldest)
        {
            var sql = "SELECT * FROM ApplicationErrors WHERE SiteIisId = @SiteIisId AND DateTime > @Oldest ORDER BY DateTime DESC";
            return Connection.Query<ApplicationError>(sql, new {siteIisId, oldest});
        }

        public IEnumerable<ApplicationError> GetBySite(int siteIisId)
        {
            var sql = "SELECT * FROM ApplicationErrors WHERE SiteIisId = @SiteIisId ORDER BY DateTime DESC";
            return Connection.Query<ApplicationError>(sql, new { siteIisId });
        }

        public int GetTotalCount(DateTime? oldest = null)
        {
            var sql = "SELECT COUNT(*) FROM ApplicationErrors";

            if (oldest != null)
                sql = sql + " WHERE DateTime >= @oldest";

            return (int)Connection.Query<long>(sql, oldest != null ? new { Oldest = oldest.Value } : null).Single();
        }
    }
}