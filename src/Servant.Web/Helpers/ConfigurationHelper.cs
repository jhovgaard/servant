using System;
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

            var settings = JsonSerializer.DeserializeFromString<ServantConfiguration>(configContent);
            return settings;
        }

        public static void UpdateConfigurationOnDisk(ServantConfiguration settings)
        {
            var content = JsonSerializer.SerializeToString(settings);
            System.IO.File.WriteAllText(ConfigFilePath, content);
        }
    }
}