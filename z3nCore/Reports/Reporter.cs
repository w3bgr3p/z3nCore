using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Text;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Windows.Input;
using ZennoLab.InterfacesLibrary.Enums.Log;

namespace z3nCore
{
    /// <summary>
    /// Отвечает за создание, форматирование и отправку отчетов
    /// </summary>
    public class Reporter
    {
        #region Fields & Constructor

        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly string _projectScript;
        private readonly object _lockObject = new object();
        private readonly string _ts;
        private readonly int _completionTime;


        public Reporter(IZennoPosterProjectModel project, Instance instance)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            _projectScript = project.Var("projectScript");
            _ts = $"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}";
            _completionTime = _project.TimeElapsed();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Создает и отправляет отчет об ошибке
        /// </summary>
        public string ReportError(bool toLog = true, bool toTelegram = false, bool toDb = true, bool screenshot = false)
        {
            var errorData = ExtractErrorData();
            if (errorData == null)
            {
                _project.SendInfoToLog("No error data available");
                return string.Empty;
            }

            string logReport = FormatErrorForLog(errorData);

            if (toLog)
            {
                _project.SendToLog(logReport, LogType.Warning, true, LogColor.Orange);
            }

            if (toTelegram)
            {
                string tgReport = FormatErrorForTelegram(errorData);
                SendToTelegram(tgReport);
            }

            if (screenshot)
            {
                CreateScreenshot(errorData.Url, logReport);
            }

            if (toDb)
            {
                string dbUpdate = FormatErrorForDb(errorData);
                _project.DbUpd($"status = 'dropped', last = '{dbUpdate}'", log: true);
            }

            return logReport;
        }

        /// <summary>
        /// Создает и отправляет отчет об успехе
        /// </summary>
        public string ReportSuccess(bool toLog = true, bool toTelegram = false, bool toDb = true, string customMessage = null)
        {
            var successData = ExtractSuccessData(customMessage);
            
            string logReport = FormatSuccessForLog(successData);

            if (toLog)
            {
                _project.SendToLog(logReport, LogType.Info, true, LogColor.LightBlue);
            }

            if (toTelegram)
            {
                string tgReport = FormatSuccessForTelegram(successData);
                SendToTelegram(tgReport);
            }

            if (toDb)
            {
                string dbUpdate = FormatSuccessForDb(successData);
                _project.DbUpd($"status = 'relaxing', last = '{dbUpdate}'");
            }

            return logReport;
        }

        #endregion

        #region Data Extraction

        private ErrorData ExtractErrorData()
        {
            var error = _project.GetLastError();
            if (error == null) return null;

            var errorData = new ErrorData
            {
                ActionId = error.ActionId.ToString() ?? string.Empty,
                ActionComment = error.ActionComment ?? string.Empty,
                ActionGroupId = error.ActionGroupId ?? string.Empty,
                Account = _project.Var("acc0") ?? string.Empty
            };

            Exception ex = error.Exception;
            if (ex != null)
            {
                errorData.Type = ex.GetType()?.Name ?? "UnknownType";
                errorData.Message = ex.Message ?? "No message";
                errorData.StackTrace = ProcessStackTrace(ex.StackTrace);
                errorData.InnerMessage = ex.InnerException?.Message ?? string.Empty;
            }

            try
            {
                errorData.Url = _instance.ActiveTab.URL;
            }
            catch
            {
                errorData.Url = string.Empty;
            }

            return errorData;
        }

        private SuccessData ExtractSuccessData(string customMessage = null)
        {
            return new SuccessData
            {
                Script = Path.GetFileName(_projectScript),
                Account = _project.Var("acc0"),
                LastQuery = _project.Var("lastQuery"),
                ElapsedTime = _project.TimeElapsed(),
                CustomMessage = customMessage,
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }

        private string ProcessStackTrace(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace)) return string.Empty;
            
            return stackTrace.Split(new[] { 'в' }, StringSplitOptions.None)
                .Skip(1)
                .FirstOrDefault()?.Trim() ?? string.Empty;
        }

        #endregion

        #region Formatting - Error

        private string FormatErrorForLog(ErrorData data)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(data.Account)) 
                sb.AppendLine($"acc: {data.Account}");
            if (!string.IsNullOrEmpty(data.ActionId)) 
                sb.AppendLine($"id: {data.ActionId}");
            if (!string.IsNullOrEmpty(data.ActionComment)) 
                sb.AppendLine($"actionComment: {data.ActionComment}");
            if (!string.IsNullOrEmpty(data.Type)) 
                sb.AppendLine($"type: {data.Type}");
            if (!string.IsNullOrEmpty(data.Message)) 
                sb.AppendLine($"msg: {data.Message}");
            if (!string.IsNullOrEmpty(data.InnerMessage)) 
                sb.AppendLine($"innerMsg: {data.InnerMessage}");
            if (!string.IsNullOrEmpty(data.StackTrace)) 
                sb.AppendLine($"stackTrace: {data.StackTrace}");
            if (!string.IsNullOrEmpty(data.Url)) 
                sb.AppendLine($"url: {data.Url}");

            return sb.ToString().Replace("\\", "");
        }

        private string FormatErrorForTelegram(ErrorData data)
        {
            var sb = new StringBuilder();
            string script = Path.GetFileName(_projectScript).EscapeMarkdown();

            sb.AppendLine($"⛔️\\#fail  \\#acc{data.Account}  \\#{script}");
            
            if (!string.IsNullOrEmpty(data.ActionId)) 
                sb.AppendLine($"ActionId: `{data.ActionId.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(data.ActionComment))
                sb.AppendLine($"actionComment: `{data.ActionComment.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(data.Type)) 
                sb.AppendLine($"type: `{data.Type.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(data.Message)) 
                sb.AppendLine($"msg: `{data.Message.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(data.StackTrace))
                sb.AppendLine($"stackTrace: `{data.StackTrace.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(data.InnerMessage))
                sb.AppendLine($"innerMsg: `{data.InnerMessage.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(data.Url)) 
                sb.AppendLine($"url: `{data.Url.EscapeMarkdown()}`");

            string report = sb.ToString();
            _project.Var("failReport", report);
            return report;
        }

        private string FormatErrorForDb(ErrorData data)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"- {_ts} {_completionTime}");
            if (!string.IsNullOrEmpty(data.Account)) 
                sb.AppendLine($"acc: {data.Account}");
            if (!string.IsNullOrEmpty(data.ActionId)) 
                sb.AppendLine($"id: {data.ActionId}");
            if (!string.IsNullOrEmpty(data.ActionComment)) 
                sb.AppendLine($"actionComment: {data.ActionComment}");
            if (!string.IsNullOrEmpty(data.Type)) 
                sb.AppendLine($"type: {data.Type}");
            if (!string.IsNullOrEmpty(data.Message)) 
                sb.AppendLine($"msg: {data.Message}");
            if (!string.IsNullOrEmpty(data.InnerMessage)) 
                sb.AppendLine($"innerMsg: {data.InnerMessage}");
            if (!string.IsNullOrEmpty(data.StackTrace)) 
                sb.AppendLine($"stackTrace: {data.StackTrace}");
            if (!string.IsNullOrEmpty(data.Url)) 
                sb.AppendLine($"url: {data.Url}");
            if (!string.IsNullOrEmpty(data.Screenshot)) 
                sb.AppendLine($"screenshot: {data.Screenshot}");
            return sb.ToString().Replace("\\", "");
            
        }

        #endregion

        #region Formatting - Success

        private string FormatSuccessForLog(SuccessData data)
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

            return sb.ToString().Replace(@"\", "");
        }
        private string FormatSuccessForTelegram(SuccessData data)
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
        private string FormatSuccessForDb(SuccessData data)
        {
            
            var sb = new StringBuilder();
            sb.AppendLine($"+ {_ts} {_completionTime}");
            if (!string.IsNullOrEmpty(data.Account)) 
                sb.AppendLine($"acc: {data.Account}");
            if (!string.IsNullOrEmpty(data.LastQuery)) 
                sb.AppendLine($"lastQuery: {data.LastQuery.Replace("'", "''")}");
            return sb.ToString().Replace("\\", "");
        }

        #endregion

        #region Delivery

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
            var creds = _project.SqlGet("apikey, extra", "_api", where: "id = 'tg_logger'");
            var credsParts = creds.Split('¦');

            string token = credsParts[0].Trim();
            var extraParts = credsParts[1].Trim().Split('/');
            string chatId = extraParts[0].Trim();
            string topicId = extraParts[1].Trim();

            return new TelegramCredentials 
            { 
                Token = token, 
                ChatId = chatId, 
                TopicId = topicId 
            };
        }

        #endregion

        #region Screenshot Processing

        /// <summary>
        /// Создает скриншот с опциональным watermark
        /// </summary>
        private void CreateScreenshot(string url, string watermark = null)
        {
            if (string.IsNullOrEmpty(url)) return;

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

            _project.SendInfoToLog($"Screenshot created: {screenshotPath}");
        }

        private string GenerateScreenshotPath()
        {
            var sb = new StringBuilder();
            sb.Append($"[{_project.Name}]")
                .Append($"[{Time.Now()}]")
                .Append(_project.LastExecutedActionId)
                .Append(".jpg");

            return Path.Combine(
                _project.Path, 
                ".failed", 
                _project.Variables["projectName"].Value, 
                sb.ToString()
            );
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
            watermark = WrapWatermark(watermark, 200);
            ZennoPoster.ImageProcessingWaterMarkTextFromScreenshot(
                _instance.Port, 
                path, 
                "horizontally", 
                "lefttop", 
                watermark,
                0, 
                "Iosevka, 15pt, condensed, [255;255;0;0]", 
                5, 
                5, 
                100, 
                ""
            );
        }

        private void CreateBasicScreenshot(string path)
        {
            ZennoPoster.ImageProcessingCropFromScreenshot(
                _instance.Port, 
                path, 
                0, 0, 1280, 720, 
                "pixels"
            );
        }

        private void ResizeScreenshot(string path)
        {
            if (File.Exists(path))
            {
                ZennoPoster.ImageProcessingResizeFromFile(
                    path, path, 50, 50, "percent", true, false
                );
                Thread.Sleep(300);
            }
        }

        /// <summary>
        /// Переносит длинный текст на новые строки для watermark
        /// </summary>
        private string WrapWatermark(string input, int limit)
        {
            if (string.IsNullOrEmpty(input) || limit <= 0) return input;

            var lines = input.Split(new[] { '\n' }, StringSplitOptions.None);
            var processedLines = new List<string>();

            foreach (var line in lines)
            {
                var sb = new StringBuilder();
                char[] delimiters = new[] { '/', '?', '&', '=' };
                int length = line.Length;
                int position = 0;

                while (position < length)
                {
                    int nextPosition = Math.Min(position + limit, length);
                    int searchLength = nextPosition - position;
                    int breakPosition = -1;

                    if (searchLength > 0)
                    {
                        breakPosition = line.LastIndexOfAny(delimiters, nextPosition - 1, searchLength);
                    }

                    if (breakPosition <= position)
                    {
                        int takeLength = nextPosition - position;
                        sb.AppendLine(line.Substring(position, takeLength));
                        position = nextPosition;
                    }
                    else
                    {
                        int takeLength = breakPosition - position + 1;

                        if (takeLength == 1)
                        {
                            takeLength = Math.Min(limit, length - position);
                        }

                        sb.AppendLine(line.Substring(position, takeLength));
                        position += takeLength;
                    }
                }

                processedLines.Add(sb.ToString().TrimEnd('\n'));
            }

            return string.Join("\n", processedLines);
        }

        #endregion

        #region Data Transfer Objects

        private class ErrorData
        {
            public string ActionId { get; set; } = string.Empty;
            public string ActionComment { get; set; } = string.Empty;
            public string ActionGroupId { get; set; } = string.Empty;
            public string Account { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string StackTrace { get; set; } = string.Empty;
            public string InnerMessage { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            
            public string Screenshot { get; set; } = string.Empty;
        }

        private class SuccessData
        {
            public string Script { get; set; } = string.Empty;
            public string Account { get; set; } = string.Empty;
            public string LastQuery { get; set; } = string.Empty;
            public double ElapsedTime { get; set; }
            public string CustomMessage { get; set; }
            public string Timestamp { get; set; }
        }

        private class TelegramCredentials
        {
            public string Token { get; set; }
            public string ChatId { get; set; }
            public string TopicId { get; set; }
        }

        #endregion
    }
}