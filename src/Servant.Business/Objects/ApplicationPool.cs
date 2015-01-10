using System;
using Servant.Business.Objects.Enums;

namespace Servant.Business.Objects
{
    public class ApplicationPool
    {
        public string Name { get; set; }
        public InstanceState State { get; set; }
        public string ClrVersion { get; set; }
        public string PipelineMode { get; set; }
        public bool AutoStart { get; set; }
        public bool DisallowOverlappingRotation { get; set; }
        public bool DisallowRotationOnConfigChange { get; set; }
        public TimeSpan RecycleInterval { get; set; }
        public long RecyclePrivateMemoryLimit { get; set; }
        public long RecycleVirtualMemoryLimit { get; set; }
        public long RecycleRequestsLimit { get; set; }
    }
}