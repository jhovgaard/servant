using System.Linq;

namespace Servant.Business.Objects
{
    public class Settings
    {
        public string Hostname { get; set; }
        public int Port { get; set; }
        public bool Debug { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public string GetBinding()
        {
            var hostname = Hostname;
            var defaultHostnames = new[] { "localhost", "*" };
            if (defaultHostnames.Contains(hostname))
                hostname = "localhost";

            return string.Format("http://{0}:{1}", hostname, Port);
        }
    }
}