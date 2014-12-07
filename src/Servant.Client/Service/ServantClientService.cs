using System;
using System.Diagnostics;
using System.Security.Principal;
using System.ServiceProcess;
using Servant.Client.Infrastructure;

namespace Servant.Client.Service
{
    partial class ServantClientService : ServiceBase
    {
        public ServantClientService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            ServiceStart(args);
        }

        protected override void OnStop()
        {
            ServiceStop();
        }

        public void ServiceStart(string[] args)
        {
            if (!IsAnAdministrator())
            {
                MessageHandler.LogException("Administrator access required.");
                return;
            }

            try
            {
                SetRecoveryOptions(ServiceConfig.ServiceName);
                Servant.Start();
            }
            catch (Exception ex)
            {
                MessageHandler.LogException(ex.Message);
                throw;
            }
        }

        public void ServiceStop()
        {
        }

        private static bool IsAnAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();

            if (identity == null)
            {
                return false;
            }

            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }


        // http://stackoverflow.com/a/6877830/122479
        public static void SetRecoveryOptions(string serviceName)
        {
            int exitCode;
            using (var process = new Process())
            {
                var startInfo = process.StartInfo;
                startInfo.FileName = "sc";
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                // tell Windows that the service should restart if it fails
                startInfo.Arguments = string.Format("failure \"{0}\" reset= 0 actions= restart/60000", serviceName);

                if (Environment.OSVersion.Version.Major >= 6)
                {
                    startInfo.Verb = "runas";
                }

                process.Start();
                process.WaitForExit();

                exitCode = process.ExitCode;
            }

            if (exitCode != 0)
                throw new InvalidOperationException();
        }
    }
}
