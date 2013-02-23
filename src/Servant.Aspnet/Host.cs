using System;
using Servant.Business.Objects;
using Servant.Manager.Infrastructure;

namespace Servant.Aspnet
{
    public class Host  : IHost
    {
        public bool LogParsingStarted { get; set; }
        public bool Debug { get; set; }
        public DateTime StartupTime { get; set; }

        public void Start(Settings settings = null)
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

        public void LoadSettings()
        {
        }
    }
}