using System.Collections.Generic;
using Servant.Business.Objects;
using Servant.Manager.Reports;

namespace Servant.Manager.Views.Home.Models
{
    public class IndexModel
    {
        public string SiteName { get; set; }
        public string Host { get; set; }
        public List<LogEntry> LogEntries { get; set; }
        public List<AgentstringWithCount> AgentstringsByCount { get; set; }
    }
}