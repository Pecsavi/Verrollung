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


        static LoggerService()
        {
            var configJson = File.ReadAllText("settings.json");
            var configDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(configJson);

            var config = new XmlLoggingConfiguration("nlog.config");
           
            LogManager.Configuration = config;

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
