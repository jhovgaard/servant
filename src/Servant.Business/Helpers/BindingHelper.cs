using System;

namespace Servant.Business.Helpers
{
    public static class BindingHelper
    {
        public static string SafeFinializeBinding(string binding)
        {
            try
            {
                binding = FinializeBinding(binding);
            }
            catch (UriFormatException)
            {
                return null;
            }

            return binding;
        }

        public static string FinializeBinding(string binding)
        {
            if (String.IsNullOrWhiteSpace(binding))
                return null;

            var uri = new Uri(binding.Contains("://") ? binding : "http://" + binding);

            return String.Format("{0}://{1}:{2}{3}",
                                 uri.Scheme,
                                 uri.Host,
                                 uri.Port,
                                 uri.AbsolutePath);
        }
    }
}