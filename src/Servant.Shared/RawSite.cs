namespace Servant.Shared
{
    public class RawSite
    {
        public long IisId { get; private set; }

        public RawSite(Microsoft.Web.Administration.Site site)
        {
            IisId = site.Id;

        }
    }
}
