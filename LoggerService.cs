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
       

        private static readonly Logger programLogger = LogManager.GetLogger("ProgramLogger");
        private static readonly Logger activityLogger = LogManager.GetLogger("ActivityLogger");


        static LoggerService()
        {
            LogManager.Setup().LoadConfigurationFromFile("nlog.config");
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


        // Felhasználói aktivitás naplózása (közös fájlba)
        public static void UserActivity(string message)
        {
            activityLogger.Info(message);
        }


    }

}
