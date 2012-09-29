using System;
using Nancy.Hosting.Self;
using Servant.Business.Objects;
using Servant.Business.Services;

namespace Servant.Server.Selfhost
{
    public static class Host
    {
        public static NancyHost ServantHost;
        public static string Binding;

        public static void Start(Settings settings = null)
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

        public static void Stop()
        {
            ServantHost.Stop();
        }

        public static void Kill()
        {
            Stop();
            ServantHost = null;
        }
    }
}