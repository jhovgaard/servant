using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using Servant.Agent.Infrastructure;
using Servant.Shared;
using Servant.Shared.Communication;
using TinyIoC;

namespace Servant.Agent
{
    public class ConsoleManager
    {
        protected Process Process;
        protected List<CmdExeLine> ResponseLines = new List<CmdExeLine>();

        public ConsoleManager()
        {
            var configuration = TinyIoCContainer.Current.Resolve<ServantAgentConfiguration>();
            if (!configuration.DisableConsoleAccess)
            {
                LoadProcess();
            }
        }

        private void LoadProcess()
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

            Process = new Process
            {
                StartInfo = startInfo,
            };

            Process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    ResponseLines.Add(new CmdExeLine(args.Data, false));
                }
            };

            Process.ErrorDataReceived += (sender, args) =>
            {
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
                SocketClient.ReplyOverHttp(new CommandResponse(CommandResponse.ResponseType.CmdExe) { Message =  Json.SerializeToString(ResponseLines), Success = true });
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