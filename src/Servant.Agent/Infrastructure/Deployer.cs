using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using ICSharpCode.SharpZipLib.Zip;
using Servant.Business.Objects;
using Servant.Shared;
using Servant.Shared.SocketClient;

namespace Servant.Agent.Infrastructure
{
    public static class Deployer
    {
        private static byte[] DownloadUrl(string url)
        {
            return new WebClient().DownloadData(url);
        }

        private static void SendResponse(int deploymentId, string message, bool success = true)
        {
            SocketClient.SocketClient.ReplyOverHttp(new CommandResponse(CommandResponse.ResponseType.Deployment) { Message = Json.SerializeToString(new { DeploymentId = deploymentId, Message = message }), Success = success });
        }

        public static void Deploy(Deployment deployment)
        {
            var sw = new Stopwatch();
            var fullSw = new Stopwatch();

            fullSw.Start();
            SendResponse(deployment.Id, "Received deployment request.");
            Site site = SiteManager.GetSiteByName(deployment.SiteName);
            var rootPath = site.SitePath;
            var directoryName = new DirectoryInfo(rootPath).Name;
            
            if (directoryName.StartsWith("servant-"))
            {
                rootPath = rootPath.Substring(0, rootPath.LastIndexOf(@"\", System.StringComparison.Ordinal));
            }
            var newPath = Path.Combine(rootPath, "servant-" + DateTime.Now.ToString("ddMMMyyyy-HHmmss"));

            var fullPath = Environment.ExpandEnvironmentVariables(newPath);
            Directory.CreateDirectory(fullPath);
            SendResponse(deployment.Id, "Created directory: " + fullPath);

            sw.Start();
            var zipFile = DownloadUrl(deployment.Url);
            sw.Stop();
            SendResponse(deployment.Id, string.Format("Completed package download in {0} seconds.", sw.Elapsed.TotalSeconds));

            var fastZip = new FastZip();
            var stream = new MemoryStream(zipFile);
            fastZip.ExtractZip(stream, fullPath, FastZip.Overwrite.Always, null, null, null, true, true);
            SendResponse(deployment.Id, "Completed package unzipping.");

            site.SitePath = newPath;
            SiteManager.UpdateSite(site);
            fullSw.Stop();
            SendResponse(deployment.Id, string.Format("Changed site path to {0}. Deployment completed in {1} seconds.", fullPath, sw.Elapsed.TotalSeconds));
        }
    }
}