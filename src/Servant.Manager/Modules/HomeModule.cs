using Nancy;
using Servant.Business.Services;
using Servant.Manager.Helpers;

namespace Servant.Manager.Modules
{
    public class HomeModule : BaseModule
    {
        public HomeModule(ApplicationErrorService applicationErrorService)
        {
            Get["/"] = p => {
                var latestErrors = applicationErrorService.GetByDateTimeDescending(10);
                latestErrors = EventLogHelper.AttachSite(latestErrors);
                Model.UnhandledExceptions = latestErrors;

                return View["Index", Model];
            };
        }
    }
}