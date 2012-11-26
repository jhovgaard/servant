using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Servant.Business.Objects.Enums;
using Site = Servant.Business.Objects.Site;

namespace Servant.Manager.Helpers
{
    public class SiteManager : IDisposable
    {
        private readonly Microsoft.Web.Administration.ServerManager _manager;

        public SiteManager()
        {
            _manager = new Microsoft.Web.Administration.ServerManager();
        }

        public IEnumerable<Site> GetSites()
        {
            foreach (var site in _manager.Sites)
            {
                yield return ParseSite(site);
            }
        }

        public Servant.Business.Objects.Site GetSiteById(int iisId) 
        {
            var iisSite = _manager.Sites.SingleOrDefault(x => x.Id == iisId);
            
            return iisSite == null
                ? null
                : ParseSite(iisSite);
        }

        private Servant.Business.Objects.Site ParseSite(Microsoft.Web.Administration.Site site)
        {
            if (site == null)
                return null;

            var applicationPoolState = _manager.ApplicationPools[site.Applications[0].ApplicationPoolName].State;

            var allowedProtocols = new[] { "http"};
            return new Site {
                    IisId = (int)site.Id,
                    Name = site.Name,
                    ApplicationPool = site.Applications[0].ApplicationPoolName,
                    SitePath = site.Applications[0].VirtualDirectories[0].PhysicalPath,
                    SiteState = (InstanceState)Enum.Parse(typeof(InstanceState), site.State.ToString()),
                    ApplicationPoolState = (InstanceState)Enum.Parse(typeof(InstanceState),  applicationPoolState.ToString()),
                    LogFileDirectory = site.LogFile.Directory,
                    Bindings = site.Bindings
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

        public void UpdateSite(Servant.Business.Objects.Site site)
        {
            var iisSite = _manager.Sites.SingleOrDefault(x => x.Id == site.IisId);
            var application = iisSite.Applications[0];

            application.VirtualDirectories[0].PhysicalPath = site.SitePath;
            iisSite.Name = site.Name;
            application.ApplicationPoolName = site.ApplicationPool;

            // Normalizing and preparing bindings
            var bindingsToAdd = ConvertBindingsToBindingInformations(site.Bindings);
                
            // Commits bindings
            iisSite.Bindings.Clear();
            foreach (var binding in bindingsToAdd)
                iisSite.Bindings.Add(binding, "http");
                
            _manager.CommitChanges();
        }

        public string[] GetApplicationPools()
        {
            return _manager.ApplicationPools.Select(x => x.Name).OrderBy(x => x).ToArray();
        }

        public Servant.Business.Objects.Enums.SiteStartResult StartSite(Servant.Business.Objects.Site site)
        {
            var iisSite = _manager.Sites.SingleOrDefault(x => x.Id == site.IisId);
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

        public void StopSite(Servant.Business.Objects.Site site)
        {
            var iisSite = _manager.Sites.SingleOrDefault(x => x.Id == site.IisId);
            if (iisSite == null)
                throw new SiteNotFoundException("Site " + site.Name + " was not found on IIS");

            iisSite.Stop();
        }

        public class SiteNotFoundException : Exception
        {
            public SiteNotFoundException(string message) : base(message) {}
        }

        public Site GetSiteByName(string name)
        {
            return ParseSite(_manager.Sites.SingleOrDefault(x => x.Name == name));
        }


        public bool IsBindingInUse(string binding)
        {
            return IsBindingInUse(binding, 0);
        }
        public bool IsBindingInUse(string binding, int iisSiteId)
        {
            var bindingInformations = ConvertBindingsToBindingInformations(new[] {binding});
            return GetBindingInUse(iisSiteId, bindingInformations) != null;
        }

        public CreateSiteResult CreateSite(Site site)
        {
            var bindingInformations = ConvertBindingsToBindingInformations(site.Bindings);
                
            // Check bindings
            var bindingInUse = GetBindingInUse(0, bindingInformations); // 0 never exists
            if(bindingInUse != null)
                return CreateSiteResult.BindingAlreadyInUse;

            // Create site
            _manager.Sites.Add(site.Name, "http", bindingInformations.First(), site.SitePath);
            var iisSite = _manager.Sites.SingleOrDefault(x => x.Name == site.Name);

            // Add bindings
            iisSite.Bindings.Clear();
            foreach (var binding in bindingInformations)
                iisSite.Bindings.Add(binding, "http");

            // Set/create application pool
            if (string.IsNullOrWhiteSpace(site.ApplicationPool)) // Auto create application pool
            {
                var appPoolName = site.Name;
                var existingApplicationPoolNames = _manager.ApplicationPools.Select(x => x.Name).ToList();
                var newNameCount = 1;

                while(existingApplicationPoolNames.Contains(appPoolName))
                {
                    appPoolName = site.Name + "_" + newNameCount;
                    newNameCount++;
                }

                _manager.ApplicationPools.Add(appPoolName);
                iisSite.ApplicationDefaults.ApplicationPoolName = appPoolName;
            }
            else
            {
                iisSite.ApplicationDefaults.ApplicationPoolName = site.ApplicationPool;
            }

            _manager.CommitChanges();

            return CreateSiteResult.Success;
        }

        private string GetBindingInUse(int iisId, IEnumerable<string> bindingInformations)
        {
            var sites = _manager.Sites.Where(x => x.Id != iisId);
            foreach (var iisSite in sites)
                foreach (var binding in iisSite.Bindings)
                    if (bindingInformations.Contains(binding.BindingInformation))
                        return binding.BindingInformation;

            return null;
        }

        public void Dispose()
        {
            _manager.Dispose();
        }
    }
}