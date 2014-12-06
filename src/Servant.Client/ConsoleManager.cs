using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using Servant.Shared;
using Servant.Shared.SocketClient;

namespace Servant.Client
{
    public class ConsoleManager
    {
        protected Process Process;
        protected List<CmdExeLine> ResponseLines = new List<CmdExeLine>();

        public ConsoleManager()
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                FileName = Environment.ExpandEnvironmentVariables(@"%windir%\System32\cmd.exe"),
                Arguments = "/K"
            };

            // add '>' to distinguish PROMPT from other output
            //startInfo.EnvironmentVariables["PROMPT"] = "$P$G";

            // dir cmd would list folders then files alpabetically
            // consistent with FileBrowser ui.
            startInfo.EnvironmentVariables["DIRCMD"] = "/OG /ON";

            Process = new Process
            {
                StartInfo = startInfo,
            };
            
            Process.OutputDataReceived += (sender, args) =>
            {
                Console.WriteLine("Output:" + args.Data);
                if (!string.IsNullOrEmpty(args.Data))
                {
                    ResponseLines.Add(new CmdExeLine(args.Data, false));    
                }
            };

            Process.ErrorDataReceived += (sender, args) =>
            {
                Console.WriteLine("Error:" + args.Data);
                if (!string.IsNullOrEmpty(args.Data))
                    ResponseLines.Add(new CmdExeLine(args.Data, true));
            };

            Process.Start();
            Process.BeginOutputReadLine();
            Process.BeginErrorReadLine();

            var timer = new Timer(500);
            timer.Elapsed += SendBacklog;
            timer.Start();
        }

        private void SendBacklog(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (ResponseLines.Any())
            {
                SocketClient.SocketClient.ReplyOverHttp(new CommandResponse(CommandResponse.ResponseType.CmdExe) { Message =  Json.SerializeToString(ResponseLines), Success = true });
                ResponseLines.Clear();
            }
        }

        public void SendCommand(string data)
        {
            Process.StandardInput.Write(data);
            Process.StandardInput.Flush();
            
            var cdRegex = new Regex(@"^(cd(\s|\.)+)");

            if (cdRegex.IsMatch(data))
            {
                Process.StandardInput.Write("\n");
                Process.StandardInput.Flush();
            }

        }

        public class CmdExeLine
        {
            public string Output { get; set; }
            public bool Error { get; set; }

            public CmdExeLine(string output, bool error)
            {
                Output = output;
                Error = error;
            }
        }
    }
}