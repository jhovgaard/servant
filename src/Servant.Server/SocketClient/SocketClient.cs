using System;
using Nancy.Json;
using Nancy.TinyIoc;
using Servant.Business.Objects;
using Servant.Web.Helpers;
using WebSocketSharp;

namespace Servant.Server.SocketClient
{
    public static class SocketClient
    {   
        public static void Connect()
        {
            var configuration = TinyIoCContainer.Current.Resolve<ServantConfiguration>();

            if (string.IsNullOrWhiteSpace(configuration.ServantIoKey))
            {
                return;
            }

            var client = GetClient(configuration);
            client.Connect();
        }

        private static WebSocket GetClient(ServantConfiguration configuration)
        {
            var url = "ws://" + configuration.ServantIoUrl + "/Client?installationGuid=" + configuration.InstallationGuid + "&organizationGuid=" + configuration.ServantIoKey + "&servername=" + System.Environment.MachineName;
            var ws = new WebSocket(url);
            var serializer = new JavaScriptSerializer();

            ws.OnMessage += (sender, e) =>
            {
                var request = serializer.Deserialize<CommandRequest>(e.Data);

                switch (request.Command)
                {
                    case CommandRequestType.GetSites:
                        var sites = SiteManager.GetSites();
                        var result = serializer.Serialize(sites);
                        ws.Send(result);
                        break;
                    case CommandRequestType.UpdateSite:
                        var site = serializer.Deserialize<Site>(request.JsonObject);

                        Site originalSite = SiteManager.GetSiteByName(request.Value);

                        originalSite.ApplicationPool = site.ApplicationPool;
                        originalSite.Name = site.Name;
                        originalSite.SiteState = site.SiteState;
                        originalSite.Bindings = site.Bindings;
                        originalSite.LogFileDirectory = site.LogFileDirectory;
                        originalSite.SitePath = site.SitePath;
                        originalSite.Bindings = site.Bindings;

                        SiteManager.UpdateSite(originalSite);

                        ws.Send("ok");
                        break;
                    case CommandRequestType.GetApplicationPools:
                        var appPools = SiteManager.GetApplicationPools();
                        ws.Send(serializer.Serialize(appPools));
                        break;
                    case CommandRequestType.GetCertificates:
                        ws.Send(serializer.Serialize(SiteManager.GetCertificates()));
                        break;
                    case CommandRequestType.StartSite:
                        var startSite = SiteManager.GetSiteByName(request.Value);
                        SiteManager.StartSite(startSite);
                        ws.Send("ok");
                        break;
                    case CommandRequestType.StopSite:
                        var stopSite = SiteManager.GetSiteByName(request.Value);
                        SiteManager.StopSite(stopSite);
                        ws.Send("ok");
                        break;
                    case CommandRequestType.RecycleApplicationPool:
                        var recycleSite = SiteManager.GetSiteByName(request.Value);
                        SiteManager.RecycleApplicationPoolBySite(recycleSite.IisId);
                        ws.Send("ok");
                        break;
                    case CommandRequestType.RestartSite:
                        var restartSite = SiteManager.GetSiteByName(request.Value);
                        SiteManager.RestartSite(restartSite.IisId);
                        ws.Send("ok");
                        break;
                    case CommandRequestType.DeleteSite:
                        var deleteSite = SiteManager.GetSiteByName(request.Value);
                        SiteManager.DeleteSite(deleteSite.IisId);
                        ws.Send("ok");
                        break;
                    case CommandRequestType.CreateSite:
                        var createSite = serializer.Deserialize<Site>(request.JsonObject);
                        var id = SiteManager.CreateSite(createSite);
                        ws.Send(id.ToString());
                        break;
                }
            };

            ws.OnError += (sender, args) =>
            {
                if (args.Message == "A WebSocket connection has already been established.")
                    return;

                ws.Close();
            };

            ws.OnClose += (sender, args) =>
            {
                ws = null;
                var client = GetClient(configuration);

                client.Connect();
            };

            ws.OnOpen += (sender, args) => Console.WriteLine("Successfully connected to ws://" + configuration.ServantIoUrl);

            return ws;
        }
    }
}