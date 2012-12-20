using System.Linq;
using Dapper;
using Servant.Business.Objects;

namespace Servant.Business.Services
{
    public class SettingsService : SqlLiteService<Settings>
    {
        private Settings _settings;
        public Settings LocalSettings
        {
            get
            {
                if (_settings != null)
                    return _settings;

                var settings = Connection.Query<Settings>("SELECT * FROM Settings LIMIT 1").FirstOrDefault();
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

        public void DeleteAll()
        {
            Connection.Execute("DELETE FROM Settings");
        }

        public new void Insert(Settings settings)
        {
            Connection.Execute(
                "INSERT INTO Settings (ServantUrl, Debug, Username, Password, SetupCompleted, ParseLogs) VALUES(@ServantUrl, @Debug, @Username, @Password, @SetupCompleted, @ParseLogs)",
                settings);
        }
    }
}