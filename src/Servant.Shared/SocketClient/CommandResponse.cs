using System;

namespace Servant.Shared.SocketClient
{
    public class CommandResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Guid Guid { get; set; }
        public ResponseType Type { get; set; }

        public CommandResponse()
        {
            Guid = System.Guid.NewGuid();
        }

        public CommandResponse(Guid guid)
        {
            Guid = guid;
            Type = ResponseType.Iis;
        }

        public CommandResponse(ResponseType type)
        {
            Type = type;
        }

        public enum ResponseType
        {
            Iis,
            CmdExe
        }
    }
}