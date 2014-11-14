using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Servant.Business.Helpers;
using Servant.Business.Objects;
using Servant.Business.Objects.Enums;
using Servant.Shared.Helpers;

namespace Servant.Shared
{
    public static class Validators
    {
       public static ManageSiteResult ValidateSite(Site site, Site originalSite)
        {
            var certificates = SiteManager.GetCertificates();

            var result = new ManageSiteResult();
            
            if (!site.Bindings.Any())
            {
                result.Errors.Add("Minimum one binding is required.");
            }

            if (string.IsNullOrWhiteSpace(site.Name))
                result.Errors.Add("Name is required.");

            var existingSite = SiteManager.GetSiteByName(site.Name);
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

        //public void ValidateSiteApplications(Site site)
        //{
        //    for (int i = 0; i < site.Applications.Count; i++)
        //    {
        //        var application = site.Applications[i];

        //        if (!application.Path.StartsWith("/"))
        //            application.Path = "/" + application.Path;

        //        if (string.IsNullOrWhiteSpace(application.DiskPath))
        //        {
        //            AddPropertyError("diskpath[" + i + "]", "Disk Path is required.");
        //        }
        //        else
        //        {
        //            if (!FileSystemHelper.DirectoryExists(application.DiskPath))
        //            {
        //                FileSystemHelper.CreateDirectory(application.DiskPath);
        //            }
        //        }

        //        if (string.IsNullOrWhiteSpace(application.Path))
        //            AddPropertyError("path[" + i + "]", "Path is required.");

        //        if (!FileSystemHelper.IsPathValid(application.Path))
        //            AddPropertyError("path[" + i + "]", "Path cannot contain the following characters: ?, ;, :, @, &, =, +, $, ,, |, \", <, >, *.");

        //        var existingApplicationByPath = site.Applications.SingleOrDefault(x => x != site.Applications[i] && x.Path == site.Applications[i].Path);
        //        if (site.SitePath != null && existingApplicationByPath != null)
        //            AddPropertyError("path[" + i + "]", "There's already an application with this path.");

        //        if (!FileSystemHelper.IsPathValid(application.DiskPath))
        //            AddPropertyError("diskpath[" + i + "]", "Path cannot contain the following characters: ?, ;, :, @, &, =, +, $, ,, |, \", <, >, *.");
        //    }
        //}
    }
}
