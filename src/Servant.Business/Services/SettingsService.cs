using Servant.Business.Objects;

namespace Servant.Business.Services
{
    public class SettingsService : Service<Settings>
    {
        public SettingsService() : base("Settings") { }

        public Settings LocalSettings
        {
            get
            {
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

                return settings;
            }
        }
    }
}