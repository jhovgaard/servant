namespace Servant.Business.Objects.Reporting
{
    public class ServerStats
    {
        public int TotalRequests { get; set; }
        public string DataSent { get; set; }
        public string DataRecieved { get; set; }
        public int TotalSites { get; set; }
        public int AverageResponeTime { get; set; }
        public int TotalErrors { get; set; }
        public int UnusedApplicationPools { get; set; }
    }
}