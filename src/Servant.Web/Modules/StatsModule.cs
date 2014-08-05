using System.Linq;
using Nancy;
using Servant.Web.Helpers;

namespace Servant.Web.Modules
{
    public class StatsModule: BaseModule
    {
        public StatsModule() : base("/stats/")
        {
            Get["/cleanupapplicationpools/"] = p => {
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