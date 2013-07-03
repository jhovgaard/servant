using Nancy;

namespace Servant.Web.Modules
{
    public class TestModule : NancyModule
    {
        public TestModule() : base("/test/")
        {
            Get["/"] = p =>
                           {
                               return View["Test"];
                           };
        }
    }
}