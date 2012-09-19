using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using Servant.Server.Selfhost;

namespace Servant.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Welcome to Servant for IIS.");
            System.Console.WriteLine();

            if(!IsAnAdministrator())
            {
                System.Console.ForegroundColor = ConsoleColor.Red;
                System.Console.Write("Error: ");
                System.Console.ResetColor();
                System.Console.Write("Servant needs to run as administrator to access IIS.");
                System.Console.WriteLine();
                System.Console.ForegroundColor = ConsoleColor.DarkYellow;
                System.Console.Write("Solution: ");
                System.Console.ResetColor();
                System.Console.Write("Right click Servant.Server.exe and select 'Run as administrator'.");
                System.Console.WriteLine();
                System.Console.WriteLine();
                System.Console.WriteLine("Press any key to exit...");
                System.Console.ReadLine();
                
                return;
            }

            var binding = new UriBuilder("http", (Environment.MachineName), 1234).Uri.ToString();
            
            Host.Start(binding);
            
            System.Console.WriteLine("Parsing IIS log files. Please hang on, this is heavy first time.");
            var sw = new Stopwatch();

            sw.Start();
            Helpers.RequestLogHelper.SyncDatabaseWithServer();
            sw.Stop();
            System.Console.WriteLine("Done in {0} seconds.", sw.Elapsed.Seconds);
            System.Console.WriteLine("Parsing event logs, almost done.");
            sw.Reset();
            sw.Start();
            Helpers.EventLogHelper.SyncDatabaseWithServer();
            sw.Start();
            System.Console.WriteLine("Done in {0} seconds.", sw.Elapsed.Seconds);
            System.Console.WriteLine();
            System.Console.WriteLine("You can now manage your server from " + binding);

            try
            {
                var startInfo = new ProcessStartInfo("explorer.exe", binding);
                Process.Start(startInfo);
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Could not start browser: " + e.Message);
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Press any key to exit...");
            System.Console.ReadLine();
        }

        public static bool IsAnAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
