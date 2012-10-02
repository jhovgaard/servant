using System.Threading;
using Servant.Business.Services;

namespace Servant.Manager.Helpers
{
    public static class SynchronizationHelper
    {
         public static void SyncServer()
         {
             var logEntryService = new LogEntryService();
             var sites = SiteHelper.GetSites();
             RequestLogHelper.FlushLog();
             Thread.Sleep(20); // Venter på at IIS har skrevet loggen

             foreach (var site in sites)
             {
                 var latestEntry = logEntryService.GetLatestEntry(site);
                 RequestLogHelper.InsertNewInDbBySite(site, latestEntry);
                 Thread.Sleep(2000);
             }
         }
    }
}