using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using Nancy.ViewEngines;
using Servant.Server.Selfhost;

namespace Servant.Server
{
    public class Bootstrapper : Servant.Web.Bootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            pipelines.OnError.AddItemToEndOfPipeline((ctx, ex) =>
            {
                ErrorWriter.WriteEntry(ex.ToString(), EventLogEntryType.Error);
                var client = new Mindscape.Raygun4Net.RaygunClient("MpnyAIdZ9T5iQjP/NrOl3w==");
                new Thread(() => client.Send(ex)).Start();
                return null;
            });
        }

        protected override NancyInternalConfiguration InternalConfiguration
        {
            get
            {
                return NancyInternalConfiguration.WithOverrides(x => x.ViewLocationProvider = typeof (ResourceViewLocationProvider));
            }
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            var assembly = typeof(Servant.Web.Bootstrapper).Assembly;
            ResourceViewLocationProvider.Ignore.Add(Assembly.Load("Nancy.ViewEngines.Razor, Version=0.21.1.0, Culture=neutral, PublicKeyToken=null"));
            ResourceViewLocationProvider.RootNamespaces.Clear();
            ResourceViewLocationProvider.RootNamespaces.Add(assembly, "Servant.Web.Views");
        }
    }
}