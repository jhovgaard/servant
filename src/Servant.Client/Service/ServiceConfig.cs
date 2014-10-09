using System.ServiceProcess;

namespace Servant.Client.Service
{
    class ServiceConfig
    {
        public static string DisplayName
        {
            get { return "Servant Client"; }
        }

        public static string ServiceName
        {
            get { return "ServantClient"; }
        }

        public static string Description
        {
            get
            {
                return "Servant is a piece of software that transforms your regular Internet Information Services (IIS) Manager to a beautiful, fast and web-based management tool.";
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
