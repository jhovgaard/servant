using System;
using System.Net;
using System.Timers;
using Exceptionless;
using Nancy.Hosting.Self;
using Servant.Business;
using Servant.Business.Objects;
using Servant.Web.Helpers;
using Servant.Web.Infrastructure;

namespace Servant.Server.Selfhost
{
    public class Host : IHost
    {
        private NancyHost ServantHost { get; set; }
        public bool LogParsingStarted { get; set; }
        public bool Debug { get; set; }
        public DateTime StartupTime { get; set; }
        private static Timer _timer;

        public Host()
        {
            LogParsingStarted = false;
            StartupTime = DateTime.Now;
            _timer = new Timer(60000);
            _timer.Elapsed += SyncDatabaseJob;
        }

        public void Start(ServantConfiguration configuration = null)
        {
            if (configuration == null)
            {
                configuration = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<ServantConfiguration>();
                Debug = configuration.Debug;
            }

            if (ServantHost == null)
            {
                var uri = new Uri(configuration.ServantUrl.Replace("*", "localhost"));
                CreateHost(uri);

                //StartLogParsing();
                try
                {
                    ServantHost.Start();
                }
                catch (HttpListenerException) // Tries to start Servant on another port
                {
                    var servantUrl = configuration.ServantUrl.Replace("*", "localhost");
                    var portPosition = servantUrl.LastIndexOf(":");
                    if (portPosition != -1)
                        servantUrl = servantUrl.Substring(0, portPosition);
                    servantUrl += ":54445";

                    var newUri = new Uri(servantUrl);
                    CreateHost(uri);
                    ServantHost.Start();

                    configuration.ServantUrl = newUri.ToString();
                    ConfigurationHelper.UpdateConfiguration(configuration);
                }
                
            }

            if(configuration.EnableErrorMonitoring)
                _timer.Start();

            if(Debug)
                Console.WriteLine("Host started on {0}", configuration.ServantUrl);
        }

        private void CreateHost(Uri uri)
        {
            ServantHost = new NancyHost(uri, new Bootstrapper(), new HostConfiguration { UnhandledExceptionCallback = ex => ex.ToExceptionless().Submit()});
        }

        public void Stop()
        {
            ServantHost.Stop();

            if (Debug)
                Console.WriteLine("Servant was stopped.");
        }

        public void Kill()
        {
            Stop();
            ServantHost = null;
            if(Debug)
                Console.WriteLine("Host was killed.");
        }

        public void StartLogParsing()
        {
            if(LogParsingStarted)
                throw new Exception("Log parsing already started.");
            
            LogParsingStarted = true;
            _timer.Start();
            
            if(Debug)
                Console.WriteLine("Log parsing started.");
        }

        public void StopLogParsing()
        {
            var configuration = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<ServantConfiguration>();

            if (configuration.Debug)
                Console.WriteLine("Stopping log parsing...");

            LogParsingStarted = false;
            _timer.Stop();

            if (configuration.Debug)
                Console.WriteLine("Log parsing stopped.");
        }

        public void RemoveCertificateBinding(int port)
        {
            CertificateHandler.RemoveCertificateBinding(port);
        }

        public void AddCertificateBinding(int port)
        {
            // Ensure Certificate is installed
            if (!Program.IsServantCertificateInstalled())
            {
                Program.InstallServantCertificate();
            }

            CertificateHandler.AddCertificateBinding(port);
        }

        public void StartWebSocket()
        {
            SocketClient.SocketClient.IsStopped = false;
            SocketClient.SocketClient.Connect();
        }

        void SyncDatabaseJob(object sender, ElapsedEventArgs e)
        {
            var configuration = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<ServantConfiguration>();
            _timer.Stop();
            if (configuration.Debug)
                Console.WriteLine("Started SyncDatabaseJob (IsRunning: {0})", LogParsingStarted);
           
            try
            {
                //Manager.Helpers.EventLogHelper.SyncServer();
                //Manager.Helpers.SynchronizationHelper.SyncServer();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error on SyncDatabaseJob ({0}: {1}", ex.GetType(), ex.Message);
            }

            if (LogParsingStarted)
                _timer.Start();
        }
    }
}