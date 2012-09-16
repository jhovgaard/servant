using System;

namespace Servant.Business.Objects
{
    public class IisLogFile
    {
        public string Path { get; set; }
        public DateTime LastWriteTime { get; set; }
    }
}