using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Servant.Business.Helpers;
using Servant.Business.Objects;
using Servant.Business.Objects.Enums;
using CreateSiteResult = Servant.Business.Objects.Enums.CreateSiteResult;
using Site = Servant.Business.Objects.Site;

namespace Servant.Manager.Helpers
{
    public class SiteManager : IDisposable
    {
        private readonly Microsoft.Web.Administration.ServerManager _manager;

        public SiteManager()
        {
            _manager = new Microsoft.Web.Administration.ServerManager();
            try
            {
                var testForIis = _manager.Sites.FirstOrDefault();
            }
            catch (COMException)
            {
                throw new Exception("Looks like IIS is not installed.");
            }

        }

        public IEnumerable<Site> GetSites()
        {
            foreach (var site in _manager.Sites)
            {
                yield return ParseSite(site);
            }
        }

        public Microsoft.Web.Administration.Site GetIisSiteById(int iisId)
        {
            return _manager.Sites.SingleOrDefault(x => x.Id == iisId);
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

            var allowedProtocols = new[] { "http", "https"};
            return new Site {
                    IisId = (int)site.Id,
                    Name = site.Name,
                    ApplicationPool = site.Applications[0].ApplicationPoolName,
                    SitePath = site.Applications[0].VirtualDirectories[0].PhysicalPath,
                    SiteState = (InstanceState)Enum.Parse(typeof(InstanceState), site.State.ToString()),
                    ApplicationPoolState = (InstanceState)Enum.Parse(typeof(InstanceState),  applicationPoolState.ToString()),
                    LogFileDirectory = site.LogFile.Directory,
                    Bindings = GetBindings(site).ToList()
                };
        }

        private IEnumerable<Binding> GetBindings(Microsoft.Web.Administration.Site iisSite)
        {
            var allowedProtocols = new[] { "http", "https" };
            var certificates = GetCertificates();
            
            foreach (var binding in iisSite.Bindings.Where(x => allowedProtocols.Contains(x.Protocol)))
            {
                var servantBinding = new Binding();

                if (binding.Protocol == "https" && binding.CertificateHash != null)
                {
                    var certificate = certificates.SingleOrDefault(cert => cert.GetCertHash().SequenceEqual(binding.CertificateHash));
                    if (certificate != null)
                    {
                        servantBinding.CertificateName = certificate.FriendlyName;
                        servantBinding.CertificateHash = binding.CertificateHash;    
                    }
                }
                servantBinding.Protocol = (Protocol) Enum.Parse(typeof(Protocol), binding.Protocol);
                servantBinding.Hostname = binding.Host;
                servantBinding.Port = binding.EndPoint.Port;
                var endPointAddress = binding.EndPoint.Address.ToString();
                servantBinding.IpAddress = endPointAddress == "0.0.0.0" ? "*" : endPointAddress;

                yield return servantBinding;
            }
        }

        public static List<X509Certificate2> GetCertificates()
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.OpenExistingOnly);
            return store.Certificates.Cast<X509Certificate2>().Where(x => !string.IsNullOrWhiteSpace(x.FriendlyName)).ToList();
        }

        public static string GetSitename(Servant.Business.Objects.Site site) {
            if(site == null)
                return "Unknown";
            
            return site.Name;
        }

        private static IEnumerable<string> ConvertBindingsToBindingInformations(IEnumerable<Binding> bindings)
        {
            var bindingsToAdd = bindings
                .Select(binding => string.Format("*:{0}:{1}", binding.Port, binding.Hostname))
                .ToList();

            return bindingsToAdd.Distinct();
        }

        public void UpdateSite(Servant.Business.Objects.Site site)
        {
            var iisSite = _manager.Sites.SingleOrDefault(x => x.Id == site.IisId);
            var application = iisSite.Applications[0];

            application.VirtualDirectories[0].PhysicalPath = site.SitePath;
            iisSite.Name = site.Name;
            application.ApplicationPoolName = site.ApplicationPool;

            // Commits bindings
            iisSite.Bindings.Clear();
            foreach (var binding in site.Bindings)
            {
                if (binding.Protocol == Protocol.https)
                    iisSite.Bindings.Add(binding.ToIisBindingInformation(), binding.CertificateHash, "My");
                else
                    iisSite.Bindings.Add(binding.ToIisBindingInformation(), binding.Protocol.ToString());
            }
                
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

            try
            {
                iisSite.Start();
                return SiteStartResult.Started;
            }
            catch (Microsoft.Web.Administration.ServerManagerException)
            {
                return SiteStartResult.BindingIsAlreadyInUse;
            }
            catch (FileLoadException)
            {
                return SiteStartResult.CannotAccessSitePath;
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

        public bool IsBindingInUse(string rawBinding, string ipAddress, int iisSiteId = 0)
        {
            var binding = BindingHelper.ConvertToBinding(BindingHelper.FinializeBinding(rawBinding), ipAddress);
            return IsBindingInUse(binding, iisSiteId);
        }

        public bool IsBindingInUse(Binding binding, int iisSiteId = 0)
        {
            var bindingInformations = ConvertBindingsToBindingInformations(new[] {binding});
            return GetBindingInUse(iisSiteId, bindingInformations) != null;
        }

        public Business.Objects.CreateSiteResult CreateSite(Site site)
        {
            var result = new Business.Objects.CreateSiteResult();
            

            var bindingInformations = site.Bindings.Select(x=> x.ToIisBindingInformation()).ToList();
                
            // Check bindings
            var bindingInUse = GetBindingInUse(0, bindingInformations); // 0 never exists
            if (bindingInUse != null)
            {
                result.Result = CreateSiteResult.BindingAlreadyInUse;
                return result;
            }

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

            result.Result = CreateSiteResult.Success;
            result.IisSiteId = (int) iisSite.Id;
            return result;
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

        public void RestartSite(int iisSiteId)
        {
            var site = GetSiteById(iisSiteId);
            StopSite(site);
            StartSite(site);
        }

        public void RecycleApplicationPoolBySite(int iisSiteId)
        {
            var site = GetIisSiteById(iisSiteId);
            _manager.ApplicationPools[site.Applications[0].ApplicationPoolName].Recycle();
        }

        public void DeleteSite(int iisId)
        {
            var siteToDelete = GetIisSiteById(iisId);
            var applicationPoolname = siteToDelete.Applications[0].ApplicationPoolName;

            var sitesWithApplicationPoolname =
                from site in _manager.Sites
                let application = site.Applications[0]
                where application.ApplicationPoolName == applicationPoolname
                select site;

            siteToDelete.Delete();
        
            if (sitesWithApplicationPoolname.Count() == 1)
                _manager.ApplicationPools[applicationPoolname].Delete();

            _manager.CommitChanges();
        }
    }
}