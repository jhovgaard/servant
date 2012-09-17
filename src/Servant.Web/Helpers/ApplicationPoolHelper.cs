using System.Collections.Generic;
using System.Linq;

namespace Servant.Web.Helpers
{
    public static class ApplicationPoolHelper
    {
         public static IEnumerable<string> GetUnusedApplicationPools()
         {
             using(var manager = new Microsoft.Web.Administration.ServerManager())
             {
                 IEnumerable<string> appPoolsInUse = manager.Sites.Select(x => x.Applications.First().ApplicationPoolName).ToList();

                 foreach(var appPool in manager.ApplicationPools)
                 {
                     if (!appPoolsInUse.Contains(appPool.Name))
                     {
                         yield return appPool.Name;    
                     }
                 }
             }
         }

        public static void Delete(string applicationPool)
        {
            using(var manager = new Microsoft.Web.Administration.ServerManager())
            {
                manager.ApplicationPools.Single(x => x.Name.ToLower() == applicationPool.ToLower()).Delete();
                manager.CommitChanges();
            }
        }
    }
}