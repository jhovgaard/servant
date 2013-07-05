using Nancy.Testing;
using Servant.Business.Objects;

namespace Servant.Web.Tests.Helpers
{
    public class BrowserBuilder
    {
        private readonly Nancy.Bootstrapper.INancyBootstrapper _bootstrapper;
        private Browser _browser;

        public BrowserBuilder()
        {
            _bootstrapper = new Bootstrapper();
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
            Nancy.TinyIoc.TinyIoCContainer.Current.Register<ServantConfiguration>(new ServantConfiguration() { 
                Password = "CsVHVDq2Dri7EFNscyFt5V/MpdxbZ8XRynGt+LxtMO3KafHRzy0AOJbBWWxkZ6gbOFBUuTPQxGcpnjcxXBN/qA==", // "servant"
                SetupCompleted = true,
                Debug = true
            });
            return this;
        }
    }
}