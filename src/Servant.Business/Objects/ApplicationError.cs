using System;
namespace Servant.Business.Objects
{
    public class ApplicationError : Entity
    {
        /// <summary>
        /// Windows' "Index" property
        /// </summary>
        public int SiteIisId { get; set; }
        public string Message { get; set; }
        public string ExceptionType { get; set; }
        public DateTime DateTime { get; set; }
        public string FullMessage { get; set; }

        public Site Site { get; set; }
    }
}