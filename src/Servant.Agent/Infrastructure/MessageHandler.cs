using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using TinyIoC;

namespace Servant.Agent.Infrastructure
{
    public static class MessageHandler
    {
        private static readonly ServantAgentConfiguration Configuration = TinyIoCContainer.Current.Resolve<ServantAgentConfiguration>();
        static readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
        static string _logFilePath = Path.Combine(Path.GetDirectoryName(ExecutingAssembly.Location), "log.txt");

        public static void Print(string message)
        {
            Write(message, LogType.Message);
        }

        public static void LogException(Exception exception)
        {
            LogException(exception.Message + Environment.NewLine + exception.StackTrace);
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

                ReportException(text);
            }

            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + ": " + text);
            WriteLine(text);
        }

        private enum LogType
        {
            Exception,
            Message
        }

        private static void ReportException(string text)
        {
            var exceptionUrl = new Uri(Configuration.ServantIoHost + "/exceptions/log");
            new WebClient().UploadValuesAsync(exceptionUrl, new NameValueCollection
                                                            {
                                                                {"InstallationGuid", Configuration.InstallationGuid.ToString()},
                                                                {"Message", text}
                                                            });
        }

        private static void WriteLine(string line)
        {
            var filePath = _logFilePath;

            try
            {
                File.AppendAllText(filePath, string.Format("{0}: {1}", DateTime.Now, line + Environment.NewLine));

                var logFile = new FileInfo(filePath);
                if (logFile.Length > (5 * 1024 * 1024))
                {
                    var lines = File.ReadAllLines(filePath).Skip(30).ToArray();
                    File.WriteAllLines(filePath, lines);
                }
            }
            catch (IOException)
            {
            }
        }
    }
}
