using System;
using System.Diagnostics;
using System.IO;
using System.Timers;
using Servant.Client.Infrastructure;

namespace Servant.Client
{
    public static class Servant
    {
        private static readonly Timer Timer;

        static Servant()
        {
            Timer = new Timer(60 * 60 * 1000);
            Timer.Elapsed += (sender, args) => Update();
            Timer.Start();
            Update();
        }

        public static void Start()
        {
            Timer.Start();

            SocketClient.SocketClient.IsStopped = false;
            new System.Threading.Thread(SocketClient.SocketClient.Initialize).Start();
        }

        public static void Update()
        {
            MessageHandler.Print("Checking for updates...");

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();

            var path = Path.GetDirectoryName(assembly.Location);

            if (path == null)
            {
                MessageHandler.LogException("Wrong path when trying to call updater.");
                return;
            }

            var path1 = Path.Combine(path, "Servant.Updater.exe");
            var path2 = assembly.Location;

            var update = new Process { StartInfo = new ProcessStartInfo("cmd.exe", string.Format("/K \"\"{0}\" \"{1}\"\"", path1, path2)) { UseShellExecute = false, CreateNoWindow = true } };

            if (Environment.OSVersion.Version.Major >= 6)
            {
                update.StartInfo.Verb = "runas";
            }

            update.Start();
        }
    }
}
