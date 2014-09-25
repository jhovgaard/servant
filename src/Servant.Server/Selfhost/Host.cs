using System;
using System.Diagnostics;
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
        public bool Debug { get; set; }
        public DateTime StartupTime { get; set; }
        private static Timer _timer;

        public Host()
        {
            StartupTime = DateTime.Now;
            _timer = new Timer(3600000);
            _timer.Elapsed += (sender, args) => Update();
            _timer.Start();
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

        public void Update()
        {
            if (Debug)
                Console.WriteLine("Checking for updates...");

            var path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var update = new Process() { StartInfo = new ProcessStartInfo(System.IO.Path.Combine(path, "Servant.Updater.exe")) { UseShellExecute = false, CreateNoWindow = true } };
            if (Environment.OSVersion.Version.Major >= 6)
            {
                update.StartInfo.Verb = "runas";
            }
            update.Start();
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
            new System.Threading.Thread(SocketClient.SocketClient.Connect).Start();
        }
    }
}