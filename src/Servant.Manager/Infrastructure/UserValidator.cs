using System.Diagnostics;
using Nancy.Authentication.Basic;
using Nancy.Security;
using Servant.Business.Services;

namespace Servant.Manager.Infrastructure
{
    public class UserValidator : IUserValidator
    {
        private readonly SettingsService _settingsService;

        public UserValidator(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public IUserIdentity Validate(string username, string password)
        {
            var sw = new Stopwatch();
            sw.Start();
            var settings = _settingsService.LocalSettings;
            sw.Stop();
            sw.Reset();
            sw.Start();
            var isUsernameCorrect = username == settings.Username;
            sw.Stop();
            sw.Reset();
            sw.Start();
            var isPasswordCorrect = Business.Helpers.SecurityHelper.IsPasswordValid(password, settings.Password);
            sw.Stop();
            sw.Reset();


            if(isUsernameCorrect && isPasswordCorrect)
                return new UserIdentity(username, null);

            return null;
        }
    }
}