using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Web.Administration;
using Servant.Business.Helpers;
using Servant.Business.Objects;
using Servant.Business.Objects.Enums;
using Binding = Servant.Business.Objects.Binding;
using Site = Servant.Business.Objects.Site;

namespace Servant.Shared
{
    public static class SiteManager
    {
        static SiteManager()
        {
            using (var manager = new ServerManager())
            {
                try
                {
                    var testForIis = manager.Sites.FirstOrDefault();
                    var testForIisExpress = manager.WorkerProcesses.FirstOrDefault();
                }
                catch (COMException)
                {
                    throw new Exception("Looks like IIS is not installed.");
                }
                catch (NotImplementedException)
                {
                    throw new Exception("Servant doesn't support IIS Express.");
                }
            }

        }

        public static IEnumerable<Site> GetSites(bool excludeAppPools = false)
        {
            using (var manager = new ServerManager())
            {
                foreach (var site in manager.Sites)
                {
                    if (site.Bindings.Select(x => x.Protocol).Any(x => x == "ftp")) // Servant doesn't support FTP sites
                        continue;

                    var parsedSite = ParseSite(site, excludeAppPools, manager.ApplicationPools.ToList());
                    if (parsedSite != null)
                        yield return parsedSite;
                }    
            }
        }

        public static Microsoft.Web.Administration.Site GetIisSiteById(int iisId)
        {
            using (var manager = new ServerManager())
            {
                return manager.Sites.SingleOrDefault(x => x.Id == iisId);
            }
        }

        public static Site GetSiteById(int iisId) 
        {
            using (var manager = new ServerManager())
            {
                var iisSite = manager.Sites.SingleOrDefault(x => x.Id == iisId);

                return iisSite == null
                    ? null
                    : ParseSite(iisSite);    
            }
        }

        private static Site ParseSite(Microsoft.Web.Administration.Site site, bool excludeAppPools = false, List<Microsoft.Web.Administration.ApplicationPool> applicationPools = null)
        {
            if (site == null)
                return null;



            var servantSite = new Site {
                    IisId = (int)site.Id,
                    Name = site.Name,
                    ApplicationPool = site.Applications[0].ApplicationPoolName,
                    SitePath = site.Applications[0].VirtualDirectories[0].PhysicalPath,
                    SiteState = (InstanceState)Enum.Parse(typeof(InstanceState), site.State.ToString()),
                    LogFileDirectory = site.LogFile.Directory,
                    Bindings = GetBindings(site).ToList(),
                };



            
            if (!excludeAppPools)
            {
                if (applicationPools == null)
                {
                    using (var manager = new ServerManager())
                    {
                        applicationPools = manager.ApplicationPools.ToList();
                    }
                }

                ObjectState applicationPoolState = applicationPools.Single(x => x.Name == site.Applications[0].ApplicationPoolName).State;
                servantSite.ApplicationPoolState = (InstanceState)Enum.Parse(typeof(InstanceState), applicationPoolState.ToString());
            }

            foreach (var directory in site.Applications[0].VirtualDirectories.Skip(1))
            {
                servantSite.Applications.Add(new SiteApplication
                {
                    ApplicationPool = "",
                    Path = directory.Path,
                    DiskPath = directory.PhysicalPath,
                    IsApplication = false
                });
            }

            if (site.Applications.Count > 1)
            {
                foreach (var application in site.Applications.Skip(1))
                {
                    servantSite.Applications.Add(new SiteApplication
                        {
                            ApplicationPool = application.ApplicationPoolName,
                            Path = application.Path,
                            DiskPath = application.VirtualDirectories[0].PhysicalPath,
                            IsApplication = true
                        });
                }
            }

            return servantSite;
        }

        private static IEnumerable<Binding> GetBindings(Microsoft.Web.Administration.Site iisSite)
        {
            var allowedProtocols = new[] { "http", "https" };
            var certificates = GetCertificates();
            
            foreach (var binding in iisSite.Bindings.Where(x => allowedProtocols.Contains(x.Protocol)))
            {
                var servantBinding = new Binding();

                if (binding.Protocol == "https")
                {
                    if(binding.CertificateHash == null)
                        continue;

                    var certificate = certificates.SingleOrDefault(cert => cert.Hash.SequenceEqual(binding.CertificateHash));
                    if (certificate != null)
                    {
                        servantBinding.CertificateName = certificate.Name;
                        servantBinding.CertificateThumbprint = certificate.Thumbprint;
                    }
                    else
                        continue;
                }
                servantBinding.Protocol = (Protocol) Enum.Parse(typeof(Protocol), binding.Protocol);
                servantBinding.Hostname = binding.Host;
                servantBinding.Port = binding.EndPoint.Port;
                var endPointAddress = binding.EndPoint.Address.ToString();
                servantBinding.IpAddress = endPointAddress == "0.0.0.0" ? "*" : endPointAddress;

                yield return servantBinding;
            }
        }

        public static IEnumerable<Certificate> GetCertificates()
        {
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.OpenExistingOnly);
            var certs = store.Certificates.Cast<X509Certificate2>().ToList();

            foreach(var cert in certs)
            {
                var name = cert.FriendlyName;
                if (string.IsNullOrWhiteSpace(name)) // Extracts common name if friendly name isn't available.
                {
                    var commonName = cert.Subject.Split(',').SingleOrDefault(x => x.StartsWith("CN"));
                    if(commonName != null)
                    {
                        var locationOfEquals = commonName.IndexOf('=');
                        name = commonName.Substring(locationOfEquals+1, commonName.Length-(locationOfEquals+1));
                    }
                }

                yield return new Certificate { Name = name, Hash = cert.GetCertHash(), Thumbprint = cert.Thumbprint};
            }
        }

        public static string GetSitename(Site site) {
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

        public static ManageSiteResult UpdateSite(Site site)
        {
            var result = new ManageSiteResult { IisSiteId = site.IisId };

            using (var manager = new ServerManager())
            {
                var iisSite = manager.Sites.SingleOrDefault(x => x.Id == site.IisId);

                if (iisSite == null)
                {
                    result.Result = SiteResult.UnknownSiteId;
                    return result;
                }

                var iisSiteWithSameName = manager.Sites.SingleOrDefault(x => x.Id != site.IisId && x.Name == site.Name);

                if (iisSiteWithSameName != null)
                {
                    result.Result = SiteResult.NameAlreadyInUse;
                    return result;
                }

                var mainApplication = iisSite.Applications.First();
                var rootPathDirectory = mainApplication.VirtualDirectories.SingleOrDefault(x => x.Path == "/");
                if (rootPathDirectory == null)
                {
                    mainApplication.VirtualDirectories.Add("/", site.SitePath);

                }
                else
                {
                    rootPathDirectory.PhysicalPath = site.SitePath;    
                }

                // In some scenarios Microsoft.Web.Administation fails to save site if property-set is detected with same name. 
                //I believe it deletes and insert sites on updates and this makes a name conflict. Fixed by the hack below:
                if(site.Name != iisSite.Name) 
                    iisSite.Name = site.Name;

                // If the application pool does not exists on the server, create it
                if (manager.ApplicationPools.SingleOrDefault(x => x.Name == site.ApplicationPool) == null)
                {
                    manager.ApplicationPools.Add(site.ApplicationPool);
                }

                mainApplication.ApplicationPoolName = site.ApplicationPool;
                
                // Update log file path
                iisSite.LogFile.Directory = site.LogFileDirectory;

                // Commits bindings
                iisSite.Bindings.Clear();
                foreach (var binding in site.Bindings)
                {
                    if (binding.Protocol == Protocol.https)
                    {
                        var certificate = GetCertificates().Single(x => x.Thumbprint == binding.CertificateThumbprint);
                        iisSite.Bindings.Add(binding.ToIisBindingInformation(), certificate.Hash, "My");
                    }
                    else
                        iisSite.Bindings.Add(binding.ToIisBindingInformation(), binding.Protocol.ToString());
                }

                // Deletes virtual applications
                var applicationsToDelete = iisSite.Applications.Skip(1).Where(application => !site.Applications.Where(x => x.IsApplication).Select(a => a.Path).Contains(application.Path)).ToList();
                foreach (var application in applicationsToDelete)
                {
                    application.Delete();
                     iisSite.Applications.Remove(application); // Bug in Microsoft.Web.Administration when changing from directory - application
                }

                // Deletes virtual directories
                var directoriesToDelete = mainApplication.VirtualDirectories.Where(directory => directory.Path != "/" && !site.Applications.Where(x => !x.IsApplication).Select(a => a.Path).Contains(directory.Path)).ToList(); // Exclude "/" because it's the root application's directory.
                foreach (var directory in directoriesToDelete)
                {
                    directory.Delete();
                    mainApplication.VirtualDirectories.Remove(directory); // Bug in Microsoft.Web.Administration when changing from directory - application
                }

                //Intelligently updates virtual applications + directories
                foreach (var application in site.Applications)
                {
                    if (!application.Path.StartsWith("/"))
                        application.Path = "/" + application.Path;

                    if (application.IsApplication)
                    {
                        if (application.Path.EndsWith("/"))
                        {
                            application.Path = application.Path.Substring(0, application.Path.Length - 1);
                        }

                        var iisApp = iisSite.Applications.SingleOrDefault(x => x.Path == application.Path);

                        if (iisApp == null)
                        {
                            iisSite.Applications.Add(application.Path, application.DiskPath);
                            iisApp = iisSite.Applications.Single(x => x.Path == application.Path);
                        }

                        iisApp.VirtualDirectories[0].PhysicalPath = application.DiskPath;
                        iisApp.ApplicationPoolName = application.ApplicationPool;
                    } 
                    else // Directory
                    {
                        var virtualDirectory = mainApplication.VirtualDirectories.SingleOrDefault(x => x.Path == application.Path);
                        if (virtualDirectory == null)
                        {
                            mainApplication.VirtualDirectories.Add(application.Path, application.DiskPath);
                        }
                        else
                        {
                            virtualDirectory.PhysicalPath = application.DiskPath;
                        }
                    }
                }

                
                manager.CommitChanges();
            }

            return result;
        }

        public static List<Business.Objects.ApplicationPool> GetApplicationPools()
        {
            using (var manager = new ServerManager())
            {
                return manager.ApplicationPools.Select(x => new Business.Objects.ApplicationPool
                            {
                                Name = x.Name,
                                State = (InstanceState) Enum.Parse(typeof (InstanceState), x.State.ToString()),
                                
                            }).OrderBy(x => x.Name).ToList();
            }
        }

        public static SiteStartResult StartSite(Site site)
        {
            using (var manager = new ServerManager())
            {
                var iisSite = manager.Sites.SingleOrDefault(x => x.Id == site.IisId);
                if (iisSite == null)
                    throw new SiteNotFoundException("Site " + site.Name + " was not found on IIS");

                try
                {
                    iisSite.Start();
                    return SiteStartResult.Started;
                }
                catch (ServerManagerException)
                {
                    return SiteStartResult.BindingIsAlreadyInUse;
                }
                catch (FileLoadException e)
                {
                    if (e.Message.Contains("being used by another"))
                    {
                        return SiteStartResult.PortInUseByAnotherService;
                    }

                    return SiteStartResult.CannotAccessSitePath;
                }
            }
        }

        public static void StopSite(Site site)
        {
            using (var manager = new ServerManager())
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
            using (var manager = new ServerManager())
            {
                return ParseSite(manager.Sites.SingleOrDefault(x => x.Name == name));    
            }
        }

        public static bool IsBindingInUse(string rawBinding, string ipAddress, int iisSiteId = 0)
        {
            var binding = BindingHelper.ConvertToBinding(BindingHelper.FinializeBinding(rawBinding), ipAddress);
            return IsBindingInUse(binding, iisSiteId);
        }

        public static bool IsBindingInUse(Binding binding, int iisSiteId = 0)
        {
            var bindingInformations = ConvertBindingsToBindingInformations(new[] {binding});
            return GetBindingInUse(iisSiteId, bindingInformations.ToList()) != null;
        }

        public static Business.Objects.ManageSiteResult CreateSite(Site site)
        {
            var result = new Business.Objects.ManageSiteResult();


            var bindingInformations = site.Bindings.Select(x => x.ToIisBindingInformation()).ToList();

            // Check bindings
            var bindingInUse = GetBindingInUse(0, bindingInformations); // 0 never exists
            if (bindingInUse != null)
            {
                result.Result = SiteResult.BindingAlreadyInUse;
                return result;
            }

            using (var manager = new ServerManager())
            {
                if (manager.Sites.Any(x => x.Name == site.Name))
                {
                    result.Result = SiteResult.NameAlreadyInUse;
                    return result;
                }

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
                    var appPoolName = site.Name;
                    var existingApplicationPoolNames = manager.ApplicationPools.Select(x => x.Name).ToList();
                    var newNameCount = 1;

                    while (existingApplicationPoolNames.Contains(appPoolName))
                    {
                        appPoolName = site.Name + "_" + newNameCount;
                        newNameCount++;
                    }

                    manager.ApplicationPools.Add(appPoolName);
                    iisSite.ApplicationDefaults.ApplicationPoolName = appPoolName;
                }
                else
                {
                    iisSite.ApplicationDefaults.ApplicationPoolName = site.ApplicationPool;
                }

                //Add Virtual apps/directories
                foreach (var application in site.Applications)
                {
                    if (!application.Path.StartsWith("/"))
                        application.Path = "/" + application.Path;

                    if (application.IsApplication)
                    {
                        if (application.Path.EndsWith("/"))
                        {
                            application.Path.Remove(application.Path.Length - 1, 1);
                        }

                        iisSite.Applications.Add(application.Path, application.DiskPath);
                    }
                    else // Directory
                    {
                        iisSite.Applications.First().VirtualDirectories.Add(application.Path, application.DiskPath);
                    }
                }

                manager.CommitChanges();

                var created = false;
                var sw = new Stopwatch();
                sw.Start();
                while (!created && sw.Elapsed.TotalSeconds < 3)
                {
                    try
                    {
                        if (iisSite.State == ObjectState.Started || iisSite.State == ObjectState.Stopped)
                        {
                            created = true;
                        }
                    }
                    catch (COMException)
                    {
                        System.Threading.Thread.Sleep(100);
                    }

                }
                sw.Stop();

                if (created)
                {
                    result.Result = SiteResult.Success;
                    result.IisSiteId = (int) iisSite.Id;
                }
                else
                {
                    result.Result = SiteResult.Failed;
                }

                return result;
            }
        }

        private static string GetBindingInUse(int iisId, List<string> bindingInformations)
        {
            // IIS only allows one of each https binding.
            var httpsBindings = new List<string>();

            using (var manager = new ServerManager())
            {
                var sites = manager.Sites.Where(x => x.Id != iisId);
                foreach (var iisSite in sites)
                    foreach (var binding in iisSite.Bindings)
                    {
                        if(binding.Protocol == "https")
                            httpsBindings.Add(binding.BindingInformation.Substring(0, binding.BindingInformation.LastIndexOf(":")));

                        if (bindingInformations.Contains(binding.BindingInformation))
                            return binding.BindingInformation;
                    }

                foreach (var binding in bindingInformations)
                {
                    var ipPortCombi = binding.Substring(0, binding.LastIndexOf(":"));
                    if (httpsBindings.Contains(ipPortCombi))
                        return binding;
                }

                return null;    
            }
        }

        public static void RestartSite(int iisSiteId)
        {
            var site = GetSiteById(iisSiteId);
            StopSite(site);
            StartSite(site);
        }

        public static void RecycleApplicationPoolBySite(int iisSiteId)
        {
            var site = GetIisSiteById(iisSiteId);
            using (var manager = new ServerManager())
            {
                manager.ApplicationPools[site.Applications[0].ApplicationPoolName].Recycle();    
            }
        }

        public static void DeleteSite(int iisId)
        {
            using (var manager = new ServerManager())
            {
                var siteToDelete = manager.Sites.SingleOrDefault(x => x.Id == iisId);
                var applicationPoolname = siteToDelete.Applications[0].ApplicationPoolName;

                var sitesWithApplicationPoolname =
                    from site in manager.Sites
                    let application = site.Applications[0]
                    where application.ApplicationPoolName == applicationPoolname
                    select site;

                siteToDelete.Delete();

                if (sitesWithApplicationPoolname.Count() == 1)
                    manager.ApplicationPools[applicationPoolname].Delete();

                manager.CommitChanges();
            }

            System.Threading.Thread.Sleep(500);
        }
    }
}