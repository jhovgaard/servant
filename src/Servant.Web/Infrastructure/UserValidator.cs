using Nancy.Authentication.Basic;
using Nancy.Security;
using Servant.Business.Objects;
using Servant.Web.Helpers;

namespace Servant.Web.Infrastructure
{
    public class UserValidator : IUserValidator
    {
        public IUserIdentity Validate(string username, string password)
        {
            var configuration = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<ServantConfiguration>();
            var isUsernameCorrect = username == configuration.Username;
            var isPasswordCorrect = Business.Helpers.SecurityHelper.IsPasswordValid(password, configuration.Password);
            
            if(isUsernameCorrect && isPasswordCorrect)
                return new UserIdentity(username, null);

            return null;
        }
    }
}