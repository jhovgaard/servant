using System;
using Servant.Business.Services;

//using Servant.Server.Selfhost;

namespace Servant.Manager.Modules
{
    public class SettingsModule : BaseModule
    {
        public SettingsModule(SettingsService settingsService) : base("/settings/")
        {
            Get["/"] = p => {
                var settings = settingsService.LocalSettings;
                Model.OriginalBinding = settings.Hostname + ":" + settings.Port;
                Model.Settings = settings;

                return View["Index", Model];
            };

            Post["/"] = p => {
              
                string[] bindingInfo = Request.Form.Binding.ToString()
                    .Replace("http://", "")
                    .Split(':');
                string newHostname = "";

                if(bindingInfo == null)
                    AddPropertyError("binding", "Servant binding can't be empty.");
                else
                    newHostname = bindingInfo[0];

                if(string.IsNullOrWhiteSpace(newHostname))
                    AddPropertyError("binding", "Binding is required.");
                
                if (string.IsNullOrWhiteSpace(Request.Form.Username))
                    AddPropertyError("username", "Username is required.");

                
                var newPort = bindingInfo.Length > 1 ? Convert.ToInt32(bindingInfo[1]) : 80;
                var settings = settingsService.LocalSettings;
                var originalBinding = settings.Hostname + ":" + settings.Port;
                
                var bindingIsChanged = newHostname != settings.Hostname || newPort != settings.Port;

                settings.Debug = Request.Form.Debug;
                settings.Hostname = newHostname;
                settings.Port = newPort;
                settings.Username = Request.Form.Username;

                if(!HasErrors)
                {
                    if (!string.IsNullOrWhiteSpace(Request.Form.Password))
                        settings.Password = Business.Helpers.SecurityHelper.HashPassword(Request.Form.Password);

                    settingsService.DeleteAll();
                    settingsService.Insert(settings);
                
                    if(bindingIsChanged)
                    {
                        //Host.Kill();
                        //Host.Start();    
                        return true;
                    }
                }

                Model.OriginalBinding = originalBinding;
                Model.Settings = settings;
                return View["Index", Model];
            };
        }
    }
}