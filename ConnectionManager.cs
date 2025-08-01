using Dlubal.RSTAB8;
using NLog;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Verrollungsnachweis
{

    public enum ConnectionErrorType
    {
        NoApplication,
        NoModel,
        LicenseIssue,
        UnknownError,
        ModelNotActive
    }

    public class RstabConnectionException : Exception
    {
        public ConnectionErrorType ErrorType { get; }
       
        public RstabConnectionException(ConnectionErrorType type, string message) : base(message)
        {
            ErrorType = type;
          
        }
        public RstabConnectionException(ConnectionErrorType type, string message,  Exception innerException) : base(message, innerException)
        {
            ErrorType = type;
          
        }

    }

    public class ConnectionManager
    {
        private static readonly Lazy<ConnectionManager> _instance =
        new Lazy<ConnectionManager>(() => new ConnectionManager());

        private ConnectionManager() { }

        public static ConnectionManager Instance => _instance.Value;
        private IApplication app;
        private IModel model;
       
        
        public (IApplication, IModel) GetConnect()
        {
            try
            {
                app = Marshal.GetActiveObject("RSTAB8.Application") as IApplication;
                
                    LockAndUnlockLicense(() =>
                    {
                        model = app.GetActiveModel();
                        
                    });
                if (model == null)
                {
                    throw new RstabConnectionException(ConnectionErrorType.NoModel, "Kein aktives Modell gefunden. Bitte öffnen Sie ein Modell in der RSTAB.", new Exception("model == null"));
                }
                LoggerService.Info("Kapcsolat sikeresen létrejött az RSTAB alkalmazással.");
                    return (app, model);
            }
                
            catch (COMException ex)
            {
                throw new RstabConnectionException(ConnectionErrorType.NoApplication, "Keine aktive RSTAB gefunden. Bitte starten Sie die RSTAB und versuchen Sie es erneut.", ex);
            }
            catch (Exception ex)
            {
                throw new RstabConnectionException(ConnectionErrorType.UnknownError, $"Ein unbekannter Fehler ist aufgetreten:   {ex.Message}", ex);
            }

        }

        public bool IsConnected()
        {
            try
            {
                if (Marshal.GetActiveObject("RSTAB8.Application") as IApplication == app)
                {
                    LockAndUnlockLicense(() =>
                    {
                        if (app.GetModelCount() == 0)
                        {
                            throw new RstabConnectionException(ConnectionErrorType.NoModel, "Kein aktives Modell gefunden. Bitte öffnen Sie ein Modell in der RSTAB.", new Exception("model == null"));
                        }
                    });
                    bool isActiveModel = false;
                    LockAndUnlockLicense(() =>
                    {
                        if (app.GetActiveModel() == model)
                        {
                            isActiveModel = true;
                        }
                    });
                    if (isActiveModel)
                    {
                        return true;
                    }
                    else
                    {
                        throw new RstabConnectionException(ConnectionErrorType.ModelNotActive, "Die verwendete Model ist entweder nicht vorne oder wurde geschlossen.", new Exception("model == null"));

                    }
                }
                else
                {
                    throw new RstabConnectionException(ConnectionErrorType.NoApplication,"Die RSTAB - App ist nicht aktiv oder wurde geschlossen.");

                   
                }
            }
            catch (COMException ex)
            {
                throw new RstabConnectionException(ConnectionErrorType.NoApplication, "Keine aktive RSTAB gefunden. Bitte starten Sie die RSTAB und versuchen Sie es erneut.",ex);
            }
            catch (Exception ex)
            {
                throw new RstabConnectionException(ConnectionErrorType.UnknownError, $"Ein unbekannter Fehler ist aufgetreten:   {ex.Message}", ex);
            }
        }

        public void LockAndUnlockLicense(Action action)
        {
            try
            {
                app.LockLicense();
                action();
            }
            finally
            {
                app.UnlockLicense();
            }
        }

        internal static void LogRunningRstabProcesses()
        {
            Process[] processes = Process.GetProcessesByName("RSTAB64");
            if (processes.Length == 0)
            {
                LoggerService.Info("Keine Laufende RSTAB64");
                return;
            }

            foreach (Process process in processes)
            {
                bool hasWindow = process.MainWindowHandle != IntPtr.Zero;
                LoggerService.Info($"RSTAB64 prozess: PID={process.Id}, Gibt es Fenster: {hasWindow}");
            }
        }




        internal static void Kill_Background_Process(string processName)
        {

            LogRunningRstabProcesses();

            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                Environment.Exit(0);
                return;
            }
            foreach (Process process in processes)
            {
                if (process.MainWindowHandle == IntPtr.Zero)
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
        }
        internal void CloseConnection()
        {
            try
            {

          
            if (model != null)
            {
                Marshal.ReleaseComObject(model); //proba
                model = null;
                
            }
            if (app != null)
            {
                if (app.IsComLicensed())
                {
                    app.UnlockLicense();
                }
                Marshal.ReleaseComObject(app);
                app = null;
            }
            Kill_Background_Process("RSTAB64");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Environment.Exit(0);
            }
            catch (Exception)
            {
                Environment.Exit(1);
            }
        }
        internal void CloseConnectionWithoutExit()
        {

            if (model != null)
            {
                Marshal.ReleaseComObject(model);
                model = null;

            }
            if (app != null)
            {
                if (app.IsComLicensed())
                {
                    app.UnlockLicense();
                }
                Marshal.ReleaseComObject(app);
                app = null;
            }
            Kill_Background_Process("RSTAB64");
            GC.Collect();
            GC.WaitForPendingFinalizers();


        }
    }
}