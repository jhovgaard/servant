using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Servant.Business.Objects;

namespace Servant.Business
{
    public static class LogParser
    {
        public static IEnumerable<LogEntry> ParseFile(string path, int iisSiteId, int startAtLineRow = 0)
        {
            var fields = new List<string>();

            // Read line:
            var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
            var sr = new System.IO.StreamReader(fs);
            var lines = new List<string>();

            while (!sr.EndOfStream)
                lines.Add(sr.ReadLine());  

            var logRow = 0;

            for (var lineNumber = 0; lineNumber < lines.Count(); lineNumber++)
            {
                var line = lines[lineNumber];

                if (line.StartsWith("#"))
                {
                    if (line.StartsWith("#Fields"))
                        fields = line.Substring(9).Split(' ').ToList();

                    continue;
                }

                logRow++;

                if(logRow <= startAtLineRow)
                    continue;

                // Parse entry
                var fieldValues = line.Split(' ');
                var entry = new LogEntry();

                DateTime? time = null;
                DateTime? date = null;

                for (var i  = 0; i < fields.Count; i++)
                {
                    var field = fields[i];
                    var value = fieldValues[i];

                    switch (field)
                    {
                        case "date":
                            date = DateTime.Parse(value);
                            break;
                        case "time":
                            time = DateTime.Parse(value);
                            break;
                        case "s-ip":
                            entry.ServerIpAddress = value;
                            break;
                        case "cs-method":
                            entry.HttpMethod = value;
                            break;
                        case "cs-uri-stem":
                            entry.Uri = value;
                            break;
                        case "cs-uri-query":
                            if(value != "-")
                                entry.Querystring = "?" + value;
                            break;
                        case "s-port":
                            entry.Port = Convert.ToInt32(value);
                            break;
                        case "cs-username":
                            entry.Username = value;
                            break;
                        case "c-ip":
                            entry.ClientIpAddress = value;
                            break;
                        case "cs(User-Agent)":
                            entry.Agentstring = value;
                            break;
                        case "sc-status":
                            entry.HttpStatusCode = Convert.ToInt32(value);
                            break;
                        case "sc-substatus":
                            entry.HttpSubStatusCode = Convert.ToInt32(value);
                            break;
                        case "time-taken":
                            entry.TimeTaken = Convert.ToInt32(value);
                            break;
                    }
                }

                entry.DateTime = new DateTime(
                    date.HasValue ? date.Value.Year : 0,
                    date.HasValue ? date.Value.Month : 0,
                    date.HasValue ? date.Value.Day : 0,
                    time.HasValue ? time.Value.Hour : 0,
                    time.HasValue ? time.Value.Minute : 0,
                    time.HasValue ? time.Value.Second : 0,
                    time.HasValue ? time.Value.Millisecond : 0);

                entry.LogRow = logRow;
                entry.LogFilename = System.IO.Path.GetFileName(path);
                entry.SiteIisId = iisSiteId;

                yield return entry;
            } 
        }
    }
}
