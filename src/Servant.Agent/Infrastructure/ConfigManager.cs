using System;
using System.IO;
using Servant.Shared;
using TinyIoC;

namespace Servant.Agent.Infrastructure
{
    public static class ConfigManager
    {
        private static readonly string ConfigFileDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private const string ConfigFileFileName = "config.json";

        public static ServantAgentConfiguration GetConfigurationFromDisk()
        {
            var configFile = Path.Combine(ConfigFileDirectory, ConfigFileFileName);
            ServantAgentConfiguration configuration;

            if (!File.Exists(configFile))
            {
                configuration = new ServantAgentConfiguration { ServantIoHost = "https://www.servant.io", InstallationGuid = Guid.NewGuid() };
        
                UpdateConfiguration(configuration);
            }
            else
            {
                configuration = Json.DeserializeFromString<ServantAgentConfiguration>(File.ReadAllText(configFile));
            }
            
            return configuration;
        }

        public static void UpdateConfiguration(ServantAgentConfiguration configuration, string saveToDirectory = null)
        {
            var configFile = Path.Combine(saveToDirectory ?? ConfigFileDirectory, ConfigFileFileName);
            File.WriteAllText(configFile, Json.SerializeToString(configuration));
            TinyIoCContainer.Current.Register(configuration);
        }
    }
}