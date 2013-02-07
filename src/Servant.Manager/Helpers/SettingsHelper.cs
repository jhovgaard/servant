using System;
using Servant.Business.Objects;
using ServiceStack.Text;

namespace Servant.Manager.Helpers
{
    public static class SettingsHelper
    {
        private static Settings _settings;
        public static Settings Settings 
        {
            get { return _settings ?? (_settings = GetSettings()); }
        }

        private static readonly string ConfigFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        

        private static Settings GetSettings()
        {
            if (!System.IO.File.Exists(ConfigFilePath))
                return new Settings() {ServantUrl = "http://localhost:54444/", Username = "admin"};

            var configContent = System.IO.File.ReadAllText(ConfigFilePath);

            var settings = JsonSerializer.DeserializeFromString<Settings>(configContent);
            return settings;
        }

        public static void UpdateSettings(Settings settings)
        {
            var content = JsonSerializer.SerializeToString(settings);
            System.IO.File.WriteAllText(ConfigFilePath, content);
            _settings = settings;
        }
    }
}