using System.Collections.Generic;
using System.Linq;
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
        public List<Binding> Bindings { get; set; }
        public string[] RawBindings { get; set; }
        public List<SiteApplication> Applications { get; set; }

        public Site()
        {
            Bindings = new List<Binding>() { new Binding  {UserInput = "", IpAddress = "*"}};
            Applications = new List<SiteApplication>();
        }

        public bool HasApplications()
        {
            return Applications.Any();
        }
    }
}