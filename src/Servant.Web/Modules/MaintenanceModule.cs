using System;
using System.Linq;
using System.Text;
using Servant.Business.Services;
using Nancy;
using Servant.Web.Helpers;

namespace Servant.Web.Modules
{
    public class MaintenanceModule : BaseModule
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