using Nancy;
using Nancy.ModelBinding;
using Nancy.Validation;
using Servant.Business.Helpers;
using Servant.Business.Objects;
using Servant.Manager.Infrastructure;

namespace Servant.Manager.Modules
{
    public class SettingsModule : BaseModule
    {
        public SettingsModule() : base("/settings/")
        {
            var host = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<IHost>();
            var settings = Helpers.SettingsHelper.Settings;

            Get["/"] = p => {
                Model.OriginalServantUrl = settings.ServantUrl;
                Model.Settings = settings;

                return View["Index", Model];
            };

            Post["/"] = p => {
                var formSettings = this.Bind<Settings>();

                if (BindingHelper.SafeFinializeBinding(formSettings.ServantUrl) == null)
                    AddPropertyError("servanturl", "The URL is invalid.");
                else
                    formSettings.ServantUrl = BindingHelper.FinializeBinding(formSettings.ServantUrl);
                                 

                var validationResult = this.Validate(formSettings);
                AddValidationErrors(validationResult);

                var bindingIsChanged = formSettings.ServantUrl != settings.ServantUrl;
                
                if(!HasErrors)
                {
                    formSettings.Password = string.IsNullOrWhiteSpace(formSettings.Password) 
                        ? settings.Password 
                        : Business.Helpers.SecurityHelper.HashPassword(formSettings.Password);

                    formSettings.SetupCompleted = true;
                    formSettings.ParseLogs = settings.ParseLogs;

                    Helpers.SettingsHelper.UpdateSettings(formSettings);
                    AddMessage("Settings have been saved.");

                    if(bindingIsChanged)
                    {
                        new System.Threading.Thread(() =>
                        {
                            System.Threading.Thread.Sleep(50);
                            host.Kill();
                            host.Start();
                        }).Start();

                        Model.IsWildcard = Settings.ServantUrl.StartsWith("https://*") ||
                                           Settings.ServantUrl.StartsWith("http://*");

                        Model.NewUrl = Settings.ServantUrl;
                        return View["BindingChanged", Model];
                    }
                }

                Model.OriginalServantUrl = settings.ServantUrl;
                Model.Settings = formSettings;

                return View["Index", Model];
            };

            Post["/startlogparsing/"] = _ => {
                
                var start = (bool) Request.Form.Start;
                
                if(!settings.ParseLogs && start)
                    host.StartLogParsing();

                if(settings.ParseLogs && !start)
                    host.StopLogParsing();

                settings.ParseLogs = start;
                Helpers.SettingsHelper.UpdateSettings(settings);
                
                return Response.AsRedirect("/settings/");
            };
        }
    }
}