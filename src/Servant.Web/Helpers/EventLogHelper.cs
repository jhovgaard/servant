using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text.RegularExpressions;
using Servant.Business.Objects;
using Servant.Business.Objects.Enums;

namespace Servant.Web.Helpers
{
    public static class EventLogHelper
    {
        public static ApplicationError ParseEntry(EventRecord eventRecord)
        {
            try
            {
                var iisIdRegex = new Regex(@"/LM/W3SVC/(\d{1,9}).+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                // Entries kan forekomme uden et IIS id.
                var iisIdString = eventRecord.Properties[8].Value.ToString();
                var iisIdMatch = iisIdRegex.Match(iisIdString);
                var iisIdResult = (iisIdMatch.Groups.Count > 1 && !string.IsNullOrEmpty(iisIdMatch.Groups[1].Value)) ? Convert.ToInt32(iisIdMatch.Groups[1].Value) : 0;

                if (iisIdResult == 0)
                    return null;
                var fullMessage = eventRecord.Properties[18].Value.ToString();
                var error = new ApplicationError
                {
                    Id = (int)eventRecord.RecordId,
                    SiteIisId = iisIdResult,
                    DateTime = eventRecord.TimeCreated.Value.ToUniversalTime(),
                    ExceptionType = eventRecord.Properties[17].Value.ToString(),
                    Message = eventRecord.Properties[1].Value.ToString(),
                    FullMessage = fullMessage.Replace(Environment.NewLine, "<br />").Trim(),
                    ThreadInformation = eventRecord.Properties[29].Value.ToString().Replace(Environment.NewLine, "<br />").Trim(),
                    Url = eventRecord.Properties[19].Value.ToString(),
                    ClientIpAddress = eventRecord.Properties[21].Value.ToString()
                };

                var indexOfBreak = fullMessage.IndexOf("\n   ", System.StringComparison.InvariantCulture);
                var description = fullMessage.Substring(0, indexOfBreak);
                
                error.Description = description;

                return error;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Servant for IIS", "Error parsing entry: " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine + Environment.NewLine + Environment.NewLine + "EventRecord: " + Environment.NewLine + eventRecord.ToXml());
                return null;
            }
        }

        public static ApplicationError GetById(int eventLogId)
        {
            var query = string.Format(@"<QueryList>
                                            <Query Id=""0"" Path=""Application"">
                                            <Select Path=""Application"">*[System[(EventRecordID={0})]]</Select>
                                            </Query>
                                        </QueryList>", eventLogId);

            var elq = new EventLogQuery("Application", PathType.LogName, query);
            using (var elr = new EventLogReader(elq))
            {
                var eventInstance = elr.ReadEvent();
                return eventInstance == null
                    ? null
                    : ParseEntry(eventInstance);
            }
        }

        public static IEnumerable<ApplicationError> GetByDateTimeDescending(int max = 0)
        {
            var query = @"<QueryList>
                              <Query Id=""0"" Path=""Application"">
                                <Select Path=""Application"">*[System[Provider[@Name='ASP.NET 2.0.50727.0' or @Name='ASP.NET 4.0.30319.0'] and (Level=2 or Level=3 or Level=4)]]</Select>
                              </Query>
                            </QueryList>";

            var elq = new EventLogQuery("Application", PathType.LogName, query) {ReverseDirection = true};
            using (var elr = new EventLogReader(elq))
            {
                var events = new List<EventRecord>();

                max = (max == 0) ? int.MaxValue : max;
                var i = 0;
                var entries = 0;

                for (var eventInstance = elr.ReadEvent(); null != eventInstance && entries < max; eventInstance = elr.ReadEvent(), i++)
                {
                    var parsedEvent = ParseEntry(eventInstance);
                    if (parsedEvent != null && parsedEvent.SiteIisId != 0)
                    {
                        entries++;
                        yield return parsedEvent;
                    }
                }
            }
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

            using (var elr = new EventLogReader(elq))
            {
                var events = new List<EventRecord>();
                for (var eventInstance = elr.ReadEvent(); null != eventInstance; eventInstance = elr.ReadEvent())
                {
                    if (eventInstance.Properties.Count() > 9 && eventInstance.Properties[8].Value.ToString().StartsWith("/LM/W3SVC/" + siteIisId + "/"))
                        events.Add(eventInstance);
                }
                return events.Select(ParseEntry);
            }
        }

        public static List<ApplicationError> AttachSite(IEnumerable<ApplicationError> errors, IEnumerable<Site> sites)
        {
            foreach (var applicationError in errors)
            {
                applicationError.Site = sites.SingleOrDefault(x => x.IisId == applicationError.SiteIisId);
            }

            return errors.ToList();
        }
    }
}