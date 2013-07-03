using System.Diagnostics;

namespace Servant.Server.Selfhost
{
    public static class ErrorWriter
    {
         public static void WriteEntry(string message, EventLogEntryType type)
         {
             EventLog.WriteEntry("Servant for IIS", message, type);
         }
    }
}