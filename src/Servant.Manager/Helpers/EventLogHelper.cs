using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text.RegularExpressions;
using Servant.Business.Objects;
using Servant.Business.Objects.Enums;

namespace Servant.Manager.Helpers
{
    public static class EventLogHelper
    {
        public static ApplicationError ParseEntry(EventRecord eventRecord)
        {
            var iisIdRegex = new Regex(@"/LM/W3SVC/(\d).+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            // Entries kan forekomme uden et IIS id.
            var iisIdResult = iisIdRegex.Match(eventRecord.Properties[8].Value.ToString()).Groups[1].Value;
            var iisId = string.IsNullOrWhiteSpace(iisIdResult ) ? 0 : Convert.ToInt32(iisIdResult);

            var error = new ApplicationError
                {
                    Id = (int) eventRecord.RecordId,
                    SiteIisId = iisId,
                    DateTime = DateTime.Parse(eventRecord.Properties[2].Value.ToString()).ToUniversalTime(),
                    ExceptionType = eventRecord.Properties[17].Value.ToString(),
                    Message = eventRecord.Properties[1].Value.ToString(),
                    FullMessage = eventRecord.Properties[18].Value.ToString().Replace(Environment.NewLine, "<br />").Trim(),
                    ThreadInformation = eventRecord.Properties[29].Value.ToString().Replace(Environment.NewLine, "<br />").Trim(),
                    Url =eventRecord.Properties[19].Value.ToString(),
                    ClientIpAddress = eventRecord.Properties[21].Value.ToString()
                };

            return error;
        }

        public static ApplicationError GetById(int eventLogId)
        {

            var query = string.Format(@"<QueryList>
                                            <Query Id=""0"" Path=""Application"">
                                            <Select Path=""Application"">*[System[(EventRecordID={0})]]</Select>
                                            </Query>
                                        </QueryList>", eventLogId);

            var elq = new EventLogQuery("Application", PathType.LogName, query);
            var elr = new EventLogReader(elq);
            var eventInstance = elr.ReadEvent();
            return eventInstance == null
                ? null
                : ParseEntry(eventInstance);
        }

        public static IEnumerable<ApplicationError> GetByDateTimeDescending(int max = 0)
        {
            var query = @"<QueryList>
                              <Query Id=""0"" Path=""Application"">
                                <Select Path=""Application"">*[System[Provider[@Name='ASP.NET 2.0.50727.0' or @Name='ASP.NET 4.0.30319.0'] and (Level=2 or Level=3)]]</Select>
                              </Query>
                            </QueryList>";

            var elq = new EventLogQuery("Application", PathType.LogName, query) {ReverseDirection = true};
            var elr = new EventLogReader(elq);

            var events = new List<EventRecord>();

            max = (max == 0) ? int.MaxValue : max;
            var i = 0;
            for (var eventInstance = elr.ReadEvent(); null != eventInstance && i < max; eventInstance = elr.ReadEvent(), i++)
                events.Add(eventInstance);

            return events.Select(ParseEntry).Where(x => x.SiteIisId != 0);
        }

        public static IEnumerable<ApplicationError> GetBySite(int siteIisId, StatsRange range)
        {
            Int64 msLookback = 0;

            switch (range)
            {
                case StatsRange.LastMonth:
                    msLookback = 2592000000;
                    break;
                case StatsRange.LastWeek:
                    msLookback = 604800000;
                    break;
                case StatsRange.Last24Hours:
                    msLookback = 86400000;
                    break;
            }

            var query = string.Format(@"<QueryList>
                              <Query Id=""0"" Path=""Application"">
                                <Select Path=""Application"">*[System[Provider[@Name='ASP.NET 2.0.50727.0' or @Name='ASP.NET 4.0.30319.0'] and (Level=2 or Level=3){0}]]</Select>
                              </Query>
                            </QueryList>", (msLookback == 0) ? null : "and TimeCreated[timediff(@SystemTime) &lt;= " + msLookback + "]");

            var elq = new EventLogQuery("Application", PathType.LogName, query) { ReverseDirection = true};
            var elr = new EventLogReader(elq);

            var events = new List<EventRecord>();
            for (var eventInstance = elr.ReadEvent(); null != eventInstance; eventInstance = elr.ReadEvent())
            {
                if (eventInstance.Properties[8].Value.ToString().StartsWith("/LM/W3SVC/" + siteIisId + "/"))
                    events.Add(eventInstance);
            }

            return events.Select(ParseEntry);
        }

        public static List<ApplicationError> AttachSite(IEnumerable<ApplicationError> errors)
        {
            var siteManager = new SiteManager();
            var sites = siteManager.GetSites().ToList();
            foreach (var applicationError in errors)
            {
                applicationError.Site = sites.SingleOrDefault(x => x.IisId == applicationError.SiteIisId);
            }

            return errors.ToList();
        }
    }
}