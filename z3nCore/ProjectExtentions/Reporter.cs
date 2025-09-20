using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Text;
using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using ZennoLab.InterfacesLibrary.Enums.Log;

namespace z3nCore
{
    public class Reporter
    {
        protected readonly IZennoPosterProjectModel _project;
        protected readonly Instance _instance;
        private readonly string _projectScript;
        private readonly object LockObject = new object();
        
        public Reporter(IZennoPosterProjectModel project, Instance instance, bool log = false, string classEmoji = null)
        {
            _project = project;
            _instance = instance;
            _projectScript = project.Var("projectScript");
        }
        
        public string GetErrorData()
        {
            var error = _project.GetLastError();
            if (error == null) 
            {
                _project.SendInfoToLog("noErrorData");
                return "";
            }

            var sb = new StringBuilder();

            string actionId = error.ActionId.ToString() ?? "";
            string actionComment = error.ActionComment ?? "";
            string actionGroupId = error.ActionGroupId ?? "";
            string acc = _project.Var("acc0") ?? "";
            string type = "";
            string msg = "";
            string stackTrace = "";
            string innerMsg = "";
            string url = "";
            
            Exception ex = error.Exception;
            if (ex != null) 
            {
                try 
                {
                    var typeEx = ex.GetType();
                    type = typeEx?.Name ?? "noType";
                    msg = ex.Message ?? "noMessage";
                    stackTrace = ex.StackTrace ?? string.Empty;
                    stackTrace = stackTrace.Split(new[] { 'в' }, StringSplitOptions.None).Skip(1).FirstOrDefault()?.Trim() ?? string.Empty;
                    innerMsg = ex.InnerException?.Message ?? string.Empty;
                }
                catch (Exception exp)
                {
                    _project.SendWarningToLog(exp.Message);
                }
            }
            
            try { url = _instance.ActiveTab.URL; } catch { }

            if (!string.IsNullOrEmpty(acc)) sb.AppendLine($"acc: {acc}");
            if (!string.IsNullOrEmpty(actionId)) sb.AppendLine($"id: {actionId}");
            if (!string.IsNullOrEmpty(actionComment)) sb.AppendLine($"actionComment: {actionComment}");
            if (!string.IsNullOrEmpty(type)) sb.AppendLine($"type: {type}");
            if (!string.IsNullOrEmpty(msg)) sb.AppendLine($"msg: {msg}");
            if (!string.IsNullOrEmpty(innerMsg)) sb.AppendLine($"innerMsg: {innerMsg}");
            if (!string.IsNullOrEmpty(stackTrace)) sb.AppendLine($"stackTrace: {stackTrace}");
            if (!string.IsNullOrEmpty(url)) sb.AppendLine($"url: {url}");
            
            string errorReport = sb.ToString();
            _project.SendInfoToLog(errorReport.Replace("\\",""));
            
            return errorReport;
        }

        public string MkTgReport(string acc, string actionId, string actionComment, string type, string msg, string stackTrace, string innerMsg, string url)
        {
            var sb = new StringBuilder();
            string script = Path.GetFileName(_projectScript).EscapeMarkdown();

            sb.AppendLine($"⛔️\\#fail  \\#acc{acc}  \\#{script}");
            if (!string.IsNullOrEmpty(actionId)) sb.AppendLine($"ActionId: `{actionId.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(actionComment)) sb.AppendLine($"actionComment: `{actionComment.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(type)) sb.AppendLine($"type: `{type.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(msg)) sb.AppendLine($"msg: `{msg.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(stackTrace)) sb.AppendLine($"stackTrace: `{stackTrace.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(innerMsg)) sb.AppendLine($"innerMsg: `{innerMsg.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(url)) sb.AppendLine($"url: `{url.EscapeMarkdown()}`");
            
            string failReport = sb.ToString();
            _project.Var("failReport", failReport);
            return failReport;
        }
        
        public void MkScreenshot(string url, string watermark = null)
        {
            if (string.IsNullOrEmpty(url)) return;
            
            try
            {
                var sb = new StringBuilder();
                //sb.Append(_project.Name)
                sb.Append($"[{_project.Name}]")
                    .Append($"[{Time.Now()}]")
                    .Append(_project.LastExecutedActionId)
                    .Append(".jpg");
            
                string screenshotPath = Path.Combine(_project.Path, ".failed", _project.Variables["projectName"].Value, sb.ToString());
                _project?.SendInfoToLog(screenshotPath);
                
                
                lock (LockObject)
                {
                    ZennoPoster.ImageProcessingCropFromScreenshot(_instance.Port, screenshotPath, 0, 0, 1280, 720, "pixels");
                    ZennoPoster.ImageProcessingResizeFromFile(screenshotPath, screenshotPath, 50, 50, "percent", true, false);
                    
                }
                if (!string.IsNullOrEmpty(watermark)) AddWatermark(watermark, screenshotPath);
                
            }
            catch (Exception e)
            {
                _project.SendInfoToLog(e.Message ?? "Error during screenshot processing");
            }
        }

        public void AddWatermark(string watermarkText, string pathIn, string pathOut = null)
        {
            if (string.IsNullOrWhiteSpace(pathOut)) pathOut = pathIn;
            
            using (Image image = Image.FromFile(pathIn))
            using (Graphics imageGraphics = Graphics.FromImage(image))
            {
                Font font = new Font("Cascadia Code", 10);
                SizeF textSize = imageGraphics.MeasureString(watermarkText, font);
    
                int x = 5;
                int y = 5;
    
                Color watermarkColor = Color.FromArgb(255, Color.Red); 
                SolidBrush brush = new SolidBrush(watermarkColor);
    
                imageGraphics.DrawString(watermarkText, font, brush, x, y);
                    
                lock (LockObject)
                {
                    image.Save(pathOut);
                }
                font.Dispose();
                brush.Dispose();
            }
        }

        public string ErrorReport(bool toTg = false, bool toDb = false, bool screensot = false)
        {
            // Получаем все данные об ошибке в локальных переменных
            var error = _project.GetLastError();
            if (error == null) 
            {
                _project.SendInfoToLog("noErrorData");
                return "";
            }

            string actionId = error.ActionId.ToString() ?? "";
            string actionComment = error.ActionComment ?? "";
            string acc = _project.Var("acc0") ?? "";
            string type = "";
            string msg = "";
            string stackTrace = "";
            string innerMsg = "";
            string url = "";
            
            Exception ex = error.Exception;
            if (ex != null) 
            {
                try 
                {
                    var typeEx = ex.GetType();
                    type = typeEx?.Name ?? "noType";
                    msg = ex.Message ?? "noMessage";
                    stackTrace = ex.StackTrace ?? string.Empty;
                    stackTrace = stackTrace.Split(new[] { 'в' }, StringSplitOptions.None).Skip(1).FirstOrDefault()?.Trim() ?? string.Empty;
                    innerMsg = ex.InnerException?.Message ?? string.Empty;
                }
                catch (Exception exp)
                {
                    _project.SendWarningToLog(exp.Message);
                }
            }
            
            try { url = _instance.ActiveTab.URL; } catch { }

            // Формируем основной отчет
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(acc)) sb.AppendLine($"acc: {acc}");
            if (!string.IsNullOrEmpty(actionId)) sb.AppendLine($"id: {actionId}");
            if (!string.IsNullOrEmpty(actionComment)) sb.AppendLine($"actionComment: {actionComment}");
            if (!string.IsNullOrEmpty(type)) sb.AppendLine($"type: {type}");
            if (!string.IsNullOrEmpty(msg)) sb.AppendLine($"msg: {msg}");
            if (!string.IsNullOrEmpty(innerMsg)) sb.AppendLine($"innerMsg: {innerMsg}");
            if (!string.IsNullOrEmpty(stackTrace)) sb.AppendLine($"stackTrace: {stackTrace}");
            if (!string.IsNullOrEmpty(url)) sb.AppendLine($"url: {url}");
            
            string errorReport = sb.ToString().Replace("\\", "");
            //string beautified = errorReport.Replace("\\", "");
            _project.SendInfoToLog(errorReport);
            
            // Отправляем в телеграм если нужно
            if (toTg)
            {
                string tgRep = MkTgReport(acc, actionId, actionComment, type, msg, stackTrace, innerMsg, url);
                ToTelegram(tgRep);
            }
            
            // Делаем скриншот если нужно
            if (screensot)
            {
                MkScreenshot(url,errorReport );
            }
            
            // Обновляем базу если нужно
            if (toDb) 
                _project.DbUpd($"status = '! dropped: {errorReport}'", log:true);
            return errorReport;
        }

        public void ToTelegram(string reportString)
        {
            var creds = _project.DbGet("apikey, extra", "_api", where: "id = 'tg_logger'");
            var credsParts = creds.Split('|');

            string token = credsParts[0].Trim();
            var extraParts = credsParts[1].Trim().Split('/');
            string group = extraParts[0].Trim();
            string topic = extraParts[1].Trim();

            string encodedReport = Uri.EscapeDataString(reportString);
            string url = string.Format(
                "https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}&reply_to_message_id={3}&parse_mode=MarkdownV2",
                token, group, encodedReport, topic
            );

            _project.GET(url);
        }

        public string SuccessReport(bool log = false, bool ToTg = false)
        {
            var sb = new StringBuilder();
            string script = Path.GetFileName(_projectScript).EscapeMarkdown();
            string acc = _project.Var("acc0") ?? "";
            
            sb.AppendLine($"✅️\\#succsess  \\#acc{acc}  \\#{script}");
            
            string lastQuery = string.Empty;
            try 
            { 
                lastQuery = _project.Var("lastQuery"); 
            }
            catch (Exception ex) 
            { 
                _project.SendWarningToLog(ex.Message); 
            }

            if (!string.IsNullOrEmpty(lastQuery))
            {
                sb.Append("LastUpd: `")
                    .Append(lastQuery.EscapeMarkdown())
                    .AppendLine("` ");
            }

            sb.Append("TookTime: ")
                .Append(_project.TimeElapsed())
                .AppendLine("s ");

            var reportText = sb.ToString();
            if (ToTg) ToTelegram(reportText);
            reportText = reportText.Replace(@"\", "");
            if (log) _project.SendInfoToLog(reportText);
            
            return reportText;
        }
        
        public void SuccessToTelegram(string message = null)
        {
            var creds = _project.DbGet("apikey, extra", "_api", where: "id = 'tg_logger'");
            var credsParts = creds.Split('|');
            
            string token = credsParts[0].Trim();
            var extraParts = credsParts[1].Trim().Split('/');
            string group = extraParts[0].Trim();
            string topic = extraParts[1].Trim();
            
            string reportText;
            
            var failReportValue = _project.Variables["failReport"].Value;
            if (!string.IsNullOrEmpty(failReportValue))
            {
                reportText = failReportValue.EscapeMarkdown();
            }
            else
            {
                var sb = new StringBuilder();
                string projectName = Path.GetFileName(_project.Var("projectScript"));
                
                sb.Append("✅️\\#succsess  \\#")
                  .Append(projectName.EscapeMarkdown())
                  .Append(" \\#")
                  .Append(_project.Var("acc0").EscapeMarkdown())
                  .AppendLine();
                
                string lastQuery = string.Empty;
                try 
                { 
                    lastQuery = _project.Var("lastQuery"); 
                }
                catch (Exception ex) 
                { 
                    _project.SendWarningToLog(ex.Message); 
                }
                
                if (!string.IsNullOrEmpty(lastQuery))
                {
                    sb.Append("LastUpd: `")
                      .Append(lastQuery.EscapeMarkdown())
                      .AppendLine("` ");
                }
                
                if (!string.IsNullOrEmpty(message))
                {
                    sb.Append("Message:`")
                      .Append(message.EscapeMarkdown())
                      .AppendLine("` ");
                }
                
                sb.Append("TookTime: ")
                  .Append(_project.TimeElapsed())
                  .AppendLine("s ");
                
                reportText = sb.ToString();
            }
            
            string encodedReport = Uri.EscapeDataString(reportText);
            string url = string.Format(
                "https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}&reply_to_message_id={3}&parse_mode=MarkdownV2",
                token, group, encodedReport, topic
            );
            
            _project.GET(url);
            
            string lastQueryForLog = string.Empty;
            try 
            { 
                lastQueryForLog = _project.Var("lastQuery"); 
            }
            catch { }
            
            string toLog = string.Format(
                "{0}✔️ All jobs done. Elapsed: {1}s \n███ ██ ██  ██ █  █  █  ▓▓▓ ▓▓ ▓▓  ▓  ▓  ▓  ▒▒▒ ▒▒ ▒▒ ▒  ▒  ░░░ ░░  ░░ ░ ░ ░ ░ ░ ░  ░  ░  ░   ░   ░   ░    ░    ░    ░     ░        ░",
                lastQueryForLog, _project.TimeElapsed()
            );
            LogColor logColor = toLog.Contains("fail") ? LogColor.Orange : LogColor.Green;
            _project.SendToLog(toLog.Trim(), LogType.Info, true, logColor);
        }
        
        
    }
}