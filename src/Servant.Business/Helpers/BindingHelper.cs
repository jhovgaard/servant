using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
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
            catch (UriFormatException)
            {
                return null;
            }
            catch (ArgumentException)
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
                                 uri.Host.Replace(WildcardIdentifier, "*"),
                                 uri.Port,
                                 uri.AbsolutePath);
        }

        public static Binding ConvertToBinding(string finalizedBinding, string ipAddress, X509Certificate2 certificate = null)
        {
            if (finalizedBinding == null)
                return null;

            finalizedBinding = finalizedBinding.Replace("*", WildcardIdentifier);

            var uri = new Uri(finalizedBinding);
            var hostname = uri.Host == WildcardIdentifier ? "*" : uri.Host;
            return new Binding
                {
                    Hostname = hostname,
                    Port = uri.Port,
                    Protocol = (Protocol) Enum.Parse(typeof (Protocol), uri.Scheme),
                    CertificateName = certificate != null ? certificate.FriendlyName : null,
                    CertificateHash = certificate != null ? certificate.GetCertHash() : null,
                    IpAddress = ipAddress ?? "*"
                };
        }
    }
}