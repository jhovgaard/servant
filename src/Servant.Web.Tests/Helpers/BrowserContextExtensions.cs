using Nancy.Testing;

namespace Servant.Web.Tests.Helpers
{
    public static class BrowserContextExtensions
    {
         public static BrowserContext Authenticated(this BrowserContext context)
         {
             context.BasicAuth("admin", "servant");
             return context;
         }
    }
}