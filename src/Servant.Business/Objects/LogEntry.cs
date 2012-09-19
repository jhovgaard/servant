using System;

namespace Servant.Business.Objects
{
    public class LogEntry : Entity
    {
        /// <summary>
        /// UTC Date
        /// </summary>
        public DateTime DateTime { get; set; }
        public string ServerIpAddress { get; set; }
        public string HttpMethod { get; set; }
        public string Uri { get; set; }
        public string Querystring { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string ClientIpAddress { get; set; }
        public string Agentstring { get; set; }
        public int HttpStatusCode { get; set; }
        public int HttpSubStatusCode { get; set; }
        public int TimeTaken { get; set; }
        public int SiteIisId { get; set; }
        public int LogRow { get; set; }
    }
}