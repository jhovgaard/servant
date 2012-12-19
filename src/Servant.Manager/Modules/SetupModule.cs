using System;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Validation;
using Servant.Business.Helpers;
using Servant.Business.Objects;
using Servant.Business.Services;
using Nancy.Validation.DataAnnotations;
using Servant.Manager.Infrastructure;

namespace Servant.Manager.Modules
{
    public class SetupModule : BaseModule 
    {
        public SetupModule()
        {
            var settingsService = TinyIoC.TinyIoCContainer.Current.Resolve<SettingsService>();
            var host = TinyIoC.TinyIoCContainer.Current.Resolve<IHost>();
            var settings = settingsService.LocalSettings;

            Get["/setup/confirm/"] = _ =>
            {
                string url = Request.Query.Url;
                Model.Url = url;

                return View["confirm", Model];
            };

            Get["/setup/restartservant/"] = _ =>
            {
                new System.Threading.Thread(() =>
                    {
                        host.LoadSettings();
                        host.Kill();
                        host.Start();        
                    }).Start();
                return true;
            };

            if(!settings.SetupCompleted)
            {
                Get["/setup/1/"] = _ => {
                    Model.Settings = settings;
                    Model.AcceptTerms = false;
                    Model.OriginalServantUrl = settings.ServantUrl;
                    return View["1", Model];
                };

                Post["/setup/1/"] = _ => {
                    var formSettings = this.Bind<Settings>();
                    var originalInputtedServantUrl = formSettings.ServantUrl;
                    formSettings.ServantUrl = BindingHelper.FinializeBinding(formSettings.ServantUrl);
                    formSettings.ParseLogs = true;

                    var validationResult = this.Validate(formSettings);

                    var bindingIsChanged = formSettings.ServantUrl != settings.ServantUrl;
                    var acceptTerms = (bool)Request.Form.AcceptTerms;

                    AddValidationErrors(validationResult);

                    if(!acceptTerms)
                        AddPropertyError("acceptterms", "You must agree and accept.");

                    if(string.IsNullOrWhiteSpace(formSettings.Password))
                        AddPropertyError("password", "Password cannot be empty.");

                    if(!HasErrors)
                    {
                        formSettings.Password = SecurityHelper.HashPassword(formSettings.Password);
                        formSettings.SetupCompleted = true;
                        settingsService.DeleteAll();
                        settingsService.Insert(formSettings);
                        settingsService.ReloadLocalSettings();

                        if (!settings.ParseLogs && formSettings.ParseLogs) 
                            host.StartLogParsing();

                        return Response.AsRedirect("/setup/confirm/?url=" + Uri.EscapeDataString(formSettings.ServantUrl));
                    }

                    formSettings.ServantUrl = originalInputtedServantUrl;

                    Model.OriginalServantUrl = settings.ServantUrl;
                    Model.Settings = formSettings;
                    Model.AcceptTerms = Request.Form.AcceptTerms;

                    return View["1", Model];
                };
            }
        }
    }
}