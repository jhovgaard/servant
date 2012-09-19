using System;
using System.Collections.Generic;
using System.Linq;
using Servant.Business.Objects;
using Servant.Business.Objects.Enums;

namespace Servant.Server.Helpers
{
    public static class SiteHelper
    {
        public static IEnumerable<Site> GetSites()
        {
            using (var manager = new Microsoft.Web.Administration.ServerManager())
            {
                foreach (var site in manager.Sites)
                {
                    yield return ParseSite(site);
                }
            }
        }

        public static Servant.Business.Objects.Site GetSiteById(int iisId) 
        {
            using (var manager = new Microsoft.Web.Administration.ServerManager())
            {
                var iisSite = manager.Sites.SingleOrDefault(x => x.Id == iisId);

                return iisSite == null
                    ? null
                    : ParseSite(iisSite);
            }
        }

        private static Servant.Business.Objects.Site ParseSite(Microsoft.Web.Administration.Site site) {
            if (site == null)
                return null;

            var allowedProtocols = new[] { "http"};
            return new Servant.Business.Objects.Site {
                    IisId = (int)site.Id,
                    Name = site.Name,
                    ApplicationPool = site.Applications[0].ApplicationPoolName,
                    SitePath = site.Applications[0].VirtualDirectories[0].PhysicalPath,
                    State = (SiteState)Enum.Parse(typeof(Servant.Business.Objects.Enums.SiteState), site.State.ToString()),
                    LogFileDirectory = site.LogFile.Directory,
                    HttpBindings = site.Bindings
                        .ToList()
                        .Where(x => allowedProtocols.Contains(x.Protocol))
                        .Select(x => (string.IsNullOrEmpty(x.Host) ? "*" : x.Host) + ":" + x.EndPoint.Port)
                        .ToArray()
                };

            
        }

        public static string GetSitename(Servant.Business.Objects.Site site) {
            if(site == null)
                return "Unknown";
            
            return site.Name;
        }

        private static IEnumerable<string> ConvertBindingsToBindingInformations(string[] bindings)
        {
            var bindingsToAdd = new List<string>();
            foreach (var binding in bindings.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var hostInfos = binding
                    .ToLower()
                    .Replace("http://", string.Empty)
                    .Replace("https://", string.Empty)
                    .Split(':');
                var host = hostInfos[0];
                var port = hostInfos.Length == 1 ? 80 : Convert.ToInt32(hostInfos[1]);

                bindingsToAdd.Add(string.Format("*:{0}:{1}", port, host));
            }

            return bindingsToAdd.Distinct();
        }

        public static void UpdateSite(Servant.Business.Objects.Site site)
        {
            using (var manager = new Microsoft.Web.Administration.ServerManager())
            {
                var iisSite = manager.Sites.SingleOrDefault(x => x.Id == site.IisId);
                var application = iisSite.Applications[0];

                application.VirtualDirectories[0].PhysicalPath = site.SitePath;
                iisSite.Name = site.Name;
                application.ApplicationPoolName = site.ApplicationPool;

                // Normalizing and preparing bindings
                var bindingsToAdd = ConvertBindingsToBindingInformations(site.HttpBindings);
                
                // Commits bindings
                iisSite.Bindings.Clear();
                foreach (var binding in bindingsToAdd)
                    iisSite.Bindings.Add(binding, "http");
                
                manager.CommitChanges();
            }
        }

        public static string[] GetApplicationPools()
        {
            using (var manager = new Microsoft.Web.Administration.ServerManager())
            {
                return manager.ApplicationPools.Select(x => x.Name).OrderBy(x => x).ToArray();
            }
        }

        public static Servant.Business.Objects.Enums.SiteStartResult StartSite(Servant.Business.Objects.Site site)
        {
            using (var manager = new Microsoft.Web.Administration.ServerManager())
            {
                var iisSite = manager.Sites.SingleOrDefault(x => x.Id == site.IisId);
                if (iisSite == null)
                    throw new SiteNotFoundException("Site " + site.Name + " was not found on IIS");

                try {
                    iisSite.Start();
                    return SiteStartResult.Started;
                }
                catch (Microsoft.Web.Administration.ServerManagerException)
                {
                    return SiteStartResult.BindingIsAlreadyInUse;
                }
            }
        }

        public static void StopSite(Servant.Business.Objects.Site site)
        {
            using (var manager = new Microsoft.Web.Administration.ServerManager())
            {
                var iisSite = manager.Sites.SingleOrDefault(x => x.Id == site.IisId);
                if (iisSite == null)
                    throw new SiteNotFoundException("Site " + site.Name + " was not found on IIS");

                iisSite.Stop();
            }
        }

        public class SiteNotFoundException : Exception
        {
            public SiteNotFoundException(string message) : base(message) {}
        }

        public static Site GetSiteByName(string name)
        {
            using(var manager = new Microsoft.Web.Administration.ServerManager())
            {
                return ParseSite(manager.Sites.SingleOrDefault(x => x.Name == name));
            }
        }

        public static bool IsBindingInUse(string binding)
        {
            var bindingInformations = ConvertBindingsToBindingInformations(new[] {binding});
            return GetBindingInUse(0, bindingInformations) != null;
        }

        public static CreateSiteResult CreateSite(Site site)
        {
            using (var manager = new Microsoft.Web.Administration.ServerManager())
            {
                var bindingInformations = ConvertBindingsToBindingInformations(site.HttpBindings);
                
                // Check bindings
                var bindingInUse = GetBindingInUse(0, bindingInformations); // 0 never exists
                if(bindingInUse != null)
                    return CreateSiteResult.BindingAlreadyInUse;

                // Create site
                manager.Sites.Add(site.Name, "http", bindingInformations.First(), site.SitePath);
                var iisSite = manager.Sites.SingleOrDefault(x => x.Name == site.Name);

                // Add bindings
                iisSite.Bindings.Clear();
                foreach (var binding in bindingInformations)
                    iisSite.Bindings.Add(binding, "http");

                // Set/create application pool
                if (string.IsNullOrWhiteSpace(site.ApplicationPool)) // Auto create application pool
                {
                    manager.ApplicationPools.Add(site.Name);
                    iisSite.ApplicationDefaults.ApplicationPoolName = site.Name;
                }
                else
                {
                    iisSite.ApplicationDefaults.ApplicationPoolName = site.ApplicationPool;
                }

                manager.CommitChanges();
            }

            return CreateSiteResult.Success;
        }

        private static string GetBindingInUse(int iisId, IEnumerable<string> bindingInformations)
        {
            using (var manager = new Microsoft.Web.Administration.ServerManager())
            {
                var sites = manager.Sites.Where(x => x.Id != iisId);
                foreach (var iisSite in sites)
                    foreach (var binding in iisSite.Bindings)
                        if (bindingInformations.Contains(binding.BindingInformation))
                            return binding.BindingInformation;
            }

            return null;
        }
    }
}