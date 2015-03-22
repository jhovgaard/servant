using System.Linq;
using Servant.Shared;
using Servant.Shared.Helpers;
using Servant.Shared.Objects;
using Servant.Shared.Objects.Enums;

namespace Servant.Agent.Infrastructure
{
    public static class Validators
    {
        public static ManageSiteResult ValidateSite(IisSite site, IisSite originalSite)
        {
            var certificates = SiteManager.GetCertificates();

            var result = new ManageSiteResult();

            if (!site.Bindings.Any())
            {
                result.Errors.Add("Minimum one binding is required.");
            }

            if (string.IsNullOrWhiteSpace(site.Name))
                result.Errors.Add("Name is required.");

            IisSite existingSite = SiteManager.GetSiteByName(site.Name);
            if (originalSite == null)
            {
                originalSite = new IisSite() { IisId = 0};
            }

            if (site.Name != null && existingSite != null && site.Name.ToLower() == existingSite.Name.ToLower() && existingSite.IisId != originalSite.IisId)
                result.Errors.Add("There's already a site with this name.");

            if (string.IsNullOrWhiteSpace(site.SitePath))
                result.Errors.Add("Site path is required.");
            else
            {
                if (!FileSystemHelper.IsPathValid(site.SitePath))
                {
                    result.Errors.Add("Path cannot contain the following characters: ?, ;, :, @, &, =, +, $, ,, |, \", <, >, *.");
                }
                else
                {
                    if (!FileSystemHelper.DirectoryExists(site.SitePath))
                    {
                        FileSystemHelper.CreateDirectory(site.SitePath);
                    }
                }

                if (!FileSystemHelper.IsPathValid(site.LogFileDirectory))
                {
                    result.Errors.Add("Log File Directory cannot contain the following characters: ?, ;, :, @, &, =, +, $, ,, |, \", <, >, *.");
                }
                else
                {
                    if (!FileSystemHelper.DirectoryExists(site.LogFileDirectory))
                    {
                        FileSystemHelper.CreateDirectory(site.LogFileDirectory);
                    }
                }

            }

            result.Result = result.Errors.Any() ? SiteResult.ValidationError : SiteResult.Success;

            return result;
        }
    }
}