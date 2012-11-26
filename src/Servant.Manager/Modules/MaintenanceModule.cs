using Servant.Business.Services;
using Servant.Manager.Helpers;

namespace Servant.Manager.Modules
{
    public class MaintenanceModule : BaseModule
    {
        public MaintenanceModule(LogEntryService logEntryService) :base("/maintenance/")
        {
            Get["/readlogs/"] = p => {
                EventLogHelper.SyncServer();
                return "YOLO!";
            };

        }
    }
}