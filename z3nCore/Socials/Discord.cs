using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Newtonsoft.Json.Linq;
using z3nCore.Utilities;
namespace z3nCore
{
    public class Discord_
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Sleeper _idle;
        private readonly Logger _log;
        private string _status;
        private string _token;
        private string _login;
        private string _pass;
        private string _2fa;
        
        
        public Discord_(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _log = new Logger(project, log: log, classEmoji: "👾");
            _idle = new Sleeper(1337, 2078);
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
            _project.DbUpd($"servers = '{result}'", "__discord");
            return servers;
        }
    }
    public class Discord
    {
        #region Members & constructor
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _log;
        private readonly Sleeper _idle;
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
            _idle = new Sleeper(1337, 2078);
            LoadCreds();
        }
        #endregion
        #region Authentication
        
        private void LoadCreds()
        {
            var creds = _project.SqlGetDicFromLine("status, token, login, password, otpsecret", "_discord");
            _status = creds["status"];
            _token = creds["token"];
            _login = creds["login"];
            _pass = creds["password"];
            _2fa = creds["otpsecret"];

            _log.Send($"Creds loaded: status={_status}, login={_login}, hasToken={!string.IsNullOrEmpty(_token)}, has2FA={!string.IsNullOrEmpty(_2fa)}");

            if (string.IsNullOrEmpty(_login) || string.IsNullOrEmpty(_pass))
                throw new Exception($"invalid credentials login:[{_login}] pass:[{_pass}]");
        }
        private void TokenSet()
        {
            var jsCode = "function login(token) {\r\n    setInterval(() => {\r\n        document.body.appendChild(document.createElement `iframe`).contentWindow.localStorage.token = `\"${token}\"`\r\n    }, 50);\r\n    setTimeout(() => {\r\n        location.reload();\r\n    }, 1000);\r\n}\r\n    login(\'discordTOKEN\');\r\n".Replace("discordTOKEN", _token);
            _instance.ActiveTab.MainDocument.EvaluateScript(jsCode);
            _log.Send($"Token injected: length={_token?.Length ?? 0}");
            Thread.Sleep(5000);
        }
        private string TokenGet(bool saveToDb = false)
        {
            var stats = new Traffic(_project,_instance).Get("https://discord.com/api/v9/science",  reload:true).RequestHeaders;
            string patern = @"(?<=uthorization:\ ).*";
            string token = System.Text.RegularExpressions.Regex.Match(stats, patern).Value;
            _log.Send($"Token extracted: length={token?.Length ?? 0}, valid={!string.IsNullOrEmpty(token)}");
            if (saveToDb) _project.DbUpd($"token = '{token}', status = 'ok'", "_discord");
            return token;
        }
        
        private string Login()
        {
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
                _log.Send("Captcha detected, solving...");
                _project.CapGuru();
                Thread.Sleep(5000);
                _project.Deadline(60);
                goto capcha;
            }
            
            _log.Send("2FA required, entering code...");
            _instance.HeSet(("input:text", "autocomplete", "one-time-code", "regexp", 0), OTP.Offline(_2fa));
            _instance.HeClick(("button", "type", "submit", "regexp", 0));
            Thread.Sleep(3000);
            return "ok";
        }

        private void InputCredentials()
        {
            _instance.HeSet(("input:text", "aria-label", "Email or Phone Number", "text", 0), _login);
            _instance.HeSet(("input:password", "aria-label", "Password", "text", 0), _pass);
            _instance.HeClick(("button", "type", "submit", "regexp", 0));
        }
        public string GetState(bool log = false)
        {
            _project.log("getting state");
            _idle.Sleep();
            if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Continue\\ in\\ Browser", "regexp", 0).IsVoid) return "appDetected";
            if (!_instance.ActiveTab.FindElementByAttribute("section", "aria-label", "User\\ area", "regexp", 0).IsVoid) return "logged";
            if (!_instance.ActiveTab.FindElementByAttribute("div", "innertext", "Are\\ you\\ human\\?", "regexp", 0).IsVoid) return "capctha";
            if (!_instance.ActiveTab.FindElementByAttribute("input:text", "autocomplete", "one-time-code", "regexp", 0).IsVoid) return "input_otp";
            var helperText = _instance.ActiveTab.FindElementByAttribute("div", "class", "helperTextContainer__", "regexp", 0).InnerText;
            if (helperText != "") return helperText;
            if (!_instance.ActiveTab.FindElementByAttribute("input:text", "aria-label", "Email or Phone Number", "text", 0).IsVoid) return "input_credentials";
            
            return "";
        }
        public string Load(bool log = false)
        {
            string state = null;
            var emu = _instance.UseFullMouseEmulation;
            _instance.UseFullMouseEmulation = false;
            bool tokenUsed = false;
            bool credentialsUsed = false;

            _instance.Go("https://discord.com/channels/@me");
            _project.Deadline();
        start:
        _project.Deadline(60);
            state = null;
            while (string.IsNullOrEmpty(state))
            {
                state = GetState();
            }
            
            _log.Send($"Page state detected: {state}, tokenUsed={tokenUsed}");

            if (!tokenUsed)
            {
                _log.Send("Attempting token auth...");
                TokenSet();
                tokenUsed = true;
                goto start;
            }
            
            
            switch (state){
                case "input_credentials":
                    _log.Send($"Token auth failed, using credentials: login={_login}");
                    credentialsUsed = true;
                    InputCredentials();
                    goto start;
                    
                case "appDetected":
                    _log.Send("appDetected ");
                    _instance.HeClick(("span", "innertext", "Continue\\ in\\ Browser", "regexp", 0));
                    goto start;
                    
                case "capctha":
                    _log.Send("!W captcha ");
                    _project.CapGuru();
                    goto start;
                    
                case "input_otp":
                    _log.Send("2FA required, entering code...");
                    _instance.HeSet(("input:text", "autocomplete", "one-time-code", "regexp", 0), OTP.Offline(_2fa));
                    _instance.HeClick(("button", "type", "submit", "regexp", 0));
                    goto start;                 
                    
                case "logged":
                    _instance.HeClick(("button", "innertext", "Apply", "regexp", 0), thr0w: false);
                    
                    var account = _instance.ActiveTab.FindElementByAttribute("div", "class", "avatarWrapper__", "regexp", 0).FirstChild.GetAttribute("aria-label");
                    _log.Send($"logged with {account}");
                    if (credentialsUsed)
                    {
                        TokenGet(true);
                    }
                    _instance.UseFullMouseEmulation = emu;
                    return state;
                
                default:
                    _log.Warn(state);
                    return state;
            }
           
        }
        #endregion
        #region Stats & Info
        
        public List<string> Servers(bool toDb = false, bool log = false)
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

            string result = string.Join("\n", servers);
            _log.Send($"Servers found: count={servers.Count}, inFolders={folders.Count}, toDb={toDb}\n[{string.Join(", ", servers)}]");
            
            if(toDb) _project.DbUpd($"servers = '{result}'", "__discord");
            return servers;
        }
        public List<string> GetRoles(string gmChannelLink, string gmMessage = "gm", bool log = false)
        {
            _idle.Sleep();
            var username = _instance.HeGet(("section", "aria-label", "User\\ area", "regexp", 0)).Split('\n')[0];

            if (_instance.ActiveTab.FindElementByAttribute("div", "id", "popout_", "regexp", 0).IsVoid)
            {
                if (_instance.ActiveTab.FindElementByAttribute("span", "innertext", username, "text", 1).IsVoid)
                {
                    _log.Send($"Triggering GM to show username: channel={gmChannelLink}, message={gmMessage}");
                    GM(gmChannelLink, gmMessage);
                    _idle.Sleep();
                }

                _instance.HeClick(("span", "innertext", username, "text", 1));
            }
            
            HtmlElement pop = _instance.GetHe(("div", "id", "popout_", "regexp", 0));
            var rolesHeList = pop.FindChildByAttribute("div", "data-list-id", "roles", "regexp", 0).GetChildren(false).ToList();
            
            var roles = new List<string>();
            foreach (var role in rolesHeList)
            {
                roles.Add(role.GetAttribute("aria-label"));
            }
            
            _log.Send($"Roles extracted: user={username}, count={roles.Count}\n[{string.Join(", ", roles)}]");
            return roles;
        }
        public void GM(string gmChannelLink, string message = "gm")
        {
            try
            {
                _instance.HeClick(("a", "href", gmChannelLink, "regexp", 0), deadline:3);
            }
            catch
            {
                _instance.Go(gmChannelLink);
            }

            var err = "";
            try
            {
                _instance.HeGet(("h2", "innertext", "NO\\ TEXT\\ CHANNELS", "regexp", 0), deadline:3);
                err = "notOnServer";
            }
            catch
            {
            }
            try
            {
                _instance.HeGet(("div", "innertext", "You\\ do\\ not\\ have\\ permission\\ to\\ send\\ messages\\ in\\ this\\ channel.", "regexp", 0), deadline:0);
                err = "no permission to send messages";
            }
            catch
            {
            }
            
            if (err != "") _log.Warn(err, thrw: true);

            _instance.HeClick(("div", "aria-label", "Message\\ \\#", "regexp", 0));
            _instance.WaitFieldEmulationDelay();
            _instance.SendText($"{message}" +"{ENTER}", 15);
            
            _log.Send($"Message sent: channel={gmChannelLink}, text='{message}'");
        }
        public void UpdateServerInfo(string gmChannelLink)
        {
            var roles = GetRoles(gmChannelLink);
            var serverName = _instance.HeGet(("header", "class", "header_", "regexp", 0));
            _project.ClmnAdd(serverName, _project.ProjectTable());
            _project.DbUpd($"{serverName} = '{string.Join(", ", roles)}'");
        }

        #endregion
        public void Auth()
        {
            var emu = _instance.UseFullMouseEmulation;
            var d = "M12.7 20.7a1 1 0 0 1-1.4 0l-5-5a1 1 0 1 1 1.4-1.4l3.3 3.29V4a1 1 0 1 1 2 0v13.59l3.3-3.3a1 1 0 0 1 1.4 1.42l-5 5Z";
            _instance.UseFullMouseEmulation = true;
            _instance.ActiveTab.FullEmulationMouseMove(700,350);

            _instance.HeGet(("button", "data-mana-component", "button", "regexp", 1));
            _project.Deadline();
            
            int scrollAttempts = 0;
            while (true)
            {
                _project.Deadline(30);
                if (!_instance.ActiveTab.FindElementByAttribute("button", "data-mana-component", "button", "regexp", 1).GetAttribute("innerhtml").Contains(d))
                    break;
                _instance.ActiveTab.FullEmulationMouseWheel(0, 1000);
                scrollAttempts++;
            }

            _instance.HeClick(("button", "data-mana-component", "button", "regexp", 1));
            _log.Send($"Auth completed: scrolls={scrollAttempts}");
            _instance.UseFullMouseEmulation = emu;
        }
        
    }

}
