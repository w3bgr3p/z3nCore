using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Text;
using System.IO;

namespace z3nCore
{
         public class Logger
    {
        protected readonly IZennoPosterProjectModel _project;
        protected bool _logShow = false;
        private string _emoji = null;
        private static readonly Process CurrentProcess = Process.GetCurrentProcess();

        private bool _acc = false;
        private bool _port = false;
        private bool _time = false;
        private bool _memory = false;
        private bool _caller = false;
        private bool _wrap = false;
        private bool _force = false;

        private static readonly Dictionary<string, LogColor> ColorMap = new Dictionary<string, LogColor> { 
            { "`.", LogColor.Default },
            { "`w", LogColor.Gray },
            { "`y", LogColor.Yellow },
            { "`o", LogColor.Orange },
            { "`r", LogColor.Red },
            { "`p", LogColor.Pink },
            { "`v", LogColor.Violet },
            { "`b", LogColor.Blue },
            { "`lb", LogColor.LightBlue },
            { "`t", LogColor.Turquoise },
            { "`g", LogColor.Green },
            { "!W", LogColor.Orange },
            { "!E", LogColor.Orange },
            { "relax", LogColor.LightBlue },              
            { "Err", LogColor.Orange },
        };


        public Logger(IZennoPosterProjectModel project, bool log = false, string classEmoji = null)
        {
            _project = project;
            _logShow = log || _project.Var("debug") == "True";
            _emoji = classEmoji;
            SetFromConfig();
        }

        private void SetFromConfig()
        {
            var flags = _project.Var("cfgLog").Split(',').Select(x => x.Trim()).ToList();
            foreach (string flag in flags)
            {
                switch (flag)
                {
                    case "acc":
                        _acc = true;
                        continue;
                    case "port":
                        _port = true;
                        continue;
                    case "time":
                        _time = true;
                        continue;
                    case "memory":
                        _memory = true;
                        continue;
                    case "caller":
                        _caller = true;
                        continue;
                    case "wrap": 
                        _wrap = true;
                        continue;
                    case "force":
                        _force = true;
                        continue;
                    default:
                        continue;
                }
            }
            
        }

        public void Send(string toLog, [CallerMemberName] string callerName = "", bool show = false, bool thr0w = false, bool toZp = true, int cut = 0, bool wrap = true)
        {
            if (!show && !_logShow) return;
            string header = string.Empty;
            string body = toLog;

            if (_wrap)
            {
                var stackFrame = new System.Diagnostics.StackFrame(1);
                header = LogHeader(stackFrame, callerName);
                body = LogBody(toLog, cut);
            }
            string toSend = header + body;
            (LogType type, LogColor color) = LogColour(header, toLog);
            Execute(toSend, type, color, toZp, thr0w);

        }

        private string LogHeader(System.Diagnostics.StackFrame stackFrame, string callerName)
        {
            
            var sb = new StringBuilder(128);
            
            string formated = null;
            if (_acc)
                try
                {
                    string acc0 = _project.Var("acc0");
                    if (!string.IsNullOrEmpty(acc0)) sb.Append( $"  🤖 [{acc0}]");
                }
                catch { }
            if (_port)
                try
                {
                    string port = _project.Var("instancePort");
                    if (!string.IsNullOrEmpty(port)) sb.Append($"  🔌 [{port}]");
                }
                catch { }
            if (_time)
                try
                {
                    string totalAge = _project.Age<string>();
                    if (!string.IsNullOrEmpty(totalAge)) sb.Append($"  ⏱️ [{totalAge}]");
                }
                catch { }
            if (_memory)
                try
                {
                    CurrentProcess.Refresh(); 
                    long processMemory = CurrentProcess.WorkingSet64 / 1024 / 1024;
                    sb.Append("  📊 [").Append(processMemory).Append(" Mb]");
                }
                catch { }
            if (_caller)
                try
                {
                    var callingMethod = stackFrame.GetMethod();
                    if (callingMethod == null || callingMethod.DeclaringType == null || callingMethod.DeclaringType.FullName.Contains("Zenno") || callingMethod.Name == "L0g")   
                        sb.Append($"  🔳 [{_project.Name.Replace(".zp", "")}]");
                    else
                        sb.Append($"  🔲 [{callingMethod.DeclaringType.Name}.{callerName}]");
                }
                catch { }

            return sb.Length > 0 ? sb.ToString() : string.Empty;
        }
        private string LogBody(string toLog, int cut)
        {
            if (!string.IsNullOrEmpty(toLog))
            {
                if (cut != 0)
                {
                    int lineCount = 1;
                    for (int i = 0; i < toLog.Length; i++)
                    {
                        if (toLog[i] == '\n') lineCount++;
                    }
    
                    if (lineCount > cut)
                    {
                        var sb = new StringBuilder(toLog.Length);
                        for (int i = 0; i < toLog.Length; i++)
                        {
                            char c = toLog[i];
                            if (c == '\r' || c == '\n')
                                sb.Append(' ');
                            else
                                sb.Append(c);
                        }
                        toLog = sb.ToString();
                    }
                }
                if (!string.IsNullOrEmpty(_emoji))
                {
                    toLog = $"[ {_emoji} ] {toLog}";
                }
                return $"\n          {toLog.Trim()}";
            }
            return string.Empty;

        }
        private (LogType, LogColor) LogColour(string header, string toLog)
        {
            LogType type = LogType.Info;
            LogColor color = LogColor.Default;

            string combined = (header ?? "") + (toLog ?? "");
            foreach (var pair in ColorMap)
            {
                if (combined.Contains(pair.Key))
                {
                    color = pair.Value;
                    break;
                }
            }

            if (combined.Contains("!W")) type = LogType.Warning;
            if (combined.Contains("!E")) type = LogType.Error;

            return (type, color);
        }
        private void Execute(string toSend, LogType type, LogColor color, bool toZp, bool thr0w)
        {
            _project.SendToLog(toSend, type, toZp, color);
            if (thr0w) throw new Exception($"{toSend}");
        }
        private static string EscapeMarkdownV2(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var sb = new StringBuilder(text.Length * 2); 
            foreach (char c in text)
            {
                switch (c)
                {
                    case '_':
                    case '*':
                    case '[':
                    case ']':
                    case '(':
                    case ')':
                    case '~':
                    case '`':
                    case '>':
                    case '#':
                    case '+':
                    case '-':
                    case '=':
                    case '|':
                    case '{':
                    case '}':
                    case '.':
                    case '!':
                        sb.Append('\\').Append(c);
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
        public void SendToTelegram(string message = null)
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
                reportText = EscapeMarkdownV2(failReportValue);
            }
            else
            {
                var sb = new StringBuilder();
                string projectName = Path.GetFileName(_project.Var("projectScript"));
                
                sb.Append("✅️\\#succsess  \\#")
                  .Append(EscapeMarkdownV2(projectName))
                  .Append(" \\#")
                  .Append(EscapeMarkdownV2(_project.Var("acc0")))
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
                      .Append(EscapeMarkdownV2(lastQuery))
                      .AppendLine("` ");
                }
                
                if (!string.IsNullOrEmpty(message))
                {
                    sb.Append("Message:`")
                      .Append(EscapeMarkdownV2(message))
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
