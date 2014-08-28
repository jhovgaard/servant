using System;
using System.Threading;
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

            bool connected = false;
            while (!connected)
            {
                configuration = TinyIoCContainer.Current.Resolve<ServantConfiguration>(); // Genindlæser så man kan ændre key.
                var client = GetClient(configuration);
                client.Connect();
                connected = client.IsAlive;
            }
        }

        private static WebSocket GetClient(ServantConfiguration configuration)
        {
            var url = "ws://" + configuration.ServantIoUrl + "/Client?installationGuid=" + configuration.InstallationGuid + "&organizationGuid=" + configuration.ServantIoKey + "&servername=" + System.Environment.MachineName;
            using (var ws = new WebSocket(url))
            {
                var serializer = new JavaScriptSerializer();
                var pingTimer = new System.Timers.Timer(2000);
                pingTimer.Elapsed += (sender, args) =>
                                     {
                                         ws.Ping();
                                     };
                pingTimer.Enabled = false;

                ws.OnMessage += (sender, e) =>
                {
                    var request = serializer.Deserialize<CommandRequest>(e.Data);

                    switch (request.Command)
                    {
                        case CommandRequestType.Unauthorized:
                            ws.Close();
                            break;
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
                    var socket = (WebSocketSharp.WebSocket)sender;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: " + args.Message);
                    Console.ResetColor();

                    if (socket.ReadyState == WebSocketState.Open)
                    {
                        Connect();
                    }
                };

                ws.OnClose += (sender, args) =>
                {
                    Console.WriteLine(DateTime.Now.ToLongTimeString() + ": Lost connection to Servant.io");
                    pingTimer.Enabled = false;
                };

                ws.OnOpen +=
                    (sender, args) =>
                    {
                        Console.WriteLine(DateTime.Now.ToLongTimeString() + ": Successfully connected to ws://" + configuration.ServantIoUrl);
                        pingTimer.Enabled = true;
                    };
                ws.Log.Output = (data, s) => { };
                ws.Log.Level = LogLevel.Fatal;

                return ws;
            }
        }
    }
}