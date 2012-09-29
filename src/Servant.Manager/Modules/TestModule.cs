using Nancy;

namespace Servant.Manager.Modules
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