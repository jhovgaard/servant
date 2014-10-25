using System;
using System.Linq;
using System.Net.Security;
using System.Text.RegularExpressions;
using System.Threading;
using Servant.Business;
using Servant.Business.Objects;
using Servant.Business.Objects.Enums;
using Servant.Client.Infrastructure;
using Servant.Shared;
using Servant.Shared.SocketClient;
using TinyIoC;
using WebSocketSharp;

namespace Servant.Client.SocketClient
{
    public static class SocketClient
    {
        static bool _isRetrying;
        public static bool IsStopped;

        public static void Connect()
        {
            var configuration = TinyIoCContainer.Current.Resolve<ServantClientConfiguration>();

            if (string.IsNullOrWhiteSpace(configuration.ServantIoKey))
            {
                return;
            }

            bool connected = false;
            _isRetrying = true;
            while (!connected && !IsStopped)
            {
                configuration = TinyIoCContainer.Current.Resolve<ServantClientConfiguration>(); // Genindlæser så man kan ændre key.
                var client = GetClient(configuration);
                client.Connect();
                connected = client.IsAlive;
                if(!connected)
                    Thread.Sleep(2000);
            }
            _isRetrying = false;
        }

        private static WebSocket GetClient(ServantClientConfiguration configuration)
        {
            var url = "wss://" + configuration.ServantIoHost + "/Client?installationGuid=" + configuration.InstallationGuid + "&organizationGuid=" + configuration.ServantIoKey + "&servername=" + System.Environment.MachineName;		

            using (var ws = new WebSocket(url))
            {
#if(!DEBUG)
                ws.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => sslPolicyErrors == SslPolicyErrors.None;
#endif

                var pingTimer = new System.Timers.Timer(2000);
                pingTimer.Elapsed += (sender, args) =>
                                     {
                                         ws.Ping();
                                     };
                pingTimer.Enabled = false;

                ws.OnMessage += (sender, e) =>
                {
                    var request = Json.DeserializeFromString<CommandRequest>(e.Data);

                    try
                    {
                        switch (request.Command)
                        {
                            case CommandRequestType.Unauthorized:
                                IsStopped = true;
                                MessageHandler.LogException("Servant.io key was not recognized.");
                                ws.Close();
                                break;
                            case CommandRequestType.GetSites:
                                var sites = SiteManager.GetSites();
                                var result = Json.SerializeToString(sites);
                                ws.Send(Json.SerializeToString(new CommandResponse(request.Guid)
                                                         {
                                                             Message = result,
                                                             Success = true
                                                         }));
                                break;
                            case CommandRequestType.UpdateSite:
                                var site = Json.DeserializeFromString<Site>(request.JsonObject);

                                var originalSite = SiteManager.GetSiteByName(request.Value);

                                if (originalSite == null)
                                {
                                    ws.Send(Json.SerializeToString(new CommandResponse(request.Guid) { Message = Json.SerializeToString(new ManageSiteResult { Result = SiteResult.SiteNameNotFound }), Success = false }));
                                    return;
                                }

                                var validationResult = Validators.ValidateSite(site, originalSite);
                                if (validationResult.Errors.Any())
                                {
                                    ws.Send(Json.SerializeToString(new CommandResponse(request.Guid) { Message = Json.SerializeToString(validationResult) }));
                                    return;
                                }

                                site.IisId = originalSite.IisId;

                                var updateResult = SiteManager.UpdateSite(site);

                                ws.Send(Json.SerializeToString(new CommandResponse(request.Guid) { Message = Json.SerializeToString(updateResult), Success = true }));
                                break;
                            case CommandRequestType.GetApplicationPools:
                                var appPools = SiteManager.GetApplicationPools();
                                ws.Send(Json.SerializeToString(new CommandResponse(request.Guid) {Message = Json.SerializeToString(appPools), Success = true}));
                                break;
                            case CommandRequestType.GetCertificates:
                                ws.Send(Json.SerializeToString(new CommandResponse(request.Guid) {Message = Json.SerializeToString(SiteManager.GetCertificates()), Success = true}));
                                break;
                            case CommandRequestType.StartSite:
                                var startSite = SiteManager.GetSiteByName(request.Value);
                                var startResult = SiteManager.StartSite(startSite);
                                ws.Send(Json.SerializeToString(new CommandResponse(request.Guid) { Success = startResult == SiteStartResult.Started, Message = startResult.ToString() }));
                                break;
                            case CommandRequestType.StopSite:
                                var stopSite = SiteManager.GetSiteByName(request.Value);
                                SiteManager.StopSite(stopSite);
                                ws.Send(Json.SerializeToString(new CommandResponse(request.Guid) {  Success = true }));
                                break;
                            case CommandRequestType.RecycleApplicationPool:
                                var recycleSite = SiteManager.GetSiteByName(request.Value);
                                SiteManager.RecycleApplicationPoolBySite(recycleSite.IisId);
                                ws.Send(Json.SerializeToString(new CommandResponse(request.Guid) { Message = "ok", Success = true }));
                                break;
                            case CommandRequestType.RestartSite:
                                var restartSite = SiteManager.GetSiteByName(request.Value);
                                SiteManager.RestartSite(restartSite.IisId);
                                ws.Send(Json.SerializeToString(new CommandResponse(request.Guid) { Message = "ok", Success = true }));
                                break;
                            case CommandRequestType.DeleteSite:
                                var deleteSite = SiteManager.GetSiteByName(request.Value);
                                SiteManager.DeleteSite(deleteSite.IisId);
                                ws.Send(Json.SerializeToString(new CommandResponse(request.Guid) { Message = "ok", Success = true }));
                                break;
                            case CommandRequestType.CreateSite:
                                var createSite = Json.DeserializeFromString<Site>(request.JsonObject);
                                var createResult = SiteManager.CreateSite(createSite);
                                ws.Send(Json.SerializeToString(new CommandResponse(request.Guid) { Message = Json.SerializeToString(createResult), Success = true }));
                                break;
                            case CommandRequestType.ForceUpdate:
                                Servant.Update();
                                ws.Send(Json.SerializeToString(new CommandResponse(request.Guid) { Message = "Started", Success = true }));
                                break;
                            case CommandRequestType.DeploySite:
                                Deployer.Deploy(request.Value, Json.DeserializeFromString<string>(request.JsonObject));
                                ws.Send(Json.SerializeToString(new CommandResponse(request.Guid) { Message = "ok", Success = true }));
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        ws.Send(Json.SerializeToString(new CommandResponse(request.Guid) {Success = false, Message = "ex:" + ex.Message + Environment.NewLine + ex.StackTrace }));
                    }
                };

                ws.OnError += (sender, args) =>
                {
                    var isInternalError = args.Message == "An exception has occurred while receiving a message.";

                    var socket = (WebSocket)sender;

                    MessageHandler.LogException("WS Error: " + args.Message);

                    if (socket.ReadyState == WebSocketState.Open && !isInternalError)
                    {
                        Connect();
                    }
                };

                ws.OnClose += (sender, args) =>
                {
                    MessageHandler.LogException("Lost connection to " + configuration.ServantIoHost);
                    pingTimer.Enabled = false;

                    if (!_isRetrying)
                    {
                        Connect();
                    }
                };

                ws.OnOpen += (sender, args) =>
                    {
                        MessageHandler.Print("Successfully connected to " + configuration.ServantIoHost);
                        pingTimer.Enabled = true;
                    };
                ws.Log.Output = (data, s) =>
                                {
#if DEBUG
                                    MessageHandler.Print(data.Message);
#else
                                    if (data.Level == LogLevel.Error || data.Level == LogLevel.Fatal)
                                    {
                                        MessageHandler.LogException(data.Message);
                                    }
#endif
                                };
                ws.Log.Level = LogLevel.Fatal;

                return ws;
            }
        }
    }
}