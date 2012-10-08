using System.Linq;
using Nancy;
using Servant.Business.Services;
using Servant.Manager.Helpers;

namespace Servant.Manager.Modules
{
    public class HomeModule : BaseModule
    {
        public HomeModule(ApplicationErrorService applicationErrorService, LogEntryService logEntryService)
        {
            Get["/"] = p => {
                var latestErrors = applicationErrorService.GetByDateTimeDescending(10);
                latestErrors = EventLogHelper.AttachSite(latestErrors);
                Model.UnhandledExceptions = latestErrors.ToList();
                Model.TotalRequestsToday = logEntryService.GetTodayTotalCount();

                return View["Index", Model];
            };
        }
    }
}