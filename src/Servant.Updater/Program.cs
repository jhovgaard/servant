using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;

namespace Servant.Updater
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = args[0];
            var servantZipUrl = args[1];
            
            //var path = @"C:\Code\servant\src\Servant.Server\bin\Release\Servant.Server.exe";
            //var servantZipUrl = "https://github.com/jhovgaard/servant/releases/download/1.1/servant-1.1.zip";
            var servantDirectory = System.IO.Path.GetDirectoryName(path);

            var downloadLocation = System.IO.Path.Combine(servantDirectory, "servant-1.1.zip");

            Console.WriteLine("Downloading " + servantZipUrl + "...");
            new WebClient().DownloadFile(servantZipUrl, downloadLocation);
            var fastZip = new FastZip();
            
            Console.WriteLine("Uninstalling current version");
            var uninstallResult = Uninstall(path);
            Console.WriteLine("Extracting new version");
            fastZip.ExtractZip(downloadLocation, servantDirectory, FastZip.Overwrite.Always, null, null, null, false);
            var installResult = Install(path);
            Console.WriteLine("Installing new version");
        }

        static string Uninstall(string path)
        {
            var uninstall = new Process() { StartInfo = new ProcessStartInfo(path, "uninstall") { CreateNoWindow = false, UseShellExecute = false, RedirectStandardOutput = true } };
            if (Environment.OSVersion.Version.Major >= 6)
            {
                uninstall.StartInfo.Verb = "runas";
            }
            uninstall.Start();
            return uninstall.StandardOutput.ReadToEnd();
        }

        static string Install(string path)
        {
            var uninstall = new Process() { StartInfo = new ProcessStartInfo(path, "install") { CreateNoWindow = false, UseShellExecute = false, RedirectStandardOutput = true } };
            if (Environment.OSVersion.Version.Major >= 6)
            {
                uninstall.StartInfo.Verb = "runas";
            }
            uninstall.Start();
            return uninstall.StandardOutput.ReadToEnd();
        }
    }
}
