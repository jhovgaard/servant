using NUnit.Framework;
using Nancy;
using Nancy.Helpers;
using Nancy.Testing;
using Servant.Web.Tests.Helpers;

namespace Servant.Web.Tests
{
    [TestFixture]
    public class SetupModuleTests
    {
        [Test]
        public void Redirects_To_Setup_Page_When_No_Config()
        {
            var browser = new BrowserBuilder().WithoutConfiguration().Build();
            var response = browser.Get("/", with => with.HttpRequest());

            response.ShouldHaveRedirectedTo("/setup/1/");
        }

        [Test]
        public void Can_Show_Setup_Page()
        {
            var browser = new BrowserBuilder().WithoutConfiguration().Build();
            var response = browser.Get("/setup/1/", with => with.HttpRequest());

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Test]
        public void CanSaveConfiguration()
        {
            var browser = new BrowserBuilder().WithoutConfiguration().Build();
            const string servantUrl = "http://localhost:54444/";
            var response = browser.Post("/setup/1/", with =>
                {
                    with.FormValue("servanturl", servantUrl);
                    with.FormValue("username", "admin");
                    with.FormValue("password", "servant");
                    with.FormValue("autosendcrashreport", "true");
                    with.FormValue("acceptterms", "true");
                });

            response.ShouldHaveRedirectedTo("/setup/confirm/?url=" + HttpUtility.UrlEncode(servantUrl));
        }

        [Test]
        public void Blocks_Setup_Page_When_Already_Installed()
        {
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();
            var response = browser.Get("(/setup/1/", with => with.HttpRequest());

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}