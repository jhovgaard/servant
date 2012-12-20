using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using Servant.Manager.Infrastructure;

namespace Servant.Aspnet
{
    public class Bootstrapper : Servant.Manager.Bootstrapper
    {
        
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            TinyIoCContainer.Current.Register<IHost, Host>();
            base.ApplicationStartup(container, pipelines);
        }
    }
}