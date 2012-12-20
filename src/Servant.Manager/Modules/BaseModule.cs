using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Nancy.Security;
using Nancy;
using Nancy.Validation;
using Servant.Business.Objects;
using Servant.Business.Services;
using Servant.Manager.Views.Shared.Models;

namespace Servant.Manager.Modules
{
    public class BaseModule : NancyModule 
    {
        public dynamic Model = new ExpandoObject();
        protected PageModel Page { get; set; }
        public bool HasErrors { get { return Model.Errors.Count != 0; }}
        private SettingsService _settingsService;
        private const string MessageKey = "Message";

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
            _settingsService = Nancy.TinyIoc.TinyIoCContainer.Current.Resolve<SettingsService>();

            var nonAuthenticatedModules = new List<Type> { typeof(SetupModule) };
            var requiresAuthentication = !nonAuthenticatedModules.Contains(this.GetType());

            Before += ctx =>
            {
                Page = new PageModel
                {
                    Servername = System.Environment.MachineName
                };

                Model.Page = Page;
                Model.Errors = new List<Error>();

                return null;
            };

            After += ctx =>
            {
                var redirectStatusCodes = new [] { HttpStatusCode.TemporaryRedirect, HttpStatusCode.SeeOther, HttpStatusCode.MovedPermanently };
                Model.ErrorsAsJson = new Nancy.Json.JavaScriptSerializer().Serialize(Model.Errors);
                
                if (!redirectStatusCodes.Contains(ctx.Response.StatusCode))
                {
                    Model.Message = Session[MessageKey];
                    Session[MessageKey] = null;
                }
                
                if (!_settingsService.LocalSettings.SetupCompleted && requiresAuthentication)
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

        public void AddMessage(string message, params string[] args)
        {
            Session[MessageKey] = string.Format(message, args);
        }
    }
}