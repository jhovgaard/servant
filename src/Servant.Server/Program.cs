using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Servant.Manager.Helpers;
using Servant.Manager.Infrastructure;
using Servant.Server.Selfhost;

namespace Servant.Server
{
    class Program
    {

        static void Init()
        {
            Nancy.TinyIoc.TinyIoCContainer.Current.Register<IHost, Host>().AsSingleton();
        }

        static void Main(string[] args)
        {
            Init();

            var settings = SettingsHelper.Settings;
            var host = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<IHost>();

            Console.WriteLine();
            Console.WriteLine("Welcome to Servant for IIS.");
            Console.WriteLine();

            if(!IsAnAdministrator())
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

            host.Start();

            if(settings.ParseLogs)
                host.StartLogParsing();

            Console.WriteLine("You can now manage your server from " + settings.ServantUrl);

            try
            {
                var startupUrl = settings.SetupCompleted ? settings.ServantUrl : settings.ServantUrl + "setup/1/";
                var startInfo = new ProcessStartInfo("explorer.exe", startupUrl);
                Process.Start(startInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not start browser: " + e.Message);
            }

            Console.ReadLine();
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