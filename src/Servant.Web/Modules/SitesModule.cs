using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Servant.Business.Helpers;
using Servant.Business.Objects;
using Servant.Business.Objects.Enums;
using Nancy.Responses;
using Nancy.ModelBinding;
using Servant.Web.Helpers;
using CreateSiteResult = Servant.Business.Objects.Enums.CreateSiteResult;

namespace Servant.Web.Modules
{
    public class SitesModule : BaseModule
    {
        public SitesModule() : base("/sites/")
        {           
            Get["/create/"] = p => {
                ModelIncluders.IncludeCertificates(ref Model);
                
                var site = new Site();
                Model.Site = site;
                Model.ApplicationPools = SiteManager.GetApplicationPools();
                return View["Create", Model];
            };
            
            Post["/create/"] = p => {
                ModelIncluders.IncludeCertificates(ref Model);
                ModelIncluders.IncludeApplicationPools(ref Model);

                var site = this.Bind<Site>();
                Model.Site = site;

                ValidateSite(ref site);

                if(!HasErrors)
                {
                    var result = SiteManager.CreateSite(site);

                    switch (result.Result)
                    {
                        case CreateSiteResult.NameAlreadyInUse:
                            AddPropertyError("name", "There's already a site with that name.");
                            break;
                        case CreateSiteResult.BindingAlreadyInUse:
                            AddPropertyError("httpbindings", "The binding is already in use.");
                            break;
                        case CreateSiteResult.Failed:
                            AddGlobalError("Something went completely wrong :-/");
                            break;
                        case CreateSiteResult.Success:
                            System.Threading.Thread.Sleep(1000);
                            AddMessage("Site has successfully been created.", MessageType.Success);
                            return new RedirectResponse("/sites/" + result.IisSiteId + "/settings/");
                    }
                }

                return View["Create", Model];
            };

            Get[@"/(?<Id>[\d]{1,4})/settings/"] = p =>
            {
                ModelIncluders.IncludeCertificates(ref Model);
                ModelIncluders.IncludeApplicationPools(ref Model);

                Site site = SiteManager.GetSiteById(p.Id);

                Model.Site = site;
                Model.ApplicationPools = SiteManager.GetApplicationPools();
                return View["Settings", Model];
            };

            Post[@"/(?<Id>[\d]{1,4})/settings/"] = p =>
            {
                ModelIncluders.IncludeCertificates(ref Model);
                ModelIncluders.IncludeApplicationPools(ref Model);

                Site site = SiteManager.GetSiteById(p.Id);
                site.Name = Request.Form.Name;
                site.SitePath = Request.Form.SitePath;
                site.ApplicationPool = Request.Form.ApplicationPool;
                
                ValidateSite(ref site);

                Model.Site = site;

                if(!HasErrors)
                {
                    try
                    {
                        SiteManager.UpdateSite(site);
                        AddMessage("Settings have been saved.", MessageType.Success);
                    }
                    catch (System.ArgumentException ex)
                    {
                        AddMessage("IIS error: " + ex.Message, MessageType.Error);
                    }
                }

                return View["Settings", Model];
            };

            Post[@"/(?<Id>[\d]{1,4})/stop/"] = p =>
            {
                Site site = SiteManager.GetSiteById(p.Id);
                SiteManager.StopSite(site);
                AddMessage("Site has been stopped.");
                return new RedirectResponse("/sites/" + site.IisId + "/settings/");
            };

            Post[@"/(?<Id>[\d]{1,4})/start/"] = p =>
            {
                Site site = SiteManager.GetSiteById(p.Id);
                var result = SiteManager.StartSite(site);

                switch (result)
                {
                    case SiteStartResult.BindingIsAlreadyInUse:
                        AddMessage("Could not start the site because a binding is already in use.", MessageType.Error);
                        break;
                    case SiteStartResult.CannotAccessSitePath:
                        AddMessage("Could not start the site because IIS could not obtain access to the site path. Maybe another process is using the files?", MessageType.Error);
                        break;
                    case SiteStartResult.Started:
                        AddMessage("Site has been started.");
                        break;
                }

                return new RedirectResponse("/sites/" + site.IisId + "/settings/");
            };

            Post[@"/(?<Id>[\d]{1,4})/restart/"] = p =>
            {
                Site site = SiteManager.GetSiteById(p.Id);
                SiteManager.RestartSite(site.IisId);
                AddMessage("Site has been restarted.");
                return new RedirectResponse("/sites/" + site.IisId + "/settings/");
            };

            Post[@"/(?<Id>[\d]{1,4})/recycle/"] = p =>
            {
                Site site = SiteManager.GetSiteById(p.Id);
                SiteManager.RecycleApplicationPoolBySite(site.IisId);
                AddMessage("Application pool has been recycled.");
                return new RedirectResponse("/sites/" + site.IisId + "/settings/");
            };

            Post[@"/(?<Id>[\d]{1,4})/delete/"] = p =>
            {
                Site site = SiteManager.GetSiteById(p.Id);
                SiteManager.DeleteSite(site.IisId);
                AddMessage("The site {0} was deleted.", site.Name);
                return new RedirectResponse("/");
            };

            Get[@"/(?<Id>[\d]{1,4})/applications/"] = p =>
            {
                ModelIncluders.IncludeApplicationPools(ref Model);
                Site site = SiteManager.GetSiteById(p.Id);

                Model.Site = site;
                Model.ApplicationPools = SiteManager.GetApplicationPools();
                return View["Applications", Model];
            };

            Post[@"/(?<Id>[\d]{1,4})/applications/"] = p =>
            {
                ModelIncluders.IncludeApplicationPools(ref Model);
                Site site = SiteManager.GetSiteById(p.Id);

                string[] paths = Request.Form.Path != null ? Request.Form.Path.ToString().Split(',') : null;
                string[] applicationPools = Request.Form.ApplicationPool.ToString().Split(',');
                string[] diskPaths = Request.Form.DiskPath.ToString().Split(',');

                site.Applications.Clear();
                
                if(paths != null) {
                    for (int i = 0; i < paths.Length; i++)
                    {
                    
                        site.Applications.Add(new SiteApplication
                            {
                                ApplicationPool = applicationPools[i],
                                DiskPath = diskPaths[i],
                                Path = paths[i]
                            });
                    }
                    ValidateSiteApplications(site);
                }

                if(!HasErrors)
                {
                    SiteManager.UpdateSite(site);
                    AddMessage("Applications have been saved.", MessageType.Success);
                }

                Model.Site = site;
                Model.ApplicationPools = SiteManager.GetApplicationPools();
                return View["Applications", Model];
            };

            Get[@"/(?<Id>[\d]{1,4})/errors/"] = p => {                
                StatsRange range;
                var rValue = Request.Query["r"];
                if(rValue == null)
                    range = StatsRange.Last24Hours;
                else
                    Enum.TryParse(rValue, true, out range); // Defaults "Last24hours" by position    
                
                Model.Range = range;
                
                Site site = SiteManager.GetSiteById(p.Id);
                var hasAnyErrors = true;

                var sw = new Stopwatch();
                sw.Start();
                var errors = EventLogHelper.GetBySite(site.IisId, range).ToList();
                sw.Stop();

                Model.QueryTime = sw.ElapsedMilliseconds;

                Model.HasAnyErrors = hasAnyErrors; 
                Model.Site = site;
                Model.Exceptions = errors.ToList();
                
                return View["Errors", Model];
            };

            Get[@"/(?<Id>[\d]{1,4})/errors/(?<EventLogId>[\d]{1,7})/"] = p =>{
                Site site = SiteManager.GetSiteById(p.Id);
                ApplicationError exception = EventLogHelper.GetById(p.EventLogId);
                Model.Site = site;
                Model.Exception = exception;

                return View["Error", Model];
            };
        }


        public void ValidateSite(ref Site site)
        {
            string[] bindingsUserInputs = Request.Form.BindingsUserInput.ToString().Split(',');
            string[] bindingsCertificateThumbprint = Request.Form.BindingsCertificateThumbprint.ToString().Split(',');
            string[] bindingsIpAddresses = Request.Form.BindingsIpAddress.ToString().Split(',');

            site.Bindings = new List<Binding>();
            var certificates = SiteManager.GetCertificates();

            for (var i = 0; i < bindingsUserInputs.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(bindingsUserInputs[i]))
                    continue;

                var isValid = true;
                var userinput = bindingsUserInputs[i];

                var finalizedHost = BindingHelper.SafeFinializeBinding(userinput);
                var ip = bindingsIpAddresses[i];

                if (string.IsNullOrWhiteSpace(ip))
                    ip = "*";

                if (finalizedHost == null)
                {
                    AddPropertyError("bindingsuserinput[" + i + "]", "The binding is invalid.");
                    isValid = false;
                }
                else if (!BindingHelper.IsIpValid(ip))
                {
                    AddPropertyError("bindingsipaddress[" + i + "]", string.Format("The IP {0} is not valid.", ip));
                    isValid = false;
                }
                else if (SiteManager.IsBindingInUse(finalizedHost, bindingsIpAddresses[i], site.IisId))
                {
                    AddPropertyError("bindingsuserinput[" + i + "]", string.Format("The binding {0} is already in use.", finalizedHost));
                    isValid = false;
                }

                Binding binding;

                if (isValid)
                {
                    var certificate = certificates.SingleOrDefault(x => x.Thumbprint == bindingsCertificateThumbprint[i]);
                    binding = BindingHelper.ConvertToBinding(finalizedHost, ip, certificate);
                }
                else
                {
                    binding = new Binding()
                    {
                        CertificateName = bindingsCertificateThumbprint[i],
                        UserInput = bindingsUserInputs[i],
                        IpAddress = ip
                    };
                }

                site.Bindings.Add(binding);
            }

            if (!site.Bindings.Any())
            {
                AddPropertyError("bindingsipaddress[0]", "Minimum one binding is required.");
                site.Bindings.Add(new Binding() { UserInput = "" });
            }

            if (string.IsNullOrWhiteSpace(site.Name))
                AddPropertyError("name", "Name is required.");

            var existingSite = SiteManager.GetSiteByName(site.Name);
            if (site.Name != null && existingSite != null && existingSite.IisId != site.IisId)
                AddPropertyError("name", "There's already a site with this name.");

            if (string.IsNullOrWhiteSpace(site.SitePath))
                AddPropertyError("sitepath", "Site path is required.");
            else
            {
                if (!FileSystemHelper.IsPathValid(site.SitePath))
                {
                    AddPropertyError("sitepath", "Path cannot contain the following characters: ?, ;, :, @, &, =, +, $, ,, |, \", <, >, *.");
                }
                else
                {
                    if (!FileSystemHelper.DirectoryExists(site.SitePath))
                    {
                        FileSystemHelper.CreateDirectory(site.SitePath);
                    }    
                }
            }
        }

        public void ValidateSiteApplications(Site site)
        {
            for (int i = 0; i < site.Applications.Count; i++)
            {
                var application = site.Applications[i];

                if (!application.Path.StartsWith("/"))
                    application.Path = "/" + application.Path;

                if (string.IsNullOrWhiteSpace(application.DiskPath))
                {
                    AddPropertyError("diskpath[" + i + "]", "Disk Path is required.");
                }
                else
                {
                    if (!FileSystemHelper.DirectoryExists(application.DiskPath))
                    {
                        FileSystemHelper.CreateDirectory(application.DiskPath);
                    }
                }
                
                if (string.IsNullOrWhiteSpace(application.Path))
                    AddPropertyError("path[" + i + "]", "Path is required.");

                if (!FileSystemHelper.IsPathValid(application.Path))
                    AddPropertyError("path[" + i + "]", "Path cannot contain the following characters: ?, ;, :, @, &, =, +, $, ,, |, \", <, >, *.");
                
                var existingApplicationByPath = site.Applications.SingleOrDefault(x => x != site.Applications[i] && x.Path == site.Applications[i].Path);
                if (site.SitePath != null && existingApplicationByPath != null)
                    AddPropertyError("path[" + i + "]", "There's already an application with this path.");

                if (!FileSystemHelper.IsPathValid(application.DiskPath))
                    AddPropertyError("diskpath[" + i + "]", "Path cannot contain the following characters: ?, ;, :, @, &, =, +, $, ,, |, \", <, >, *.");
            }
        }
    }
}
