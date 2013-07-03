using NUnit.Framework;
using Nancy;
using Servant.Business.Objects;
using Servant.Web.Helpers;
using Servant.Web.Tests.Helpers;

namespace Servant.Web.Tests
{
    [TestFixture]
    public class SettingsModuleTests
    {
        [Test]
        public void Can_Show_Settings_Page()
        {
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();

            var response = browser.Get("/settings/", with =>
                {
                    with.HttpRequest();
                    with.Authenticated();
                });

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Test]
        public void Can_Save_Settings()
        {
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();
            var configuration = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<ServantConfiguration>();

            var response = browser.Post("/settings/", with =>
                {
                    with.Authenticated();
                    with.HttpRequest();
                    with.FormValue("servanturl", configuration.ServantUrl);
                    with.FormValue("debug", true.ToString());
                    with.FormValue("autosendcrashreport", true.ToString());
                    with.FormValue("username", configuration.Username);
                    with.FormValue("password", "servant");
                });

            configuration = ConfigurationHelper.GetConfigurationFromDisk();

            Assert.AreEqual(true, configuration.Debug);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }
}