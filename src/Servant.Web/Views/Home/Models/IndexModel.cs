using System.Collections.Generic;
using Servant.Business.Objects;

namespace Servant.Web.Views.Home.Models
{
    public class IndexModel
    {
        public string SiteName { get; set; }
        public string Host { get; set; }
        public List<LogEntry> LogEntries { get; set; }
        public List<Reports.AgentstringWithCount> AgentstringsByCount { get; set; }
    }
}