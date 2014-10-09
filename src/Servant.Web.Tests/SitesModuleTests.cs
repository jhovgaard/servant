using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Nancy;
using Nancy.Testing;
using Servant.Business.Objects;
using Servant.Business.Objects.Enums;
using Servant.Shared;
using Servant.Web.Helpers;
using Servant.Web.Tests.Helpers;

namespace Servant.Web.Tests
{
    [TestFixture]
    public class SitesModuleTests
    {
        private Site _testSite = new Site
        {
            ApplicationPool = null,
            Bindings = new List<Binding> { new Binding { UserInput = "http://unit-test-site.com:80", Port = 80, Hostname = "unit-test-site.com", Protocol = Protocol.http, IpAddress = "*"} },
            SitePath = @"c:\inetpub\wwwroot",
            Name = "unit-test-site"
        };

        [TestFixtureTearDown]
        public void Cleanup()
        {
            var site = SiteManager.GetSiteByName(_testSite.Name);
            if(site != null)
                SiteManager.DeleteSite(site.IisId);
        }

        [Test]
        public void Can_Show_CreateSite_Page()
        {
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();
            var response = browser.Get("/sites/create", with =>
                {
                    with.Authenticated();
                    with.HttpRequest();
                });

            ServantAsserts.ResponseIsOkAndContainsData(response);
        }
 
        [Test]
        public void Can_Create_Site()
        {
            Cleanup();
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();

            var response = browser.Post("/sites/create/", with =>
                {
                    with.Authenticated();
                    with.HttpRequest();
                    with.FormValue("name", _testSite.Name);
                    with.FormValue("sitepath", @"c:\inetpub\wwwroot");
                    with.FormValue("bindingsuserinput", "http://unit-test-site.com");
                    with.FormValue("bindingsipaddress", "*");
                    with.FormValue("bindingscertificatename", "Servant");
                    with.FormValue("applicationpool", "");
                });

            var body = response.Body.AsString();

            var urlRegex = new Regex(@"/sites/(\d+)/settings", RegexOptions.IgnoreCase);
            string headerLocation;
            response.Headers.TryGetValue("Location", out headerLocation);
            
            Assert.IsTrue(urlRegex.IsMatch(headerLocation ?? ""));
        }

        [Test]
        public void Can_Show_Settings_Page()
        {
            var testSite = GetTestSiteFromIis();
            
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();
            var response = browser.Get("/sites/" + testSite.IisId + "/settings/", with =>
                {
                    with.HttpRequest(); 
                    with.Authenticated();
                });

            ServantAsserts.ResponseIsOkAndContainsData(response);
        }
        
        [Test]
        public void Can_Save_Site_Settings()
        {
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();
            var originalSite = GetTestSiteFromIis();

            var response = browser.Post("/sites/" + originalSite.IisId + "/settings/", with =>
                {
                    with.Authenticated();
                    with.HttpRequest();
                    with.FormValue("name", originalSite.Name);
                    with.FormValue("sitepath", originalSite.SitePath);
                    with.FormValue("bindingsuserinput", "http://unit-test-site-edited.com");
                    with.FormValue("bindingsipaddress", "*");
                    with.FormValue("bindingscertificatename", "Servant");
                    with.FormValue("applicationpool", originalSite.ApplicationPool);
                });

            var body = response.Body.AsString();

            StringAssert.Contains("var message = \"Settings have been saved.\"", body);
        }

        [Test]
        public void Cannot_Save_Site_Settings_With_Errors()
        {
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();
            var originalSite = GetTestSiteFromIis();

            var response = browser.Post("/sites/" + originalSite.IisId + "/settings/", with =>
            {
                with.Authenticated();
                with.HttpRequest();
                with.FormValue("name", originalSite.Name);
                with.FormValue("sitepath", originalSite.SitePath);
                with.FormValue("bindingsuserinput", "http://unit-test-site-edite%%d.com");
                with.FormValue("bindingsipaddress", "*");
                with.FormValue("bindingscertificatename", "Servant");
                with.FormValue("applicationpool", originalSite.ApplicationPool);
            });

            Assert.AreEqual(response.StatusCode, HttpStatusCode.OK);
            
            var body = response.Body.AsString();

            StringAssert.Contains("\"Message\":\"The binding is invalid.\",\"PropertyName\":\"bindingsuserinput[0]\"", body);
        }

        [Test]
        public void Can_Delete_Site()
        {
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();
            var originalSite = GetTestSiteFromIis();

            var response = browser.Post("/sites/" + originalSite.IisId + "/delete/", with =>
                {
                    with.Authenticated();
                    with.HttpRequest();
                });

            response.ShouldHaveRedirectedTo("/");
        }

        [Test]
        public void Can_Create_Application()
        {
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();
            var site = GetTestSiteFromIis();

            var response = browser.Post("/sites/" + site.IisId + "/applications/", with =>
            {
                with.Authenticated();
                with.HttpRequest();
                with.FormValue("path", "virtualapptest");
                with.FormValue("applicationpool", site.ApplicationPool);
                with.FormValue("diskpath", "C:");
            });

            var body = response.Body.AsString();
            StringAssert.Contains("var message = \"Applications have been saved.\";", body);
        }

        [Test]
        public void Cannot_Create_Application_With_Invalid_Path()
        {
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();
            var site = GetTestSiteFromIis();

            var response = browser.Post("/sites/" + site.IisId + "/applications/", with =>
            {
                with.Authenticated();
                with.HttpRequest();
                with.FormValue("path", "virtual%¤#<*>apptest");
                with.FormValue("applicationpool", site.ApplicationPool);
                with.FormValue("diskpath", "C:");
            });

            var body = response.Body.AsString();
            StringAssert.Contains("Path cannot contain the following characters", body);
        }

        private Site GetTestSiteFromIis()
        {
            var testSite = SiteManager.GetSiteByName(_testSite.Name);

            if (testSite == null)
                SiteManager.CreateSite(_testSite);

            testSite = SiteManager.GetSiteByName(_testSite.Name);

            return testSite;
        }

    }
}