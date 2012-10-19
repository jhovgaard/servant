using Nancy;
using Nancy.Bootstrapper;
using Servant.Manager.Infrastructure;
using TinyIoC;

namespace Servant.Aspnet
{
    public class Bootstrapper : Servant.Manager.Bootstrapper
    {
        
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            TinyIoCContainer.Current.Register<IHost, Host>();
        }
    }
}