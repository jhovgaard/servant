using System;
using System.Reflection;
using Servant.Shared.Objects;

namespace Servant.Agent.Infrastructure
{
    public class ServantAgentConfiguration
    {
        public ServantVersion Version;

        public ServantAgentConfiguration()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Version = new ServantVersion(version.Major, version.Minor, version.Build);
        }

        public Guid InstallationGuid { get; set; }
        public string ServantIoKey { get; set; }
        public string ServantIoHost { get; set; }
        public bool DisableConsoleAccess { get; set; }
    }
}