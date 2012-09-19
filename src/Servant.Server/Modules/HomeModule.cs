using Servant.Business.Services;
using EventLogHelper = Servant.Server.Helpers.EventLogHelper;

namespace Servant.Server.Modules
{
    public class HomeModule : Server.Modules.BaseModule
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