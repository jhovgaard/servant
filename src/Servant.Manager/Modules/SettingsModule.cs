using System;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Validation;
using Servant.Business.Objects;
using Servant.Business.Services;
using Servant.Manager.Infrastructure;

//using Servant.Server.Selfhost;

namespace Servant.Manager.Modules
{
    public class SettingsModule : BaseModule
    {
        public SettingsModule(SettingsService settingsService, IHost host) : base("/settings/")
        {
            Get["/"] = p => {
                var settings = settingsService.LocalSettings;
                Model.OriginalServantUrl = settings.ServantUrl;
                Model.Settings = settings;

                return View["Index", Model];
            };

            Post["/"] = p => {
                var settings = settingsService.LocalSettings;
                var formSettings = this.Bind<Settings>();
                formSettings.ServantUrl = Business.Helpers.SettingsHelper.FinializeUrl(formSettings.ServantUrl);

                var validationResult = this.Validate(formSettings);

                var bindingIsChanged = formSettings.ServantUrl != settings.ServantUrl;
                
                if(validationResult.IsValid)
                {
                    formSettings.Password = string.IsNullOrWhiteSpace(formSettings.Password) 
                        ? settings.Password 
                        : Business.Helpers.SecurityHelper.HashPassword(formSettings.Password);
                    
                    settingsService.DeleteAll();
                    settingsService.Insert(formSettings);

                    if(bindingIsChanged)
                    {
                        host.Kill();
                        host.Start();    
                        return true;
                    }
                }

                AddValidationErrors(validationResult);
                Model.OriginalServantUrl = settings.ServantUrl;
                Model.Settings = formSettings;

                return View["Index", Model];
            };

            Post["/startlogparsing/"] = _ => {
                
                var settings = settingsService.LocalSettings;
                var start = (bool) Request.Form.Start;
                
                if(!settings.ParseLogs && start)
                    host.StartLogParsing();

                if(settings.ParseLogs && !start)
                    host.StopLogParsing();

                settings.ParseLogs = start;
                settingsService.DeleteAll();
                settingsService.Insert(settings);

                return Response.AsRedirect("/settings/");
            };
        }
    }
}