using Nancy.Testing;
using Servant.Business.Objects;

namespace Servant.Web.Tests
{
    public class BrowserBuilder
    {
        private readonly Nancy.Bootstrapper.INancyBootstrapper _bootstrapper;
        private Browser _browser;

        public BrowserBuilder()
        {
            _bootstrapper = new Servant.Server.Bootstrapper();
        }

        public BrowserBuilder WithoutConfiguration()
        {
            Nancy.TinyIoc.TinyIoCContainer.Current.Register<ServantConfiguration>(new ServantConfiguration());
            return this;
        }

        public Browser Build()
        {
            return new Browser(_bootstrapper);
        }

        public BrowserBuilder WithDefaultConfiguration()
        {
            Nancy.TinyIoc.TinyIoCContainer.Current.Register<ServantConfiguration>(new ServantConfiguration() { Password = "servant", SetupCompleted = true});
            return this;
        }
    }
}