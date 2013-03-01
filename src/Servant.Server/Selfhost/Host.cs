﻿using System;
using System.Net;
using System.Timers;
using Nancy.Hosting.Self;
using Servant.Business;
using Servant.Business.Objects;
using Servant.Web.Helpers;

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
            {
                var uri = new Uri(_settings.ServantUrl.Replace("*", "localhost"));
                ServantHost = new NancyHost(uri);
                
                try
                {
                    ServantHost.Start();
                }
                catch (HttpListenerException ex) // Tries to start Servant on another port
                {
                    var servantUrl =_settings.ServantUrl.Replace("*", "localhost");
                    var portPosition = servantUrl.LastIndexOf(":");
                    if (portPosition != -1)
                        servantUrl = servantUrl.Substring(0, portPosition);
                    servantUrl += ":54445";

                    var newUri = new Uri(servantUrl);
                    ServantHost = new NancyHost(newUri);
                    ServantHost.Start();

                    _settings.ServantUrl = newUri.ToString();
                    SettingsHelper.UpdateSettings(_settings);
                }
                
            }

            if(_settings.ParseLogs)
                _timer.Start();

            if(Debug)
                Console.WriteLine("Host started on {0}", _settings.ServantUrl);
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

        public void StartLogParsing()
        {
            if(LogParsingStarted)
                throw new Exception("Log parsing already started.");
            
            LogParsingStarted = true;
            _timer.Start();
            
            if(Debug)
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

        public void RemoveCertificateBinding(int port)
        {
            CertificateHandler.RemoveCertificateBinding(port);
        }

        public void AddCertificateBinding(int port)
        {
            CertificateHandler.AddCertificateBinding(port);
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