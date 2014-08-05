using System.Collections.Generic;
using System.Linq;

namespace Servant.Web.Helpers
{
    public static class ApplicationPoolHelper
    {
        public static void Delete(string applicationPool)
        {
            using(var manager = new Microsoft.Web.Administration.ServerManager())
            {
                manager.ApplicationPools.Single(x => x.Name.ToLower() == applicationPool.ToLower()).Delete();
                manager.CommitChanges();
            }
        }

        public static IEnumerable<string> GetAll()
        {
            using (var manager = new Microsoft.Web.Administration.ServerManager())
            {
                return manager.ApplicationPools.Select(x => x.Name);
            }
        }
    }
}