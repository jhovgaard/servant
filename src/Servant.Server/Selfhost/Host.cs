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
        public bool Debug { get; set; }
        public DateTime StartupTime { get; set; }
        private static bool _isRunningDbSync;
        private IScheduler Scheduler { get; set; }
        private static Settings _localSettings;

        public Host()
        {
            LogParsingStarted = false;
            StartupTime = DateTime.Now;
        }

        public void Start(Settings settings = null)
        {
            _localSettings = settings;

            if (settings == null)
                LoadSettings();
            
            if (ServantHost == null)
                ServantHost = new NancyHost(new Uri(_localSettings.ServantUrl));
            
            ServantHost.Start();

            if(_localSettings.ParseLogs)
                InitScheduler();

            if(Debug)
                Console.WriteLine("Host started on {0}", _localSettings.ServantUrl);
        }

        public void InitScheduler()
        {
            var factory = new Quartz.Impl.StdSchedulerFactory();

            Scheduler = factory.GetScheduler();
            Scheduler.Start();
        }

        private void UnscheduleJob()
        {
            Scheduler.Clear();
        }

        private void ScheduleJob()
        {
            UnscheduleJob();
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
            if(Debug)
                Console.WriteLine("Host was killed.");
        }

        public void StartLogParsing()
        {
            if(Scheduler == null)
                InitScheduler();

            if(LogParsingStarted)
                throw new Exception("Log parsing already started.");
            
            LogParsingStarted = true;
            ScheduleJob();

            if(_localSettings.Debug)
                Console.WriteLine("Log parsing started.");
        }

        public void StopLogParsing()
        {
            if (_localSettings.Debug)
                Console.WriteLine("Stopping log parsing...");

            LogParsingStarted = false;
            UnscheduleJob();

            if (_localSettings.Debug)
                Console.WriteLine("Log parsing stopped.");
        }

        public void LoadSettings()
        {
            var settingsService = new SettingsService();
            _localSettings = settingsService.LocalSettings;
            Debug = _localSettings.Debug;
        }

        public class SyncDatabaseJob : IJob
        {
            public void Execute(IJobExecutionContext context)
            {
                if (_localSettings.Debug)
                    Console.WriteLine("Executing SyncDatabaseJob");

                if (_isRunningDbSync) return;

                _isRunningDbSync = true;
                Manager.Helpers.EventLogHelper.SyncDatabaseWithServer();
                Manager.Helpers.SynchronizationHelper.SyncServer();
                _isRunningDbSync = false;
            }
        }

    }
}