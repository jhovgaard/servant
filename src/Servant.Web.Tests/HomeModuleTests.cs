using NUnit.Framework;
using Nancy;
using Nancy.Testing;
using Servant.Web.Tests.Helpers;

namespace Servant.Web.Tests
{
    [TestFixture]
    public class HomeModuleTests
    {
        [Test]
        public void Can_Show_Home_Page()
        {
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();
            var response = browser.Get("/", with =>
                {
                    with.Authenticated();
                    with.HttpRequest();
                });

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [Test]
        public void Cannot_Show_Home_Page_With_Wrong_Credentials()
        {
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();
            var response = browser.Get("/", with =>
            {
                with.BasicAuth("wronguser", "wrongpass");
                with.HttpRequest();
            });

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}