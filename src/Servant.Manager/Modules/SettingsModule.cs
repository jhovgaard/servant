using Nancy;
using Nancy.ModelBinding;
using Nancy.Validation;
using Servant.Business.Helpers;
using Servant.Business.Objects;
using Servant.Business.Services;
using Servant.Manager.Infrastructure;

//using Servant.Server.Selfhost;

namespace Servant.Manager.Modules
{
    public class SettingsModule : BaseModule
    {
        private SettingsService settingsService = TinyIoC.TinyIoCContainer.Current.Resolve<SettingsService>();

        public SettingsModule() : base("/settings/")
        {
            var host = TinyIoC.TinyIoCContainer.Current.Resolve<IHost>();

            Get["/"] = p => {
                var settings = settingsService.LocalSettings;
                Model.OriginalServantUrl = settings.ServantUrl;
                Model.Settings = settings;

                return View["Index", Model];
            };

            Post["/"] = p => {
                var settings = settingsService.LocalSettings;
                var formSettings = this.Bind<Settings>();
                formSettings.ServantUrl = BindingHelper.FinializeBinding(formSettings.ServantUrl);

                var validationResult = this.Validate(formSettings);

                var bindingIsChanged = formSettings.ServantUrl != settings.ServantUrl;
                
                if(validationResult.IsValid)
                {
                    formSettings.Password = string.IsNullOrWhiteSpace(formSettings.Password) 
                        ? settings.Password 
                        : Business.Helpers.SecurityHelper.HashPassword(formSettings.Password);

                    formSettings.SetupCompleted = true;
                    formSettings.ParseLogs = settings.ParseLogs;

                    settingsService.DeleteAll();
                    settingsService.Insert(formSettings);
                    settingsService.ReloadLocalSettings();
                    
                    host.LoadSettings();
                    AddMessage("Settings have been saved.");

                    if(bindingIsChanged)
                    {
                        new System.Threading.Thread(() =>
                        {
                            System.Threading.Thread.Sleep(50);
                            host.Kill();
                            host.Start();
                        }).Start();
                        
                        return Response.AsRedirect(new System.Uri(formSettings.ServantUrl + "settings/").ToString());
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
                settingsService.ReloadLocalSettings();

                return Response.AsRedirect("/settings/");
            };
        }
    }
}