using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceProcess;
using Servant.Business.Objects;
using Servant.Manager.Helpers;
using Servant.Manager.Infrastructure;
using Servant.Server.Selfhost;
using Servant.Server.WindowsService;

namespace Servant.Server
{
    class Program
    {
        private static Settings Settings { get; set; }
        static void Init()
        {
            Nancy.TinyIoc.TinyIoCContainer.Current.Register<IHost, Host>().AsSingleton();
        }

        private static void InstallServantCertificate()
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            var certificatePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "servant.pfx");
            var cert = new X509Certificate2(certificatePath, "myservantpass994") { FriendlyName = "Servant"};
            store.Add(cert);
            store.Close();
        }

        private static bool IsServantCertificateInstalled()
        {
            var certificates = SiteManager.GetCertificates();
            return certificates.Any(x => x.Thumbprint == "8D2673EE6B9076E3C96299048A5032FA401E01C4");
        }

        static void Main(string[] args)
        {
            Init();

            if (!IsServantCertificateInstalled())
                InstallServantCertificate();   

            Settings = SettingsHelper.Settings;

            if (!IsAnAdministrator())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("Error: ");
                Console.ResetColor();
                Console.Write("Servant needs to run as administrator to access IIS.");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("Solution: ");
                Console.ResetColor();
                Console.Write("Right click Servant.Server.exe and select 'Run as administrator'.");
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            if (!IsIisInstalled())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("Error: ");
                Console.ResetColor();
                Console.Write("IIS needs to be installed to use Servant.");
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

                var command = args.FirstOrDefault() ?? "";

            switch (command)
            {
                case "install":
                    if (IsAlreadyInstalled())
                    {
                        Console.WriteLine("Servant is already installed. Use /uninstall to uninstall.");
                        Console.ReadLine();
                        return;
                    }
                    ManagedInstallerClass.InstallHelper(new[] {"/LogToConsole=false", Assembly.GetExecutingAssembly().Location });
                    var startController = new ServiceController("Servant for IIS");
                    startController.Start();
                    StartBrowser();
                    Console.WriteLine("Servant was successfully installed. Please complete the installation from your browser on " + Settings.ServantUrl);
                    break;

                case "uninstall":
                    Console.WriteLine();
                    Console.WriteLine("Trying to uninstall the Servant service...");
                    try
                    {
                        ManagedInstallerClass.InstallHelper(new[] { "/u", "/LogToConsole=false", Assembly.GetExecutingAssembly().Location });
                        Console.WriteLine("The service was successfully uninstalled.");
                    }
                    catch (Exception)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("An error occurred while trying to uninstall Servant.");
                        Console.ResetColor();
                    }

                    break;
                default:
                    if(Environment.UserInteractive || (args != null && args.Length > 0))
                    {
                        Console.WriteLine();
                        Console.WriteLine("Welcome to Servant for IIS.");
                        Console.WriteLine();

                        StartServant();
                        Console.WriteLine("You can now manage your server from " + Settings.ServantUrl);
                        StartBrowser();
                        while (true)
                            Console.ReadLine();
                    }
                    
                    ServiceBase.Run(new ServantService());
                    break;

            }
        }
        
        public static bool IsAlreadyInstalled()
        {
            var startController = new ServiceController("Servant for IIS");
            return startController.Container != null;
        }

        public static void StartServant()
        {
            var host = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<IHost>();
            host.Start();
        }

        public static void StartBrowser()
        {
            try
            {
                var startupUrl = Settings.SetupCompleted ? Settings.ServantUrl : Settings.ServantUrl + "setup/1/";
                var startInfo = new ProcessStartInfo("explorer.exe", startupUrl);
                Process.Start(startInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not start browser: " + e.Message);
            }
        }

        public static bool IsAnAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static bool IsIisInstalled()
        {
            using(var manager = new Microsoft.Web.Administration.ServerManager()) {
                try
                {
                    var test = manager.Sites.First();
                    return true;
                }
                catch (COMException)
                {
                    return false;
                }
            }
        }
    }
} 