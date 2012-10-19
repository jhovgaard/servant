using System;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Validation;
using Servant.Business.Objects;
using Servant.Business.Services;
using Nancy.Validation.DataAnnotations;
using Servant.Manager.Infrastructure;

namespace Servant.Manager.Modules
{
    public class SetupModule : BaseModule 
    {
        public SetupModule(SettingsService settingsService)
        {
            var settings = settingsService.LocalSettings;
            var host = TinyIoC.TinyIoCContainer.Current.Resolve<IHost>();

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
                    formSettings.ServantUrl = Business.Helpers.SettingsHelper.FinializeUrl(formSettings.ServantUrl);
                    
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
                        formSettings.Password = Business.Helpers.SecurityHelper.HashPassword(formSettings.Password);
                        formSettings.SetupCompleted = true;
                        settingsService.DeleteAll();
                        settingsService.Insert(formSettings);
                        
                        if(!settings.ParseLogs && formSettings.ParseLogs) 
                            host.StartLogParsing();

                        if (bindingIsChanged)
                        {
                            new System.Threading.Thread(() =>
                            {
                                System.Threading.Thread.Sleep(50);
                                host.Kill();
                                host.Start();
                            }).Start();

                            return Response.AsRedirect(new System.Uri(formSettings.ServantUrl).ToString());
                        }

                        return Response.AsRedirect("/");
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