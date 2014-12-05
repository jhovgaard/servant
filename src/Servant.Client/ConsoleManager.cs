using System;
using System.Diagnostics;
using Servant.Shared.SocketClient;

namespace Servant.Client
{
    public class ConsoleManager
    {
        protected static Process Process;

        public ConsoleManager()
        {
            var startInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
            };

            startInfo.FileName = System.Environment.ExpandEnvironmentVariables(@"%windir%\System32\cmd.exe");
            startInfo.Arguments = "/Q";

            // add '>' to distinguish PROMPT from other output
            //startInfo.EnvironmentVariables["PROMPT"] = "$P$G";

            // dir cmd would list folders then files alpabetically
            // consistent with FileBrowser ui.
            //startInfo.EnvironmentVariables["DIRCMD"] = "/OG /ON";

            Process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };
            
            Process.OutputDataReceived += (sender, args) =>
            {
                Console.WriteLine(args.Data);
                SocketClient.SocketClient.ReplyOverHttp(new CommandResponse(CommandResponse.ResponseType.CmdExe) { Message = args.Data, Success = true });
            };

            Process.ErrorDataReceived += (sender, args) =>
            {
                Console.WriteLine(args.Data);
                SocketClient.SocketClient.ReplyOverHttp(new CommandResponse(CommandResponse.ResponseType.CmdExe) { Message = args.Data, Success = true });
            };

            Process.Start();
            Process.BeginOutputReadLine();
            Process.BeginErrorReadLine();
        }

        public void SendCommand(string data)
        {
            Process.StandardInput.Write(data + "\r\n");
            Process.StandardInput.Flush();
        }
    }
}