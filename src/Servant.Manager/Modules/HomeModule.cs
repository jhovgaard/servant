using System;
using System.Diagnostics;
using System.Linq;
using Nancy;
using Nancy.Security;
using Servant.Business.Services;
using Servant.Manager.Helpers;

namespace Servant.Manager.Modules
{
    public class HomeModule : BaseModule
    {
        public HomeModule(ApplicationErrorService applicationErrorService, LogEntryService logEntryService)
        {
            Get["/"] = p => {
                var latestErrors = applicationErrorService.GetByDateTimeDescending(10).ToList();
                latestErrors = EventLogHelper.AttachSite(latestErrors);
                Model.UnhandledExceptions = latestErrors;
                Model.TotalRequestsToday = logEntryService.GetTotalCount(DateTime.UtcNow.Date);
                return View["Index", Model];
            };
        }
    }
}