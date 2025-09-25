using System;

using System.Runtime.CompilerServices;

using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class Telegram
    {
        protected readonly IZennoPosterProjectModel _project;
        protected readonly bool _logShow;
        
        protected readonly NetHttp _http;

        protected string _token;
        protected string _group;
        protected string _topic;

        public Telegram(IZennoPosterProjectModel project, bool log = false)
        {

            _project = project;
            
            _http = new NetHttp(_project);
            _logShow = log;
            LoadCreds();
        }

        protected void Log(string tolog = "", [CallerMemberName] string callerName = "", bool log = false)
        {
            if (!_logShow && !log) return;
            var stackFrame = new System.Diagnostics.StackFrame(1);
            var callingMethod = stackFrame.GetMethod();
            if (callingMethod == null || callingMethod.DeclaringType == null || callingMethod.DeclaringType.FullName.Contains("Zenno")) callerName = "null";
            _project.log($"[ 🚀  {callerName}] [{tolog}] ");
        }

        private void LoadCreds()
        {
            _token = _project.Variables["tg_logger_token"].Value;
            _group = _project.Variables["tg_logger_group"].Value;
            _topic = _project.Variables["tg_logger_topic"].Value;

        }

        public void Report()
        {


            string time = _project.ExecuteMacro(DateTime.Now.ToString("MM-dd HH:mm"));
            string report = "";

            if (!string.IsNullOrEmpty(_project.Variables["failReport"].Value))
            {
                string encodedFailReport = Uri.EscapeDataString(_project.Variables["failReport"].Value);
                string failUrl = $"https://api.telegram.org/bot{_token}/sendMessage?chat_id={_group}&text={encodedFailReport}&reply_to_message_id={_topic}&parse_mode=MarkdownV2";
                _http.GET(failUrl);
            }
            else
            {
                report = $"✅️ [{time}]{_project.Name}";

                string successReport = $"✅️  \\#{_project.Name.EscapeMarkdown()} \\#{_project.Variables["acc0"].Value} \n";

                string encodedReport = Uri.EscapeDataString(successReport);
                //_project.Variables["REPORT"].Value = encodedReport;
                string url = $"https://api.telegram.org/bot{_token}/sendMessage?chat_id={_group}&text={encodedReport}&reply_to_message_id={_topic}&parse_mode=MarkdownV2";
                _http.GET(url);
            }
            string toLog = $"✔️ All jobs done. Elapsed: {_project.TimeElapsed()}s \n███ ██ ██  ██ █  █  █  ▓▓▓ ▓▓ ▓▓  ▓  ▓  ▓  ▒▒▒ ▒▒ ▒▒ ▒  ▒  ░░░ ░░  ░░ ░ ░ ░ ░ ░ ░  ░  ░  ░   ░   ░   ░    ░    ░    ░     ░        ░          ░";
            if (toLog.Contains("fail")) _project.SendToLog(toLog.Trim(), LogType.Info, true, LogColor.Orange);
            else _project.SendToLog(toLog.Trim(), LogType.Info, true, LogColor.Green);
        }




    }

}
