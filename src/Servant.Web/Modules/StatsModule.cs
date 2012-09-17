using System.Linq;
using Nancy;
using Servant.Business.Services;

namespace Servant.Web.Modules
{
    public class StatsModule: BaseModule
    {
        public StatsModule(LogEntryService logEntryService, ApplicationErrorService applicationErrorService) : base("/stats/")
        {
            Get["/"] = p => {
                var serverStats = new Servant.Business.Objects.Reporting.ServerStats();
                serverStats.TotalRequests = logEntryService.GetTotalCount();
                serverStats.DataRecieved = "Disabled";
                serverStats.DataSent = "Disabled";
                serverStats.TotalSites = Helpers.SiteHelper.GetSites().Count();
                serverStats.AverageResponeTime = (int)logEntryService.GetAverageResponseTime();
                serverStats.TotalErrors = applicationErrorService.GetTotalCount();
                serverStats.UnusedApplicationPools = Helpers.ApplicationPoolHelper.GetUnusedApplicationPools().Count();

                Model.ServerStats = serverStats;
                return View["Index", Model];
            };

            Get["/cleanupapplicationpools/"] = p => {
                Model.UnusedApplicationPools = Helpers.ApplicationPoolHelper.GetUnusedApplicationPools();
                return View["CleanupApplicationPools", Model];
            };

            Post["/cleanupapplicationpools/"] = p => {
                var applicationPools = Request.Form.ApplicationPools.ToString().Split(',');

                foreach(var applicationPool in applicationPools)
                {
                    Helpers.ApplicationPoolHelper.Delete(applicationPool);
                }

                return Response.AsRedirect("/stats/");
            };

        }
    }
}