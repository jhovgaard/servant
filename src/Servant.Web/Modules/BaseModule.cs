using System.Collections.Generic;
using System.Dynamic;
using Servant.Web.Objects;
using Servant.Web.Views.Shared.Models;
using Nancy;

namespace Servant.Web.Modules
{
    public class BaseModule : NancyModule 
    {
        public dynamic Model = new ExpandoObject();
        protected PageModel Page { get; set; }

        public BaseModule()
        {
            SetupModelDefaults();
        }

        public BaseModule(string modulePath) : base(modulePath)
        {
            SetupModelDefaults();
        }
            
        public void SetupModelDefaults() 
        {
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
        }

        public void AddGlobalError(string message)
        {
            Model.Errors.Add(new Error { IsGlobal = true, Message = message});
        }

        public void AddPropertyError(string propertyName, string message)
        {
            Model.Errors.Add(new Error { IsGlobal = false, PropertyName = propertyName, Message = message });
        }
    }
}