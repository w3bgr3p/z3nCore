using ZennoLab.CommandCenter;

using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Text;
using System;
using System.IO;
using System.Linq;
namespace z3nCore
{
    public static class Reporter
    {

        public static string ErrorReport(this IZennoPosterProjectModel project, Instance instance, bool log = false, bool ToTg = false)
        {

            var error = project.GetLastError();
            if (error == null) 
                return "errNotFound";

            var sb = new StringBuilder();

            string actionId = error.ActionId.ToString() ?? "noActionId";
            string actionComment = error.ActionComment ?? "noActionComment";
            string actionGroupId = error.ActionGroupId ?? "noGroup";

            string type = "no";
            string msg = "noException";
            string stackTrace = "no";
            string innerMsg = "no";

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
                    project.SendWarningToLog(exp.Message);
                }
            }

            sb.AppendLine($"id: {actionId}")
              .AppendLine($"comment: {actionComment}")
              .AppendLine($"exType: {type}")
              .AppendLine($"exMsg: {msg}")
              .AppendLine($"innerMsg: {innerMsg}")
              .AppendLine($"stackTrace: {stackTrace}");

            if (log) project.SendInfoToLog(sb.ToString().Replace("\\",""));

            //REPORT
            sb.Clear(); 
            sb.AppendLine($"⛔️\\#fail  \\#{Path.GetFileName(project.Var("projectScript"))?.EscapeMarkdown() ?? "Unknown"} \\#{project?.Variables?["acc0"]?.Value ?? "Unknown"}")
              .AppendLine($"Error: `{actionId.EscapeMarkdown()}`")
              .AppendLine($"Comment: `{actionComment.EscapeMarkdown()}`")
              .AppendLine($"Type: `{type.EscapeMarkdown()}`")
              .AppendLine($"Msg: `{msg.EscapeMarkdown()}`")
              .AppendLine($"Trace: `{stackTrace.EscapeMarkdown()}`");

            if (!string.IsNullOrEmpty(innerMsg))
                sb.AppendLine($"Inner: `{innerMsg.EscapeMarkdown()}`");

            string browser = string.Empty;
            try { browser = instance.BrowserType.ToString(); } catch { }

            if (browser == "Chromium")
                sb.AppendLine($"Page: `{instance.ActiveTab?.URL?.EscapeMarkdown() ?? "Unknown"}`");

            string failReport = sb.ToString();
            if (project?.Variables?["failReport"] != null)
                project.Variables["failReport"].Value = failReport;

            //SCREEN
            if (browser == "Chromium")
            try
            {
                sb.Clear(); 
                sb.Append(project?.Path ?? "")
                  .Append(".failed\\")
                  .Append(project?.Variables?["projectName"]?.Value ?? "Unknown")
                  .Append("\\")
                  .Append(project?.Name ?? "Unknown")
                  .Append(" • ")
                  .Append(project?.Variables?["acc0"]?.Value ?? "Unknown")
                  .Append(" • [")
                  .Append(project?.LastExecutedActionId ?? "Unknown")
                  .Append(" - ")
                  .Append(actionId)
                  .Append("].jpg");
                
                string screenshotPath = sb.ToString();
                project?.SendInfoToLog(screenshotPath);
                ZennoPoster.ImageProcessingResizeFromScreenshot(instance?.Port ?? 0, screenshotPath, 50, 50, "percent", true, false);
            }
            catch (Exception e)
            {
                if (log) project?.SendInfoToLog(e.Message ?? "Error during screenshot processing");
            }
            if (ToTg) project.ToTelegram(failReport);
            
            return failReport;
            
                        
        }
        private static void ToTelegram(this IZennoPosterProjectModel project, string reportString)
        {
            var creds = project.DbGet("apikey, extra", "_api", where: "id = 'tg_logger'");
            var credsParts = creds.Split('|');
    
            string token = credsParts[0].Trim();
            var extraParts = credsParts[1].Trim().Split('/');
            string group = extraParts[0].Trim();
            string topic = extraParts[1].Trim();

            var report = project.Variables["failReport"].Value;
            
            string encodedReport = Uri.EscapeDataString(reportString);
            string url = string.Format(
                "https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}&reply_to_message_id={3}&parse_mode=MarkdownV2",
                token, group, encodedReport, topic
            );
    
            project.GET(url);
        }
        public static string SuccessReport(this IZennoPosterProjectModel project, bool log = false, bool ToTg = false)
        {
            var sb = new StringBuilder();
            string projectName = Path.GetFileName(project.Var("projectScript")).EscapeMarkdown();

            sb.Append("✅️\\#succsess  \\#")
                .Append(projectName)
                .Append(" \\#")
                .Append(project.Var("acc0").EscapeMarkdown())
                .AppendLine();

            string lastQuery = string.Empty;
            try 
            { 
                lastQuery = project.Var("lastQuery"); 
            }
            catch (Exception ex) 
            { 
                project.SendWarningToLog(ex.Message); 
            }

            if (!string.IsNullOrEmpty(lastQuery))
            {
                sb.Append("LastUpd: `")
                    .Append(lastQuery.EscapeMarkdown())
                    .AppendLine("` ");
            }


            sb.Append("TookTime: ")
                .Append(project.TimeElapsed())
                .AppendLine("s ");

            var reportText = sb.ToString();
            if (log) project.SendInfoToLog(reportText);
            if (ToTg)project.ToTelegram(reportText);
            return reportText;
        }

    }
}