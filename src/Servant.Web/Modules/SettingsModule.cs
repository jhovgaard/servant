using System;
using System.Threading;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Validation;
using Servant.Business;
using Servant.Business.Helpers;
using Servant.Business.Objects;
using Servant.Web.Infrastructure;

namespace Servant.Web.Modules
{
    public class SettingsModule : BaseModule
    {
        public SettingsModule() : base("/settings/")
        {
            var host = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<IHost>();
            var configuration = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<ServantConfiguration>();

            Get["/"] = p => {
                Model.OriginalServantUrl = configuration.ServantUrl;
                Model.Settings = configuration;
                return View["Index", Model];
            };

            Post["/"] = p => {
                var formSettings = this.Bind<ServantConfiguration>();

                if (BindingHelper.SafeFinializeBinding(formSettings.ServantUrl) == null)
                    AddPropertyError("servanturl", "The URL is invalid.");
                else
                    formSettings.ServantUrl = BindingHelper.FinializeBinding(formSettings.ServantUrl);
                                 

                var validationResult = this.Validate(formSettings);
                AddValidationErrors(validationResult);

                var bindingIsChanged = formSettings.ServantUrl != configuration.ServantUrl;
                
                if(!HasErrors)
                {
                    formSettings.Password = string.IsNullOrWhiteSpace(formSettings.Password) 
                        ? configuration.Password 
                        : Business.Helpers.SecurityHelper.HashPassword(formSettings.Password);

                    formSettings.SetupCompleted = true;
                    formSettings.EnableErrorMonitoring = configuration.EnableErrorMonitoring;

                    Helpers.ConfigurationHelper.UpdateConfigurationOnDisk(formSettings);
                    AddMessage("Settings have been saved.");

                    if(bindingIsChanged)
                    {
                        var oldIsHttps = configuration.ServantUrl.StartsWith("https://");
                        var newIsHttps = formSettings.ServantUrl.StartsWith("https://");

                        if (oldIsHttps)
                        {
                            var port = new Uri(configuration.ServantUrl).Port;
                            host.RemoveCertificateBinding(port);
                        }

                        if (newIsHttps)
                        {
                            var port = new Uri(formSettings.ServantUrl).Port;
                            host.AddCertificateBinding(port);
                        }

                        Model.IsWildcard = Configuration.ServantUrl.StartsWith("https://*") ||
                                           Configuration.ServantUrl.StartsWith("http://*");

                        Model.NewUrl = formSettings.ServantUrl;
                        return View["BindingChanged", Model];
                    }
                }

                Model.OriginalServantUrl = configuration.ServantUrl;
                Model.Settings = formSettings;

                return View["Index", Model];
            };

            Post["/startlogparsing/"] = _ => {
                
                var start = (bool) Request.Form.Start;
                
                if(!configuration.EnableErrorMonitoring && start)
                    host.StartLogParsing();

                if(configuration.EnableErrorMonitoring && !start)
                    host.StopLogParsing();

                configuration.EnableErrorMonitoring = start;
                Helpers.ConfigurationHelper.UpdateConfigurationOnDisk(configuration);
                
                return Response.AsRedirect("/settings/");
            };
        }
    }
}