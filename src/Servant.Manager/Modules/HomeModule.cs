using System;
using System.Linq;
using Servant.Manager.Helpers;

namespace Servant.Manager.Modules
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