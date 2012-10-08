using System;
using Nancy.Hosting.Self;
using Quartz;
using Servant.Business.Objects;
using Servant.Business.Services;
using Servant.Manager.Infrastructure;

namespace Servant.Server.Selfhost
{
    public class Host : IHost
    {
        private NancyHost ServantHost { get; set; }
        public bool LogParsingStarted { get; set; }
        public DateTime StartupTime { get; set; }
        private static bool _isRunningDbSync;
        private IScheduler Scheduler { get; set; }

        public Host()
        {
            LogParsingStarted = false;
            StartupTime = DateTime.Now;
        }

        public void Start(Settings settings = null)
        {
            if(settings == null)
            {
                var settingsService = new SettingsService();
                settings = settingsService.LocalSettings;    
            }
            
            if (ServantHost == null)
                ServantHost = new NancyHost(new Uri(settings.ServantUrl));
            
            ServantHost.Start();

            if(settings.ParseLogs)
                InitScheduler();
        }

        public void InitScheduler()
        {
            var factory = new Quartz.Impl.StdSchedulerFactory();

            Scheduler = factory.GetScheduler();
            Scheduler.Start();
        }

        private void UnscheduleJob()
        {
            Scheduler.UnscheduleJob(new TriggerKey("SyncDatabaseTrigger"));
        }

        private void ScheduleJob()
        {
            var job = JobBuilder
                .Create<SyncDatabaseJob>()
                .WithIdentity("SyncDatabaseJob", null)
                .RequestRecovery(false)
                .Build();

            var trigger = TriggerBuilder
                .Create()
                .WithIdentity("SyncDatabaseTrigger", null)
                .ForJob("SyncDatabaseJob")
                .WithSchedule(DailyTimeIntervalScheduleBuilder.Create().WithIntervalInSeconds(60))
                .StartNow()
                .Build();

            Scheduler.ScheduleJob(job, trigger);
            Scheduler.TriggerJob(new JobKey("SyncDatabaseJob"));
        }

        public void Stop()
        {
            ServantHost.Stop();
        }

        public void Kill()
        {
            Stop();
            ServantHost = null;
        }

        public void StartLogParsing()
        {
            if(Scheduler == null)
                InitScheduler();

            if(LogParsingStarted)
                throw new Exception("Log parsing already started.");
            
            LogParsingStarted = true;
            ScheduleJob();

            Console.WriteLine("Log parsing started.");
        }

        public void StopLogParsing()
        {
            Console.WriteLine("Stopping log parsing...");
            LogParsingStarted = false;
            UnscheduleJob();
            Console.WriteLine("Log parsing stopped.");
        }

        public class SyncDatabaseJob : IJob
        {
            public void Execute(IJobExecutionContext context)
            {
                Console.WriteLine("Executing SyncDatabaseJob");
                if (_isRunningDbSync) return;

                _isRunningDbSync = true;
                Manager.Helpers.SynchronizationHelper.SyncServer();
                Manager.Helpers.EventLogHelper.SyncDatabaseWithServer();
                _isRunningDbSync = false;
            }
        }

    }
}