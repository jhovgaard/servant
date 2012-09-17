using System;
using System.Collections.Generic;
using System.Linq;
using Servant.Business.Objects;

namespace Servant.Business.Services
{
    public class ApplicationErrorService : Service<ApplicationError>
    {
        public ApplicationErrorService() : base("ApplicationErrors") { }

        public IEnumerable<ApplicationError> GetByDateTimeDescending(int max = 0)
        {
            var errors = Table
                .All()
                .OrderByDateTimeDescending();

            if (max != 0)
                errors = errors.Take(max);

            return errors.Cast<ApplicationError>();
        }

        public ApplicationError GetLatest() {
            IEnumerable<ApplicationError> errors = Table
                .All()
                .OrderByDateTimeDescending()
                .Take(1)
                .Cast<ApplicationError>();

            return errors.SingleOrDefault();
        }

        public IEnumerable<ApplicationError> GetTodaysBySite(Site site)
        {
            return Table.All().Where(Table.SiteIisId == site.IisId && Table.DateTime >= DateTime.UtcNow.Date).OrderbyDateTimeDescending().Cast<ApplicationError>();
        }

        public IEnumerable<ApplicationError> GetLastWeekBySite(Site site)
        {
            return Table.All().Where(Table.SiteIisId == site.IisId && Table.DateTime >= DateTime.UtcNow.Date.AddDays(-7)).OrderbyDateTimeDescending().Cast<ApplicationError>();
        }

        public IEnumerable<LogEntry> GetLastMonthBySite(Site site)
        {
            return Table.FindAll(Table.DateTime >= DateTime.UtcNow.Date.AddMonths(-1)).OrderbyDateTimeDescending().Cast<ApplicationError>();
        }

        public IEnumerable<ApplicationError> GetBySite(Site site)
        {
            return Table.FindAllBySiteIisId(site.IisId).OrderbyDateTimeDescending().Cast<ApplicationError>();
        }

        public int GetTotalCount()
        {
            return Table.All().Count();
        }
    }
}