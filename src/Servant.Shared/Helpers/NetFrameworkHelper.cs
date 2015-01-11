using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace Servant.Shared.Helpers
{
    public static class NetFrameworkHelper
    {
        public static IEnumerable<string> GetAllVersions()
        {
            // Opens the registry key for the .NET Framework entry. 
            using (RegistryKey ndpKey = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, "").OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\"))
            {
                // As an alternative, if you know the computers you will query are running .NET Framework 4.5  
                // or later, you can use: 
                // using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,  
                // RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\"))
                foreach (string versionKeyName in ndpKey.GetSubKeyNames())
                {
                    if (versionKeyName.StartsWith("v"))
                    {
                        yield return versionKeyName;
                    }
                }
            }

        }
    }
}