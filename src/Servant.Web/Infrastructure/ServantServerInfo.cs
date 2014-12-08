using System.Collections.Generic;
using Servant.Business.Objects;

namespace Servant.Web.Infrastructure
{
    public class ServantServerInfo
    {
        public List<ApplicationPool> ApplicationPools { get; set; }
        public List<Certificate> Certificates { get; set; } 
    }
}