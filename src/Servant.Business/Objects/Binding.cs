using Servant.Business.Objects.Enums;

namespace Servant.Business.Objects
{
    public class Binding
    {
        public Protocol Protocol { get; set; }
        public string Hostname { get; set; }
        public int Port { get; set; }
        public string CertificateName { get; set; }
        public byte[] CertificateHash { get; set; }
        public new string ToString()
        {
            return Protocol.ToString() + "://" + (string.IsNullOrWhiteSpace(Hostname) ? "*" : Hostname) + ":" + Port;
        }

        public string ToIisBindingInformation()
        {
            return string.Format("*:{0}:{1}", Port, Hostname);
        }
    }
}