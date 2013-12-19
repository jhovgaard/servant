using System;
using Nancy;
using Nancy.Testing;
using NUnit.Framework;

namespace Servant.Web.Tests.Helpers
{
    public static class ServantAsserts
    {
        public static void ResponseIsOkAndContainsData(BrowserResponse response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Assert.Fail("Response StatusCode is not 200 OK.");
            }

            try
            {
                if (response.Body.AsString().Length <= 0)
                {
                    Assert.Fail("Response body is empty.");
                }
            }
            catch (NullReferenceException)
            {
                Assert.Fail("Response body is empty.");
            }
            
        }
    }
}