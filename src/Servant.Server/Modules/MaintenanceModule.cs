using Servant.Business.Services;
using Servant.Server.Helpers;

namespace Servant.Server.Modules
{
    public class MaintenanceModule : Server.Modules.BaseModule
    {
        public MaintenanceModule(LogEntryService logEntryService) :base("/maintenance/")
        {
            Get["/readlogs/"] = p => {
                RequestLogHelper.SyncDatabaseWithServer();
                EventLogHelper.SyncDatabaseWithServer();
                
                return "YOLO!";
            };

        }
    }
}