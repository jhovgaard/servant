using System;
using System.Collections.Generic;
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
                latestErrors = EventLogHelper.AttachSite(latestErrors, Page.Sites);
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

                var serializer = new Nancy.Json.JavaScriptSerializer();

                if (HasErrors)
                {
                    return new { Type = MessageType.Error.ToString(), Errors = serializer.Serialize(Model.Errors) }; ;
                }

                var response = MailchimpHelper.Subscribe(email, firstname, lastname);

                if (response != "true")
                {
                    MailchimpResponse result = serializer.Deserialize<MailchimpResponse>(response);

                    if (result.Code == 214)
                    {
                        SetNewsletterRead();
                    }

                    if (result.Code == 502)
                    {
                        AddPropertyError("email", "Looks like the email is not valid.");
                        return new { Type = MessageType.Error, Errors = serializer.Serialize(Model.Errors) };
                    }

                    return new { Message = result.Error, Type = MessageType.Error.ToString() };
                }

                SetNewsletterRead();

                return new { Message = response, Type = MessageType.Success.ToString() };
            };
        }

        private void SetNewsletterRead()
        {
            var configuration = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<ServantConfiguration>();
            configuration.HaveSeenNewsletter = true;
            Helpers.ConfigurationHelper.UpdateConfiguration(configuration);
        }

        public class MailchimpResponse
        {
            public int Code { get; set; }
            public string Error { get; set; }
        }
    }
}