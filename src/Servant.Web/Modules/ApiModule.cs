using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using Nancy;
using Servant.Business.Objects;
using Servant.Business.Objects.Enums;
using Servant.Web.Helpers;
using Servant.Web.Performance;

namespace Servant.Web.Modules
{
    public class ApiModule : BaseModule
    {
        public ApiModule() : base("/api/")
        {
            var configuration = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<ServantConfiguration>();

            Before += ctx =>
            {
                if (!configuration.EnableApi && (string) ctx.Request.Query.Key != configuration.ServantIoKey)
                {
                    return new NotFoundResponse();
                }

                return null;
            };

            Get["/"] = p => "Servant API";

            #region Stats
            Get["/stats/"] = p =>
            {
                var sites = SiteManager.GetSites(true).ToList();
                var drives = DriveInfo.GetDrives().Where(x => x.DriveType == DriveType.Fixed).Select(
                        x => new { x.Name, x.TotalSize, x.AvailableFreeSpace });

                return Response.AsJson(new
                {
                    System.Environment.MachineName,
                    PerformanceData.SystemUpTime,
                    PerformanceData.TotalMemory,
                    PerformanceData.PhysicalAvailableMemory,
                    PerformanceData.AverageCpuUsage,
                    PerformanceData.AverageGetRequestPerSecond,
                    PerformanceData.CurrentConnections,
                    Drives = drives,
                    Sites = sites.Count(),
                    SitesStopped = sites.Count(x => x.SiteState != InstanceState.Started)
                });
            };
            #endregion

            #region Sites
            Get["/sites/{id?}/"] = p =>
            {
                var id = p.id;

                if (id.HasValue)
                {
                    var site = SiteManager.GetSiteById((int) id);

                    return site == null ? new NotFoundResponse() : Response.AsJson(site);
                }

                var sites = SiteManager.GetSites();
                return Response.AsJson(sites);
            };

            Post["/sites/{id}/stop/"] = p =>
            {
                var id = (int?)p.id;

                if (!id.HasValue)
                {
                    return new NotFoundResponse();
                }

                var site = SiteManager.GetSiteById((int)p.id);
                SiteManager.StopSite(site);
                return Response.AsJson(site);
            };

            Post["/sites/{id}/start/"] = p =>
            {
                var id = (int?)p.id;

                if (!id.HasValue)
                {
                    return new NotFoundResponse();
                }

                var site = SiteManager.GetSiteById((int)p.id);
                SiteManager.StartSite(site);
                return Response.AsJson(site);
            };

            Post["/sites/{id}/restart/"] = p =>
            {
                var id = (int?)p.id;

                if (!id.HasValue)
                {
                    return new NotFoundResponse();
                }

                Site site = SiteManager.GetSiteById(p.Id);
                SiteManager.RestartSite(site.IisId);
                return Response.AsJson(site);
            };

            Post["/sites/{id}/recycle/"] = p =>
            {
                var id = (int?)p.id;

                if (!id.HasValue)
                {
                    return new NotFoundResponse();
                }

                Site site = SiteManager.GetSiteById(p.Id);
                SiteManager.RecycleApplicationPoolBySite(site.IisId);
                return Response.AsJson(site);
            };

            Post["/sites/{id}/deploy/"] = p =>
            {
                var id = (int?) p.id;
                if (!id.HasValue)
                {
                    return Response.AsText("IIS Site ID is missing.").WithStatusCode(HttpStatusCode.BadRequest);
                }

                if (!Request.Files.Any())
                {
                    return Response.AsText("Zipfile is missing.").WithStatusCode(HttpStatusCode.BadRequest);
                }

                Site site = SiteManager.GetSiteById(p.Id);
                var rootPath = site.SitePath;
                var directoryName = new DirectoryInfo(rootPath).Name;
                if (directoryName.StartsWith("servant-"))
                {
                    rootPath = Directory.GetParent(rootPath).FullName;
                }

                var newPath = Path.Combine(rootPath, "servant-" + Path.GetFileNameWithoutExtension(Request.Files.First().Name));
                Directory.CreateDirectory(newPath);

                var zip = Request.Files.First().Value;
                var fastZip = new FastZip();
                fastZip.ExtractZip(zip, newPath, FastZip.Overwrite.Always, null, null, null, true, true);

                site.SitePath = newPath;
                SiteManager.UpdateSite(site);

                return Response.AsJson(site);
            };
            #endregion
        }
    }
}