using System;
using System.IO;
using Servant.Shared;
using TinyIoC;

namespace Servant.Client.Infrastructure
{
    public static class ConfigManager
    {
        private static readonly string ConfigFileDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private const string ConfigFileFileName = "config.json";

        public static ServantClientConfiguration GetConfigurationFromDisk()
        {
            var configFile = Path.Combine(ConfigFileDirectory, ConfigFileFileName);
            ServantClientConfiguration configuration;

            if (!File.Exists(configFile))
            {
                configuration = new ServantClientConfiguration { ServantIoHost = "www.servant.io:2650", InstallationGuid = Guid.NewGuid() };
        
                UpdateConfiguration(configuration);
            }
            else
            {
                configuration = Json.DeserializeFromString<ServantClientConfiguration>(File.ReadAllText(configFile));
            }
            
            return configuration;
        }

        public static void UpdateConfiguration(ServantClientConfiguration configuration, string saveToDirectory = null)
        {
            var configFile = Path.Combine(saveToDirectory ?? ConfigFileDirectory, ConfigFileFileName);
            File.WriteAllText(configFile, Json.SerializeToString(configuration));
            TinyIoCContainer.Current.Register(configuration);
        }
    }
}