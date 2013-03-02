using System;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Validation;
using Servant.Business;
using Servant.Business.Helpers;
using Servant.Business.Objects;
using Servant.Web.Infrastructure;

namespace Servant.Web.Modules
{
    public class SetupModule : BaseModule 
    {
        public SetupModule()
        {
            var settings = Helpers.SettingsHelper.Settings;
            var host = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<IHost>();

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
                    Model.AutoSendCrashReport = true;
                    Model.OriginalServantUrl = settings.ServantUrl;

                    return View["1", Model];
                };

                Post["/setup/1/"] = _ => {
                    var formSettings = this.Bind<Settings>();   
                    var originalInputtedServantUrl = formSettings.ServantUrl;

                    if(BindingHelper.SafeFinializeBinding(formSettings.ServantUrl) == null)
                        AddPropertyError("servanturl", "URL is invalid.");
                    else
                        formSettings.ServantUrl = BindingHelper.FinializeBinding(formSettings.ServantUrl);
                    
                    var validationResult = this.Validate(formSettings);

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
                        formSettings.AutoSendCrashReport = (bool)Request.Form.AutoSendCrashReport;
                        Helpers.SettingsHelper.UpdateSettings(formSettings);
                        
                        if (!settings.EnableErrorMonitoring && formSettings.EnableErrorMonitoring) 
                            host.StartLogParsing();

                        var isHttps = formSettings.ServantUrl.StartsWith("https://");
                        if (isHttps)
                        {
                            var port = new Uri(formSettings.ServantUrl).Port;
                            host.AddCertificateBinding(port);
                        }

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