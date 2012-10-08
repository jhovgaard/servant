using System;
using Servant.Business.Objects;
using Servant.Manager.Infrastructure;

namespace Servant.Aspnet
{
    public class Host  : IHost
    {
        public bool LogParsingStarted { get; set; }
        public DateTime StartupTime { get; set; }

        public void Start(Settings settings = null)
        {
            throw new System.NotImplementedException();
        }

        public void Stop()
        {
            throw new System.NotImplementedException();
        }

        public void Kill()
        {
            throw new System.NotImplementedException();
        }

        public void StartLogParsing()
        {
            throw new System.NotImplementedException();
        }

        public void StopLogParsing()
        {
            throw new System.NotImplementedException();
        }
    }
}