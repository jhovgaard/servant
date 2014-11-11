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
        static ServantClientConfiguration configuration = TinyIoCContainer.Current.Resolve<ServantClientConfiguration>();

        static HubConnection connection = new HubConnection("http://localhost:51652/",
                new Dictionary<string, string>() {  
                    {"installationGuid", configuration.InstallationGuid.ToString()},
                    {"organizationGuid", configuration.ServantIoKey},
                    {"servername", Environment.MachineName},
                    {"version", configuration.Version.ToString()},
                }) { TransportConnectTimeout = TimeSpan.FromSeconds(10)};

        private static void Connect()
        {
            if (string.IsNullOrWhiteSpace(configuration.ServantIoKey))
            {
                Console.WriteLine("Cannot connect without a key.");
                return;
            }

            connection.Start(new WebSocketTransport()).Wait(new TimeSpan(0, 0, 5));

            if (connection.State != ConnectionState.Connected)
            {
                System.Threading.Thread.Sleep(2000);
                connection.Stop(TimeSpan.FromSeconds(0)); // Trigger nyt kald til Connect()
            }
            else
            {
                Console.WriteLine("Successfully connected to Servant.io.");    
            }
        }

        public static void Initialize()
        {
            var myHub = connection.CreateHubProxy("ServantClientHub");

            Connect();

            connection.Closed += () => {
                Console.WriteLine("Connection Closed...");
                Connect();
            };
            
            connection.Reconnecting += () =>
            {
                Console.WriteLine("Reconnecting...");
                //connection.Stop();
            };

            connection.Error += exception =>
                                {
                                    try
                                    {
                                        var wc = new WebClient();
                                        var hostname = configuration.ServantIoHost.Substring(0, configuration.ServantIoHost.IndexOf(":"));

                                        var exceptionUrl = "https://" + hostname + "/exceptions/log";
#if(DEBUG)
                                        exceptionUrl = "http://localhost:51652/exceptions/log";
#endif
                                        wc.UploadValues(exceptionUrl, new NameValueCollection() {
                                                                      {"InstallationGuid", configuration.InstallationGuid.ToString() },
                                                                      {"Message", exception.Message},
                                                                      {"Stacktrace", exception.StackTrace}
                                                                  });

                                    }
                                    catch (Exception)
                                    {
                                    }
                                };

            
            myHub.On<CommandRequest>("Request", request =>
            {
                switch (request.Command)
                {
                    case CommandRequestType.Unauthorized:
                        IsStopped = true;
                        MessageHandler.LogException("Servant.io key was not recognized.");
                        connection.Stop();
                        break;
                    case CommandRequestType.GetSites:
                        var sites = SiteManager.GetSites();
                        var result = Json.SerializeToString(sites);
                        myHub.Invoke<CommandResponse>("CommandResponse", new CommandResponse(request.Guid)
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
                            myHub.Invoke<CommandResponse>("CommandResponse", new CommandResponse(request.Guid) { Message = Json.SerializeToString(new ManageSiteResult { Result = SiteResult.SiteNameNotFound }), Success = false });
                            return;
                        }

                        var validationResult = Validators.ValidateSite(site, originalSite);
                        if (validationResult.Errors.Any())
                        {
                            myHub.Invoke<CommandResponse>("CommandResponse", new CommandResponse(request.Guid) { Message = Json.SerializeToString(validationResult) });
                            return;
                        }

                        site.IisId = originalSite.IisId;

                        var updateResult = SiteManager.UpdateSite(site);

                        myHub.Invoke<CommandResponse>("CommandResponse", new CommandResponse(request.Guid) { Message = Json.SerializeToString(updateResult), Success = true });
                        break;
                    case CommandRequestType.GetApplicationPools:
                        var appPools = SiteManager.GetApplicationPools();
                        myHub.Invoke<CommandResponse>("CommandResponse", new CommandResponse(request.Guid) { Message = Json.SerializeToString(appPools), Success = true });
                        break;
                    case CommandRequestType.GetCertificates:
                        myHub.Invoke<CommandResponse>("CommandResponse", new CommandResponse(request.Guid) { Message = Json.SerializeToString(SiteManager.GetCertificates()), Success = true });
                        break;
                    case CommandRequestType.StartSite:
                        var startSite = SiteManager.GetSiteByName(request.Value);
                        var startResult = SiteManager.StartSite(startSite);
                        myHub.Invoke<CommandResponse>("CommandResponse", new CommandResponse(request.Guid) { Success = startResult == SiteStartResult.Started, Message = startResult.ToString() });
                        break;
                    case CommandRequestType.StopSite:
                        var stopSite = SiteManager.GetSiteByName(request.Value);
                        SiteManager.StopSite(stopSite);
                        myHub.Invoke<CommandResponse>("CommandResponse", new CommandResponse(request.Guid) { Success = true });
                        break;
                    case CommandRequestType.RecycleApplicationPool:
                        var recycleSite = SiteManager.GetSiteByName(request.Value);
                        SiteManager.RecycleApplicationPoolBySite(recycleSite.IisId);
                        myHub.Invoke<CommandResponse>("CommandResponse", new CommandResponse(request.Guid) { Message = "ok", Success = true });
                        break;
                    case CommandRequestType.RestartSite:
                        var restartSite = SiteManager.GetSiteByName(request.Value);
                        SiteManager.RestartSite(restartSite.IisId);
                        myHub.Invoke<CommandResponse>("CommandResponse", new CommandResponse(request.Guid) { Message = "ok", Success = true });
                        break;
                    case CommandRequestType.DeleteSite:
                        var deleteSite = SiteManager.GetSiteByName(request.Value);
                        SiteManager.DeleteSite(deleteSite.IisId);
                        myHub.Invoke<CommandResponse>("CommandResponse", new CommandResponse(request.Guid) { Message = "ok", Success = true });
                        break;
                    case CommandRequestType.CreateSite:
                        var createSite = Json.DeserializeFromString<Site>(request.JsonObject);
                        var createResult = SiteManager.CreateSite(createSite);
                        myHub.Invoke<CommandResponse>("CommandResponse", new CommandResponse(request.Guid) { Message = Json.SerializeToString(createResult), Success = true });
                        break;
                    case CommandRequestType.ForceUpdate:
                        Servant.Update();
                        myHub.Invoke<CommandResponse>("CommandResponse", new CommandResponse(request.Guid) { Message = "Started", Success = true });
                        break;
                    case CommandRequestType.DeploySite:
                        myHub.Invoke<CommandResponse>("CommandResponse", new CommandResponse(request.Guid) { Message = "ok", Success = true });
                        Deployer.Deploy(request.Value, Json.DeserializeFromString<string>(request.JsonObject));
                        break;
                }
            });
        }
    }
}