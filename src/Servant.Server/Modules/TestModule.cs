using Nancy;

namespace Servant.Server.Modules
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