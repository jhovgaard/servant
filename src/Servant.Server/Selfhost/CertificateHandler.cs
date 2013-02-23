using System.Diagnostics;

namespace Servant.Server.Selfhost
{
    public static class CertificateHandler
    {
         public static void RemoveCertificateBinding(int port)
         {
             var command = string.Format("http delete sslcert ipport=0.0.0.0:{0}", port);
             ExecuteNetshCommand(command);
         }

         public static void AddCertificateBinding(int port)
         {
             var command = string.Format("http add sslcert ipport=0.0.0.0:{0} certhash=8D2673EE6B9076E3C96299048A5032FA401E01C4 appid={{dc97f9b1-1653-490f-90f6-6fe008c9701a}}", port);
             ExecuteNetshCommand(command);
         }

        public static bool IsCertificateBound(int port)
        {
            var command = string.Format("http show sslcert ipport=0.0.0.0:{0}", port);
            return !ExecuteNetshCommand(command).Contains("The system cannot find the file specified.");   
        }

        private static string ExecuteNetshCommand(string command)
        {
            var p = new Process
            {
                StartInfo =
                {
                    FileName = "netsh.exe",
                    Arguments = command,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                }
            };
            p.Start();
            var output = p.StandardOutput.ReadToEnd();
            return output;
        }
    }
}