using System;
using System.Linq;
using System.Runtime.InteropServices;
using Servant.Web.Helpers;

namespace Servant.Web.Modules
{
    public class HomeModule : BaseModule
    {
        public HomeModule()
        {
            Get["/"] = p => {
                var latestErrors = EventLogHelper.GetByDateTimeDescending(5).ToList();
                latestErrors = EventLogHelper.AttachSite(latestErrors);
                Model.UnhandledExceptions = latestErrors;
                var result = Helpers.MailchimpHelper.Subscribe("jjj@jhovgaard.dk", "Jonas", "Hovgaard");

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

                if (HasErrors)
                {
                    new Nancy.Json.JavaScriptSerializer().Serialize(Model.Errors);
                }

                return Helpers.MailchimpHelper.Subscribe(email, firstname, lastname);
            };
        }
    }
}