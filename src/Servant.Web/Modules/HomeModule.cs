using System;
using System.Linq;
using System.Runtime.InteropServices;
using Servant.Business.Objects;
using Servant.Web.Helpers;

namespace Servant.Web.Modules
{
    public class HomeModule : BaseModule
    {
        public HomeModule()
        {
            Get["/"] = p => {
                var configuration = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<ServantConfiguration>();

                var latestErrors = EventLogHelper.GetByDateTimeDescending(5).ToList();
                latestErrors = EventLogHelper.AttachSite(latestErrors);
                Model.UnhandledExceptions = latestErrors;
                Model.HaveSeenNewsletter = configuration.HaveSeenNewsletter;
                return View["Index", Model];
            };

            Post["/subscribetonewsletter"] = p => {
                var email = Request.Form.Email;
                var firstname = Request.Form.Firstname;
                var lastname = Request.Form.Lastname;

                try
                {
                    new System.Net.Mail.MailAddress(email);
                }
                catch
                {
                    AddPropertyError("email", "Looks like the email is not valid.");
                }

                var result = MailchimpHelper.Subscribe(email, firstname, lastname);
                if(result.ToString().Contains("\"code\":502"))
                    AddPropertyError("email", "Looks like the email is not valid.");

                if (HasErrors)
                {
                    return new Nancy.Json.JavaScriptSerializer().Serialize(Model.Errors);
                }

                var configuration = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<ServantConfiguration>();
                configuration.HaveSeenNewsletter = true;
                Helpers.ConfigurationHelper.UpdateConfiguration(configuration);

                return result;
            };
        }
    }
}