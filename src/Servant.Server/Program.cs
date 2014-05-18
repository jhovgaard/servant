using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceProcess;
using Nancy.TinyIoc;
using Pluralsight.Crypto;
using Servant.Business;
using Servant.Business.Objects;
using Servant.Server.Selfhost;
using Servant.Web.Helpers;
using Servant.Server.WindowsService;

namespace Servant.Server
{
    static class Program
    {
        private static ServantConfiguration Configuration { get; set; }

        static void Init()
        {
            Nancy.TinyIoc.TinyIoCContainer.Current.Register<IHost, Selfhost.Host>().AsSingleton();
            TinyIoCContainer.Current.Register<ServantConfiguration>(ConfigurationHelper.GetConfigurationFromDisk());
        }

        public static Assembly Resolver(object sender, ResolveEventArgs args)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            AssemblyName assemblyName = new AssemblyName(args.Name);

            string path = assemblyName.Name + ".dll";

            if (assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture) == false)
            {
                path = String.Format(@"{0}\{1}", assemblyName.CultureInfo, path);
            }
            path = "Servant.Server.Resources." + path;

            Console.WriteLine("Trying to resolve: " + path);
            using (Stream stream = executingAssembly.GetManifestResourceStream(path))
            {
                if (stream == null)
                    return null;

                byte[] assemblyRawBytes = new byte[stream.Length];
                stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
                Console.WriteLine("Resolved: " + path);
                return Assembly.Load(assemblyRawBytes);
            }
        }

        public static void InstallServantCertificate()
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            //CRASH!
                // Servant certifikatet kan ikke bindes til Azure serveren, ved mindre det bliver eksporteret og importeret først. Den siger det der med local user blablal.. 

            X509Certificate2 cert;
            using (var ctx = new CryptContext())
            {
                ctx.Open();
                cert = ctx.CreateSelfSignedCertificate(
                    new SelfSignedCertProperties
                    {
                        IsPrivateKeyExportable = true,
                        KeyBitLength = 4096,
                        Name = new X500DistinguishedName("CN=\"Servant\"; C=\"Denmark\"; O=\"Denmark\"; OU=\"Denmark\";"),
                        ValidFrom = DateTime.Today,
                        ValidTo = DateTime.Today.AddYears(10)
                    });
            }
            cert.FriendlyName = "Servant";
            store.Add(cert);
            store.Close();

            System.Threading.Thread.Sleep(1000); // Wait for certificate to be installed
        }
        
        public static bool IsServantCertificateInstalled()
        {
            var certificates = SiteManager.GetCertificates();
            return certificates.Any(x => x.Name == "Servant");
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += Resolver;
            Init();

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

            Configuration = TinyIoCContainer.Current.Resolve<ServantConfiguration>();

            if (Configuration.IsHttps())
            {
                if (!IsServantCertificateInstalled())
                    InstallServantCertificate();

                var servantPort = new Uri(Configuration.ServantUrl).Port;
                if (!CertificateHandler.IsCertificateBound(servantPort))
                {
                    CertificateHandler.AddCertificateBinding(servantPort);
                }
            }

            switch (command)
            {
                case "install":
                    if (IsAlreadyInstalled())
                    {
                        Console.WriteLine("Servant is already installed. Use /uninstall to uninstall.");
                        Console.ReadLine();
                        return;
                    }
                    const string servantServiceName = "Servant for IIS";
                    
                    var existingServantService = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == servantServiceName);
                    if (existingServantService != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Servant is already running on this machine.");
                        Console.ReadLine();
                        return;
                    }
                    ManagedInstallerClass.InstallHelper(new[] { "/LogToConsole=false", Assembly.GetExecutingAssembly().Location });
                    var startController = new ServiceController("Servant for IIS");
                    startController.Start();
                    StartBrowser();
                    Console.WriteLine("Servant was successfully installed. Please complete the installation from your browser on " + Configuration.ServantUrl);
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
                        Console.WriteLine("You can now manage your server from " + Configuration.ServantUrl);
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
            var host = TinyIoCContainer.Current.Resolve<IHost>();
            host.Start();
        }

        public static void StartBrowser()
        {
            try
            {
                var startupUrl = Configuration.SetupCompleted ? Configuration.ServantUrl : Configuration.ServantUrl + "setup/1/";
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