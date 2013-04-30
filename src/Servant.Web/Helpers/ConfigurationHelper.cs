using System;
using Nancy.TinyIoc;
using Servant.Business.Objects;
using ServiceStack.Text;

namespace Servant.Web.Helpers
{
    public static class ConfigurationHelper
    {
        private static readonly string ConfigFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        public static ServantConfiguration GetConfigurationFromDisk()
        {
            if (!System.IO.File.Exists(ConfigFilePath))
                return new ServantConfiguration();

            var configContent = System.IO.File.ReadAllText(ConfigFilePath);

            var configuration = JsonSerializer.DeserializeFromString<ServantConfiguration>(configContent);
            return configuration;
        }

        public static void UpdateConfiguration(ServantConfiguration configuration)
        {
            var content = JsonSerializer.SerializeToString(configuration);
            System.IO.File.WriteAllText(ConfigFilePath, content);
            TinyIoCContainer.Current.Register(configuration);
        }
    }
}