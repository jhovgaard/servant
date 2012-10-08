using Nancy;
using Nancy.Authentication.Basic;

namespace Servant.Manager.Modules
{
    public class AuthenticationModule : NancyModule
    {
        public AuthenticationModule()
        {
            Get["/logout/"] = _ =>
                                  {
                return new Response().WithStatusCode(401);
            };
        }
    }
}