using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Servant.Business.Helpers
{
    public static class SettingsHelper
    {
        public static string FinializeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var uri = new Uri(url.Contains("://") ? url : "http://" + url);

            return string.Format("{0}://{1}:{2}{3}",
                                 uri.Scheme,
                                 uri.Host,
                                 uri.Port,
                                 uri.AbsolutePath);
        }
    }
}