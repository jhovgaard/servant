using Servant.Business.Objects.Enums;

namespace Servant.Business.Objects
{
    public class ApplicationPool
    {
        public string Name { get; set; }
        public InstanceState State { get; set; }
    }
}