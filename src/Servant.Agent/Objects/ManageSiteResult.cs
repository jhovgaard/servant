using System.Collections.Generic;
using Servant.Agent.Objects.Enums;

namespace Servant.Agent.Objects
{
    public class ManageSiteResult
    {
        public int IisSiteId { get; set; }
        public SiteResult Result { get; set; }
        public List<string> Errors { get; set; }

        public ManageSiteResult()
        {
            Errors = new List<string>();
        }
    }
}