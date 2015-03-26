using System;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using Servant.Shared;

namespace Servant.Updater.Service
{
    internal static class ServiceHelper
    {
        public static void Uninstall(string directory = null)
        {
            MessageHandler.Print("Trying to uninstall the Servant Updater service...");
            try
            {
                ManagedInstallerClass.InstallHelper(new[] { "/u", "/LogToConsole=false", directory ?? Assembly.GetExecutingAssembly().Location });
                MessageHandler.Print("The service was successfully uninstalled.");
            }
            catch (UnauthorizedAccessException)
            {
                MessageHandler.Print("Could not uninstall service. Insufficient permissions.");
            }
            catch (Exception e)
            {
                MessageHandler.Print("There was an error during uninstallation. " + e.Message);
            }
        }

        public static void Install()
        {
            var installPossible = true;

            if (new ServiceController(ServiceConfig.ServiceName).Container != null)
            {
                MessageHandler.Print("Servant Updater is already installed. Use /uninstall or /u to uninstall.");
                installPossible = false;
            }

            if (ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == ServiceConfig.ServiceName) != null)
            {
                MessageHandler.Print("Servant Updater is already running on this machine.");
                installPossible = false;
            }

            if (installPossible)
            {
                MessageHandler.Print("Trying to install Servant Updater as Windows service...");
                try
                {
                    ManagedInstallerClass.InstallHelper(new[] { "/LogToConsole=false", Assembly.GetExecutingAssembly().Location });
                    MessageHandler.Print("The Servant Updater service was installed.");
                }
                catch (UnauthorizedAccessException)
                {
                    MessageHandler.Print("Could not install service. Insufficient permissions.");
                }
                catch (Exception e)
                {
                    MessageHandler.Print("There was an error during installation. " + e.Message);
                }
            }
        }
    }
}
