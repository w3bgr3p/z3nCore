using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Newtonsoft.Json.Linq;

namespace z3nCore
{
    public class Discord
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _log;
        private string _status;
        private string _token;
        private string _login;
        private string _pass;
        private string _2fa;
        
        public Discord(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _log = new Logger(project, log: log, classEmoji: "👾");
            LoadCreds();
        }
        
        private void LoadCreds()
        {
            var creds = _project.SqlGetDicFromLine("status, token, login, password, otpsecret", "_discord");
            _status = creds["status"];
            _token = creds["token"];
            _login = creds["login"];
            _pass = creds["password"];
            _2fa = creds["otpsecret"];

            if (string.IsNullOrEmpty(_login) || string.IsNullOrEmpty(_pass))
                throw new Exception($"invalid credentials login:[{_login}] pass:[{_pass}]");
        }
        
        private void TokenSet()
        {
            var jsCode = "function login(token) {\r\n    setInterval(() => {\r\n        document.body.appendChild(document.createElement `iframe`).contentWindow.localStorage.token = `\"${token}\"`\r\n    }, 50);\r\n    setTimeout(() => {\r\n        location.reload();\r\n    }, 1000);\r\n}\r\n    login(\'discordTOKEN\');\r\n".Replace("discordTOKEN", _token);
            _instance.ActiveTab.MainDocument.EvaluateScript(jsCode);
        }
        private string TokenGet()
        {
            var stats = new Traffic(_project,_instance).Get("https://discord.com/api/v9/science",  reload:true).RequestHeaders;
            string patern = @"(?<=uthorization:\ ).*";
            string token = System.Text.RegularExpressions.Regex.Match(stats, patern).Value;
            return token;
        }
        private string Login()
        {
            _project.SendInfoToLog("DLogin");
            _project.Deadline();

            _instance.CloseExtraTabs();
            _instance.HeSet(("input:text", "aria-label", "Email or Phone Number", "text", 0), _login);
            _instance.HeSet(("input:password", "aria-label", "Password", "text", 0), _pass);
            _instance.HeClick(("button", "type", "submit", "regexp", 0));


        capcha:
            while (_instance.ActiveTab.FindElementByAttribute("div", "innertext", "Are\\ you\\ human\\?", "regexp", 0).IsVoid &&
                _instance.ActiveTab.FindElementByAttribute("input:text", "autocomplete", "one-time-code", "regexp", 0).IsVoid) Thread.Sleep(1000);

            if (!_instance.ActiveTab.FindElementByAttribute("div", "innertext", "Are\\ you\\ human\\?", "regexp", 0).IsVoid)
            {
                _project.CapGuru();
                Thread.Sleep(5000);
                _project.Deadline(60);

                goto capcha;
            }
            _instance.HeSet(("input:text", "autocomplete", "one-time-code", "regexp", 0), OTP.Offline(_2fa));
            _instance.HeClick(("button", "type", "submit", "regexp", 0));
            Thread.Sleep(3000);
            return "ok";
        }
        public string Load(bool log = false)
        {
            //CredsFromDb();
            string state = null;
            var emu = _instance.UseFullMouseEmulation;
            _instance.UseFullMouseEmulation = false;
            bool tokenUsed = false;
            _instance.ActiveTab.Navigate("https://discord.com/channels/@me", "");

        start:
            state = null;
            while (string.IsNullOrEmpty(state))
            {
                _instance.HeClick(("button", "innertext", "Continue\\ in\\ Browser", "regexp", 0), thr0w: false);
                if (!_instance.ActiveTab.FindElementByAttribute("input:text", "aria-label", "Email or Phone Number", "text", 0).IsVoid) state = "login";
                if (!_instance.ActiveTab.FindElementByAttribute("section", "aria-label", "User\\ area", "regexp", 0).IsVoid) state = "logged";
            }

            _log.Send(state);


            if (state == "login" && !tokenUsed)
            {
                TokenSet();
                tokenUsed = true;
                //Thread.Sleep(5000);					
                goto start;
            }

            else if (state == "login" && tokenUsed)
            {
                var login = Login();
                if (login == "ok")
                {
                    Thread.Sleep(5000);
                    goto start;
                }
                else if (login == "capcha")
                    _log.Send("!W capcha");
                _project.CapGuru();
                _instance.UseFullMouseEmulation = emu;
                state = "capcha";
            }

            else if (state == "logged")
            {
                _instance.HeClick(("button", "innertext", "Apply", "regexp", 0), thr0w: false);
                state = _instance.ActiveTab.FindElementByAttribute("div", "class", "avatarWrapper__", "regexp", 0).FirstChild.GetAttribute("aria-label");

                _log.Send(state);
                var token = TokenGet();
                if (string.IsNullOrEmpty(token))
                _project.DbUpd($"token = '{token}', status = 'ok'", "_discord");
                _project.Var("discordSTATUS", "ok");
                _instance.UseFullMouseEmulation = emu;
            }
            return state;

        }
        public List<string> Servers()
        {
            _instance.UseFullMouseEmulation = true;
            var folders = new List<HtmlElement>();
            var servers = new List<string>();
            var list = _instance.ActiveTab.FindElementByAttribute("div", "aria-label", "Servers", "regexp", 0).GetChildren(false).ToList();
            foreach (HtmlElement item in list)
            {

                if (item.GetAttribute("class").Contains("listItem"))
                {
                    var server = item.FindChildByTag("div", 1).FirstChild.GetAttribute("data-dnd-name");
                    servers.Add(server);
                }

                if (item.GetAttribute("class").Contains("wrapper"))
                {
                    _instance.HeClick(item);
                    var FolderServer = item.FindChildByTag("ul", 0).GetChildren(false).ToList();
                    foreach (HtmlElement itemInFolder in FolderServer)
                    {
                        var server = itemInFolder.FindChildByTag("div", 1).FirstChild.GetAttribute("data-dnd-name");
                        servers.Add(server);
                    }
                }

            }

            string result = string.Join(" , ", servers);
            _project.DbUpd($"servers = '{result}'", "_discord");
            return servers;
        }
    }


}
