using Nancy.Authentication.Basic;
using Nancy.Security;
using Servant.Web.Helpers;

namespace Servant.Web.Infrastructure
{
    public class UserValidator : IUserValidator
    {
        public IUserIdentity Validate(string username, string password)
        {
            var settings = SettingsHelper.Settings;
            var isUsernameCorrect = username == settings.Username;
            var isPasswordCorrect = Business.Helpers.SecurityHelper.IsPasswordValid(password, settings.Password);
            
            if(isUsernameCorrect && isPasswordCorrect)
                return new UserIdentity(username, null);

            return null;
        }
    }
}