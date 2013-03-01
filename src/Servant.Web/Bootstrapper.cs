using System;
using System.Diagnostics;
using Nancy.Authentication.Basic;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.Session;
using Nancy.TinyIoc;
using Servant.Business;
using Servant.Web.Infrastructure;

namespace Servant.Web
{
    public class Bootstrapper : Nancy.DefaultNancyBootstrapper
    {
        private byte[] _favicon;

        protected override byte[] FavIcon
        {
            get { return _favicon ?? (_favicon = LoadFavIcon()); }
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);
            TinyIoCContainer.Current.Register<IHost, DummyHost>();
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            var host = TinyIoCContainer.Current.Resolve<IHost>();

            pipelines.EnableBasicAuthentication(new BasicAuthenticationConfiguration(container.Resolve<IUserValidator>(), "Servant"));
            CookieBasedSessions.Enable(pipelines);
            
            var sw = new Stopwatch();

            pipelines.BeforeRequest.InsertBefore("Debugging", nancyContext => 
            {
                sw.Reset();
                sw.Start();
                
                return nancyContext.Response;
            });
            
            // Irriterede mig at den ikke returnerede UTF8
            pipelines.AfterRequest.InsertAfter("EncodingFix", nancyContext =>
            {
                if (nancyContext.Response.ContentType == "text/html")
                    nancyContext.Response.ContentType = "text/html; charset=utf8";
            });

            pipelines.AfterRequest.InsertAfter("RebootHandler", ctx =>
            {
                sw.Stop();
                if (host.Debug)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(DateTime.Now.ToLongTimeString() + ": " + ctx.Request.Method + " " + ctx.Request.Url + "(" + sw.ElapsedMilliseconds + "ms)");
                    Console.ResetColor();
                }
            });
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);
            Conventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("css", "css"));
            Conventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("scripts", "scripts"));
            Conventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("images", "images"));
        }

        private static byte[] LoadFavIcon()
        {
            var icon = System.IO.File.ReadAllBytes(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images/favicon.ico"));
            return icon;
        }
    }
}