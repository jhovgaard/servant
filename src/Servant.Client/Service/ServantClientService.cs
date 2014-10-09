using System;
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
    }
}
