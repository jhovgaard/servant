using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using Servant.Business.Services;
using Servant.Server.Selfhost;

namespace Servant.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            var settings = new SettingsService().LocalSettings;
            var binding = settings.GetBinding();

            Console.WriteLine(settings.Debug);

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

            RegisterLogParser();
            
            Host.Start();
            
            Console.WriteLine("You can now manage your server from " + binding);

            try
            {
                var startInfo = new ProcessStartInfo("explorer.exe", binding);
                Process.Start(startInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not start browser: " + e.Message);
            }

            Servant.Manager.Helpers.SynchronizationHelper.SyncServer();
        }

        public static bool IsAnAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void RegisterLogParser()
        {
            //'/s' : indicates regsvr32.exe to run silently.
            var dllPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logparser.dll");
            var fileinfo = "/s \"" + dllPath + "\"";

            var reg = new Process {
                StartInfo = {
                        FileName = "regsvr32.exe",
                        Arguments = fileinfo,
                        UseShellExecute = false,
                        CreateNoWindow = false,
                        RedirectStandardOutput = true,
                        WindowStyle = ProcessWindowStyle.Normal
                    }
            };
            reg.Start();
            reg.WaitForExit();
            reg.Close();
        }
    }
}
