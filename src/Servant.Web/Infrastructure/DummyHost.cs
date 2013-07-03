using System;
using Servant.Business;
using Servant.Business.Objects;

namespace Servant.Web.Infrastructure
{
    public class DummyHost : IHost
    {
        public bool LogParsingStarted { get; set; }
        public bool Debug { get; set; }
        public DateTime StartupTime { get; set; }
        public void Start(ServantConfiguration configuration = null)
        {
        }

        public void Stop()
        {
        }

        public void Kill()
        {
        }

        public void StartLogParsing()
        {
        }

        public void StopLogParsing()
        {
        }

        public void RemoveCertificateBinding(int port)
        {
        }

        public void AddCertificateBinding(int port)
        {
        }
    }
}