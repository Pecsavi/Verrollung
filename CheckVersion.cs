
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
        private static string installerUrl;
        private static string serverVersion;
        private static string versionUrl;
        static readonly HttpClient client = new HttpClient();

        public static async Task<bool> InitializeAsync()
        {
            if (!LoadVersionUrlFromSettings())
                return true; 

            string json = await FetchVersionJsonAsync(versionUrl);
            if (string.IsNullOrEmpty(json))
            {
                LoggerService.Info("Versionsprüfung übersprungen: keine verfügbare Versionsdatei (offline mode).");
                return true;
            }

            if (!TryExtractServerVersion(json))
                return true;

            return await CompareVersionsAsync();
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
                return await client.GetStringAsync(url);

            }
            catch (Exception ex)
            {
                LoggerService.Error(ex, $"Die Datei version.json konnte von der folgenden URL nicht heruntergeladen werden");
                return null;
            }
        }

        private static bool TryExtractServerVersion(string json)
        {
            
            try
            {
                var versions = JsonConvert.DeserializeObject<Dictionary<string, ProgramVersionInfo>>(json);
                if (versions.TryGetValue("Verrollung", out ProgramVersionInfo info))
                {
                    serverVersion = info.version;
                    installerUrl = info.installer;
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

        private static async Task<bool> CompareVersionsAsync()
        {
            try
            {
                var localVersion = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).ProductVersion;
                if (localVersion != serverVersion)
                {
                    LoggerService.Info($"Aktualisierung erforderlich: local version = {localVersion}, server version = {serverVersion}");

                    var result = MessageBox.Show(
                        $"Aktualisierung erforderlich:\nEine neuere Version ist verfügbar ({localVersion} → {serverVersion}).\nMöchten Sie sie jetzt installieren?",
                        "Version Ausweichung",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        LoggerService.Info("Der Benutzer hat das Update akzeptiert.");

                        string installerPath = await DownloadInstallerAsync(installerUrl);
                        if (!string.IsNullOrEmpty(installerPath))
                        {
                            RunInstallerAndExit(installerPath);
                        }
                        return false; // ne folytassa a programot
                    }
                }
                return true; // nincs frissítés, vagy nem kérte a felhasználó
            }
            catch (Exception ex)
            {
                LoggerService.Error(ex, "Die lokale Version konnte nicht ermittelt oder verglichen werden.");
                return true;
            }
        }


        private static async Task<string> DownloadInstallerAsync(string url)
        {
            try
            {
                LoggerService.Info($"Installationsprogramm wird heruntergeladen");
                string tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(url));
                var data = await client.GetByteArrayAsync(url);
                File.WriteAllBytes(tempPath, data); // .NET Framework: szinkron írás
                return tempPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Das Herunterladen des Installers ist fehlgeschlagen. Bitte versuchen Sie es später erneut.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoggerService.Error(ex, "Das Herunterladen des Installers ist fehlgeschlagen.");
                return null;
            }
        }

        private static void RunInstallerAndExit(string installerPath)
        {
            try
            {
               
                ConnectionManager.Instance.CloseConnectionWithoutExit();
                NLog.LogManager.Shutdown();

                string exePath = Application.ExecutablePath;

                string restartScript = Path.Combine(Path.GetTempPath(), "restart.bat");
                File.WriteAllText(restartScript, $@"@echo off
echo Warten auf das Ende des Programms...
:wait
tasklist | find /i ""{Path.GetFileName(exePath)}"" >nul
if not errorlevel 1 (
    timeout /t 2 >nul
    goto wait
)
start """" ""{installerPath}""
timeout /t 5 >nul
start """""""" """"{{exePath}}""""
""
");

                Process.Start(new ProcessStartInfo
                {
                    FileName = restartScript,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });

                Environment.Exit(0);
                
            }
            catch (Exception)
            {
                //LoggerService.Error(ex, "Das Starten des Installers oder der Neustart ist fehlgeschlagen.");
                MessageBox.Show("Das Update kann nicht gestartet werden.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
