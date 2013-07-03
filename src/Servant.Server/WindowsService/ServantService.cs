using System;
using System.Diagnostics;
using System.ServiceProcess;
using Servant.Web.Infrastructure;

namespace Servant.Server.WindowsService
{
    partial class ServantService : ServiceBase
    {
        public ServantService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                Program.StartServant();
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Servant.Server: " + ex.Message, EventLogEntryType.Error);
                throw;
            }
        }
    }
}
