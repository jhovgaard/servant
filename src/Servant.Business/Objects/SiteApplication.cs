using Servant.Business.Objects.Enums;

namespace Servant.Business.Objects
{
    public class SiteApplication
    {
        public string Path { get; set; }
        public string ApplicationPool { get; set; }
        public string DiskPath { get; set; }
    }
}