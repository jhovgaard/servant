using Nancy.Authentication.Basic;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.Session;
using Servant.Manager.Infrastructure;
using TinyIoC;

namespace Servant.Manager
{
    public class Bootstrapper : Nancy.DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            pipelines.EnableBasicAuthentication(new BasicAuthenticationConfiguration(container.Resolve<IUserValidator>(), "Servant"));
            CookieBasedSessions.Enable(pipelines);

            // Irriterede mig at den ikke returnerede UTF8
            pipelines.AfterRequest.InsertAfter("EncodingFix", nancyContext =>
            {
                if (nancyContext.Response.ContentType == "text/html")
                    nancyContext.Response.ContentType = "text/html; charset=utf8";
            });
            
            pipelines.AfterRequest.InsertAfter("RebootHandler", ctx => {
                if(ctx.Items.ContainsKey("RebootNancyHost"))
                {
                    new System.Threading.Thread(() =>
                                                    {
                                                        System.Threading.Thread.Sleep(10);
                                                        var host = TinyIoC.TinyIoCContainer.Current.Resolve<IHost>();
                                                        host.Kill();
                                                        host.Start();
                                                    }).Start();                    
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
    }
}