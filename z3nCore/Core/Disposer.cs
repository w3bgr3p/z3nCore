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
    public class Disposer
    {
        #region Fields

        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Reporter _reporter;
        private readonly Logger _logger;

        #endregion

        #region Constructor

        public Disposer(IZennoPosterProjectModel project, Instance instance, bool enableLogging = false)
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

            // Генерируем отчеты если есть аккаунт
            if (!string.IsNullOrEmpty(acc0))
            {
                GenerateReports(isSuccess);
            }

            // Сохраняем cookies если нужно
            SaveCookiesIfNeeded(acc0, accRnd);

            // Логируем завершение сессии
            LogSessionComplete(isSuccess);

            // Очищаем переменные и останавливаем инстанс
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