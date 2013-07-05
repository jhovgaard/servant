using Nancy;
using Nancy.Testing.Fakes;

namespace Servant.Web.Tests.Helpers
{
    public class Bootstrapper : Server.Bootstrapper
    {
        // Using this because http://stackoverflow.com/questions/15138994/sequence-contains-more-than-one-element-an-nancybootstrapperbase-class
        protected override IRootPathProvider RootPathProvider
        {
            get
            {
                return new FakeRootPathProvider();
            }
        }
    }
}