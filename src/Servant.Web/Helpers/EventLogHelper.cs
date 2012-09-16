using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Servant.Business.Objects;
using Servant.Business.Services;

namespace Servant.Web.Helpers
{
    public static class EventLogHelper
    {
        private static readonly ApplicationErrorService ApplicationErrorService = new ApplicationErrorService();

        public static ApplicationError ParseEntry(EventLogEntry windowsEntry)
        { 
            var typeRegex = new Regex(@"Exception type: (.+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var messageRegex = new Regex(@"Exception message: (.+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var iisIdRegex = new Regex(@"Application domain: /LM/W3SVC/(\d).+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            
            var type = typeRegex.Match(windowsEntry.Message).Groups[1].Value;
            var message = messageRegex.Match(windowsEntry.Message).Groups[1].Value;
            
            // Entries kan forekomme uden et IIS id.
            var iisIdResult = iisIdRegex.Match(windowsEntry.Message).Groups[1].Value;
            var iisId = string.IsNullOrWhiteSpace(iisIdResult ) ? 0 : Convert.ToInt32(iisIdResult);

            return new ApplicationError { 
                Id = windowsEntry.Index,
                DateTime = windowsEntry.TimeGenerated.ToUniversalTime(), 
                SiteIisId = iisId,
                Message = message,
                ExceptionType = type,
                FullMessage = windowsEntry.Message.Replace(Environment.NewLine, "<br />")
            };
        }

        public static IEnumerable<EventLogEntry> GetAspNetEvents()
        {
            var log = new EventLog("application");
            var entries = log.Entries
                .Cast<EventLogEntry>()
                .Where(x => x.Source.Contains("ASP.NET"));

            return entries;
        }

        public static IEnumerable<ApplicationError> GetUnhandledExceptionEntries(int max = 0) 
        {
            var entries = GetAspNetEvents();
            entries = entries.OrderByDescending(x => x.TimeGenerated);

            if (max > 0)
                entries = entries.Take(max);

            return entries.Select(eventLogEntry => ParseEntry(eventLogEntry));
        }

        public static ApplicationError GetById(int eventLogId)
        {
            var log = new EventLog("application");
            var entry = log.Entries
                .Cast<EventLogEntry>()
                .SingleOrDefault(x => x.Index == eventLogId);

            return ParseEntry(entry);
            
        }

        public static void SyncDatabaseWithServer()
        {
            var latestError = ApplicationErrorService.GetLatest();
            var newErrors = GetNewByLatestError(latestError);
            ApplicationErrorService.Insert(newErrors);
        }

        private static IEnumerable<ApplicationError> GetNewByLatestError(ApplicationError latestError)
        {
            var entries = GetAspNetEvents();
            if (latestError != null)
                entries = entries.Where(x => x.Index > latestError.Id);

            return entries.Select(eventLogEntry => ParseEntry(eventLogEntry));
        }

        public static IEnumerable<ApplicationError> AttachSite(IEnumerable<ApplicationError> errors)
        {
            var sites = SiteHelper.GetSites().ToList();
            foreach (var applicationError in errors) {
                applicationError.Site = sites.SingleOrDefault(x => x.IisId == applicationError.SiteIisId);
                yield return applicationError;
            }
        }
    }
}