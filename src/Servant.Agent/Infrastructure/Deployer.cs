using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using ICSharpCode.SharpZipLib.Zip;
using Servant.Business.Objects;
using Servant.Business.Objects.Enums;
using Servant.Shared;
using Servant.Shared.SocketClient;
using TinyIoC;

namespace Servant.Agent.Infrastructure
{
    public static class Deployer
    {
        private static readonly ServantAgentConfiguration Configuration = TinyIoCContainer.Current.Resolve<ServantAgentConfiguration>();

        private static byte[] DownloadUrl(string url)
        {
            return new WebClient().DownloadData(url);
        }

        private static void SendResponse(int deploymentId, DeploymentResponseType type, string message, bool success = true)
        {
            SocketClient.SocketClient.ReplyOverHttp(new CommandResponse(CommandResponse.ResponseType.Deployment) { Message = Json.SerializeToString(new DeploymentResponse() { DeploymentId = deploymentId, Message = message, InstallationGuid = Configuration.InstallationGuid, Success = success, Type = type }), Success = success });
        }

        public static void Deploy(Deployment deployment)
        {
            var sw = new Stopwatch();
            var fullSw = new Stopwatch();

            fullSw.Start();
            SendResponse(deployment.Id, DeploymentResponseType.DeploymentRequestReceived,  "Received deployment request.");
            Site site = SiteManager.GetSiteByName(deployment.SiteName);
            var originalPath = site.SitePath;

            var rootPath = site.SitePath;
            var directoryName = new DirectoryInfo(rootPath).Name;
            
            if (directoryName.StartsWith("servant-"))
            {
                rootPath = rootPath.Substring(0, rootPath.LastIndexOf(@"\", System.StringComparison.Ordinal));
            }
            var newPath = Path.Combine(rootPath, "servant-" + DateTime.Now.ToString("ddMMMyyyy-HHmmss"));

            var fullPath = Environment.ExpandEnvironmentVariables(newPath);
            Directory.CreateDirectory(fullPath);
            SendResponse(deployment.Id, DeploymentResponseType.CreateDirectory, "Created directory: " + fullPath);

            sw.Start();
            var zipFile = DownloadUrl(deployment.Url);
            sw.Stop();
            SendResponse(deployment.Id, DeploymentResponseType.PackageDownload, string.Format("Completed package download in {0} seconds.", sw.Elapsed.TotalSeconds));

            var fastZip = new FastZip();
            var stream = new MemoryStream(zipFile);
            fastZip.ExtractZip(stream, fullPath, FastZip.Overwrite.Always, null, null, null, true, true);
            SendResponse(deployment.Id, DeploymentResponseType.PackageUnzipping, "Completed package extracting.");

            site.SitePath = newPath;
            SiteManager.UpdateSite(site);
            if (site.ApplicationPoolState == InstanceState.Started)
            {
                SiteManager.RecycleApplicationPool(site.ApplicationPool);    
            }
            fullSw.Stop();

            SendResponse(deployment.Id, DeploymentResponseType.ChangeSitePath, string.Format("Changed site path to {0}. Deployment completed in {1} seconds.", fullPath, fullSw.Elapsed.TotalSeconds));

            if (deployment.WarmupAfterDeploy)
            {
                var statusCode = GetReturnedStatusCode(site, deployment.WarmupUrl);
                var msg = statusCode == null ? "Could not contact IIS site" : string.Format("Site locally returned HTTP {0} {1}.", (int) statusCode, statusCode);

                SendResponse(deployment.Id, DeploymentResponseType.Warmup, msg, statusCode.HasValue && statusCode.Value == HttpStatusCode.OK);

                if (deployment.RollbackOnError)
                {
                    // Roll-back if not 200 OK
                    if (statusCode != HttpStatusCode.OK)
                    {
                        site.SitePath = originalPath;
                        SiteManager.UpdateSite(site);
                        if (site.ApplicationPoolState == InstanceState.Started)
                        {
                            SiteManager.RecycleApplicationPool(site.ApplicationPool);
                        }

                        GetReturnedStatusCode(site, deployment.WarmupUrl);
                        SendResponse(deployment.Id, DeploymentResponseType.Rollback, string.Format("Rollback completed. Site path is now: {0}.", originalPath));
                    }
                }
            }
        }

        public static HttpStatusCode? GetReturnedStatusCode(Site site, string warmupUrl)
        {
            var uri = new Uri(warmupUrl);
            var testUrl = uri.ToString().Replace(uri.Host, "127.0.0.1");

            var request = WebRequest.Create(testUrl) as HttpWebRequest;
            request.Host = uri.Host + ":" + uri.Port;
            HttpWebResponse response;
            try
            {
                 response = (HttpWebResponse)request.GetResponse();

            }
            catch (WebException ex)
            {
                var exceptionResponse = (HttpWebResponse) ex.Response;

                if (exceptionResponse == null)
                {
                    return null;
                }

                return exceptionResponse.StatusCode;
            }
            return response.StatusCode;
        }
    }
}