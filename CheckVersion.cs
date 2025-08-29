
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Verrollungsnachweis
{
    

    internal class CheckVersion
    {
        private static async Task<string> GetServerVersionAsync()
        {
            try
            {
                var settingsJson = File.ReadAllText("settings.json");
                var settings = JsonConvert.DeserializeObject<Dictionary<string, string>>(settingsJson);

                if (!settings.TryGetValue("VersionPath", out string versionUrl))
                    return null;

                var client = new HttpClient();
                string json = await client.GetStringAsync(versionUrl);

                var versions = JsonConvert.DeserializeObject<Dictionary<string, ProgramsInfo>>(json);
                if (versions.TryGetValue("Verrollungsnachweis", out ProgramsInfo info))
                {
                    return info.Version;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static string GetLocalVersion()
        {
            try
            {
                return FileVersionInfo.GetVersionInfo(
                    System.Reflection.Assembly.GetExecutingAssembly().Location
                ).ProductVersion;
            }
            catch
            {
                return null;
            }
        }

        private static void CompareVersions(string localVersion, string serverVersion)
        {
            if (string.IsNullOrEmpty(localVersion) || string.IsNullOrEmpty(serverVersion))
                return;

            Version local = new Version(localVersion);
            Version server = new Version(serverVersion);

            if (server > local)
            {
                MessageBox.Show(
                    $"Eine neuere Version des Programms Verrollungsnachweis ist verfügbar.\n\nLokale Version: {localVersion}\nVerfügbare " +
                    $"Version: {serverVersion}\n\nDie Aktualisierung erfolgt über das Launcher-Programm.","Update verfügbar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }

        }


        public static async Task RunVersionCheckAsync()
        {
            string serverVersion = await GetServerVersionAsync();
            string localVersion = GetLocalVersion();
            CompareVersions(localVersion, serverVersion);
        }


    }


}
