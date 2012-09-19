using System;
using Nancy.Hosting.Self;

namespace Servant.Server.Selfhost
{
    public static class Host
    {
        public static NancyHost ServantHost;
        
         public static void Start(string binding)
         {
             if(ServantHost == null)
                ServantHost = new NancyHost(new Uri(binding));
             ServantHost.Start();
         }

        public static void Stop()
        {
            ServantHost.Stop();
        }
    }
}