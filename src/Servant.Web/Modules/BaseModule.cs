using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using Nancy.Security;
using Nancy;
using Nancy.Validation;
using Servant.Business.Objects;
using Servant.Web.Helpers;
using Servant.Web.Views.Shared.Models;

namespace Servant.Web.Modules
{
    public class BaseModule : NancyModule 
    {
        public dynamic Model = new ExpandoObject();
        protected PageModel Page { get; set; }
        public bool HasErrors { get { return Model.Errors.Count != 0; }}
        public ServantConfiguration Configuration { get; set; }
        private const string MessageKey = "Message";
        private const string MessageTypeKey = "MessageType";

        public BaseModule()
        {
            Setup();
        }

        public BaseModule(string modulePath) : base(modulePath)
        {
            Setup();
        }
            
        public void Setup()
        {
            Configuration = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<ServantConfiguration>();

            var nonAuthenticatedModules = new List<Type> { typeof(SetupModule) };
            var requiresAuthentication = !nonAuthenticatedModules.Contains(this.GetType());

            Before += ctx =>
            {
                Model.Title = "Servant for IIS";
                Page = new PageModel
                {
                    Servername = System.Environment.MachineName,
                    Sites = SiteManager.GetSites().OrderBy(x => x.Name)
                };
                var fileVersion = FileVersionInfo.GetVersionInfo(typeof (BaseModule).Assembly.Location).FileVersion.Split('.');
                Model.Version = string.Join(".", fileVersion.Take(2));
                Model.Page = Page;
                Model.Errors = new List<Error>();
                Model.InstallationGuid = Configuration.InstallationGuid;
                bool IsDevelopment;
                bool.TryParse(System.Configuration.ConfigurationManager.AppSettings["IsDevelopment"], out IsDevelopment);
                Model.IsDevelopment = IsDevelopment;
                Model.AutoSendCrashReport = Configuration.AutoSendCrashReport;

                return null;
            };

            After += ctx =>
            {
                var redirectStatusCodes = new [] { HttpStatusCode.TemporaryRedirect, HttpStatusCode.SeeOther, HttpStatusCode.MovedPermanently };
                Model.ErrorsAsJson = new Nancy.Json.JavaScriptSerializer().Serialize(Model.Errors);
                
                if (!redirectStatusCodes.Contains(ctx.Response.StatusCode))
                {
                    Model.Message = Session[MessageKey];
                    Model.MessageType = Session[MessageTypeKey];
                    Session[MessageKey] = null;
                }
                
                if (!Configuration.SetupCompleted && requiresAuthentication)
                    ctx.Response = Response.AsRedirect("/setup/1/");
            };


            if (requiresAuthentication)
            {
                this.RequiresAuthentication();
            }

        }

        public void AddGlobalError(string message)
        {
            Model.Errors.Add(new Error(message));
        }

        public void AddPropertyError(string propertyName, string message)
        {
            Model.Errors.Add(new Error(false,message,propertyName));
        }

        public void AddValidationErrors(ModelValidationResult result)
        {
            Model.Errors.AddRange(Helpers.ErrorHelper.ConvertValidationResultToErrorList(result));
        }

        public enum MessageType
        {
            Success,
            Error,
            Info
        }

        public void AddMessage(string message, params string[] args)
        {
            AddMessage(message, MessageType.Info, args);
        }

        public void AddMessage(string message, MessageType type = MessageType.Info, params string[] args)
        {
            Session[MessageKey] = string.Format(message, args);
            Session[MessageTypeKey] = type;
        }
    }
}