using System.Collections.Generic;
using Nancy.ViewEngines.Razor;

namespace Servant.Web
{
    public class RazorConfiguration : IRazorConfiguration
    {
        public IEnumerable<string> GetAssemblyNames()
        {
            return new[] { typeof(Business.IHost).Assembly.ToString(), typeof(RazorConfiguration).Assembly.ToString() };
        }

        public IEnumerable<string> GetDefaultNamespaces()
        {
            yield return typeof (Helpers.StringHelper).Namespace;
        }

        public bool AutoIncludeModelNamespace { get { return false; } }
    }
}