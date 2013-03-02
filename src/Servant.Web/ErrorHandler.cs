using System;
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
            if (!isDevelopment)
            {
                var content = System.IO.File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Errors/500.html"));
                context.Response = content;    
            }
        }
    }
}