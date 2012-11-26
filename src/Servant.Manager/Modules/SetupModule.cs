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
                    formSettings.ServantUrl = BindingHelper.FinializeBinding(formSettings.ServantUrl);
                    
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
                            After += ctx => ctx.Items.Add("RebootNancyHost", true);
                            return Response.AsRedirect(formSettings.ServantUrl);
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