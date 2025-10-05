// ============================================
// РЕФАКТОРИНГ: Reporter
// Совместимость: .NET 4.6
// ============================================
// 
// ПРОБЛЕМЫ:
// - ~40% дублирования кода между GetErrorData() и ErrorReport()
// - Дублирование Telegram credentials в ToTelegram() и SuccessToTelegram()
// - Размытый API: 8 публичных методов для 2 основных сценариев
// - Смешанная ответственность в методах
//
// РЕШЕНИЯ:
// - Объединены методы сбора данных об ошибке в один приватный метод
// - Создан единый механизм работы с Telegram
// - API упрощен до 3 основных методов: ErrorReport(), SuccessReport(), FinishSession()
// - Весь флоу виден в основных методах через regions
//
// КАК ИСПОЛЬЗОВАТЬ:
// var reporter = new Reporter(project, instance);
// reporter.ErrorReport(toTg: true, toDb: true, screenshot: true);
// reporter.SuccessReport(log: true, toTg: true);
// reporter.FinishSession();
// ============================================

using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Text; 
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.Enums.Browser;

namespace z3nCore
{
    public class Disposer
    {
        #region Fields and Constructor
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly string _projectScript;
        private readonly object _lockObject = new object();
        
        public Disposer(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _projectScript = project.Var("projectScript");
        }
        #endregion

        #region Public API
        public string ErrorReport(bool toTg = false, bool toDb = false, bool screenshot = false)
        {
            // Основной флоу обработки ошибки
            var errorData = ExtractErrorData();
            if (errorData == null) 
            {
                _project.SendInfoToLog("noErrorData");
                return "";
            }

            string basicReport = FormatBasicErrorReport(errorData);
            _project.SendToLog(basicReport, LogType.Warning, true, LogColor.Orange);

            if (toTg)
            {
                string tgReport = FormatTelegramErrorReport(errorData);
                SendToTelegram(tgReport);
            }

            if (screenshot)
            {
                CreateScreenshot(errorData.Url, basicReport);
            }

            if (toDb) 
            {
                _project.DbUpd($"status = '! dropped: {basicReport}'", log: true);
            }

            return basicReport;
        }

        public string SuccessReport(bool log = false, bool toTg = false, string customMessage = null)
        {
            var successData = new SuccessData
            {
                Script = Path.GetFileName(_projectScript),
                Account = _project.Var("acc0") ?? "",
                LastQuery = GetSafeVar("lastQuery"),
                ElapsedTime = _project.TimeElapsed(),
                CustomMessage = customMessage
            };

            string reportText = FormatSuccessReport(successData);

            if (toTg) 
            {
                SendToTelegram(reportText);
            }

            if (log) 
            {
                string logText = reportText.Replace(@"\", "");
                _project.SendToLog(logText, LogType.Info, true, LogColor.LightBlue);
            }

            return reportText;
        }

        public void FinishSession()
        {
            string acc0 = _project.Var("acc0");
            string accRnd = _project.Var("accRnd");
            bool isSuccess = (!GetSafeVar("lastQuery").Contains("dropped"));
            try
            {
                if (!string.IsNullOrEmpty(acc0))
                {
                    if (isSuccess)
                    {
                        SuccessReport(log: true, toTg: true);
                    }
                    else
                    {
                        //ErrorReport(toTg: true, toDb: true, screenshot: true);
                    }
                }
            }
            catch (Exception ex)
            {
                _project.log(ex.Message);
            }
            
            if (ShouldSaveCookies(_instance, acc0, accRnd))
            {
                var pathCookies = _project.PathCookies();
                new Cookies(_project, _instance).Save("all", pathCookies);
            }
            LogSessionComplete();
            ClearAccountState(acc0);
        }
        #endregion

        #region Error Data Processing
        private ErrorData ExtractErrorData()
        {
            var error = _project.GetLastError();
            if (error == null) return null;

            var errorData = new ErrorData
            {
                ActionId = error.ActionId.ToString() ?? "",
                ActionComment = error.ActionComment ?? "",
                ActionGroupId = error.ActionGroupId ?? "",
                Account = _project.Var("acc0") ?? ""
            };

            // Извлекаем данные исключения
            Exception ex = error.Exception;
            if (ex != null) 
            {
                try 
                {
                    errorData.Type = ex.GetType()?.Name ?? "noType";
                    errorData.Message = ex.Message ?? "noMessage";
                    errorData.StackTrace = ProcessStackTrace(ex.StackTrace);
                    errorData.InnerMessage = ex.InnerException?.Message ?? "";
                }
                catch (Exception exp)
                {
                    _project.SendWarningToLog(exp.Message);
                }
            }
            
            // Получаем URL безопасно
            try { errorData.Url = _instance.ActiveTab.URL; } catch { }

            return errorData;
        }

        private string ProcessStackTrace(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace)) return string.Empty;
            return stackTrace.Split(new[] { 'в' }, StringSplitOptions.None)
                            .Skip(1)
                            .FirstOrDefault()?.Trim() ?? string.Empty;
        }

        private string FormatBasicErrorReport(ErrorData data)
        {
            var sb = new StringBuilder();
            
            if (!string.IsNullOrEmpty(data.Account)) sb.AppendLine($"acc: {data.Account}");
            if (!string.IsNullOrEmpty(data.ActionId)) sb.AppendLine($"id: {data.ActionId}");
            if (!string.IsNullOrEmpty(data.ActionComment)) sb.AppendLine($"actionComment: {data.ActionComment}");
            if (!string.IsNullOrEmpty(data.Type)) sb.AppendLine($"type: {data.Type}");
            if (!string.IsNullOrEmpty(data.Message)) sb.AppendLine($"msg: {data.Message}");
            if (!string.IsNullOrEmpty(data.InnerMessage)) sb.AppendLine($"innerMsg: {data.InnerMessage}");
            if (!string.IsNullOrEmpty(data.StackTrace)) sb.AppendLine($"stackTrace: {data.StackTrace}");
            if (!string.IsNullOrEmpty(data.Url)) sb.AppendLine($"url: {data.Url}");
            
            return sb.ToString().Replace("\\", "");
        }

        private string FormatTelegramErrorReport(ErrorData data)
        {
            var sb = new StringBuilder();
            string script = Path.GetFileName(_projectScript).EscapeMarkdown();

            sb.AppendLine($"⛔️\\#fail  \\#acc{data.Account}  \\#{script}");
            if (!string.IsNullOrEmpty(data.ActionId)) sb.AppendLine($"ActionId: `{data.ActionId.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(data.ActionComment)) sb.AppendLine($"actionComment: `{data.ActionComment.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(data.Type)) sb.AppendLine($"type: `{data.Type.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(data.Message)) sb.AppendLine($"msg: `{data.Message.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(data.StackTrace)) sb.AppendLine($"stackTrace: `{data.StackTrace.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(data.InnerMessage)) sb.AppendLine($"innerMsg: `{data.InnerMessage.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(data.Url)) sb.AppendLine($"url: `{data.Url.EscapeMarkdown()}`");
            
            string failReport = sb.ToString();
            _project.Var("failReport", failReport);
            return failReport;
        }
        #endregion

        #region Success Data Processing
        private string FormatSuccessReport(SuccessData data)
        {
            var sb = new StringBuilder();
            string script = data.Script.EscapeMarkdown();
            
            sb.AppendLine($"✅️\\#success  \\#acc{data.Account}  \\#{script}");
            
            if (!string.IsNullOrEmpty(data.LastQuery))
            {
                sb.Append("LastUpd: `")
                  .Append(data.LastQuery.EscapeMarkdown())
                  .AppendLine("` ");
            }

            if (!string.IsNullOrEmpty(data.CustomMessage))
            {
                sb.Append("Message: `")
                  .Append(data.CustomMessage.EscapeMarkdown())
                  .AppendLine("` ");
            }

            sb.Append("TookTime: ")
              .Append(data.ElapsedTime)
              .AppendLine("s ");

            return sb.ToString();
        }
        #endregion

        #region Telegram Integration
        private void SendToTelegram(string message)
        {
            var credentials = GetTelegramCredentials();
            if (credentials == null) return;

            string encodedMessage = Uri.EscapeDataString(message);
            string url = string.Format(
                "https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}&reply_to_message_id={3}&parse_mode=MarkdownV2",
                credentials.Token, credentials.ChatId, encodedMessage, credentials.TopicId
            );

            _project.GET(url);
        }

        private TelegramCredentials GetTelegramCredentials()
        {
            try
            {
                var creds = _project.DbGet("apikey, extra", "_api", where: "id = 'tg_logger'");
                var credsParts = creds.Split('|');

                string token = credsParts[0].Trim();
                var extraParts = credsParts[1].Trim().Split('/');
                string chatId = extraParts[0].Trim();
                string topicId = extraParts[1].Trim();

                return new TelegramCredentials { Token = token, ChatId = chatId, TopicId = topicId };
            }
            catch (Exception ex)
            {
                _project.SendWarningToLog($"Failed to get Telegram credentials: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region Screenshot Processing
        private void CreateScreenshot(string url, string watermark = null)
        {
            if (string.IsNullOrEmpty(url)) return;

            try
            {
                string screenshotPath = GenerateScreenshotPath();
                EnsureDirectoryExists(screenshotPath);

                lock (_lockObject)
                {
                    if (!string.IsNullOrEmpty(watermark))
                    {
                        CreateScreenshotWithWatermark(screenshotPath, watermark);
                    }
                    else
                    {
                        CreateBasicScreenshot(screenshotPath);
                    }

                    Thread.Sleep(500);
                    ResizeScreenshot(screenshotPath);
                }

                _project?.SendInfoToLog($"Screenshot created successfully: {screenshotPath}");
            }
            catch (Exception e)
            {
                _project.SendWarningToLog($"Error during screenshot processing: {e.Message}");
            }
        }

        private string GenerateScreenshotPath()
        {
            var sb = new StringBuilder();
            sb.Append($"[{_project.Name}]")
              .Append($"[{Time.Now()}]")
              .Append(_project.LastExecutedActionId)
              .Append(".jpg");

            return Path.Combine(_project.Path, ".failed", _project.Variables["projectName"].Value, sb.ToString());
        }

        private void EnsureDirectoryExists(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private void CreateScreenshotWithWatermark(string path, string watermark)
        {
            watermark = WrapWM(watermark, 200);
            ZennoPoster.ImageProcessingWaterMarkTextFromScreenshot(
                _instance.Port, path, "horizontally", "lefttop", watermark,
                0, "Iosevka, 15pt, condensed, [255;255;0;0]", 5, 5, 100, ""
            );
        }

        private void CreateBasicScreenshot(string path)
        {
            ZennoPoster.ImageProcessingCropFromScreenshot(_instance.Port, path, 0, 0, 1280, 720, "pixels");
        }

        private void ResizeScreenshot(string path)
        {
            if (File.Exists(path))
            {
                ZennoPoster.ImageProcessingResizeFromFile(path, path, 50, 50, "percent", true, false);
                Thread.Sleep(300);
            }
        }

        private string WrapWM(string input, int limit)
        {
            if (string.IsNullOrEmpty(input) || limit <= 0) return input;

            // Split the input into lines
            var lines = input.Split(new[] { '\n' }, StringSplitOptions.None);
            var processedLines = new List<string>();

            foreach (var line in lines)
            {
                //if (line.Trim().StartsWith("http", StringComparison.OrdinalIgnoreCase))
               // {
                    var sb = new StringBuilder();
                    char[] delims = new[] { '/', '?', '&', '=' };
                    int len = line.Length;
                    int pos = 0;

                    while (pos < len)
                    {
                        int nextPos = Math.Min(pos + limit, len);
                        int searchLen = nextPos - pos;
                        int breakPos = -1;

                        if (searchLen > 0)
                            breakPos = line.LastIndexOfAny(delims, nextPos - 1, searchLen);

                        if (breakPos <= pos)
                        {
                            int takeLen = nextPos - pos;
                            sb.AppendLine(line.Substring(pos, takeLen));
                            pos = nextPos;
                        }
                        else
                        {
                            int takeLen = breakPos - pos + 1;

                            if (takeLen == 1)
                            {
                                takeLen = Math.Min(limit, len - pos);
                            }

                            sb.AppendLine(line.Substring(pos, takeLen));
                            pos += takeLen;
                        }
                    }
                    processedLines.Add(sb.ToString().TrimEnd('\n'));
              //  }
              //  else
              //  {
              //      processedLines.Add(line);
              //  }
            }
            return string.Join("\n", processedLines);
        }

        #endregion

        #region Session Management
        private static bool ShouldSaveCookies(Instance instance, string acc0, string accRnd)
        {
            try
            {
                return instance.BrowserType == BrowserType.Chromium && 
                       !string.IsNullOrEmpty(acc0) && 
                       string.IsNullOrEmpty(accRnd);
            }
            catch
            {
                return false;
            }
        }

        private void ClearAccountState(string acc0)
        {
            if (!string.IsNullOrEmpty(acc0))
            {
                _project.GVar($"acc{acc0}", "");
            }
            _project.Var("acc0", "");
        }

        private void LogSessionComplete()
        {
            string lastQuery = GetSafeVar("lastQuery");
            bool isSuccess = (!lastQuery.Contains("dropped"));
            string toLog = $"All jobs done. Elapsed: {_project.TimeElapsed()}s \n███ ██ ██  ██ █  █  █  ▓▓▓ ▓▓ ▓▓  ▓  ▓  ▓  ▒▒▒ ▒▒ ▒▒ ▒  ▒  ░░░ ░░  ░░ ░ ░ ░ ░ ░ ░  ░  ░  ░   ░   ░   ░    ░    ░    ░     ░        ░";
            LogColor logColor = !isSuccess ? LogColor.Orange : LogColor.Green;
            _project.SendToLog(toLog.Trim(), LogType.Info, true, logColor);
        }
        #endregion

        #region Helper Methods
        private string GetSafeVar(string varName)
        {
            try 
            { 
                return _project.Var(varName) ?? string.Empty; 
            }
            catch 
            { 
                return string.Empty; 
            }
        }
        #endregion

        #region Data Transfer Objects
        private class ErrorData
        {
            public string ActionId { get; set; } = "";
            public string ActionComment { get; set; } = "";
            public string ActionGroupId { get; set; } = "";
            public string Account { get; set; } = "";
            public string Type { get; set; } = "";
            public string Message { get; set; } = "";
            public string StackTrace { get; set; } = "";
            public string InnerMessage { get; set; } = "";
            public string Url { get; set; } = "";
        }

        private class SuccessData
        {
            public string Script { get; set; } = "";
            public string Account { get; set; } = "";
            public string LastQuery { get; set; } = "";
            public double ElapsedTime { get; set; }
            public string CustomMessage { get; set; }
        }

        private class TelegramCredentials
        {
            public string Token { get; set; }
            public string ChatId { get; set; }
            public string TopicId { get; set; }
        }
        #endregion

        #region Legacy Methods - For Backward Compatibility
        [Obsolete("Используйте ErrorReport() напрямую")]
        public string GetErrorData() => ErrorReport();

        [Obsolete("Используйте ErrorReport(toTg: true)")]
        public string MkTgReport(string acc, string actionId, string actionComment, string type, string msg, string stackTrace, string innerMsg, string url)
        {
            // Для совместимости создаем ErrorData и форматируем
            var errorData = new ErrorData
            {
                Account = acc, ActionId = actionId, ActionComment = actionComment,
                Type = type, Message = msg, StackTrace = stackTrace, 
                InnerMessage = innerMsg, Url = url
            };
            return FormatTelegramErrorReport(errorData);
        }

        [Obsolete("Используйте CreateScreenshot() через ErrorReport(screenshot: true)")]
        public void MkScreenshot(string url, string watermark = null) => CreateScreenshot(url, watermark);

        [Obsolete("Используйте SendToTelegram() или SuccessReport(toTg: true)")]
        public void ToTelegram(string reportString) => SendToTelegram(reportString);

        [Obsolete("Используйте SuccessReport(toTg: true)")]
        public void SuccessToTelegram(string message = null) => SuccessReport(toTg: true, customMessage: message);
        #endregion
    }
}