using Servant.Business.Objects.Enums;

namespace Servant.Business.Objects
{
    public class Binding
    {
        public Binding()
        {
            IpAddress = "*";
        }

        private string _userInput;
        public string UserInput
        {
            get { return _userInput ?? (_userInput = ToString()); }
            set { _userInput = value; }
        }

        public Protocol Protocol { get; set; }
        public string Hostname { get; set; }
        public int Port { get; set; }
        public string IpAddress { get; set; }
        public string CertificateName { get; set; }
        public byte[] CertificateHash { get; set; }
        public new string ToString()
        {
            return Protocol.ToString() + "://" + (string.IsNullOrWhiteSpace(Hostname) ? "*" : Hostname) + ":" + Port;
        }

        public string ToIisBindingInformation()
        {
            return string.Format( "{0}:{1}:{2}", IpAddress, Port, Hostname);
        }
    }
}