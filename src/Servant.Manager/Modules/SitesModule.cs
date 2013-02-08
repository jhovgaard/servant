﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Servant.Business.Helpers;
using Servant.Business.Objects;
using Servant.Business.Objects.Enums;
using Nancy.Responses;
using Nancy.ModelBinding;
using Servant.Manager.Helpers;
using CreateSiteResult = Servant.Business.Objects.Enums.CreateSiteResult;

namespace Servant.Manager.Modules
{
    public class SitesModule : BaseModule
    {
        readonly SiteManager _siteManager = new SiteManager();

        public void ValidateSite(ref Site site)
        {
            string[] bindingsUserInputs = Request.Form.BindingsUserInput.ToString().Split(',');
            string[] bindingsCertificateName = Request.Form.BindingsCertificateName.ToString().Split(',');
            site.Bindings = new List<Binding>();
            var certificates = SiteManager.GetCertificates();

            for (var i = 0; i < bindingsUserInputs.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(bindingsUserInputs[i]))
                    continue;

                var isValid = true;

                var finalizedHost = BindingHelper.SafeFinializeBinding(bindingsUserInputs[i]);

                if (finalizedHost == null)
                {
                    AddPropertyError("bindingsuserinput[" + i + "]", "The binding is invalid.");
                    isValid = false;
                }
                else if (_siteManager.IsBindingInUse(finalizedHost, site.IisId))
                {
                    AddPropertyError("bindingsuserinput[" + i + "]", string.Format("The binding {0} is already in use.", finalizedHost));
                    isValid = false;
                }
                Binding binding;

                if (isValid)
                {
                    var certificate = certificates.SingleOrDefault(x => x.FriendlyName == bindingsCertificateName[i]);
                    binding = BindingHelper.ConvertToBinding(finalizedHost, certificate);
                }
                else
                {
                    binding = new Binding()
                    {
                        CertificateName = bindingsCertificateName[i],
                        UserInput = bindingsUserInputs[i]
                    };
                }

                site.Bindings.Add(binding);
            }

            if (!site.Bindings.Any())
            {
                AddPropertyError("bindingsuserinput[0]", "Minimum one binding is required.");
                site.Bindings.Add(new Binding() {UserInput = ""});
            }

            if (string.IsNullOrWhiteSpace(site.Name))
                AddPropertyError("name", "Name is required.");

            var existingSite = _siteManager.GetSiteByName(site.Name);
            if (site.Name != null && existingSite != null && existingSite.IisId != site.IisId)
                AddPropertyError("name", "There's already a site with this name.");

            if (string.IsNullOrWhiteSpace(site.SitePath))
                AddPropertyError("sitepath", "Site path is required.");

            if (site.SitePath != null && !FileSystemHelper.DirectoryExists(site.SitePath))
                AddPropertyError("sitepath", "The entered directory doesn't exist.");
        }

        public SitesModule() : base("/sites/")
        {
            Get["/"] = p  => {
                var sites = _siteManager.GetSites();
                Model.Sites = sites;
                return View["Index", Model];
            };
            
            Get["/create/"] = p => {
                ModelIncluders.IncludeCertificates(ref Model);
                
                var site = new Site();
                Model.Site = site;
                Model.ApplicationPools = _siteManager.GetApplicationPools();
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
                    var result = _siteManager.CreateSite(site);

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

                Site site = _siteManager.GetSiteById(p.Id);

                Model.Site = site;
                Model.ApplicationPools = _siteManager.GetApplicationPools();
                return View["Settings", Model];
            };

            Post[@"/(?<Id>[\d]{1,4})/settings/"] = p =>
            {
                ModelIncluders.IncludeCertificates(ref Model);
                ModelIncluders.IncludeApplicationPools(ref Model);

                Site site = _siteManager.GetSiteById(p.Id);
                site.Name = Request.Form.Name;
                site.SitePath = Request.Form.SitePath;
                site.ApplicationPool = Request.Form.ApplicationPool;
                
                ValidateSite(ref site);

                Model.Site = site;

                if(!HasErrors)
                {
                    _siteManager.UpdateSite(site);
                    AddMessage("Settings have been saved.");
                }

                return View["Settings", Model];
            };

            Post[@"/(?<Id>[\d]{1,4})/stop/"] = p =>
            {
                Site site = _siteManager.GetSiteById(p.Id);
                _siteManager.StopSite(site);
                AddMessage("Site has been stopped.");
                return new RedirectResponse("/sites/" + site.IisId + "/settings/");
            };

            Post[@"/(?<Id>[\d]{1,4})/start/"] = p =>
            {
                Site site = _siteManager.GetSiteById(p.Id);
                var result = _siteManager.StartSite(site);

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
                Site site = _siteManager.GetSiteById(p.Id);
                _siteManager.RestartSite(site.IisId);
                AddMessage("Site has been restarted.");
                return new RedirectResponse("/sites/" + site.IisId + "/settings/");
            };

            Post[@"/(?<Id>[\d]{1,4})/recycle/"] = p =>
            {
                Site site = _siteManager.GetSiteById(p.Id);
                _siteManager.RecycleApplicationPoolBySite(site.IisId);
                AddMessage("Application pool has been recycled.");
                return new RedirectResponse("/sites/" + site.IisId + "/settings/");
            };

            Post[@"/(?<Id>[\d]{1,4})/delete/"] = p =>
            {
                Site site = _siteManager.GetSiteById(p.Id);
                _siteManager.DeleteSite(site.IisId);
                AddMessage("The site {0} was deleted.", site.Name);
                return new RedirectResponse("/");
            };

            Get[@"/(?<Id>[\d]{1,4})/errors/"] = p => {                
                StatsRange range;
                var rValue = Request.Query["r"];
                if(rValue == null)
                    range = StatsRange.Last24Hours;
                else
                    Enum.TryParse(rValue, true, out range); // Defaults "Last24hours" by position    
                
                Model.Range = range;
                
                Site site = _siteManager.GetSiteById(p.Id);
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
                Site site = _siteManager.GetSiteById(p.Id);
                ApplicationError exception = EventLogHelper.GetById(p.EventLogId);
                Model.Site = site;
                Model.Exception = exception;

                return View["Error", Model];
            };
        }
    }
}
