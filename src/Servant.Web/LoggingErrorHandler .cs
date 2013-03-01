using System;
using System.Diagnostics;
using Nancy;
using Nancy.ErrorHandling;

namespace Servant.Web
{
    public class LoggingErrorHandler : IErrorHandler
    {
        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
        {
            return statusCode == HttpStatusCode.InternalServerError;
        }

        public void Handle(HttpStatusCode statusCode, NancyContext context)
        {
            object errorObject;
            context.Items.TryGetValue(NancyEngine.ERROR_EXCEPTION, out errorObject);
            var error = errorObject as Exception;

            if (error != null)
            {
                EventLog.WriteEntry("Servant for IIS", "Message: " + error.InnerException.Message + Environment.NewLine + "Stack:" + Environment.NewLine + error.InnerException.StackTrace);
            }
        }
    }
}