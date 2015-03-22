using System;
using Servant.Agent.Objects.Enums;

namespace Servant.Agent.Objects
{
    public class DeploymentResponse
    {
        public int DeploymentId { get; set; }
        public Guid InstallationGuid { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public DeploymentResponseType Type { get; set; } 
    }
}