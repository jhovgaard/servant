using System;
using System.Timers;
using Nancy.Hosting.Self;
using Servant.Business.Objects;
using Servant.Manager.Helpers;
using Servant.Manager.Infrastructure;

namespace Servant.Server.Selfhost
{
    public class Host : IHost
    {
        private NancyHost ServantHost { get; set; }
        public bool LogParsingStarted { get; set; }
        public bool Debug { get; set; }
        public DateTime StartupTime { get; set; }
        private static Settings _settings;
        private static Timer _timer;

        public Host()
        {
            LogParsingStarted = false;
            StartupTime = DateTime.Now;
            _timer = new Timer(60000);
            _timer.Elapsed += SyncDatabaseJob;
        }

        public void Start(Settings settings = null)
        {
            _settings = settings;

            
            if (settings == null)
            {
                _settings = SettingsHelper.Settings;
                Debug = _settings.Debug;
            }

            if (ServantHost == null)
                ServantHost = new NancyHost(new Uri(_settings.ServantUrl.Replace("*", "localhost")));
            
            ServantHost.Start();

            if(_settings.ParseLogs)
                _timer.Start();

            Console.WriteLine("Host started on {0}", _settings.ServantUrl);
        }

        public void Stop()
        {
            ServantHost.Stop();
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
            
            if(_settings.Debug)
                Console.WriteLine("Log parsing started.");
        }

        public void StopLogParsing()
        {
            if (_settings.Debug)
                Console.WriteLine("Stopping log parsing...");

            LogParsingStarted = false;
            _timer.Stop();

            if (_settings.Debug)
                Console.WriteLine("Log parsing stopped.");
        }

        void SyncDatabaseJob(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            if (_settings.Debug)
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