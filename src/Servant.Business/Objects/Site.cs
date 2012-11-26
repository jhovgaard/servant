using Servant.Business.Objects.Enums;

namespace Servant.Business.Objects
{
    public class Site
    {
        public int IisId { get; set; }
        public string Name { get; set; }
        public string ApplicationPool { get; set; }
        public string SitePath { get; set; }
        public InstanceState SiteState { get; set; }
        public InstanceState ApplicationPoolState { get; set; }
        public string LogFileDirectory { get; set; }
        public string[] Bindings { get; set; }
    }
}