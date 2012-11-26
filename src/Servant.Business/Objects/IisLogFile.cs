using System;

namespace Servant.Business.Objects
{
    public class IisLogFile
    {
        public string Path { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastModified { get; set; }
        public int TotalRequests { get; set; }
    }
}