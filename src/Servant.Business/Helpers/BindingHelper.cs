using System;
using System.Collections.Generic;
using Servant.Business.Objects;
using Servant.Business.Objects.Enums;

namespace Servant.Business.Helpers
{
    public static class BindingHelper
    {
        private const string WildcardIdentifier = "servant-wildcard";

        public static string SafeFinializeBinding(string binding)
        {
            try
            {
                binding = FinializeBinding(binding);
            }
            catch (UriFormatException uriEx)
            {
                return null;
            }
            catch (ArgumentException argEx)
            {
                return null;
            }

            return binding;
        }

        public static string FinializeBinding(string binding)
        {
            if (String.IsNullOrWhiteSpace(binding))
                return null;

            binding = binding.Replace("*", WildcardIdentifier);

            var uri = new Uri(binding.Contains("://") ? binding : "http://" + binding);

            return String.Format("{0}://{1}:{2}{3}",
                                 (Protocol) Enum.Parse(typeof(Protocol), uri.Scheme),
                                 uri.Host,
                                 uri.Port,
                                 uri.AbsolutePath);
        }

        public static Binding ConvertToBinding(string finalizedBinding)
        {
            if (finalizedBinding == null)
                return null;

            var uri = new Uri(finalizedBinding);
            var hostname = uri.Host == WildcardIdentifier ? "*" : uri.Host;
            return new Binding
                {
                    Hostname = hostname,
                    Port = uri.Port,
                    Protocol = (Protocol) Enum.Parse(typeof (Protocol), uri.Scheme)
                };
        }


        public static List<Binding> ConvertRawBindings(string rawBindings)
        {
            if (string.IsNullOrWhiteSpace(rawBindings))
                return null;

            return ConvertRawBindings(rawBindings.Split(','));
        }

        public static List<Binding> ConvertRawBindings(string[] rawBindings)
        {
            var bindings = new List<Binding>();
            foreach (var binding in rawBindings)
            {
                var finalizedBinding = SafeFinializeBinding(binding);
                bindings.Add(ConvertToBinding(finalizedBinding));
            }

            return bindings;
        }
    }
}