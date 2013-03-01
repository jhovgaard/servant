using System.Reflection;
using Nancy.Bootstrapper;
using Nancy.ViewEngines;

namespace Servant.Server
{
    public class Bootstrapper : Web.Bootstrapper
    {
        protected override NancyInternalConfiguration InternalConfiguration
        {
            get { return NancyInternalConfiguration.WithOverrides(x => x.ViewLocationProvider = typeof(ResourceViewLocationProvider)); }
        }

        protected override void ConfigureApplicationContainer(Nancy.TinyIoc.TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);
            
            var assembly = GetType().Assembly;
            ResourceViewLocationProvider.Ignore.Add(Assembly.Load("Nancy.ViewEngines.Razor, Version=0.16.1.0, Culture=neutral, PublicKeyToken=null"));
            ResourceViewLocationProvider.RootNamespaces.Clear();
            ResourceViewLocationProvider
                .RootNamespaces
                .Add(assembly, "Servant.Web.Views");
        }
    }
}