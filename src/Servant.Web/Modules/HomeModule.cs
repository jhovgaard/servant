using System;
using System.Linq;
using Servant.Web.Helpers;

namespace Servant.Web.Modules
{
    public class HomeModule : BaseModule
    {
        public HomeModule()
        {
            Get["/"] = p => {
                var latestErrors = EventLogHelper.GetByDateTimeDescending(5).ToList();
                latestErrors = EventLogHelper.AttachSite(latestErrors);
                Model.UnhandledExceptions = latestErrors;
                return View["Index", Model];
            };
        }
    }
}