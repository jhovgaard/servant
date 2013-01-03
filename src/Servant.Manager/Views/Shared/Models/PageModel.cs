using System.Collections.Generic;
using Servant.Business.Objects;

namespace Servant.Manager.Views.Shared.Models
{
    public class PageModel
    {
        public string Servername { get; set; }
        public IEnumerable<Site> Sites { get; set; }
    }
}