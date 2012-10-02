using System;
using Nancy.Hosting.Self;
using Servant.Business.Objects;
using Servant.Business.Services;
using Servant.Manager.Infrastructure;

namespace Servant.Server.Selfhost
{
    public class Host : IHost
    {
        public static NancyHost ServantHost;
        public static string Binding;

        public void Start(Settings settings = null)
        {
            if(settings ==null)
            {
                var settingsService = new SettingsService();
                settings = settingsService.LocalSettings;    
            }
            
            if (ServantHost == null)
                ServantHost = new NancyHost(new Uri(settings.GetBinding()));
            
            ServantHost.Start();
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
    }
}