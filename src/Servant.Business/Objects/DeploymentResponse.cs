using System;
using Servant.Business.Objects.Enums;

namespace Servant.Business.Objects
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