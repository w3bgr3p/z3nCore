using ZennoLab.CommandCenter;

using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Text;
using System;
using System.IO;
using System.Linq;
namespace z3nCore
{
    public class Reporter
    {
 
        protected readonly IZennoPosterProjectModel _project;
        protected readonly Instance _instance;


        private string ActionId {get;set; }
        private string ActionComment {get;set; }
        private string Acc {get;set; }
        private string Type {get;set; }
        private string Msg {get;set; }
        private string StackTrace {get;set; }
        private string InnerMsg {get;set; }
        private string Url {get;set; }

        private string ErrReport {get;set; }
        private readonly string _projectScript;

        
        public Reporter(IZennoPosterProjectModel project, Instance instance, bool log = false, string classEmoji = null)
        {
            _project = project;
            _instance = instance;
            _projectScript = project.Var("projectScript");
        }
        
        private string GetErrorData()
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

            ActionId = actionId;
            ActionComment =  actionComment;
            Acc = acc;
            Type = type;
            Msg = msg;
            StackTrace = stackTrace;
            InnerMsg = innerMsg;
            Url = url; 
            

            if (!string.IsNullOrEmpty(Acc)) sb.AppendLine($"acc: {Acc}");
            if (!string.IsNullOrEmpty(ActionId)) sb.AppendLine($"id: {ActionId}");
            if (!string.IsNullOrEmpty(ActionComment)) sb.AppendLine($"actionComment: {ActionComment}");
            if (!string.IsNullOrEmpty(Type)) sb.AppendLine($"type: {Type}");
            if (!string.IsNullOrEmpty(Msg)) sb.AppendLine($"msg: {Msg}");
            if (!string.IsNullOrEmpty(InnerMsg)) sb.AppendLine($"innerMsg: {InnerMsg}");
            if (!string.IsNullOrEmpty(StackTrace)) sb.AppendLine($"stackTrace: {StackTrace}");
            if (!string.IsNullOrEmpty(Url)) sb.AppendLine($"url: {Url}");
            
            _project.SendInfoToLog(sb.ToString().Replace("\\",""));
            
            ErrReport = sb.ToString();
            return sb.ToString();
        }

        private string MkTgReport()
        {
            var sb = new StringBuilder();
            string script = Path.GetFileName(_projectScript).EscapeMarkdown();

            //REPORT
            sb.Clear();
            sb.AppendLine($"⛔️\\#fail  \\#acc{Acc}  \\#{script}");
            if (!string.IsNullOrEmpty(ActionId)) sb.AppendLine($"ActionId: `{ActionId.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(ActionComment)) sb.AppendLine($"actionComment: `{ActionComment.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(Type)) sb.AppendLine($"type: `{Type.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(Msg)) sb.AppendLine($"msg: `{Msg.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(StackTrace)) sb.AppendLine($"stackTrace: `{StackTrace.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(InnerMsg)) sb.AppendLine($"innerMsg: `{InnerMsg.EscapeMarkdown()}`");
            if (!string.IsNullOrEmpty(Url)) sb.AppendLine($"url: `{Url.EscapeMarkdown()}`");
            
            string failReport = sb.ToString();
            _project.Var("failReport",failReport) ;
            return failReport;
        }
        
        private void MkScreenshot()
        {
            var sb = new StringBuilder();
            string script = Path.GetFileName(_projectScript).EscapeMarkdown();
            
            if (!string.IsNullOrEmpty(Url))
                try
                {
                    sb.Clear(); 
                    sb.Append(_project.Path).Append(".failed\\")
                        .Append(_project.Variables?["projectName"]?.Value ?? "Unknown")
                        .Append("\\")
                        .Append(_project?.Name ?? "Unknown")
                        .Append(" • ")
                        .Append(_project?.Variables?["acc0"]?.Value ?? "Unknown")
                        .Append(" • [")
                        .Append(_project?.LastExecutedActionId ?? "Unknown")
                        .Append(" - ")
                        .Append(ActionId)
                        .Append("].jpg");
                
                    string screenshotPath = sb.ToString();
                    _project?.SendInfoToLog(screenshotPath);
                    ZennoPoster.ImageProcessingResizeFromScreenshot(_instance?.Port ?? 0, screenshotPath, 50, 50, "percent", true, false);
                }
                catch (Exception e)
                {
                    _project.SendInfoToLog(e.Message ?? "Error during screenshot processing");
                }
        }    
        
        public string ErrorReport(bool toTg = false, bool toDb = false, bool screensot = false)
        {

            GetErrorData();
            if (toTg)
            {
                string tgRep = MkTgReport();
                ToTelegram(tgRep);
            }
            
            //SCREEN
            if (screensot)
            {
                MkScreenshot();
            }
            
            if (toDb) _project.DbUpd($"status = '! dropped: {ErrReport.Replace("\\","")}'", log:true);
            return ErrReport;
            
        }
        private void ToTelegram( string reportString)
        {
            var creds = _project.DbGet("apikey, extra", "_api", where: "id = 'tg_logger'");
            var credsParts = creds.Split('|');
    
            string token = credsParts[0].Trim();
            var extraParts = credsParts[1].Trim().Split('/');
            string group = extraParts[0].Trim();
            string topic = extraParts[1].Trim();

            var report = _project.Variables["failReport"].Value;
            
            string encodedReport = Uri.EscapeDataString(reportString);
            string url = string.Format(
                "https://api.telegram.org/bot{0}/sendMessage?chat_id={1}&text={2}&reply_to_message_id={3}&parse_mode=MarkdownV2",
                token, group, encodedReport, topic
            );
    
            _project.GET(url);
        }
        public string SuccessReport( bool log = false, bool ToTg = false)
        {
            var sb = new StringBuilder();
            string script = Path.GetFileName(_projectScript).EscapeMarkdown();
            sb.AppendLine($"✅️\\#succsess  \\#acc{Acc}  \\#{script}");
            
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

    }
}