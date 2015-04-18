using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Servant.Agent.Infrastructure;
using Servant.Shared;
using Servant.Shared.Communication;
using Servant.Shared.Helpers;
using Servant.Shared.Objects;
using Servant.Shared.Objects.Enums;
using TinyIoC;

namespace Servant.Agent
{
    public static class SocketClient
    {
        public static bool IsStopped;

        private static readonly ServantAgentConfiguration Configuration = TinyIoCContainer.Current.Resolve<ServantAgentConfiguration>();

        private static HubConnection _connection;
        private static IHubProxy _myHub;

        public static void Initialize()
        {
            Connect();
        }

        public static void ReplyOverHttp(CommandResponse response)
        {
            var url = Configuration.ServantIoHost + "/client/response?installationGuid=" + Configuration.InstallationGuid + "&organizationId=" + Configuration.ServantIoKey;
            var wc = new WebClient();

            var result = wc.UploadValues(url, new NameValueCollection()
            {
                {"Message", response.Message},
                {"Guid", response.Guid.ToString()},
                {"Success", response.Success.ToString()},
                {"Type", response.Type.ToString()}
            });
        }

        private static void InitializeConnection()
        {
            _connection = new HubConnection(Configuration.ServantIoHost,
                new Dictionary<string, string>()
                {
                    {"installationGuid", Configuration.InstallationGuid.ToString()},
                    {"organizationGuid", Configuration.ServantIoKey},
                    {"servername", Environment.MachineName},
                    {"version", Configuration.Version.ToString()},
                });
            
            
            _myHub = _connection.CreateHubProxy("ServantClientHub");

            _myHub.On<CommandRequest>("Request", request =>
            {
                var deployer = TinyIoCContainer.Current.Resolve<Deployer>();

                try
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
                            ReplyOverHttp(new CommandResponse(request.Guid)
                            {
                                Message = result,
                                Success = true
                            });
                            break;
                        case CommandRequestType.WipeIisConfiguration:
                            SiteManager.WipeIisConfiguration();
                            ReplyOverHttp(new CommandResponse(request.Guid)
                            {
                                Message = "Wipe completed",
                                Success = true
                            });
                            break;
                        case CommandRequestType.UpdateSite:
                            var site = Json.DeserializeFromString<IisSite>(request.JsonObject);

                            var originalSite = SiteManager.GetSiteByName(request.Value);

                            if (originalSite == null)
                            {
                                ReplyOverHttp(new CommandResponse(request.Guid)
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
                                ReplyOverHttp(new CommandResponse(request.Guid) { Message = Json.SerializeToString(validationResult) });
                                return;
                            }

                            site.IisId = originalSite.IisId;

                            var updateResult = SiteManager.UpdateSite(site);

                            ReplyOverHttp(new CommandResponse(request.Guid)
                                {
                                    Message = Json.SerializeToString(updateResult),
                                    Success = true
                                });
                            break;
                        case CommandRequestType.GetAll:
                            ReplyOverHttp(new CommandResponse(request.Guid)
                                {
                                    Message = Json.SerializeToString(new AllResponse
                                    {
                                        Sites = SiteManager.GetSites().ToList(),
                                        FrameworkVersions = NetFrameworkHelper.GetAllVersions().ToList(),
                                        ApplicationPools = SiteManager.GetApplicationPools(),
                                        Certificates = SiteManager.GetCertificates().ToList(),
                                        DefaultApplicationPool = SiteManager.GetDefaultApplicationPool(),
                                        InstalledModules = SiteManager.GetModules().OrderBy(x => x).ToList()
                                    }),
                                    Success = true
                                });
                            break;
                        case CommandRequestType.GetApplicationPools:
                            var appPools = SiteManager.GetApplicationPools();
                            ReplyOverHttp(new CommandResponse(request.Guid)
                                {
                                    Message = Json.SerializeToString(appPools),
                                    Success = true
                                });
                            break;
                        case CommandRequestType.GetCertificates:
                            ReplyOverHttp(new CommandResponse(request.Guid)
                                {
                                    Message = Json.SerializeToString(SiteManager.GetCertificates()),
                                    Success = true
                                });
                            break;
                        case CommandRequestType.StartSite:
                            var startSite = SiteManager.GetSiteByName(request.Value);
                            var startResult = SiteManager.StartSite(startSite);

                            ReplyOverHttp(new CommandResponse(request.Guid)
                                {
                                    Success = startResult == SiteStartResult.Started,
                                    Message = Json.SerializeToString(startResult)
                                });
                            break;
                        case CommandRequestType.StopSite:
                            var stopSite = SiteManager.GetSiteByName(request.Value);
                            SiteManager.StopSite(stopSite);
                            ReplyOverHttp(new CommandResponse(request.Guid) { Success = true });
                            break;
                        case CommandRequestType.RestartSite:
                            var restartSite = SiteManager.GetSiteByName(request.Value);
                            SiteManager.RestartSite(restartSite.IisId);
                            ReplyOverHttp(new CommandResponse(request.Guid) { Message = "ok", Success = true });
                            break;
                        case CommandRequestType.DeleteSite:
                            var deleteSite = SiteManager.GetSiteByName(request.Value);
                            SiteManager.DeleteSite(deleteSite.IisId);
                            ReplyOverHttp(new CommandResponse(request.Guid) { Message = "ok", Success = true });
                            break;
                        case CommandRequestType.CreateSite:
                            var createSite = Json.DeserializeFromString<IisSite>(request.JsonObject);
                            var createResult = SiteManager.CreateSite(createSite);
                            ReplyOverHttp(new CommandResponse(request.Guid)
                                {
                                    Message = Json.SerializeToString(createResult),
                                    Success = true
                                });
                            break;
                        case CommandRequestType.ForceUpdate:
                            Servant.Update();
                            ReplyOverHttp(new CommandResponse(request.Guid) { Message = "Started", Success = true });
                            break;
                        case CommandRequestType.DeploySite:
                            Task.Factory.StartNew(() => deployer.Deploy(Json.DeserializeFromString<Deployment>(request.JsonObject)));
                            break;
                        case CommandRequestType.RollbackDeployment:
                            Task.Factory.StartNew(() => deployer.Rollback(int.Parse(request.Value)));
                            break;
                        case CommandRequestType.CmdExeCommand:
                            if (!Configuration.DisableConsoleAccess)
                            {
                                var manager = TinyIoCContainer.Current.Resolve<ConsoleManager>();
                                manager.SendCommand(request.Value);
                            }
                            break;
                        case CommandRequestType.UpdateApplicationPool:
                            var applicationPool = Json.DeserializeFromString<ApplicationPool>(request.JsonObject);
                            var originalName = request.Value;
                            SiteManager.UpdateApplicationPool(originalName, applicationPool);
                            ReplyOverHttp(new CommandResponse(request.Guid) { Success = true });
                            break;
                        case CommandRequestType.StartApplicationPool:
                            SiteManager.StartApplicationPool(request.Value);
                            ReplyOverHttp(new CommandResponse(request.Guid) { Success = true });
                            break;
                        case CommandRequestType.StopApplicationPool:
                            SiteManager.StopApplicationPool(request.Value);
                            ReplyOverHttp(new CommandResponse(request.Guid) { Success = true });
                            break;
                        case CommandRequestType.RecycleApplicationPool:
                            SiteManager.RecycleApplicationPool(request.Value);
                            ReplyOverHttp(new CommandResponse(request.Guid) { Message = "ok", Success = true });
                            break;
                        case CommandRequestType.DeleteApplicationPool:
                            SiteManager.DeleteApplicationPool(request.Value);
                            ReplyOverHttp(new CommandResponse(request.Guid) { Message = "ok", Success = true });
                            break;
                        case CommandRequestType.CreateApplicationPool:
                            var applicationPoolToCreate = Json.DeserializeFromString<ApplicationPool>(request.JsonObject);
                            SiteManager.CreateApplicationPool(applicationPoolToCreate);
                            ReplyOverHttp(new CommandResponse(request.Guid) { Message = "ok", Success = true });
                            break;
                    }
                }
                catch (Exception exception)
                {
                    MessageHandler.LogException(exception);
                }
            });

            _connection.StateChanged += change =>
            {
                MessageHandler.Print("State changed to: " + change.NewState);
                switch (change.NewState)
                {
                    case ConnectionState.Disconnected:
                        _connection.Stop(new TimeSpan(0));
                        System.Threading.Thread.Sleep(2000);
                        Connect();
                        break;
                    case ConnectionState.Connected:
                        SendServerInfo(Configuration);
                        break;
                }
            };

            _connection.Error += (e) =>
            {
                MessageHandler.LogException(e);
                _connection.Dispose();
            };
        }

        private static void SendServerInfo(ServantAgentConfiguration configuration)
        {
            ReplyOverHttp(new CommandResponse(CommandResponse.ResponseType.ServerInfo) { Message = Json.SerializeToString(
                new ServerInfo()
                {
                    ServantVersion = configuration.Version,
                    ServerName = Environment.MachineName,
                    OperatingSystem = OperatingSystemHelper.GetOsVersion(),
                    TotalSites = SiteManager.TotalSites,
                    TotalApplicationPools = SiteManager.TotalApplicationPools
                }
                ), Success = true });
        }

        private static void Connect()
        {
            if (string.IsNullOrWhiteSpace(Configuration.ServantIoKey))
            {
                MessageHandler.Print("Cannot connect without a key.");
                return;
            }

            MessageHandler.Print("Connecting...");
            InitializeConnection();
            try
            {
                _connection.Start().Wait(TimeSpan.FromSeconds(5));
                if(_connection.State != ConnectionState.Connected)
                    _connection.Stop(new TimeSpan(0));
            }
            catch (WebException e)
            {
                MessageHandler.LogException(e);
            }
            catch (AggregateException e)
            {
                foreach (var ex in e.InnerExceptions)
                {
                    MessageHandler.LogException(ex);
                }
            }
        }
    }
}