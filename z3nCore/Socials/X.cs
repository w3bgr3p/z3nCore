using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using z3nCore.Utilities;


namespace z3nCore
{
    public class X
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _log;
        private readonly Sleeper _idle;
        private string _status;
        private string _token;
        private string _login;
        private string _pass;
        private string _2fa;
        private string _email;
        private string _email_pass;


        public X(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {

            _project = project;
            _instance = instance;
            _log = new Logger(project, log: log, classEmoji: "X");
            _idle = new Sleeper(1337, 2078);
            LoadCreds();

        }

        private void LoadCreds()
        {
            var creds = _project.DbGetColumns(" status, token, login, password, otpsecret, email, emailpass",
                "_twitter");
            _status = creds["status"];
            _token = creds["token"];
            _login = creds["login"];
            _pass = creds["password"];
            _2fa = creds["otpsecret"];
            _email = creds["email"];
            _email_pass = creds["emailpass"];


            if (string.IsNullOrEmpty(_login) || string.IsNullOrEmpty(_pass))
                throw new Exception($"invalid credentials login:[{_login}] pass:[{_pass}]");
        }

        #region API

        public Dictionary<string, string> UserByScreenName_()
        {
            _log.Send("UserByScreenName_ method started");
            bool secondTry = false;
            p0:
            _log.Send($"Creating Traffic object, secondTry: {secondTry}");
            var traffic = new Traffic(_project, _instance).Snapshot();
            _log.Send("Traffic snapshot created successfully");
            var all = traffic.GetAll("UserByScreenName", strict: false);
            _log.Send($"Traffic elements count: {all.Count}");
            
            if (all.Count == 0 && !secondTry)
            {
                _instance.F5();
                Thread.Sleep(5000);
                secondTry = true;
                goto p0;
            }

            if (all.Count == 0) throw new Exception(" UserByScreenName_ not Found in traffic ");


            var url = "";
            foreach (var tEl in all)
            {
                _log.Send(tEl.Url);
                url = tEl.Url;
                var body = tEl.ResponseBody;

                if (body.Contains("user"))
                    _project.ToJson(body);
            }
            _log.Send($"URL for regex: {url}");
            string user = url.Regx("(?<=screen_name%22%3A%22).*?(?=%)");
            dynamic data = _project.Json.data.user.result;
            var result = new Dictionary<string, string>();


            var __typename = data.__typename;

            if (__typename == "UserUnavailable")
            {
                var message = data.message;
                _log.Warn(message);
                result = new Dictionary<string, string>
                {
                    { "status", message },
                    { "user", user },
                };
            }
            else
            {
                var image_url = data.avatar.image_url;
                var name = data.core.name;
                var screen_name = data.core.screen_name;
                //var lang = data.profile_description_language;


                dynamic legacy = data.legacy;
                var fast_followers_count = legacy.fast_followers_count;
                var favourites_count = legacy.favourites_count;
                var followers_count = legacy.followers_count;
                var friends_count = legacy.friends_count;
                var statuses_count = legacy.statuses_count;
                var description = legacy.description;
                var default_profile_image = legacy.default_profile_image;
                var default_profile = legacy.default_profile;

                result = new Dictionary<string, string>
                {
                    { "status", "ok" },
                    { "user", user },
                    { "image_url", image_url?.ToString() ?? "" },
                    { "name", name?.ToString() ?? "" },
                    { "screen_name", screen_name?.ToString() ?? "" },
                    //{ "lang", lang?.ToString() ?? "" },
                    { "fast_followers_count", fast_followers_count?.ToString() ?? "" },
                    { "favourites_count", favourites_count?.ToString() ?? "" },
                    { "followers_count", followers_count?.ToString() ?? "" },
                    { "friends_count", friends_count?.ToString() ?? "" },
                    { "statuses_count", statuses_count?.ToString() ?? "" },
                    { "description", description?.ToString() ?? "" },
                    { "default_profile_image", default_profile_image?.ToString() ?? "" },
                    { "default_profile", default_profile?.ToString() ?? "" }
                };
            }

            return result;
        }
        public Dictionary<string, string> UserByScreenName()
        {
            _log.Send("UserByScreenName_ method started");
            bool secondTry = false;
            p0:
            _log.Send($"Creating Traffic object, secondTry: {secondTry}");
            var traffic = new Traffic(_project, _instance).Snapshot();
            _log.Send("Traffic snapshot created successfully");
            var all = traffic.GetAll("UserByScreenName", strict: false);
            _log.Send($"Traffic elements count: {all.Count}");
            
            if (all.Count == 0 && !secondTry)
            {
                _instance.F5();
                Thread.Sleep(5000);
                secondTry = true;
                goto p0;
            }

            if (all.Count == 0) throw new Exception(" UserByScreenName_ not Found in traffic ");

            var result = new Dictionary<string, string>();
            var url = "";
            foreach (var tEl in all)
            {
                _log.Send(tEl.Url);
                url = tEl.Url;
                var body = tEl.ResponseBody;
                string user = url.Regx("(?<=screen_name%22%3A%22).*?(?=%)");
                if (body.Contains("user"))
                {
                    result = body.JsonToDic();
                    result.Add("user", user);
                    break;
                }
            }

            return result;
            
        }

        public Dictionary<string, string> Settings()
        {
            bool secondTry = false;
            p0:
            _log.Send($"Creating Traffic object, secondTry: {secondTry}");
            var traffic = new Traffic(_project, _instance).Snapshot();
            _log.Send("Traffic snapshot created successfully");
            var all = traffic.GetAll("account/settings.json?", strict: false);
            _log.Send($"Traffic elements count: {all.Count}");
            
            if (all.Count == 0 && !secondTry)
            {
                _instance.F5();
                Thread.Sleep(5000);
                secondTry = true;
                goto p0;
            }

            if (all.Count == 0) throw new Exception(" settings not Found in traffic ");

            var result = new Dictionary<string, string>();
            var url = "";
            foreach (var tEl in all)
            {
                _log.Send(tEl.Url);
                url = tEl.Url;
                
                var body = tEl.ResponseBody;
                if (body.Contains("screen_name"))
                {
                    result = body.JsonToDic();
                    
                    break;
                }
            }

            return result;
        }
        public string UserNameFromSetting(bool validate = false)
        {
            var screen_name = Settings()["screen_name"];
            
            if (validate)
            {
                if (_login != screen_name)
                {
                    _project.Var("status", "WrongAccount");
                    _log.Warn("Wrong Account Detected", thrw:true);
                }
            }
            return screen_name;
        }

        public string GenerateJson(string purpouse = "tweet")
        {
            
            var randomNew = Rnd.RndFile(Path.Combine(_project.Path,".data", "news"), "json");
            _project.ToJson(File.ReadAllText(randomNew));
            var article = _project.Json.FullText;
            var ai = new Api.AI(_project,"aiio","meta-llama/Llama-3.2-90B-Vision-Instruct", true);
            var bio = _project.DbGet("bio", "_profile");
            var system = "";
            if (purpouse == "tweet")
                system =
                    $"You are an individual assistant with your own background. Your way of thinking and responding: {bio}. Your task is to summarize the provided article and express your personal opinion on its subject in a single statement, no longer than 280 characters. The statement must be a self-contained insight that summarizes the article’s key points, followed by a clear attribution of your bio-informed opinion on the subject using phrases like “I think,” “As for me,” or “I suppose.” The opinion must be explicitly yours, not a detached thesis. Your response must be a clean JSON object with a single key 'statement' containing one string, with no nesting, no additional comments, no extra characters, and no text outside the JSON structure.";
                    //$"You are an individual assistant with your own background. Your way of thinking and responding: {bio}. Your task is to summarize the provided article and express your personal opinion on its subject in a single, concise statement, no longer than 280 characters. The statement must be a self-contained insight that integrates the article’s key points with your personal bio-informed opinion on the subject, clearly addressing the topic. Your response must be a clean JSON array with a single key 'statement' containing one string, with no nesting, no additional comments, no extra characters, and no text outside the JSON structure.";
                    //$"You are an individual assistant with your own background. Your way of thinking and responding: {bio}. Your task is to summarize the provided article and express your personal opinion on its subject in a single, concise statement, no longer than 280 characters. The statement must be a self-contained insight that integrates the article’s key points with your opinion, reflecting your bio. Your response must be a clean JSON array with a single key 'statement' containing one string, with no nesting, no additional comments, no extra characters, and no text outside the JSON structure.";
            else if (purpouse == "thread") 
                system = $"You are an individual assistant with your own background. Your way of thinking and responding: {bio}. Your task is to summarize the provided article in a series of short, concise statements, each no longer than 280 characters. Start with an engaging statement to capture attention and lead into the summary. End the summary with a concluding statement that wraps up the key takeaway. Then, provide your personal opinion as a few short theses, each no longer than 280 characters. Your response must be a clean JSON object with two keys: 'summary_statements' as an array of strings for the summary parts (starting with the lead-in and ending with the conclusion), and 'opinion_theses' as an array of strings for the theses. Include no nesting beyond these arrays, no additional comments, no extra characters, and no text outside the JSON structure.";
            else if (purpouse == "opinionThread") system = 
                $"You are an individual assistant with your own background. Your way of thinking and responding: {bio}. Your task is to summarize the provided article in one concise statement, no longer than 280 characters, capturing its key points. Then, provide your personal opinion on the subject in a second statement, no longer than 280 characters, using phrases like “I think,” “As for me,” or “I suppose” to clearly attribute it as your bio-informed perspective, not a detached thesis. Your response must be a clean JSON object with two keys: 'summary_statement' containing the summary string, and 'opinion_statement' containing the opinion string, with no nesting, no additional comments, no extra characters, and no text outside the JSON structure.";
            else
                _log.Warn($"UNKNOWN SYSTEM ROLE: {system}");
            var user = article;
            string t = ai.Query(system, article).Replace("```json","").Replace("```","");
            _project.ToJson(t);
            return t;
        }

        
        #endregion

        #region UI Auth & essentials

        public void SkipDefaultButtons()
        {
            _instance.HeClick(("button", "innertext", "Accept\\ all\\ cookies", "regexp", 0), deadline: 0, thr0w: false);
            _instance.HeClick(("button", "data-testid", "xMigrationBottomBar", "regexp", 0), deadline: 0, thr0w: false);
            _instance.HeClick(("button", "innertext", "Got\\ it", "regexp", 0), deadline: 0, thr0w: false);
           
        }
        
        public bool CheckCurrent()
        {
            GoToProfile();
            var current = UserByScreenName_();
            if (current["user"].ToLower() != _login.ToLower())
            {
                _log.Warn($"wrong profile: expected {_login} detected {current["user"]}", show: true);
                _instance.ClearShit("x.com");
                _instance.ClearShit("twitter.com");
                return false;
            }

            return true;
        }
        public void GoToProfile(string profile = null)
        {
            if (string.IsNullOrEmpty(profile))
            {
                if(!_instance.ActiveTab.URL.Contains(_login))
                    _instance.HeClick(("*", "data-testid", "AppTabBar_Profile_Link", "regexp",0));
            }
            else
            {
                if(!_instance.ActiveTab.URL.Contains(profile))
                    _instance.ActiveTab.Navigate($"https://x.com/{profile}", "");
            }
        }
        private string LoginState(bool log = false)
        {
            log = _project.Variables["debug"].Value == "True";
            DateTime start = DateTime.Now;
            DateTime deadline = DateTime.Now.AddSeconds(60);
           	_log.Send($"https://x.com/{_login}");
            _instance.ActiveTab.Navigate($"https://x.com/{_login}", "");
            var status = "";

            while (string.IsNullOrEmpty(status))
            {
                Thread.Sleep(5000);
                _project.log($"{DateTime.Now - start}s check... URLNow:[{_instance.ActiveTab.URL}]");
                if (DateTime.Now > deadline) throw new Exception("timeout");

                else if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext",
                             @"Caution:\s+This\s+account\s+is\s+temporarily\s+restricted", "regexp", 0).IsVoid)
                    status = "restricted";
                else if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext",
                             @"Account\s+suspended\s+X\s+suspends\s+accounts\s+which\s+violate\s+the\s+X\s+Rules",
                             "regexp", 0).IsVoid)
                    status = "suspended";
                else if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext", @"Log\ in", "regexp", 0)
                             .IsVoid || !_instance.ActiveTab
                             .FindElementByAttribute("a", "data-testid", "loginButton", "regexp", 0).IsVoid)
                    status = "login";

                else if (!_instance.ActiveTab
                             .FindElementByAttribute("*", "innertext", "erify\\ your\\ email\\ address", "regexp", 0)
                             .IsVoid ||
                         !_instance.ActiveTab.FindElementByAttribute("div", "innertext",
                             "We\\ sent\\ your\\ verification\\ code.", "regexp", 0).IsVoid)
                    status = "emailCapcha";
                else if (!_instance.ActiveTab
                             .FindElementByAttribute("button", "data-testid", "SideNav_AccountSwitcher_Button",
                                 "regexp", 0).IsVoid)
                {
                    var check = _instance.ActiveTab
                        .FindElementByAttribute("button", "data-testid", "SideNav_AccountSwitcher_Button", "regexp", 0)
                        .FirstChild.FirstChild.GetAttribute("data-testid");
                    if (check == $"UserAvatar-Container-{_login}") status = "ok";
                    else
                    {
                        status = "mixed";
                        _project.log(
                            $"!W {status}. Detected  [{check}] instead [UserAvatar-Container-{_login}] {DateTime.Now - start}");
                    }
                }
                else if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext",
                             "Something\\ went\\ wrong.\\ Try\\ reloading.", "regexp", 0).IsVoid)
                {
                    _instance.ActiveTab.MainDocument.EvaluateScript("location.reload(true)");
                    Thread.Sleep(3000);
                    continue;
                }
            }

            _project.log($"{status} {DateTime.Now - start}");
            return status;
        }
        public void TokenSet()
        {
            var token = _token;
            string jsCode =
                _project.ExecuteMacro(
                    $"document.cookie = \"auth_token={token}; domain=.x.com; path=/; expires=${DateTimeOffset.UtcNow.AddYears(1).ToString("R")}; Secure\";\r\nwindow.location.replace(\"https://x.com\")");
            _instance.ActiveTab.MainDocument.EvaluateScript(jsCode);
        }
        public string TokenGet_()
        {
            _project.ZB("GetCookies");
            var cookJson = _project.Var("cookies");//new Cookies(project, instance).GetByJs("x.com");
            
            JArray toParse = JArray.Parse(cookJson);
            int i = 0;
            var token = "";
            while (token == "")
            {
                if (toParse[i]["name"].ToString() == "auth_token") token = toParse[i]["value"].ToString();
                i++;
            }
            var _token = token;
            _project.DbUpd($"token = '{token}'", "_twitter");
            return token;
        }
        
        public string TokenGet()
        {
            //var cookJson = _instance.GetCookies(_project,".");
            
            //var cookJson = new Cookies(_project, _instance).Get("."); //_instance.GetCookies(_project, ".");
            var cookJson = new Cookies(_project, _instance).GetByJs("x.com");
            JArray toParse = JArray.Parse(cookJson);
            int i = 0;
            var token = "";
            while (token == "")
            {
                if (toParse[i]["name"].ToString() == "auth_token") token = toParse[i]["value"].ToString();
                i++;
            }

            _token = token;
            _project.DbUpd($"token = '{token}'", "_twitter");
            return token;
        }
        public string Login()
        {
            var err = "";
            if (_instance.ActiveTab.FindElementByAttribute("input:text", "autocomplete", "username", "text", 0).IsVoid)
            {
                DateTime deadline = DateTime.Now.AddSeconds(60);

                _instance.ActiveTab.Navigate("https://x.com/", "");
                _idle.Sleep();
                _instance.HeClick(("button", "innertext", "Accept\\ all\\ cookies", "regexp", 0), deadline: 1,
                    thr0w: false);
                _instance.HeClick(("button", "data-testid", "xMigrationBottomBar", "regexp", 0), deadline: 0, thr0w: false);
                _instance.HeClick(("a", "data-testid", "login", "regexp", 0));
            }

           
            _instance.JsSet_("[autocomplete='username']", _login);
            _instance.WaitFieldEmulationDelay();
            _instance.SendText("{ENTER}", 15);
            _idle.Sleep();
            
            var toast = CatchToast();
            if (toast.Contains("Could not log you in now."))
                return toast;

            err = CatchErr();
            if (err != "") return err;
            
            
            _instance.JsSet_("[name='password']", _pass);
            _idle.Sleep();
            _instance.SendText("{ENTER}", 15);
            _idle.Sleep();
            
            err = CatchErr();
            if (err != "") return err;
            
            var codeOTP = OTP.Offline(_2fa);
            _instance.JsSet_("[name='text']", codeOTP);
            _idle.Sleep();
            _instance.SendText("{ENTER}", 15);
            _idle.Sleep();
            
            err = CatchErr();
            if (err != "") return err;

            _instance.HeClick(("button", "innertext", "Accept\\ all\\ cookies", "regexp", 0), deadline: 1,
                thr0w: false);
            _instance.HeClick(("button", "data-testid", "xMigrationBottomBar", "regexp", 0), deadline: 0, thr0w: false);
            TokenGet();
            return "ok";
        }
        private string CatchErr()
        {
            
            if (!_instance.ActiveTab
                    .FindElementByXPath("//*[contains(text(), 'Sorry, we could not find your account')]", 0)
                    .IsVoid) return "NotFound";
            if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Wrong password!')]", 0).IsVoid)
                return "WrongPass";
            
            if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Your account is suspended')]", 0).IsVoid)
                return "Suspended";
            
            if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext",
                        "Oops,\\ something\\ went\\ wrong.\\ Please\\ try\\ again\\ later.", "regexp", 0)
                    .IsVoid) return "SomethingWentWrong";
            
            if (!_instance.ActiveTab
                    .FindElementByAttribute("*", "innertext", "Suspicious\\ login\\ prevented", "regexp", 0)
                    .IsVoid) return "SuspiciousLogin";
            return "";
        }

        private string CatchToast()
        {
            var err = "";
            try{
                err = _instance.HeGet(("div", "data-testid", "toast", "regexp", 0));
            }
            catch{}
            if (err != "")
            {
                _project.warn(err);
                if (err.Contains("Could not log you in now."))
                    return err;
            }
            return err;
        }
        public string Load(bool log = false)
        {
            bool tokenUsed = false;
            DateTime deadline = DateTime.Now.AddSeconds(60);
            check:

            if (DateTime.Now > deadline) throw new Exception("timeout");

            var status = LoginState(log: true);
            try
            {
                _project.Var("status", status);
            }
            catch
            {
            }

            if (status == "login" && !tokenUsed)
            {
                if (!string.IsNullOrEmpty(_token))
                {
                    TokenSet();
                    tokenUsed = true;
                    Thread.Sleep(5000); // Увеличить задержку
                }
                else
                {
                    // Если токена нет, сразу идти на логин
                    tokenUsed = true;
                    status = Login();
                }
            }
            else if (status == "login" && tokenUsed)
            {
                status = Login();
                _project.log($"{status}");
                Thread.Sleep(3000);
            }
            else if (status == "mixed")
            {
                _instance.CloseAllTabs();
                _instance.ClearCookie("x.com");
                _instance.ClearCache("x.com");
                _instance.ClearCookie("twitter.com");
                _instance.ClearCache("twitter.com");
                goto check;
            }
            
            _project.DbUpd($"status = '{status}'", "_twitter");
            if (status == "restricted" || status == "suspended" || status == "emailCapcha" || status == "proxyTrouble" || status == "WrongPass" || status == "Suspended"|| status == "NotFound" || status.Contains("Could not log you in now."))
            {
                return status;
            }
            else if (status == "ok")
            {
                _instance.HeClick(("button", "innertext", "Accept\\ all\\ cookies", "regexp", 0), deadline: 0,
                    thr0w: false);
                _instance.HeClick(("button", "data-testid", "xMigrationBottomBar", "regexp", 0), deadline: 0,
                    thr0w: false);

                TokenGet();
                return status;
            }
            else
                _log.Send($"unknown {status}");

            goto check;
        }
        public void Auth()
        {
            _project.Deadline();
            _instance.HeClick(("button", "innertext", "Accept\\ all\\ cookies", "regexp", 0), deadline: 0,
                thr0w: false);
            _instance.HeClick(("button", "data-testid", "xMigrationBottomBar", "regexp", 0), deadline: 0, thr0w: false);

            check:
            _project.Deadline(60);
            string state = AuthState();

            _project.log(state);

            switch (state)
            {
                case "NotFound":
                case "Suspended":
                case "SuspiciousLogin":
                case "WrongPass":
                    _project.DbUpd($"status = '{state}'", "_twitter");
                    throw new Exception($"{state}");
                case "ClickLogin":
                    _instance.HeClick(("a", "data-testid", "login", "regexp", 0));
                    goto check;
                case "InputLogin":
                    _instance.HeSet(("input:text", "autocomplete", "username", "text", 0), _login, deadline: 30);
                    _instance.HeClick(("span", "innertext", "Next", "regexp", 1), "clickOut");
                    goto check;
                case "InputPass":
                    _instance.HeSet(("input:password", "autocomplete", "current-password", "text", 0), _pass);
                    _instance.HeClick(("button", "data-testid", "LoginForm_Login_Button", "regexp", 0), "clickOut");
                    goto check;
                case "InputOTP":
                    _instance.HeSet(("input:text", "data-testid", "ocfEnterTextTextInput", "text", 0),
                        OTP.Offline(_2fa));
                    _instance.HeClick(("span", "innertext", "Next", "regexp", 1), "clickOut");
                    goto check;
                case "CheckUser":
                    string userdata = _instance.HeGet(("li", "data-testid", "UserCell", "regexp", 0));
                    if (userdata.Contains(_login))
                    {
                        _instance.HeClick(("button", "data-testid", "OAuth_Consent_Button", "regexp", 0));
                        goto check;
                    }
                    else
                    {
                        throw new Exception("wrong account");
                    }
                case "AuthV1SignIn":
                    _instance.HeClick(("allow", "id"));
                    goto check;
                case "InvalidRequestToken":
                    _instance.CloseExtraTabs();
                    throw new Exception(state);
                case "AuthV1Confirm":
                    _instance.HeClick(("allow", "id"));
                    goto check;
                default:
                    _log.Send($"unknown state [{state}]");
                    break;

            }

            if (!_instance.ActiveTab.URL.Contains("x.com") && !_instance.ActiveTab.URL.Contains("twitter.com"))
                _project.log("auth done");
            else goto check;
        }
        public string AuthState()
        {
            string state = "undefined";

            if (_instance.ActiveTab.URL.Contains("oauth/authorize"))
            {
                if (!_instance.ActiveTab
                        .FindElementByXPath("//*[contains(text(), 'The request token for this page is invalid')]", 0)
                        .IsVoid)
                    return "InvalidRequestToken";
                if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'This account is suspended')]", 0)
                        .IsVoid)
                    return "Suspended";
                if (!_instance.ActiveTab.FindElementById("session").IsVoid)
                {
                    var currentAcc = _instance.HeGet(("session", "id"));
                    if (currentAcc.ToLower() == _login.ToLower())
                        state = "AuthV1Confirm";
                    else
                        state = "!WrongAccount";
                }

                else if (!_instance.ActiveTab.FindElementById("allow").IsVoid)
                {
                    if (_instance.HeGet(("allow", "id"), atr: "value") == "Sign In")
                        state = "AuthV1SignIn";
                }

                return state;
            }

            if (!_instance.ActiveTab
                    .FindElementByXPath("//*[contains(text(), 'Sorry, we could not find your account')]", 0)
                    .IsVoid) state = "NotFound";
            else if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Your account is suspended')]", 0)
                         .IsVoid) state = "Suspended";
            else if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Wrong password!')]", 0).IsVoid)
                state = "WrongPass";
            else if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext",
                             "Oops,\\ something\\ went\\ wrong.\\ Please\\ try\\ again\\ later.", "regexp", 0)
                         .IsVoid) state = "SomethingWentWrong";
            else if (!_instance.ActiveTab
                         .FindElementByAttribute("*", "innertext", "Suspicious\\ login\\ prevented", "regexp", 0)
                         .IsVoid) state = "SuspiciousLogin";



            else if (!_instance.ActiveTab.FindElementByAttribute("input:text", "autocomplete", "username", "text", 0)
                         .IsVoid) state = "InputLogin";
            else if (!_instance.ActiveTab
                         .FindElementByAttribute("input:password", "autocomplete", "current-password", "text", 0)
                         .IsVoid) state = "InputPass";
            else if (!_instance.ActiveTab
                         .FindElementByAttribute("input:text", "data-testid", "ocfEnterTextTextInput", "text", 0)
                         .IsVoid) state = "InputOTP";


            else if (!_instance.ActiveTab.FindElementByAttribute("a", "data-testid", "login", "regexp", 0).IsVoid)
                state = "ClickLogin";
            else if (!_instance.ActiveTab.FindElementByAttribute("li", "data-testid", "UserCell", "regexp", 0).IsVoid)
                state = "CheckUser";
            return state;
        }
        #endregion
        
        #region UI Actions
        public void SendSingleTweet(string tweet, string accountToMention = null)
        {
            GoToProfile(accountToMention);
            _instance.HeClick(("a", "data-testid", "SideNav_NewTweet_Button", "regexp", 0));
            _instance.HeClick(("div", "class", "notranslate\\ public-DraftEditor-content", "regexp", 0),delay:2);
            _instance.CtrlV(tweet);
    
            _instance.HeClick(("button", "data-testid", "tweetButton", "regexp", 0),delay:2);
            try
            {
                var toast = _instance.HeGet(("*", "data-testid", "toast", "regexp", 0));
                _project.log(toast);
            }
            catch (Exception ex)
            {
                _log.Warn(ex.Message,thrw:true);
            }
        }

        public void SendThread(List<string> tweets, string accountToMention = null)
        {
            GoToProfile(accountToMention);
            var title = tweets[0];
            tweets.RemoveAt(0);

            if (tweets.Count == 0)
            {
                SendSingleTweet(title, accountToMention);
                return;
            }
            _instance.JsClick_("[data-testid='SideNav_NewTweet_Button']");
            _instance.JsSet_("[data-testid='tweetTextarea_0']", title);
            _idle.Sleep();

            int tIndex = 1;
            foreach (var add in tweets)
            {
                _instance.JsClick_("[data-testid='addButton']");
                _idle.Sleep();
                _instance.JsSet_($"[data-testid='tweetTextarea_{tIndex}']", add);
                _idle.Sleep();
                tIndex++;
            }
            _instance.JsClick_("[data-testid='tweetButton']");
            try
            {
                var toast = _instance.HeGet(("*", "data-testid", "toast", "regexp", 0));
                _project.log(toast);
            }
            catch (Exception ex)
            {
                _log.Warn(ex.Message,thrw:true);
            }
        }

        public void Follow()
        {
            try
            {
                _instance.HeGet(("button", "data-testid", "-unfollow", "regexp", 0), deadline:1);
                return;
            }
            catch
            {
            }
            
            var FlwButton = _instance.HeGet(("button", "data-testid", "-follow", "regexp", 0));

            if 	(FlwButton.Contains("Follow"))
            {
                try
                {
                    _instance.HeClick(("button", "data-testid", "-follow", "regexp", 0));

                }
                catch (Exception ex)
                {
                    _log.Warn(ex.Message,thrw:true);
                }
            }

        }

        public void RandomLike(string targetAccount = null)
        {
            _project.Deadline();
            var tId = "like";
            if (targetAccount != null)
                GoToProfile(targetAccount);

            while (true)
            {
                _project.Deadline(30);
                Thread.Sleep(1000);
                var wall = _instance.ActiveTab.FindElementsByAttribute("div", "data-testid", "cellInnerDiv", "regexp").ToList();
    
                var allElements = new List<(HtmlElement Element, HtmlElement Parent, string TestId)>();
                foreach (HtmlElement tweet in wall)
                {
                    var tweetData = tweet.GetChildren(true);
                    foreach (HtmlElement he in tweetData)
                    {
                        var testId = he.GetAttribute("data-testid");
                        if (testId != "")
                        {
                            allElements.Add((he, tweet, testId));
                        }
                    }
                }

                string condition = Regex.Replace(_instance.ActiveTab.URL, "https://x.com/", "");
                var likeElements = allElements
                    .Where(x => x.TestId == tId)
                    .Where(x => allElements.Any(y => y.Parent == x.Parent && y.TestId == "User-Name" && y.Element.InnerText.Contains(condition)))
                    .Select(x => x.Element)
                    .ToList();

                if (likeElements.Count > 0)
                {
                    Random rand = new Random();
                    HtmlElement randomLike = likeElements[rand.Next(likeElements.Count)];
                    _instance.HeClick(randomLike, emu:1);
                    break;
                }
                else
                {
                    _log.Send($"No posts from [{condition}]");
                    _instance.ScrollDown();
                    continue;
                }
            }
        }
        public void RandomRetweet(string targetAccount = null)
        {
            _project.Deadline();
            if (targetAccount != null)
                GoToProfile(targetAccount);

            while (true)
            {
                _project.Deadline(30);
	            Thread.Sleep(2000);
	            var wall = _instance.ActiveTab.FindElementsByAttribute("div", "data-testid", "cellInnerDiv", "regexp").ToList();
	            _log.Send($"Total: {wall.Count}");
	            
	            var allElements = new List<(HtmlElement Element, HtmlElement Parent, string TestId)>();
	            int i = 0;
	            foreach (HtmlElement tweet in wall)
	            {
	                var tweetData = tweet.GetChildren(true);
	                foreach (HtmlElement he in tweetData)
	                {
	                    var testId = he.GetAttribute("data-testid");
	                    if (testId != "")
	                    {
	                        allElements.Add((he, tweet, testId));
	                    }
	                }
	                i++;
	            }
	            
	            string condition = Regex.Replace(_instance.ActiveTab.URL, "https://x.com/", "");
	            var element = "retweet";
	            var likeElements = allElements
	                .Where(x => x.TestId == element)
	                .Where(x => allElements.Any(y => y.Parent == x.Parent && y.TestId == "User-Name" && y.Element.InnerText.Contains(condition)))
	                .Select(x => x.Element)
	                .ToList();
	            
                _log.Send($"{likeElements.Count} elements from [{condition}] with '{element}' button");
	            
	            if (likeElements.Count > 0)
	            {
	                Random rand = new Random();
	                HtmlElement randomLike = likeElements[rand.Next(likeElements.Count)];
                    _log.Send($"rnd '{element}': {randomLike.OuterHtml}");
                    _instance.HeClick(randomLike, emu:1);
		            Thread.Sleep(1000);
		            HtmlElement Dropdown = _instance.ActiveTab.FindElementByAttribute("div", "data-testid", "Dropdown", "regexp", 0);
		            if (!Dropdown.FindChildByAttribute("div", "data-testid", "unretweetConfirm", "text", 0).IsVoid)
		            {
                        _instance.ScrollDown();
			            continue;
		            }
                    else
                    {
                        var confirm = Dropdown.FindChildByAttribute("div", "data-testid", "retweetConfirm", "text", 0);
                        _instance.HeClick(confirm, emu:1);
                        break;
                        
                    }
                    
	            }
	            else
	            {
                    _log.Send($"no posts from [{condition}]");
                    _instance.ScrollDown();
		            continue;
	            }
            }


        }

        #endregion



        #region Old & obsolete
        
        public void UpdXCreds(Dictionary<string, string> data)
        {
            if (data.ContainsKey("CODE2FA"))
            {
                _project.log($"CODE2FA raw value: {data["CODE2FA"]}"); // Логируем исходное значение
            }

            var fields = new Dictionary<string, string>
            {
                { "LOGIN", data.ContainsKey("LOGIN") ? data["LOGIN"].Replace("'", "''") : "" },
                { "PASSWORD", data.ContainsKey("PASSWORD") ? data["PASSWORD"].Replace("'", "''") : "" },
                { "EMAIL", data.ContainsKey("EMAIL") ? data["EMAIL"].Replace("'", "''") : "" },
                {
                    "EMAIL_PASSWORD",
                    data.ContainsKey("EMAIL_PASSWORD") ? data["EMAIL_PASSWORD"].Replace("'", "''") : ""
                },
                {
                    "TOKEN",
                    data.ContainsKey("TOKEN")
                        ? (data["TOKEN"].Contains('=')
                            ? data["TOKEN"].Split('=').Last().Replace("'", "''")
                            : data["TOKEN"].Replace("'", "''"))
                        : ""
                },
                {
                    "CODE2FA",
                    data.ContainsKey("CODE2FA")
                        ? (data["CODE2FA"].Contains('/')
                            ? data["CODE2FA"].Split('/').Last().Replace("'", "''")
                            : data["CODE2FA"].Replace("'", "''"))
                        : ""
                },
                { "RECOVERY_SEED", data.ContainsKey("RECOVERY_SEED") ? data["RECOVERY_SEED"].Replace("'", "''") : "" }
            };

            //var _sql = new Sql(_project, _logShow);
            try
            {
                _project.DbUpd($@"token = '{fields["TOKEN"]}', 
                login = '{fields["LOGIN"]}', 
                password = '{fields["PASSWORD"]}', 
                otpsecret = '{fields["CODE2FA"]}', 
                email = '{fields["EMAIL"]}', 
                emailpass = '{fields["EMAIL_PASSWORD"]}', 
                otpbackup = '{fields["RECOVERY_SEED"]}'", "_twitter");
            }
            catch (Exception ex)
            {
                _project.log($"!W{ex.Message}");
            }

            LoadCreds();
        }

        public void ParseProfile()
        {
            _instance.HeClick(("*", "data-testid", "AppTabBar_Profile_Link", "regexp", 0));
            string json = _instance.HeGet(("*", "data-testid", "UserProfileSchema-test", "regexp", 0));

            var jo = JObject.Parse(json);
            var main = jo["mainEntity"] as JObject;

            string dateCreated = jo["dateCreated"]?.ToString() ?? "";
            string identifier = main?["identifier"]?.ToString() ?? "";
            string username = main?["additionalName"]?.ToString() ?? "";
            string description = main?["description"]?.ToString() ?? "";
            string givenName = main?["givenName"]?.ToString() ?? "";
            string homeLocation = main?["homeLocation"]?["name"]?.ToString() ?? "";
            string ava = main?["image"]?["contentUrl"]?.ToString() ?? "";
            string banner = main?["image"]?["thumbnailUrl"]?.ToString() ?? "";

            var interactionStatistic = main?["interactionStatistic"] as JArray;
            string followers = interactionStatistic?[0]?["userInteractionCount"]?.ToString() ?? "";
            string following = interactionStatistic?[1]?["userInteractionCount"]?.ToString() ?? "";
            string tweets = interactionStatistic?[2]?["userInteractionCount"]?.ToString() ?? "";

            _project.DbUpd($@"datecreated = '{dateCreated}',
                identifier = '{identifier}',
                username = '{username}',
                description = '{description}',
                givenname = '{givenName}',
                homelocation = '{homeLocation}',
                ava = '{ava}',
                banner = '{banner}',
                followers = '{followers}',
                following = '{following}',
                tweets = '{tweets}'
                ", "__twitter");
            try
            {
                var toFill = _project.Lists["editProfile"];
                toFill.Clear();

                if (description == "") toFill.Add("description");
                if (homeLocation == "") toFill.Add("homeLocation");
                if (ava == "https://abs.twimg.com/sticky/default_profile_images/default_profile_400x400.png")
                    toFill.Add("ava");
                if (banner == "https://abs.twimg.com/sticky/default_profile_images/default_profile_normal.png")
                    toFill.Add("banner");

            }
            catch
            {
            }

        }

        public void ParseSecurity()
        {

            _instance.ActiveTab.Navigate("https://x.com/settings/your_twitter_data/account", "");

            scan:
            try
            {
                _instance.HeSet(("current_password", "name"), _pass, deadline: 1);
                _instance.HeClick(("button", "innertext", "Confirm", "regexp", 0));
            }
            catch
            {
            }

            var tIdList = _instance.ActiveTab.FindElementsByAttribute("*", "data-testid", ".", "regexp").ToList();

            if (tIdList.Count < 50)
            {
                Thread.Sleep(3000);
                goto scan;
            }

            string email = null;
            string phone = null;
            string creation = null;
            string country = null;
            string lang = null;
            string gender = null;
            string birth = null;


            foreach (HtmlElement he in tIdList)
            {
                string pName = null;
                string pValue = null;
                string testid = he.GetAttribute("data-testid");
                string href = he.GetAttribute("href");
                string text = he.InnerText;

                switch (testid)
                {
                    case "account-creation":
                        pName = text.Split('\n')[0];
                        pValue = text.Replace(pName, "").Replace("\n", " ").Trim();
                        creation = pValue;
                        continue;
                    case "pivot":
                        pName = text.Split('\n')[0];
                        pValue = text.Replace(pName, "").Replace("\n", " ").Trim();
                        switch (pName)
                        {
                            case "Phone":
                                phone = pValue;
                                break;
                            case "Email":
                                email = pValue;
                                break;
                            case "Country":
                                country = pValue;
                                break;
                            case "Languages":
                                lang = pValue;
                                break;
                            case "Gender":
                                gender = pValue;
                                break;
                            case "Birth date":
                                birth = pValue;
                                break;
                        }

                        continue;
                    default:
                        continue;
                }
            }

            _project.DbUpd($@"creation = '{creation}',
                        email = '{email}',
                        phone = '{phone}',
                        country = '{country}',
                        lang = '{lang}',
                        gender = '{gender}',
                        birth = '{birth}'
                        ", "__twitter");


            try
            {
                email = email.ToLower();
                var emails = _project.DbGet("gmail, icloud, firstmail", "_mail").ToLower();
                var address = _project.DbGet("evm_pk", "_addresses").ToLower();
                var toFill = _project.Lists["editSecurity"];
                toFill.Clear();

                if (!emails.Contains(email) || !email.Contains(address)) toFill.Add("email");

            }
            catch
            {
            }
        }

        public void Tweet()
        {
            if(!_instance.ActiveTab.URL.Contains(_login))
                _instance.HeClick(("*", "data-testid", "AppTabBar_Profile_Link", "regexp",0));

            try
            {
                int tryes = 5;
                gen:
                tryes--;
                if (tryes == 0) throw new Exception("generation problem");

                GenerateJson("tweet");
                string tweet = _project.Json.statement;

                if (tweet.Length > 280)
                {
                    _log.Warn($"Regenerating (tryes: {tryes}) (Exceed 280char) : {tweet}");
                    goto gen;
                }
                
		
                _instance.HeClick(("a", "data-testid", "SideNav_NewTweet_Button", "regexp", 0));
                _instance.HeClick(("div", "class", "notranslate\\ public-DraftEditor-content", "regexp", 0),delay:2);
                _instance.CtrlV(tweet);
                
                _instance.HeClick(("button", "data-testid", "tweetButton", "regexp", 0),delay:2);
                try
                {
                    var toast = _instance.HeGet(("*", "data-testid", "toast", "regexp", 0));
                    _project.log(toast);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex.Message,thrw:true);
                }
                
            }
            catch (Exception ex)
            {
                _log.Warn(ex.Message,thrw:true);
            }

        }
        #endregion
    }
}