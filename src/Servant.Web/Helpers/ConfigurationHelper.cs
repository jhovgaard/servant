using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Nancy.TinyIoc;
using Servant.Business.Objects;

namespace Servant.Web.Helpers
{
    public static class ConfigurationHelper
    {
        private static readonly string ConfigFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        static readonly Nancy.Json.JavaScriptSerializer Serializer = new Nancy.Json.JavaScriptSerializer();

        public static ServantConfiguration GetConfigurationFromDisk()
        {
            if (!System.IO.File.Exists(ConfigFilePath))
                return new ServantConfiguration();

            var configContent = System.IO.File.ReadAllText(ConfigFilePath);
            var configuration = Serializer.Deserialize<ServantConfiguration>(configContent);
            
            var fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetCallingAssembly().Location).FileVersion.Split('.');
            var version = string.Join(".", fileVersion.Take(2));

            // Updates configuration file to new version
            if (configuration.Version != version)
            {
                configuration.Version = version;
                UpdateConfiguration(configuration);
            }
            
            return configuration;
        }

        public static void UpdateConfiguration(ServantConfiguration configuration)
        {
            var content = Serializer.Serialize(configuration);
            System.IO.File.WriteAllText(ConfigFilePath, content);
            TinyIoCContainer.Current.Register(configuration);
        }
    }
}