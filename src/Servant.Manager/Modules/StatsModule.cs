using System.Diagnostics;
using System.Linq;
using Nancy;
using Servant.Business.Services;
using Servant.Manager.Helpers;

namespace Servant.Manager.Modules
{
    public class StatsModule: BaseModule
    {
        public StatsModule(LogEntryService logEntryService, ApplicationErrorService applicationErrorService) : base("/stats/")
        {
            Get["/"] = p => {
                var siteManager = new SiteManager();
                
                var serverStats = new Business.Objects.Reporting.ServerStats();
                
                serverStats.TotalRequests = logEntryService.GetTotalCount();
                serverStats.DataRecieved = "Not available";
                serverStats.DataSent = "Not available";
                serverStats.TotalSites = siteManager.GetSites().Count();


                serverStats.AverageResponeTime = (int)logEntryService.GetAverageResponseTime();

                serverStats.TotalErrors = applicationErrorService.GetTotalCount();
                serverStats.UnusedApplicationPools = ApplicationPoolHelper.GetUnusedApplicationPools().Count();
                Model.ServerStats = serverStats;
                return View["Index", Model];
            };

            Get["/cleanupapplicationpools/"] = p => {
                Model.UnusedApplicationPools = ApplicationPoolHelper.GetUnusedApplicationPools();
                return View["CleanupApplicationPools", Model];
            };

            Post["/cleanupapplicationpools/"] = p => {
                var applicationPools = Request.Form.ApplicationPools.ToString().Split(',');

                foreach(var applicationPool in applicationPools)
                {
                    ApplicationPoolHelper.Delete(applicationPool);
                }

                return Response.AsRedirect("/stats/");
            };

        }
    }
}