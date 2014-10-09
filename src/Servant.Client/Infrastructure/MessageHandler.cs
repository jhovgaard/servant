using System;
using System.Diagnostics;

namespace Servant.Client.Infrastructure
{
    public static class MessageHandler
    {
        public static void Print(string message)
        {
            Write(message, LogType.Message);
        }

        public static void LogException(string message)
        {
            Write(message, LogType.Exception);
        }

        private static void Write(string text, LogType logType)
        {
            if (logType == LogType.Exception)
            {
                using (var eventLog = new EventLog { Source = "Servant Client" })
                {
                    eventLog.WriteEntry(text, EventLogEntryType.Error);
                }
            }

            Console.WriteLine(text);
        }

        private enum LogType
        {
            Exception,
            Message
        }
    }
}
