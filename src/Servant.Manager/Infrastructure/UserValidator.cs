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
            var settings = _settingsService.LocalSettings;

            var isUsernameCorrect = username == settings.Username;
            var isPasswordCorrect = Business.Helpers.SecurityHelper.IsPasswordValid(password, settings.Password);

            if(isUsernameCorrect && isPasswordCorrect)
                return new UserIdentity(username, null);

            return null;
        }
    }
}