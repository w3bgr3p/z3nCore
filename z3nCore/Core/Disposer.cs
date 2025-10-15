using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System;
using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.Enums.Browser;

namespace z3nCore
{
    /// <summary>
    /// Отвечает за управление завершением сессии и координацию процесса отчетности
    /// </summary>
    public class Disposer_
    {
        #region Fields

        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Reporter _reporter;
        private readonly Logger _logger;

        #endregion

        #region Constructor

        public Disposer_(IZennoPosterProjectModel project, Instance instance, bool enableLogging = false)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            
            _reporter = new Reporter(project, instance);
            _logger = new Logger(project, false, "♻️", true);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Основной метод завершения сессии
        /// Координирует отчетность, сохранение cookies и cleanup
        /// </summary>
        public void FinishSession()
        {
            string acc0 = _project.Var("acc0");
            string accRnd = _project.Var("accRnd");
            bool isSuccess = IsSessionSuccessful();


            if (!string.IsNullOrEmpty(acc0))
            {
                GenerateReports(isSuccess);
            }

            SaveCookiesIfNeeded(acc0, accRnd);

            LogSessionComplete(isSuccess);

            CleanupAndStop(acc0);
        }

        /// <summary>
        /// Быстрое создание отчета об ошибке (для ручного вызова)
        /// </summary>
        public string ErrorReport(bool toLog = true, bool toTelegram = false, bool toDb = false, bool screenshot = false)
        {
            return _reporter.ReportError(toLog, toTelegram, toDb, screenshot);
        }

        /// <summary>
        /// Быстрое создание отчета об успехе (для ручного вызова)
        /// </summary>
        public string SuccessReport(bool toLog = true, bool toTelegram = false, bool toDb = false, string customMessage = null)
        {
            return _reporter.ReportSuccess(toLog, toTelegram, toDb, customMessage);
        }

        #endregion

        #region Private Methods - Session Management

        private bool IsSessionSuccessful()
        {
            string lastQuery = _project.Var("lastQuery");
            return !lastQuery.Contains("dropped");
        }

        private void GenerateReports(bool isSuccess)
        {
            try
            {
                if (isSuccess)
                {
                    _logger.Send("Generating success report...");
                    _reporter.ReportSuccess(
                        toLog: true,
                        toTelegram: true,
                        toDb: true
                    );
                }
                else
                {
                    _logger.Send("Generating error report...");
                    _reporter.ReportError(
                        toLog: true,
                        toTelegram: true,
                        toDb: true,
                        screenshot: true
                    );
                }
            }
            catch (Exception ex)
            {
                _project.SendWarningToLog($"Report generation failed: {ex.Message}");
            }
        }

        private void SaveCookiesIfNeeded(string acc0, string accRnd)
        {
            try
            {
                bool shouldSave = _instance.BrowserType == BrowserType.Chromium &&
                                !string.IsNullOrEmpty(acc0) &&
                                string.IsNullOrEmpty(accRnd);

                if (shouldSave)
                {
                    string cookiesPath = _project.PathCookies();
                    new Cookies(_project, _instance).Save("all", cookiesPath);
                    _logger.Send($"Cookies saved to: {cookiesPath}");
                }
            }
            catch (Exception ex)
            {
                _project.SendWarningToLog($"Cookie saving failed: {ex.Message}");
            }
        }

        private void LogSessionComplete(bool isSuccess)
        {
            try
            {
                double elapsed = _project.TimeElapsed();
                string statusText = isSuccess ? "SUCCESS" : "FAILED";
                
                string message = $"Session {statusText}. Elapsed: {elapsed}s\n" +
                               "███ ██ ██  ██ █  █  █  ▓▓▓ ▓▓ ▓▓  ▓  ▓  ▓  ▒▒▒ ▒▒ ▒▒ ▒  ▒  ░░░ ░░  ░░ ░ ░ ░ ░ ░ ░  ░  ░  ░   ░   ░   ░    ░    ░    ░     ░        ░";

                LogColor color = isSuccess ? LogColor.Green : LogColor.Orange;
                _project.SendToLog(message.Trim(), LogType.Info, true, color);
            }
            catch (Exception ex)
            {
                _project.SendWarningToLog($"Session logging failed: {ex.Message}");
            }
        }

        private void CleanupAndStop(string acc0)
        {
            try
            {
                // Очищаем глобальную переменную аккаунта
                if (!string.IsNullOrEmpty(acc0))
                {
                    _project.GVar($"acc{acc0}", string.Empty);
                }

                // Очищаем локальную переменную аккаунта
                _project.Var("acc0", string.Empty);

                // Останавливаем инстанс
                _instance.Stop();
                
                _logger.Send("Session cleanup completed");
            }
            catch (Exception ex)
            {
                _project.SendWarningToLog($"Cleanup failed: {ex.Message}");
                
                // Всё равно пытаемся остановить инстанс
                try
                {
                    _instance.Stop();
                }
                catch
                {
                    // Игнорируем ошибки при остановке
                }
            }
        }

        #endregion
    }

    public class Disposer
    {
        #region Fields & Constructor

        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Reporter _reporter;
        private readonly Logger _logger;
        

        public Disposer(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            
            _reporter = new Reporter(project, instance);
            _logger = new Logger(project, log, "♻️", true);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Основной метод завершения сессии
        /// Координирует отчетность, сохранение cookies и cleanup
        /// </summary>
        public void FinishSession()
        {
            _logger.Send("Starting session finish sequence");

            string acc0 = _project.Var("acc0");
            string accRnd = _project.Var("accRnd");
            _logger.Send($"Session context: acc0='{acc0}', accRnd='{accRnd}', browserType={_instance.BrowserType}");

            bool isSuccess = IsSessionSuccessful();
            _logger.Send($"Session status determined: {(isSuccess ? "SUCCESS" : "FAILED")}");

            if (!string.IsNullOrEmpty(acc0))
            {
                GenerateReports(isSuccess);
            }
            else
            {
                _logger.Send("Skipping report generation: acc0 is empty");
            }

            SaveCookiesIfNeeded(acc0, accRnd);

            LogSessionComplete(isSuccess);

            CleanupAndStop(acc0);

            _logger.Send("Session finish sequence completed");
        }

        /// <summary>
        /// Быстрое создание отчета об ошибке (для ручного вызова)
        /// </summary>
        public string ErrorReport(bool toLog = true, bool toTelegram = false, bool toDb = false, bool screenshot = false)
        {
            _logger.Send($"Manual error report requested: toLog={toLog}, toTelegram={toTelegram}, toDb={toDb}, screenshot={screenshot}");
            return _reporter.ReportError(toLog, toTelegram, toDb, screenshot);
        }

        /// <summary>
        /// Быстрое создание отчета об успехе (для ручного вызова)
        /// </summary>
        public string SuccessReport(bool toLog = true, bool toTelegram = false, bool toDb = false, string customMessage = null)
        {
            _logger.Send($"Manual success report requested: toLog={toLog}, toTelegram={toTelegram}, toDb={toDb}, hasCustomMessage={!string.IsNullOrEmpty(customMessage)}");
            return _reporter.ReportSuccess(toLog, toTelegram, toDb, customMessage);
        }

        #endregion

        #region Private Methods - Session Management

        private bool IsSessionSuccessful()
        {
            string lastQuery = _project.Var("lastQuery");
            bool isSuccess = !lastQuery.Contains("dropped");
            
            _logger.Send($"Checking session success: lastQuery='{lastQuery}', result={isSuccess}");
            
            return isSuccess;
        }

        private void GenerateReports(bool isSuccess)
        {
            _logger.Send($"Starting report generation for {(isSuccess ? "SUCCESS" : "ERROR")} status");
            
            try
            {
                if (isSuccess)
                {
                    _reporter.ReportSuccess(
                        toLog: true,
                        toTelegram: true,
                        toDb: true
                    );
                    _logger.Send("Success report generated successfully");
                }
                else
                {
                    _reporter.ReportError(
                        toLog: true,
                        toTelegram: true,
                        toDb: true,
                        screenshot: true
                    );
                    _logger.Send("Error report generated successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.Send($"Report generation FAILED: {ex.GetType().Name} - {ex.Message}");
                _project.SendWarningToLog($"Report generation failed: {ex.Message}");
            }
        }

        private void SaveCookiesIfNeeded(string acc0, string accRnd)
        {
            _logger.Send($"Checking if cookies should be saved: browserType={_instance.BrowserType}, hasAcc0={!string.IsNullOrEmpty(acc0)}, hasAccRnd={!string.IsNullOrEmpty(accRnd)}");
            
            try
            {
                bool shouldSave = _instance.BrowserType == BrowserType.Chromium &&
                                !string.IsNullOrEmpty(acc0) &&
                                string.IsNullOrEmpty(accRnd);

                if (!shouldSave)
                {
                    _logger.Send($"Cookies save skipped: browserType={_instance.BrowserType}, acc0Present={!string.IsNullOrEmpty(acc0)}, accRndEmpty={string.IsNullOrEmpty(accRnd)}");
                    return;
                }

                string cookiesPath = _project.PathCookies();
                _logger.Send($"Saving cookies to: '{cookiesPath}'");
                
                var cookieManager = new Cookies(_project, _instance);
                cookieManager.Save("all", cookiesPath);
                
                _logger.Send($"Cookies saved successfully to: '{cookiesPath}'");
            }
            catch (Exception ex)
            {
                _logger.Send($"Cookie save FAILED: {ex.GetType().Name} - {ex.Message}");
                _project.SendWarningToLog($"Cookie saving failed: {ex.Message}");
            }
        }

        private void LogSessionComplete(bool isSuccess)
        {
            _logger.Send("Generating final session log entry");
            
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
                _logger.Send($"Session log entry FAILED: {ex.GetType().Name} - {ex.Message}");
                _project.SendWarningToLog($"Session logging failed: {ex.Message}");
            }
        }

        private void CleanupAndStop(string acc0)
        {
            _logger.Send($"Starting cleanup: acc0='{acc0}'");
            
            try
            {
                // Очищаем глобальную переменную аккаунта
                if (!string.IsNullOrEmpty(acc0))
                {
                    _logger.Send($"Clearing global variable 'acc{acc0}'");
                    _project.GVar($"acc{acc0}", string.Empty);
                }
                else
                {
                    _logger.Send("Skipping global variable cleanup: acc0 is empty");
                }

                // Очищаем локальную переменную аккаунта
                _logger.Send("Clearing local variable 'acc0'");
                _project.Var("acc0", string.Empty);

                // Останавливаем инстанс
                _logger.Send("Stopping instance");
                _instance.Stop();
                
                _logger.Send("Cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.Send($"Cleanup FAILED: {ex.GetType().Name} - {ex.Message}");
                _project.SendWarningToLog($"Cleanup failed: {ex.Message}");
                
                // Всё равно пытаемся остановить инстанс
                try
                {
                    _logger.Send("Attempting emergency instance stop");
                    _instance.Stop();
                    _logger.Send("Emergency instance stop succeeded");
                }
                catch (Exception stopEx)
                {
                    _logger.Send($"Emergency instance stop FAILED: {stopEx.GetType().Name} - {stopEx.Message}");
                }
            }
        }

        #endregion
    }
    
    
    /// <summary>
    /// Extension методы для удобного использования
    /// </summary>
    public static partial class ProjectExtensions
    {
        /// <summary>
        /// Быстрый вызов завершения сессии
        /// </summary>
        public static void Finish(this IZennoPosterProjectModel project, Instance instance)
        {
            new Disposer(project, instance).FinishSession();
        }

        /// <summary>
        /// Быстрый вызов отчета об ошибке
        /// </summary>
        public static string ReportError(this IZennoPosterProjectModel project, Instance instance, 
            bool toLog = true, bool toTelegram = false, bool toDb = false, bool screenshot = false)
        {
            return new Disposer(project, instance).ErrorReport(toLog, toTelegram, toDb, screenshot);
        }

        /// <summary>
        /// Быстрый вызов отчета об успехе
        /// </summary>
        public static string ReportSuccess(this IZennoPosterProjectModel project, Instance instance,
            bool toLog = true, bool toTelegram = false, bool toDb = false, string customMessage = null)
        {
            return new Disposer(project, instance).SuccessReport(toLog, toTelegram, toDb, customMessage);
        }
    }

   
}