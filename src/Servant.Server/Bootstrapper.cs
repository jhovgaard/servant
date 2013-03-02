using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;
using Nancy.ViewEngines;
using Nancy.ViewEngines.Razor;
using Servant.Server.Selfhost;

namespace Servant.Server
{
    public class Bootstrapper : Web.Bootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            pipelines.OnError.AddItemToEndOfPipeline((ctx, ex) =>
            {
                ErrorWriter.WriteEntry(ex.ToString(), EventLogEntryType.Error);
                var client = new Mindscape.Raygun4Net.RaygunClient("YtmedAsAZw/ptG3cy4bSXg==");
                new Thread(() => client.Send(ex)).Start();
                return null;
            });
        }

        protected override NancyInternalConfiguration InternalConfiguration
        {
            get { return NancyInternalConfiguration.WithOverrides(x => x.ViewLocationProvider = typeof(ResourceViewLocationProvider)); }
        }

        protected override void ConfigureApplicationContainer(Nancy.TinyIoc.TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);
            
            var assembly = GetType().Assembly;
            ResourceViewLocationProvider.Ignore.Add(Assembly.Load("Nancy.ViewEngines.Razor, Version=0.16.1.0, Culture=neutral, PublicKeyToken=null"));
            ResourceViewLocationProvider
                .RootNamespaces
                .Add(assembly, "Servant.Web.Views");
        }
    }
}