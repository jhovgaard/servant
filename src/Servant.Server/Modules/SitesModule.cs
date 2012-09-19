using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Servant.Business.Objects;
using Servant.Business.Objects.Enums;
using Servant.Business.Services;
using Nancy.Responses;
using Nancy.ModelBinding;
using Servant.Server.Helpers;

namespace Servant.Server.Modules
{
    public class SitesModule : Server.Modules.BaseModule
    {
        public SitesModule(LogEntryService logEntryService, ApplicationErrorService applicationErrorService) : base("/sites/")
        {
            Get["/"] = p  => {
                var sites = SiteHelper.GetSites();
                Model.Sites = sites;
                return View["Index", Model];
            };

            Get["/create/"] = p => {
                var site = new Site();
                Model.Site = site;
                Model.ApplicationPools = SiteHelper.GetApplicationPools();
                return View["Create", Model];
            };
            
            Post["/create/"] = p => {
                var site = this.Bind<Site>();
                Model.Site = site;
                Model.ApplicationPools = SiteHelper.GetApplicationPools();

                if(string.IsNullOrWhiteSpace(site.Name))
                    AddPropertyError("name", "Name is required.");

                if (site.Name != null && SiteHelper.GetSiteByName(site.Name) != null)
                    AddPropertyError("name", "There's already a site with this name.");

                if(string.IsNullOrWhiteSpace(site.SitePath))
                    AddPropertyError("sitepath", "Site path is required.");

                if(site.SitePath != null && !FileSystemHelper.DirectoryExists(site.SitePath))
                    AddPropertyError("sitepath", "The entered directory doesn't exist.");

                if(site.HttpBindings == null)
                    AddPropertyError("httpbindings", "Minimum 1 binding is required.");
                else
                {
                    foreach(var binding in site.HttpBindings)
                    {
                        if(SiteHelper.IsBindingInUse(binding))
                            AddPropertyError("httpbindings", string.Format("The binding {0} is already in use.", binding));
                    }
                }

                
                
                if(!HasErrors)
                {
                    var result = SiteHelper.CreateSite(site);

                    if(result == CreateSiteResult.NameAlreadyInUse)
                        AddPropertyError("name", "There's already a site with that name.");

                    if(result == CreateSiteResult.BindingAlreadyInUse)
                        AddPropertyError("httpbindings", "The binding is already in use.");

                    if(result == CreateSiteResult.Failed)
                        AddGlobalError("Something went completely wrong :-/");

                    if(result == CreateSiteResult.Success)
                        return new RedirectResponse("/sites/");
                }

                return View["Create", Model];
            };

            Get[@"/(?<Id>[\d])/settings/"] = p  => {
                
                var sw = new Stopwatch();
                sw.Start();
                var site = SiteHelper.GetSiteById(p.Id);
                sw.Stop();
                                                       ;
                Model.Site = site;
                sw.Reset();
                sw.Start();
                Model.ApplicationPools = SiteHelper.GetApplicationPools();
                sw.Stop();
                return View["Settings", Model];
            };

            Post[@"/(?<Id>[\d])/settings/"] = p => {
                Site site = SiteHelper.GetSiteById(p.Id);
                
                site.Name = Request.Form.SiteName;
                site.SitePath = Request.Form.SitePath;
                site.HttpBindings = Request.Form.Bindings.ToString().Split(',');
                site.ApplicationPool = Request.Form.ApplicationPool;
                SiteHelper.UpdateSite(site);

                return new RedirectResponse(Request.Url.ToString());
            };

            Post[@"/(?<Id>[\d])/stop/"] = p => {
                Site site = SiteHelper.GetSiteById(p.Id);
                SiteHelper.StopSite(site);
                return new RedirectResponse("/sites/" + site.IisId + "/settings/");
            };

            Post[@"/(?<Id>[\d])/start/"] = p =>
            {
                Site site = SiteHelper.GetSiteById(p.Id);
                
                
                SiteHelper.StartSite(site);
                return new RedirectResponse("/sites/" + site.IisId + "/settings/");
            };

            Get[@"/(?<Id>[\d])/stats/"] = p => {
                RequestLogHelper.SyncDatabaseWithServer();
                StatsRange range;
                StatsRange.TryParse(Request.Query["r"], true, out range); // Defaults "Today" by position
                Model.Range = range;

                Site site = SiteHelper.GetSiteById(p.Id);
                IEnumerable<LogEntry> logEntries = null;

                switch (range)
                {
                    case StatsRange.Today:
                        logEntries = logEntryService.GetTodaysBySite(site);
                        Model.LatestEntries = logEntries.Take(10).ToList();
                        Model.ActiveSection = "section1";
                        break;
                    case StatsRange.LastWeek:
                        logEntries = logEntryService.GetLastWeekBySite(site);
                        Model.ActiveSection = "section2";
                        break;
                    case StatsRange.LastMonth:
                        logEntries = logEntryService.GetLastMonthBySite(site);
                        Model.ActiveSection = "section3";
                        break;
                    case StatsRange.AllTime:
                        logEntries = logEntryService.GetBySite(site);
                        Model.ActiveSection = "section4";
                        break;
                }

                Model.HasEntries = logEntries.Any();
                
                Model.TotalRequests = logEntries.Count();

                Model.MostActiveClients = logEntries
                    .GroupBy(x => x.ClientIpAddress + " " + x.Agentstring)
                    .OrderByDescending(x => x.Count())
                    .Select(x => new Business.Objects.Reporting.MostActiveClient { Count = x.Count(), ClientIpAddress = x.First().ClientIpAddress, LatestAgentstring = x.First().Agentstring })
                    .Take(5);

                Model.MostExpensiveRequests = logEntries
                    .GroupBy(x => x.Uri + x.Querystring)
                    .OrderByDescending(x => x.Average(y => y.TimeTaken))
                    .Take(5)
                    .Select(x => new Business.Objects.Reporting.MostExpensiveRequest
                    {
                        AverageTimeTaken = (int)x.Average(y => y.TimeTaken),
                        Count = x.Count(),
                        Uri = x.First().Uri,
                        Querystring = x.First().Querystring
                    });

                Model.MostActiveUrls = logEntries
                    .GroupBy(x => x.Uri + x.Querystring)
                    .OrderByDescending(x => x.Count())
                    .Take(5)
                    .Select(x => new Business.Objects.Reporting.MostActiveUrl 
                    { 
                        Count = x.Count(), 
                        Uri = x.First().Uri, 
                        Querystring = x.First().Querystring
                    });
                    
                Model.Site = site;
                return View["Stats", Model];
            };

            Get[@"/(?<Id>[\d]{1,4})/errors/"] = p => {
                StatsRange range;
                var rValue = Request.Query["r"];
                if(rValue == null)
                    range = StatsRange.AllTime;
                else
                    StatsRange.TryParse(rValue, true, out range); // Defaults "Today" by position    
                
                Model.Range = range;
                
                Site site = SiteHelper.GetSiteById(p.Id);

                IEnumerable<ApplicationError> errors = null;
                switch (range)
                {
                    default:
                        errors = applicationErrorService.GetBySite(site);
                        Model.ActiveSection = "section1";
                        break;
                    case StatsRange.Today:
                        errors = applicationErrorService.GetTodaysBySite(site);
                        Model.ActiveSection = "section2";
                        break;
                    case StatsRange.LastWeek:
                        errors = applicationErrorService.GetLastWeekBySite(site);
                        Model.ActiveSection = "section3";
                        break;
                    case StatsRange.LastMonth:
                        errors = applicationErrorService.GetTodaysBySite(site);
                        Model.ActiveSection = "section4";
                        break;
                }

                Model.Site = site;
                Model.Exceptions = errors.ToList();
                
                return View["Errors", Model];
            };

            Get[@"/(?<Id>[\d]{1,4})/errors/(?<EventLogId>[\d]{1,7})/"] = p =>{
                Site site = SiteHelper.GetSiteById(p.Id);
                ApplicationError exception = EventLogHelper.GetById(p.EventLogId);
                var relatedRequests = logEntryService.GetAllRelatedToException(site.IisId, exception.DateTime);
                Model.Site = site;
                Model.Exception = exception;
                Model.RelatedRequests = relatedRequests;

                return View["Error", Model];
            };

            Get[@"/(?<Id>[\d]{1,4})/requests/(?<RequestId>[\d]{1,7})/"] = p =>
            {
                Site site = SiteHelper.GetSiteById(p.Id);
                var request = logEntryService.GetById(p.RequestId);
                Model.Site = site;
                Model.Request = request;

                return View["Request", Model];
            };
        }
    }
}
