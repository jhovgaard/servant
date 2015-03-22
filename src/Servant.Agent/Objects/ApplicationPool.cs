using System;
using Servant.Agent.Objects.Enums;

namespace Servant.Agent.Objects
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
        public TimeSpan IdleTimeout { get; set; }
        public long MaximumWorkerProcesses { get; set; }
        public bool PingingEnabled { get; set; }
        public TimeSpan PingInterval { get; set; }
        public TimeSpan PingMaximumResponseTime { get; set; }
        public string ServiceUnavailableResponseType { get; set; }
        public bool RapidFailProtectionEnabled { get; set; }
        public TimeSpan RapidFailProtectionInterval { get; set; }
        public long RapidFailProtectionMaxCrashes { get; set; }
    }
}