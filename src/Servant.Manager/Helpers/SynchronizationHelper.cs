using System.Threading;
using Servant.Business.Services;
using Servant.Manager.Infrastructure;

namespace Servant.Manager.Helpers
{
    public static class SynchronizationHelper
    {
         public static void SyncServer()
         {
             var host = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<IHost>();

             var logEntryService = new LogEntryService();
             var siteManager = new SiteManager();
             var sites = siteManager.GetSites();
             RequestLogHelper.FlushLog();
             Thread.Sleep(20); // Venter på at IIS har skrevet loggen

             foreach (var site in sites)
             {
                 if (!host.LogParsingStarted) // Sørger for at vi kan stoppes udefra.
                     return;

                 var latestEntry = logEntryService.GetLatestEntry(site);
                 RequestLogHelper.InsertNewInDbBySite(site, latestEntry);
             }
         }
    }
}