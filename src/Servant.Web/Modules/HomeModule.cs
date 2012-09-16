using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Servant.Business.Objects;
using Servant.Business.Services;
using Nancy;
using MSUtil;
using Servant.Web.Helpers;

namespace Servant.Web.Modules
{
    public class HomeModule : BaseModule
    {
        public HomeModule(ApplicationErrorService applicationErrorService)
        {
            Get["/"] = p => {
                var latestErrors = applicationErrorService.GetByDateTimeDescending(10);
                latestErrors = EventLogHelper.AttachSite(latestErrors);

                var y = SiteHelper.GetSites().ToList();
                Model.UnhandledExceptions = latestErrors;
                return View["Index", Model];
            };
        }
    }
}