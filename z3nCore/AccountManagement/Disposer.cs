using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System;
using System.IO;
using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.Enums.Browser;

namespace z3nCore
{

    public class Disposer
    {
        #region Fields & Constructor

        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Reporter _reporter;
        private readonly Logger _logger;
        private readonly InstanceManager _instanceMgr;
        private readonly bool _showLog;
        
        public Disposer(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            _showLog = log;
            _reporter = new Reporter(project, instance);
            _instanceMgr = new InstanceManager(project, instance, log);
            _logger = new Logger(project, _showLog, "♻️", true);
        }

        #endregion

        #region Public API

        public void FinishSession( bool useLegacy = true)
        {
            _logger.Send("Starting session finish sequence");

            string acc0 = _project.Var("acc0");
            bool isSuccess = IsSessionSuccessful();
            
            _logger.Send($"Session status: {(isSuccess ? "SUCCESS" : "FAILED")}");

            if (!string.IsNullOrEmpty(acc0))
            {
                GenerateReports(isSuccess);
            }

            if (useLegacy)
            {
                _instanceMgr.SaveProfile(saveCookies: true, saveZpProfile: true);
            }

            _instanceMgr._SaveProfile();
            
            LogSessionComplete(isSuccess);

            _instanceMgr.Cleanup();

            _logger.Send("Session finish sequence completed");
        }
        
        public string ErrorReport(bool toLog = true, bool toTelegram = false, bool toDb = false, bool screenshot = false)
        {
            return _reporter.ReportError(toLog, toTelegram, toDb, screenshot);
        }
        
        public string SuccessReport(bool toLog = true, bool toTelegram = false, bool toDb = false, string customMessage = null)
        {
            return _reporter.ReportSuccess(toLog, toTelegram, toDb, customMessage);
        }

        #endregion

        #region Private Methods

        private bool IsSessionSuccessful()
        {
            string lastQuery = _project.Var("lastQuery");
            bool isSuccess = !lastQuery.Contains("dropped");
            
            _logger.Send($"Checking session success: lastQuery='{lastQuery}', result={isSuccess}");
            
            return isSuccess;
        }

        private void GenerateReports(bool isSuccess)
        {
            _logger.Send($"Generating {(isSuccess ? "SUCCESS" : "ERROR")} report");
            
            try
            {
                if (isSuccess)
                {
                    _reporter.ReportSuccess(toLog: true, toTelegram: true, toDb: true);
                }
                else
                {
                    _reporter.ReportError(toLog: true, toTelegram: true, toDb: true, screenshot: true);
                }
                _logger.Send("Report generated successfully");
            }
            catch (Exception ex)
            {
                _logger.Send($"Report generation failed: {ex.GetType().Name} - {ex.Message}");
            }
        }

        private void LogSessionComplete(bool isSuccess)
        {
            try
            {
                double elapsed = _project.TimeElapsed();
                string statusText = isSuccess ? "SUCCESS" : "FAILED";
                
                _logger.Send($"Session completed: status={statusText}, elapsed={elapsed}s");
                
                string message = $"Session {statusText}. Elapsed: {elapsed}s\n" +
                               "███ ██ ██  ██ █  █  █  ▓▓▓ ▓▓ ▓▓  ▓  ▓  ▓  ▒▒▒ ▒▒ ▒▒ ▒  ▒  ░░░ ░░  ░░ ░ ░ ░ ░ ░ ░  ░  ░  ░   ░   ░   ░    ░    ░    ░     ░        ░";

                LogColor color = isSuccess ? LogColor.Green : LogColor.Orange;
                _project.SendToLog(message.Trim(), LogType.Info, true, color);
            }
            catch (Exception ex)
            {
                _logger.Send($"Session log entry failed: {ex.Message}");
            }
        }
        
        #endregion
    }
    
    
}