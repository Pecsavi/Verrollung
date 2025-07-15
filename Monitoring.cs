using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verrollungsnachweis
{
    internal class Monitoring
    {

        public static class LocalLogger
        {
            private static readonly string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MyApp", "logs.txt");

            public static void LogError(string message, Exception ex)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                File.AppendAllText(logPath, $"{DateTime.Now}: {message} - {ex.Message}{Environment.NewLine}");
            }
        }

    }
}
