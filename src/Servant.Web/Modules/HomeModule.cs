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

                if (HasErrors)
                {
                    return new Nancy.Json.JavaScriptSerializer().Serialize(Model.Errors);
                }

                var serializer = new Nancy.Json.JavaScriptSerializer();
                var response = MailchimpHelper.Subscribe(email, firstname, lastname);

                if (response != "true")
                {
                    MailchimpResponse result = serializer.Deserialize<MailchimpResponse>(response);

                    if (result.Code == 502)
                    {
                        AddPropertyError("email", "Looks like the email is not valid.");
                        return serializer.Serialize(Model.Errors);
                    }
                    
                    return result.Error;
                }

                var configuration = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<ServantConfiguration>();
                configuration.HaveSeenNewsletter = false;
                Helpers.ConfigurationHelper.UpdateConfiguration(configuration);

                return response;
            };
        }

        public class MailchimpResponse
        {
            public int Code { get; set; }
            public string Error { get; set; }
        }
    }
}