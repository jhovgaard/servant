using System;

namespace Servant.Agent.Objects
{
    public class ServerInfo
    {
        public ServantVersion ServantVersion { get; set; }
        public string ServerName { get; set; }
        public string OperatingSystem { get; set; }
        public int TotalSites { get; set; }
        public int TotalApplicationPools { get; set; }
    }
}