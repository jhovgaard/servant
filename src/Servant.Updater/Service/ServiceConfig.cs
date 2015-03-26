using System.ServiceProcess;

namespace Servant.Updater.Service
{
    class ServiceConfig
    {
        public static string DisplayName
        {
            get { return "Servant Updater"; }
        }

        public static string ServiceName
        {
            get { return "ServantUpdater"; }
        }

        public static string Description
        {
            get
            {
                return "Service responsible for updating the Servant Agent.";
            }
        }

        public static ServiceStartMode StartType
        {
            get { return ServiceStartMode.Automatic; }
        }

        public static ServiceAccount AccountType
        {
            get { return ServiceAccount.LocalSystem; }
        }
    }
}
