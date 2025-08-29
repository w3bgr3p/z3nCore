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
        protected readonly IZennoPosterProjectModel _project;
        protected readonly Instance _instance;
        protected readonly bool _logShow;
        protected readonly string _pass;

        protected readonly NetHttp _http;
        public Discord(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _logShow = log;
            _http = new NetHttp(_project);
        }
        public void Log(string tolog = "", [CallerMemberName] string callerName = "", bool log = false)
        {
            if (!_logShow && !log) return;
            var stackFrame = new System.Diagnostics.StackFrame(1);
            var callingMethod = stackFrame.GetMethod();
            if (callingMethod == null || callingMethod.DeclaringType == null || callingMethod.DeclaringType.FullName.Contains("Zenno")) callerName = "null";
            _project.L0g($"[ 👾  {callerName}] [{tolog}] ");
        }

        public bool ManageRole(string botToken, string guildId, string roleName, string userId, bool assignRole, [CallerMemberName] string callerName = "")
        {
            Thread.Sleep(1000);
            try
            {
                var headers = new Dictionary<string, string>
                {
                    { "Authorization", $"Bot {botToken}" },
                    { "User-Agent", "DiscordBot/1.0" }
                };

                string rolesUrl = $"https://discord.com/api/v10/guilds/{guildId}/roles";
                string rolesResponse = _http.GET(rolesUrl, headers: headers, callerName: callerName);
                Thread.Sleep(1000);
                if (rolesResponse.StartsWith("Ошибка"))
                {
                    Log($"!W Не удалось получить роли сервера:{rolesUrl} {rolesResponse}", callerName, true);
                    return false;
                }

                JArray roles = JArray.Parse(rolesResponse);
                var role = roles.FirstOrDefault(r => r["name"].ToString().Equals(roleName, StringComparison.OrdinalIgnoreCase));
                if (role == null)
                {
                    Log($"!W Роль с именем '{roleName}' не найдена на сервере", callerName, true);
                    return false;
                }
                string roleId = role["id"].ToString();
                Log($"found : {roleName} (ID: {roleId})", callerName);

                string url = $"https://discord.com/api/v10/guilds/{guildId}/members/{userId}/roles/{roleId}";

                string result;
                if (assignRole)
                {
                    result = _http.PUT(url, "", proxyString: null, headers: headers, callerName: callerName);
                    Thread.Sleep(1000);
                }
                else
                {
                    result = _http.DELETE(url, proxyString: null, headers: headers, callerName: callerName);
                    Thread.Sleep(1000);
                }

                if (result.StartsWith("Ошибка"))
                {
                    Log($"!W Не удалось {(assignRole ? "выдать" : "удалить")} роль:{url} {result}", callerName, true);
                    return false;
                }

                Log($"{(assignRole ? "Роль успешно выдана" : "Роль успешно удалена")}: {roleName} для пользователя {userId}", callerName);
                return true;
            }
            catch (Exception e)
            {
                Log($"!W Ошибка при управлении ролью: [{e.Message}]", callerName, true);
                return false;
            }
        }


        public string CredsFromDb()
        {


            var resp = _project.SqlGet("status, token, login, password, otpsecret", "_discord");

            string[] discordData = resp.Split('|');
            _project.Variables["discordSTATUS"].Value = discordData[0].Trim();
            _project.Variables["discordTOKEN"].Value = discordData[1].Trim();
            _project.Variables["discordLOGIN"].Value = discordData[2].Trim();
            _project.Variables["discordPASSWORD"].Value = discordData[3].Trim();
            _project.Variables["discord2FACODE"].Value = discordData[4].Trim();

            return _project.Variables["discordSTATUS"].Value;
        }



        private void DSsetToken()
        {
            var jsCode = "function login(token) {\r\n    setInterval(() => {\r\n        document.body.appendChild(document.createElement `iframe`).contentWindow.localStorage.token = `\"${token}\"`\r\n    }, 50);\r\n    setTimeout(() => {\r\n        location.reload();\r\n    }, 1000);\r\n}\r\n    login(\'discordTOKEN\');\r\n".Replace("discordTOKEN", _project.Variables["discordTOKEN"].Value);
            _instance.ActiveTab.MainDocument.EvaluateScript(jsCode);
        }
        private string DSgetToken()
        {
            var stats = new Traffic(_project,_instance).Get("https://discord.com/api/v9/science", "RequestHeaders", reload:true);
            string patern = @"(?<=uthorization:\ ).*";
            string token = System.Text.RegularExpressions.Regex.Match(stats, patern).Value;
            return token;
        }
        private string DSlogin()
        {
            _project.SendInfoToLog("DLogin");
            _project.Deadline();

            _instance.CloseExtraTabs();
            _instance.HeSet(("input:text", "aria-label", "Email or Phone Number", "text", 0), _project.Variables["discordLOGIN"].Value);
            _instance.HeSet(("input:password", "aria-label", "Password", "text", 0), _project.Variables["discordPASSWORD"].Value);
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
            _instance.HeSet(("input:text", "autocomplete", "one-time-code", "regexp", 0), OTP.Offline(_project.Variables["discord2FACODE"].Value));
            _instance.HeClick(("button", "type", "submit", "regexp", 0));
            Thread.Sleep(3000);
            return "ok";
        }
        public string DSload(bool log = false)
        {

            CredsFromDb();

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

            Log(state);


            if (state == "login" && !tokenUsed)
            {
                DSsetToken();
                tokenUsed = true;
                //Thread.Sleep(5000);					
                goto start;
            }

            else if (state == "login" && tokenUsed)
            {
                var login = DSlogin();
                if (login == "ok")
                {
                    Thread.Sleep(5000);
                    goto start;
                }
                else if (login == "capcha")
                    Log("!W capcha");
                _project.CapGuru();
                _instance.UseFullMouseEmulation = emu;
                state = "capcha";
            }

            else if (state == "logged")
            {
                _instance.HeClick(("button", "innertext", "Apply", "regexp", 0), thr0w: false);
                state = _instance.ActiveTab.FindElementByAttribute("div", "class", "avatarWrapper__", "regexp", 0).FirstChild.GetAttribute("aria-label");

                Log(state);
                var token = DSgetToken();
                if (string.IsNullOrEmpty(token))
                _project.DbUpd($"token = '{token}', status = 'ok'", "_discord");
                _project.Var("discordSTATUS", "ok");
                _instance.UseFullMouseEmulation = emu;
            }
            return state;

        }
        public string DSservers()
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
            return result;
        }
    }


}
