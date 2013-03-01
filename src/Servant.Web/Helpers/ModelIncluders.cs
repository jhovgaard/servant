using System.Linq;

namespace Servant.Web.Helpers
{
    public static class ModelIncluders
    {
        static readonly SiteManager SiteManager = new SiteManager();

        public static void IncludeCertificates(ref dynamic model)
        {
            model.Certificates = SiteManager.GetCertificates().Select(x => x.FriendlyName).ToList();
        }

        public static void IncludeApplicationPools(ref dynamic model)
        {
            model.ApplicationPools = SiteManager.GetApplicationPools();
        }

    }
}
