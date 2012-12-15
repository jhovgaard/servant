using System;
using Servant.Business.Objects;

namespace Servant.Business.Services
{
    public class SettingsService : Service<Settings>
    {
        public SettingsService() : base("Settings")
        {
            Guid = Guid.NewGuid();

        }
        public Guid Guid { get; set; }

        private Settings _settings;
        public Settings LocalSettings
        {
            get
            {
                if (_settings != null)
                    return _settings;

                var settings = Table.All().FirstOrDefault();
                if(settings == null)
                {
                    settings = new Settings
                                   {
                                       Debug = false,
                                       Password = Helpers.SecurityHelper.HashPassword("admin"),
                                       ParseLogs = false,
                                       ServantUrl = "http://localhost:54444/",
                                       SetupCompleted = false,
                                       Username = "admin"
                                   };
                    Insert(settings);
                }
                _settings = settings;
                return settings;
            }
        }

        public void ReloadLocalSettings()
        {
            _settings = null;
        }
    }
}