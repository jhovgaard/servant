using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Servant.Agent.Infrastructure
{
    public static class MessageHandler
    {
        static readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
        static string _logFilePath = Path.Combine(Path.GetDirectoryName(ExecutingAssembly.Location), "log.txt");

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
                using (var eventLog = new EventLog { Source = "Servant Agent" })
                {
                    eventLog.WriteEntry(text, EventLogEntryType.Error);
                }
            }

            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ": " + text);
            WriteLine(text);
        }

        private enum LogType
        {
            Exception,
            Message
        }

        private static void WriteLine(string line)
        {
            var filePath = _logFilePath;

            if (!File.Exists(filePath))
            {
                var logFile = File.Create(filePath);
                logFile.Close();
            }

            try
            {
                File.AppendAllText(filePath, string.Format("{0}: {1}", DateTime.Now, line + Environment.NewLine));

                var txtfile = new FileInfo(filePath);
                if (txtfile.Length > (5 * 1024 * 1024))
                {
                    var lines = File.ReadAllLines(filePath).Skip(30).ToArray();
                    File.WriteAllLines(filePath, lines);
                }
            }
            catch (System.IO.IOException)
            {
                throw;
            }
        }
    }
}
