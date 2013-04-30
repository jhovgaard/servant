using System.Collections.Generic;
using Nancy.ViewEngines.Razor;

namespace Servant.Web
{
    public class RazorConfiguration : IRazorConfiguration
    {
        public IEnumerable<string> GetAssemblyNames()
        {
            return new[] { typeof(Business.IHost).Assembly.ToString() };
        }

        public IEnumerable<string> GetDefaultNamespaces()
        {
            return null;
        }

        public bool AutoIncludeModelNamespace { get { return false; } }
    }
}