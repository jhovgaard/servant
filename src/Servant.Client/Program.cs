using System;
using System.ServiceProcess;
using Servant.Client.Infrastructure;
using Servant.Client.Service;
using TinyIoC;

namespace Servant.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            TinyIoCContainer.Current.Register(ConfigManager.GetConfigurationFromDisk());
            TinyIoCContainer.Current.Register(typeof(ConsoleManager)).AsSingleton();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            var config = TinyIoCContainer.Current.Resolve<ServantClientConfiguration>();
#if !DEBUG
            if (Environment.UserInteractive)
            {
                var options = new CommandLineOptions();
                if (CommandLine.Parser.Default.ParseArguments(args, options))
                {
                    if (options.Install)
                    {
                        ServiceHelper.Install();
                    }
                    else if (options.Uninstall)
                    {
                        ServiceHelper.Uninstall();
                    }

                    if (options.Key != null)
                    {
                        config.ServantIoKey = options.Key;
                        ConfigManager.UpdateConfiguration(config);
                    }
                }

                return;
            }
#endif

#if DEBUG
            if (string.IsNullOrWhiteSpace(config.ServantIoKey))
            {
                Console.WriteLine("There's no Servant.io key defined. Please enter one here:");
                Console.Write(">");
                var key = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(key))
                {
                    return;
                }

                config.ServantIoKey = key;
                ConfigManager.UpdateConfiguration(config);
            }

            Servant.Start();
            Console.ReadLine();
#else
            var servicesToRun = new ServiceBase[] 
            { 
                new ServantClientService() 
            };

            ServiceBase.Run(servicesToRun);
#endif
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;

            if (exception != null)
            {
                MessageHandler.LogException(exception.Message);
            }
        }
    }
}
