using System;

namespace Servant.Business.Objects.Reporting
{
    public class MostActiveClient
    {
        public Int64 Count { get; set; }
        public string Agentstring { get; set; }
        public string ClientIpAddress { get; set; }
    }
}