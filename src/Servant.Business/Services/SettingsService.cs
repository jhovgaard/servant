using Servant.Business.Objects;

namespace Servant.Business.Services
{
    public class SettingsService : Service<Settings>
    {
        public SettingsService() : base("Settings") { }

        public Settings LocalSettings
        {
            get { return Table.All().First(); }
        }
    }
}