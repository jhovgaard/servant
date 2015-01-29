using System;

namespace Servant.Business.Objects
{
    public class Deployment
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public int OrganizationId { get; set; }
        public int CreatedByUserId { get; set; }
        public int SiteIisId { get; set; }
        public string SiteName { get; set; }
        public string Url { get; set; }
        public bool RollbackOnError { get; set; }
        public bool WarmupAfterDeploy { get; set; }
        public string WarmupUrl { get; set; }
    }
}