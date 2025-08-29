using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;
using Newtonsoft.Json;
using System.IO;


namespace Verrollungsnachweis
{
    

    public static class LoggerService
    {
       

        private static Logger programLogger = LogManager.GetLogger("ProgramLogger");
        private static Logger activityLogger = LogManager.GetLogger("ActivityLogger");

        private static readonly string logPath = Path.Combine(
           Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
           "Verrollung", "logs", "logfile.log"
       );

        static LoggerService()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));

            // load settings.json
            var configJson = File.ReadAllText("settings.json");
            var configDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(configJson);

            // get RemoteLogUrl
            string remoteUrl = configDict.ContainsKey("RemoteLogUrl") ? configDict["RemoteLogUrl"] : null;

            var nlogConfig = new LoggingConfiguration();

            // File target
            var fileTarget = new FileTarget("logfile")
            {
                FileName = logPath,
                Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}"
            };
            nlogConfig.AddTarget(fileTarget);
            nlogConfig.AddRule(LogLevel.Info, LogLevel.Fatal, fileTarget, "ProgramLogger");

            // WebService target, if URL exists
            if (!string.IsNullOrEmpty(remoteUrl))
            {
                var webTarget = new WebServiceTarget("remoteUserLog")
                {
                    Url = new Uri(remoteUrl),
                    Protocol = WebServiceProtocol.JsonPost
                };
                webTarget.Parameters.Add(new MethodCallParameter("machine", "${machinename}"));
                webTarget.Parameters.Add(new MethodCallParameter("timestamp", "${longdate}"));

                nlogConfig.AddTarget(webTarget);
                nlogConfig.AddRule(LogLevel.Info, LogLevel.Fatal, webTarget, "ActivityLogger");
            }

            LogManager.Configuration = nlogConfig;

            programLogger = LogManager.GetLogger("ProgramLogger");
            activityLogger = LogManager.GetLogger("ActivityLogger");
        }

        

        public static void Info(string message)
        {
            programLogger.Info(message);
        }

        public static void Error(string message)
        {
            programLogger.Error(message);
        }

        public static void Error(Exception ex, string message)
        {
            programLogger.Error(ex, message);
        }

        public static void Debug(string message)
        {
            programLogger.Debug(message);
        }

        public static void Debug(Exception ex, string message)
        {
            programLogger.Debug(ex, message);
        }

        public static void Warn(string message)
        {
            programLogger.Warn(message);
        }

        public static void Warn(Exception ex, string message)
        {
            programLogger.Warn(ex, message);
        }

        public static void UserActivity(string message)
        {
            activityLogger.Info(message);
        }


    }

}
