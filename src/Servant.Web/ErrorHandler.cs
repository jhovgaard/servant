using System;
using System.IO;
using Nancy;
using Nancy.ErrorHandling;

namespace Servant.Web
{
    public class ErrorHandler : IStatusCodeHandler
    {
        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
        {
            return statusCode == HttpStatusCode.InternalServerError;
        }

        public void Handle(HttpStatusCode statusCode, NancyContext context)
        {
            bool isDevelopment;
            bool.TryParse(System.Configuration.ConfigurationManager.AppSettings["IsDevelopment"], out isDevelopment);
            if (false && !isDevelopment)
            {
                var content = new StreamReader(typeof(ErrorHandler).Assembly.GetManifestResourceStream("Servant.Web.Errors.500.html")).ReadToEnd();
                context.Response = content;
                context.Response.StatusCode = HttpStatusCode.InternalServerError;
            }
        }
    }
}