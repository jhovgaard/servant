using System.Linq;

namespace Servant.Web.Helpers
{
    public static class ModelIncluders
    {
        public static void IncludeCertificates(ref dynamic model)
        {
            model.Certificates = SiteManager.GetCertificates().ToList();
        }

        public static void IncludeApplicationPools(ref dynamic model)
        {
            model.ApplicationPools = SiteManager.GetApplicationPools();
        }

    }
}
