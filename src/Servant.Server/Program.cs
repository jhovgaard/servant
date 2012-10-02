using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using Quartz;
using Servant.Business.Services;
using Servant.Manager.Infrastructure;
using Servant.Server.Selfhost;

namespace Servant.Server
{
    class Program
    {
        private static bool _isRunningDbSync;

        static void Init()
        {
            TinyIoC.TinyIoCContainer.Current.Register<IHost, Host>();
        }


        static void Main(string[] args)
        {
            Init();

            var settings = new SettingsService().LocalSettings;
            var binding = settings.GetBinding();
            var host = TinyIoC.TinyIoCContainer.Current.Resolve<IHost>();

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
            
            host.Start();
            
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


            var factory = new Quartz.Impl.StdSchedulerFactory();
            var sched = factory.GetScheduler();
            sched.Start();

            var job = JobBuilder
                .Create<SyncDatabaseJob>()
                .WithIdentity("SyncDatabaseJob", null)
                .Build();

            var trigger = TriggerBuilder
                .Create()
                .WithIdentity("SyncDatabaseTrigger", null)
                .WithSimpleSchedule(x => x.WithIntervalInMinutes(5))
                .StartNow()
                .Build();

            sched.ScheduleJob(job, trigger);
        }

        public class SyncDatabaseJob : IJob
        {
            public void Execute(IJobExecutionContext context)
            {
                if (_isRunningDbSync) return;

                _isRunningDbSync = true;
                Manager.Helpers.SynchronizationHelper.SyncServer();
                Manager.Helpers.EventLogHelper.SyncDatabaseWithServer();
                _isRunningDbSync = false;
            }
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
