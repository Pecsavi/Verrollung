
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
        private static string serverVersion;
        private static string versionUrl;

        public static async Task InitializeAsync()
        {
            if (!LoadVersionUrlFromSettings())
                return;

            string json = await FetchVersionJsonAsync(versionUrl);
            if (string.IsNullOrEmpty(json))
                return;

            if (!TryExtractServerVersion(json))
                return;

            CompareVersions();
        }

        private static bool LoadVersionUrlFromSettings()
        {
            try
            {
                var pathJson = File.ReadAllText("settings.json");
                var jsonPath = JsonConvert.DeserializeObject<Dictionary<string, string>>(pathJson);

                if (jsonPath.TryGetValue("VersionPath", out string url))
                {
                    versionUrl = url;
                    return true;
                }
                else
                {
                    MessageBox.Show("Der Pfad zur Version wurde nicht gefunden", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LoggerService.Error(ex, "Die Datei settings.json konnte nicht gelesen werden oder der Schlüssel 'VersionPath' fehlt.");
                return false;
            }
        }

        private static async Task<string> FetchVersionJsonAsync(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    return await client.GetStringAsync(url);
                }
            }
            catch (Exception ex)
            {
                LoggerService.Error(ex, $"Die Datei version.json konnte von der folgenden URL nicht heruntergeladen werden: {url}");
                return null;
            }
        }

        private static bool TryExtractServerVersion(string json)
        {
            try
            {
                var versions = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (versions.TryGetValue("Verrollung", out string version))
                {
                    serverVersion = version;
                    return true;
                }
                else
                {
                    MessageBox.Show("Die aktuelle Programmversion wurde in der Datei version.json nicht gefunden", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LoggerService.Error(ex, "Die Datei version.json konnte nicht verarbeitet werden");
                return false;
            }
        }

        private static void CompareVersions()
        {
            try
            {
                var localVersion = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).ProductVersion;
                if (localVersion != serverVersion)
                {
                    MessageBox.Show("Aktualisierung erforderlich: Eine neuere Version ist verfügbar.", "Version Ausweichung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                LoggerService.Error(ex, "Die lokale Version konnte nicht ermittelt werden.");
            }
        }
    }
}
