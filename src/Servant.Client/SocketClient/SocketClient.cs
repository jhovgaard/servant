using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Servant.Business.Objects;
using Servant.Business.Objects.Enums;
using Servant.Client.Infrastructure;
using Servant.Shared;
using Servant.Shared.SocketClient;
using TinyIoC;

namespace Servant.Client.SocketClient
{
    public static class SocketClient
    {
        public static bool IsStopped;

        private static readonly ServantClientConfiguration Configuration = TinyIoCContainer.Current.Resolve<ServantClientConfiguration>();

        private static HubConnection _connection;
        private static IHubProxy _myHub;

        public static void Initialize()
        {
            Connect();
        }

        private static void Connect()
        {
            if (_connection != null)
            {
                _connection.Dispose();
            }

            if (string.IsNullOrWhiteSpace(Configuration.ServantIoKey))
            {
                MessageHandler.Print("Cannot connect without a key.");
                return;
            }
            _connection = new HubConnection(Configuration.ServantIoHost,
                new Dictionary<string, string>()
                {
                    {"installationGuid", Configuration.InstallationGuid.ToString()},
                    {"organizationGuid", Configuration.ServantIoKey},
                    {"servername", Environment.MachineName},
                    {"version", Configuration.Version.ToString()},
                }) {TransportConnectTimeout = TimeSpan.FromSeconds(10)};
            _myHub = _connection.CreateHubProxy("ServantClientHub");

            while (_connection.State != ConnectionState.Connected)
            {
                if (_connection.State == ConnectionState.Connecting)
                {
                    Thread.Sleep(2000);
                }
                else
                {
                    MessageHandler.Print(string.Format("Trying to connect to {0}...", Configuration.ServantIoHost));
                    try
                    {
                        _connection.Start().Wait();
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(2000);
                    }
                }
            }

            _myHub.On<CommandRequest>("Request", request =>
            {
                switch (request.Command)
                {
                    case CommandRequestType.Unauthorized:
                        IsStopped = true;
                        MessageHandler.LogException("Servant.io key was not recognized.");
                        _connection.Stop();
                        break;
                    case CommandRequestType.GetSites:
                        var sites = SiteManager.GetSites();
                        var result = Json.SerializeToString(sites);
                        _myHub.Invoke<CommandResponse>("CommandResponse", new CommandResponse(request.Guid)
                        {
                            Message = result,
                            Success = true
                        });
                        break;
                    case CommandRequestType.UpdateSite:
                        var site = Json.DeserializeFromString<Site>(request.JsonObject);

                        var originalSite = SiteManager.GetSiteByName(request.Value);

                        if (originalSite == null)
                        {
                            _myHub.Invoke<CommandResponse>("CommandResponse",
                                new CommandResponse(request.Guid)
                                {
                                    Message =
                                        Json.SerializeToString(new ManageSiteResult
                                        {
                                            Result = SiteResult.SiteNameNotFound
                                        }),
                                    Success = false
                                });
                            return;
                        }

                        var validationResult = Validators.ValidateSite(site, originalSite);
                        if (validationResult.Errors.Any())
                        {
                            _myHub.Invoke<CommandResponse>("CommandResponse",
                                new CommandResponse(request.Guid) { Message = Json.SerializeToString(validationResult) });
                            return;
                        }

                        site.IisId = originalSite.IisId;

                        var updateResult = SiteManager.UpdateSite(site);

                        _myHub.Invoke<CommandResponse>("CommandResponse",
                            new CommandResponse(request.Guid)
                            {
                                Message = Json.SerializeToString(updateResult),
                                Success = true
                            });
                        break;
                    case CommandRequestType.GetApplicationPools:
                        var appPools = SiteManager.GetApplicationPools();
                        _myHub.Invoke<CommandResponse>("CommandResponse",
                            new CommandResponse(request.Guid)
                            {
                                Message = Json.SerializeToString(appPools),
                                Success = true
                            });
                        break;
                    case CommandRequestType.GetCertificates:
                        _myHub.Invoke<CommandResponse>("CommandResponse",
                            new CommandResponse(request.Guid)
                            {
                                Message = Json.SerializeToString(SiteManager.GetCertificates()),
                                Success = true
                            });
                        break;
                    case CommandRequestType.StartSite:
                        var startSite = SiteManager.GetSiteByName(request.Value);
                        var startResult = SiteManager.StartSite(startSite);
                        _myHub.Invoke<CommandResponse>("CommandResponse",
                            new CommandResponse(request.Guid)
                            {
                                Success = startResult == SiteStartResult.Started,
                                Message = startResult.ToString()
                            });
                        break;
                    case CommandRequestType.StopSite:
                        var stopSite = SiteManager.GetSiteByName(request.Value);
                        SiteManager.StopSite(stopSite);
                        _myHub.Invoke<CommandResponse>("CommandResponse",
                            new CommandResponse(request.Guid) { Success = true });
                        break;
                    case CommandRequestType.RecycleApplicationPool:
                        var recycleSite = SiteManager.GetSiteByName(request.Value);
                        SiteManager.RecycleApplicationPoolBySite(recycleSite.IisId);
                        _myHub.Invoke<CommandResponse>("CommandResponse",
                            new CommandResponse(request.Guid) { Message = "ok", Success = true });
                        break;
                    case CommandRequestType.RestartSite:
                        var restartSite = SiteManager.GetSiteByName(request.Value);
                        SiteManager.RestartSite(restartSite.IisId);
                        _myHub.Invoke<CommandResponse>("CommandResponse",
                            new CommandResponse(request.Guid) { Message = "ok", Success = true });
                        break;
                    case CommandRequestType.DeleteSite:
                        var deleteSite = SiteManager.GetSiteByName(request.Value);
                        SiteManager.DeleteSite(deleteSite.IisId);
                        _myHub.Invoke<CommandResponse>("CommandResponse",
                            new CommandResponse(request.Guid) { Message = "ok", Success = true });
                        break;
                    case CommandRequestType.CreateSite:
                        var createSite = Json.DeserializeFromString<Site>(request.JsonObject);
                        var createResult = SiteManager.CreateSite(createSite);
                        _myHub.Invoke<CommandResponse>("CommandResponse",
                            new CommandResponse(request.Guid)
                            {
                                Message = Json.SerializeToString(createResult),
                                Success = true
                            });
                        break;
                    case CommandRequestType.ForceUpdate:
                        Servant.Update();
                        _myHub.Invoke<CommandResponse>("CommandResponse",
                            new CommandResponse(request.Guid) { Message = "Started", Success = true });
                        break;
                    case CommandRequestType.DeploySite:
                        _myHub.Invoke<CommandResponse>("CommandResponse",
                            new CommandResponse(request.Guid) { Message = "ok", Success = true });
                        Deployer.Deploy(request.Value, Json.DeserializeFromString<string>(request.JsonObject));
                        break;
                }
            });

            _connection.Closed += () =>
            {
                MessageHandler.Print(string.Format("Connection to {0} closed.", Configuration.ServantIoHost));
                Connect();
            };

            _connection.Reconnecting += () => MessageHandler.Print(string.Format("Lost connection to {0}. Reconnecting...", Configuration.ServantIoHost));

            _connection.Error += exception =>
            {
                MessageHandler.LogException(exception.Message + Environment.NewLine + exception.StackTrace);

                try
                {
                    var exceptionUrl = Configuration.ServantIoHost + "/exceptions/log";
                    new WebClient().UploadValues(exceptionUrl, new NameValueCollection()
                    {
                        {"InstallationGuid", Configuration.InstallationGuid.ToString()},
                        {"Message", exception.Message},
                        {"Stacktrace", exception.StackTrace}
                    });
                }
                catch (Exception)
                {
                }

                //Connect();
            };

            MessageHandler.Print(string.Format("Successfully connected to {0}", Configuration.ServantIoHost));
        }
    }
}