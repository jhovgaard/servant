using System;
using System.IO;
using System.Net;
using ICSharpCode.SharpZipLib.Zip;
using Servant.Business.Objects;
using Servant.Shared;

namespace Servant.Client.Infrastructure
{
    public static class Deployer
    {
        private static byte[] DownloadUrl(string url)
        {
            return new WebClient().DownloadData(url);
        }

        public static void Deploy(string sitename, string url)
        {
            Site site = SiteManager.GetSiteByName(sitename);
            var rootPath = site.SitePath;
            var directoryName = new DirectoryInfo(rootPath).Name;
            if (directoryName.StartsWith("servant-"))
            {
                rootPath = Directory.GetParent(rootPath).FullName;
            }

            var newPath = Path.Combine(rootPath, "servant-" + DateTime.Now.ToString("ddMMMyyyy-HHmmss"));
            Directory.CreateDirectory(newPath);

            var zipFile = DownloadUrl(url);
            var fastZip = new FastZip();
            var stream = new MemoryStream(zipFile);
            fastZip.ExtractZip(stream, newPath, FastZip.Overwrite.Always, null, null, null, true, true);

            site.SitePath = newPath;
            SiteManager.UpdateSite(site);
        }
    }
}