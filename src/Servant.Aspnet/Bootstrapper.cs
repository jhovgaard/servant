using Nancy;
using Servant.Manager.Infrastructure;
using TinyIoC;

namespace Servant.Aspnet
{
    public class Bootstrapper : Servant.Manager.Bootstrapper
    {
        protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);
            container.Register<IHost, Host>();
        }
    }
}