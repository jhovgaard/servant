using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using Servant.Business.Objects;
using Servant.Business.Objects.Enums;
using Servant.Shared;
using Servant.Shared.SocketClient;
using TinyIoC;

namespace Servant.Agent.Infrastructure
{
    public class Deployer
    {
        private readonly ServantAgentConfiguration Configuration = TinyIoCContainer.Current.Resolve<ServantAgentConfiguration>();
        private List<DeploymentInstance> _deploymentInstances = new List<DeploymentInstance>();

        private byte[] DownloadUrl(string url)
        {
            return new WebClient().DownloadData(url);
        }

        private void SendResponse(int deploymentId, DeploymentResponseType type, string message, bool success = true)
        {
            SocketClient.SocketClient.ReplyOverHttp(new CommandResponse(CommandResponse.ResponseType.Deployment) { Message = Json.SerializeToString(new DeploymentResponse() { DeploymentId = deploymentId, Message = message, InstallationGuid = Configuration.InstallationGuid, Success = success, Type = type }), Success = success });
        }

        public void Deploy(Deployment deployment)
        {
            var existingDeployment = _deploymentInstances.SingleOrDefault(x => x.DeploymentId == deployment.Id);
            if (existingDeployment != null)
            {
                if (existingDeployment.RollbackCompleted) // Deployment have been rolled back by other server  already.
                {
                    return;
                }

                _deploymentInstances.RemoveAll(x => x.DeploymentId == deployment.Id);
            }

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
            var newPath = Path.Combine(rootPath, "servant-" + deployment.Guid);

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

            var rollbackCompleted = false;
            if (deployment.WarmupAfterDeploy)
            {
                var warmupResult = GetReturnedStatusCode(site, deployment.WarmupUrl);
                SendResponse(deployment.Id, DeploymentResponseType.WarmupResult, Json.SerializeToString(warmupResult));
                var msg = warmupResult == null ? "Could not contact IIS site" : string.Format("Site locally returned HTTP {0} {1}.", (int) warmupResult.StatusCode, warmupResult.StatusCode);

                SendResponse(deployment.Id, DeploymentResponseType.Warmup, msg, warmupResult.StatusCode == HttpStatusCode.OK);

                if (deployment.RollbackOnError)
                {
                    // Roll-back if not 200 OK
                    if (warmupResult.StatusCode != HttpStatusCode.OK)
                    {
                        site.SitePath = originalPath;
                        SiteManager.UpdateSite(site);
                        if (site.ApplicationPoolState == InstanceState.Started)
                        {
                            SiteManager.RecycleApplicationPool(site.ApplicationPool);
                        }
                        rollbackCompleted = true;
                        GetReturnedStatusCode(site, deployment.WarmupUrl);
                        
                        SendResponse(deployment.Id, DeploymentResponseType.Rollback, string.Format("Rollback completed. Site path is now: {0}.", originalPath));
                    }
                }
            }

            _deploymentInstances.Add(new DeploymentInstance() { DeploymentId = deployment.Id, DeploymentGuid = deployment.Guid, NewPath = newPath, OriginalPath = originalPath, RollbackCompleted = rollbackCompleted, IisSiteId = site.IisId });
        }

        public void Rollback(int deploymentId)
        {
            SendResponse(deploymentId, DeploymentResponseType.Rollback, string.Format("Remote rollback requested received."));
            var instance = _deploymentInstances.SingleOrDefault(x => x.DeploymentId == deploymentId && !x.RollbackCompleted);
            if (instance == null)
            {
                _deploymentInstances.Add(new DeploymentInstance() { DeploymentId = deploymentId, RollbackCompleted = true });
                return;
            }

            Site site = SiteManager.GetSiteById(instance.IisSiteId);
            site.SitePath = instance.OriginalPath;
            SiteManager.UpdateSite(site);
            if (site.ApplicationPoolState == InstanceState.Started)
            {
                SiteManager.RecycleApplicationPool(site.ApplicationPool);
            }

            instance.RollbackCompleted = true;
        }

        public static WarmupResult GetReturnedStatusCode(Site site, string warmupUrl)
        {
            var uri = new Uri(warmupUrl);
            var testUrl = uri.ToString().Replace(uri.Host, "127.0.0.1");

            var request = WebRequest.Create(testUrl) as HttpWebRequest;
            request.Host = uri.Host + ":" + uri.Port;
            HttpWebResponse response;
            try
            {
                 response = (HttpWebResponse)request.GetResponse();
                 return new WarmupResult(response);
            }
            catch (WebException ex)
            {
                var exceptionResponse = (HttpWebResponse) ex.Response;

                if (exceptionResponse == null)
                {
                    return null;
                }

                return new WarmupResult(exceptionResponse);
            }
        }
    }

    public class WarmupResult
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Body { get; set; }
        public List<KeyValuePair<string, string>> Headers { get; set; }

        public WarmupResult(HttpWebResponse response)
        {
            StatusCode = response.StatusCode;

            var encoding = Encoding.ASCII;
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
            {
                Body = reader.ReadToEnd();
            }

            Headers = Enumerable
                .Range(0, response.Headers.Count)
                .SelectMany(i => response.Headers.GetValues(i)
                .Select(v => new KeyValuePair<string, string>(response.Headers.GetKey(i), v)))
                .ToList();
        }
    }

    public class DeploymentInstance
    {
        public Guid DeploymentGuid { get; set; }
        public string OriginalPath { get; set; }
        public string NewPath { get; set; }
        public bool RollbackCompleted { get; set; }
        public int IisSiteId { get; set; }
        public int DeploymentId { get; set; }
    }
}