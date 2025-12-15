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
    public class __X
    {
        #region Fields & Constructor

        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _log;
        private readonly Sleeper _idle;
        private string _status;
        private string _token;
        private string _ct0;
        private string _login;
        private string _pass;
        private string _2fa;
        private string _email;
        private string _email_pass;

        public __X(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _log = new Logger(project, log: log, classEmoji: "X");
            _idle = new Sleeper(1337, 2078);
            LoadCreds();
        }
        public __X(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project;
            _log = new Logger(project, log: log, classEmoji: "X");
            _idle = new Sleeper(1337, 2078);
            //LoadCreds();
        }
        private void LoadCreds()
        {
            var creds = _project.DbGetColumns("status, token, ct0, login, password, otpsecret, email, emailpass", "_twitter");
            _status = creds["status"];
            _token = creds["token"];
            _ct0 = creds.ContainsKey("ct0") ? creds["ct0"] : "";
            _login = creds["login"];
            _pass = creds["password"];
            _2fa = creds["otpsecret"];
            _email = creds["email"];
            _email_pass = creds["emailpass"];

            if (string.IsNullOrEmpty(_login) || string.IsNullOrEmpty(_pass))
                throw new Exception($"invalid credentials login:[{_login}] pass:[{_pass}]");
        }

        #endregion

        #region API Methods

        public string[] BuildHeaders_()
        {
            const string BEARER = "AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA";
        
            string[] headers = {
                $"authorization: Bearer {BEARER}",
                $"cookie: auth_token={_token}; ct0={_ct0}",
                $"x-csrf-token: {_ct0}",
                "content-type: application/json",
                $"user-agent: {_project.Profile.UserAgent}",
                "x-twitter-active-user: yes",
                "x-twitter-client-language: en"
            };
            return headers;
        }
        
        
        
        public string[] BuildHeaders()
        {
            const string BEARER = "AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA";
        
            string[] headers = {
                $"User-Agent: {_project.Profile.UserAgent}",
                "Accept-Language: en-US,en;q=0.7",
                "authorization: Bearer AAAAAAAAAAAAAAAAAAAAANRILgAAAAAAnNwIzUejRCOuH5E6I8xnZz4puTs%3D1Zv7ttfk8LF81IUq16cHjhLTvJu4FA33AGWWjCpTnA",
                "content-type: application/json",
                "sec-ch-ua: \"Chromium\";v=\"112\", \"Google Chrome\";v=\"112\", \";Not A Brand\";v=\"99\"",
                "sec-ch-ua-mobile: ?0",
                "sec-ch-ua-platform: \"Windows\"",
                $"x-csrf-token: {_ct0}",
                "x-twitter-active-user: yes",
                "x-twitter-auth-type: OAuth2Session",
                "x-twitter-client-language: en",
                $"Referer: https://twitter.com/{_login}",
                "Connection: keep-alive",
            };
            return headers;
        }


        public bool TokenValidate()
        {
            if (string.IsNullOrEmpty(_token))
                return false;

            _idle.Sleep();
    
            string[] headers = {
                $"cookie: auth_token={_token}",
                $"user-agent: {_project.Profile.UserAgent}"
            };
    
            string response = _project.GET(
                "https://api.twitter.com/1.1/account/verify_credentials.json", 
                "+", 
                headers,
                parse: true
            );
    
            if (response.Contains("\"errors\"")) return false;
            if (response.Contains("\"screen_name\"")) return true;
    
            throw new Exception($"unknown response: {response}");
        }

        public Dictionary<string, string> GetMe()
        {
            _idle.Sleep();
            string response = _project.GET(
                "https://api.twitter.com/1.1/account/verify_credentials.json?include_entities=false", 
                "+", 
                BuildHeaders(), 
                log:true, 
                parse:true
            );
    
            var result = new Dictionary<string, string>();
            var json = _project.Json;
    
            try { result.Add("id", json.id_str.ToString()); } catch { result.Add("id", ""); }
            try { result.Add("screen_name", json.screen_name.ToString()); } catch { result.Add("screen_name", ""); }
            try { result.Add("name", json.name.ToString()); } catch { result.Add("name", ""); }
            try { result.Add("followers_count", json.followers_count.ToString()); } catch { result.Add("followers_count", "0"); }
            try { result.Add("friends_count", json.friends_count.ToString()); } catch { result.Add("friends_count", "0"); }
            try { result.Add("statuses_count", json.statuses_count.ToString()); } catch { result.Add("statuses_count", "0"); }
            try { result.Add("verified", json.verified.ToString()); } catch { result.Add("verified", "false"); }
    
            return result;
        }
        
        public bool Follow(string screenName)
        {
            _idle.Sleep();
            string url = "https://api.twitter.com/1.1/friendships/create.json";
            string postData = $"screen_name={screenName}";
    
            string response = _project.POST(url, postData, "+", BuildHeaders(), log:true, parse:true);
    
            try
            {
                var json = _project.Json;
                return json.following.ToString() == "True";
            }
            catch
            {
                return false;
            }
        }

        public bool Tweet(string text)
        {
            string url = "https://api.twitter.com/1.1/statuses/update.json";
            string postData = $"status={Uri.EscapeDataString(text)}";
            _idle.Sleep();
            string response = _project.POST(url, postData, "+", BuildHeaders(), log:true, parse:true);
    
            try
            {
                var json = _project.Json;
                return json.id_str != null;
            }
            catch
            {
                return false;
            }
        }

        public bool Like(string tweetId)
        {
            string url = "https://api.twitter.com/1.1/favorites/create.json";
            string postData = $"id={tweetId}";
            _idle.Sleep();
            string response = _project.POST(url, postData, "+", BuildHeaders(), log:true, parse:true);
    
            try
            {
                var json = _project.Json;
                return json.favorited.ToString() == "True";
            }
            catch
            {
                return false;
            }
        }

        public bool Retweet(string tweetId)
        {
            string url = $"https://api.twitter.com/1.1/statuses/retweet/{tweetId}.json";
            _idle.Sleep();
            string response = _project.POST(url, "", "+", BuildHeaders(), log:true, parse:true);
    
            try
            {
                var json = _project.Json;
                return json.retweeted.ToString() == "True";
            }
            catch
            {
                return false;
            }
        }

        public bool CheckRateLimit(string response)
        {
            if (response.Contains("rate limit"))
            {
                _project.warn("Rate limited!");
                Thread.Sleep(60000);
                return false;
            }
            return true;
        }

        #endregion

        #region Traffic Analysis

        public Dictionary<string, string> UserByScreenName(bool toDb = false)
        {
            _log.Send("UserByScreenName_ method started");
            bool secondTry = false;
            p0:
            _log.Send($"Creating Traffic object, secondTry: {secondTry}");
            var traffic = new Traffic(_project, _instance);
            _log.Send("Traffic snapshot created successfully");
            var all = traffic.FindAllTrafficElements("UserByScreenName", strict: false);
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
            if (toDb) _project.DicToDb(result , "__twitter");
            return result;
        }

        public Dictionary<string, string> Settings()
        {
            bool secondTry = false;
            p0:
            _log.Send($"Creating Traffic object, secondTry: {secondTry}");
            var traffic = new Traffic(_project, _instance);
            _log.Send("Traffic snapshot created successfully");
            var all = traffic.FindAllTrafficElements("account/settings.json?", strict: false);
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

        #endregion

        #region Content Generation

        /// <summary>
        /// Генерирует контент на основе новостной статьи через AI
        /// </summary>
        /// <param name="purpouse">tweet, thread, или opinionThread</param>
        public string GenerateJson(string purpouse = "tweet")
        {
            var randomNew = Rnd.RndFile(Path.Combine(_project.Path,".data", "news"), "json");
            _project.ToJson(File.ReadAllText(randomNew));
            var article = _project.Json.FullText;
            var ai = new Api.AI(_project,"aiio","meta-llama/Llama-3.2-90B-Vision-Instruct", true);
            var bio = _project.DbGet("bio", "_profile");
            var system = "";
            if (purpouse == "tweet")
                system = $"You are an individual assistant with your own background. Your way of thinking and responding: {bio}. Your task is to summarize the provided article and express your personal opinion on its subject in a single statement, no longer than 280 characters. The statement must be a self-contained insight that summarizes the article's key points, followed by a clear attribution of your bio-informed opinion on the subject using phrases like \"I think,\" \"As for me,\" or \"I suppose.\" The opinion must be explicitly yours, not a detached thesis. Your response must be a clean JSON object with a single key 'statement' containing one string, with no nesting, no additional comments, no extra characters, and no text outside the JSON structure.";
            else if (purpouse == "thread") 
                system = $"You are an individual assistant with your own background. Your way of thinking and responding: {bio}. Your task is to summarize the provided article in a series of short, concise statements, each no longer than 280 characters. Start with an engaging statement to capture attention and lead into the summary. End the summary with a concluding statement that wraps up the key takeaway. Then, provide your personal opinion as a few short theses, each no longer than 280 characters. Your response must be a clean JSON object with two keys: 'summary_statements' as an array of strings for the summary parts (starting with the lead-in and ending with the conclusion), and 'opinion_theses' as an array of strings for the theses. Include no nesting beyond these arrays, no additional comments, no extra characters, and no text outside the JSON structure.";
            else if (purpouse == "opinionThread") 
                system = $"You are an individual assistant with your own background. Your way of thinking and responding: {bio}. Your task is to summarize the provided article in one concise statement, no longer than 280 characters, capturing its key points. Then, provide your personal opinion on the subject in a second statement, no longer than 280 characters, using phrases like \"I think,\" \"As for me,\" or \"I suppose\" to clearly attribute it as your bio-informed perspective, not a detached thesis. Your response must be a clean JSON object with two keys: 'summary_statement' containing the summary string, and 'opinion_statement' containing the opinion string, with no nesting, no additional comments, no extra characters, and no text outside the JSON structure.";
            else
                _log.Warn($"UNKNOWN SYSTEM ROLE: {system}");
            
            string t = ai.Query(system, article).Replace("```json","").Replace("```","");
            _project.ToJson(t);
            return t;
        }

        #endregion

        #region Authentication & State

        public void SkipDefaultButtons()
        {
            _instance.HeClick(("button", "innertext", "Accept\\ all\\ cookies", "regexp", 0), deadline: 0, thr0w: false);
            _instance.HeClick(("button", "data-testid", "xMigrationBottomBar", "regexp", 0), deadline: 0, thr0w: false);
            _instance.HeClick(("button", "innertext", "Got\\ it", "regexp", 0), deadline: 0, thr0w: false);
        }

        public bool CheckCurrent()
        {
            GoToProfile();
            var current = UserByScreenName();
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

        public void Retry()
        {
            _project.Deadline();
            Thread.Sleep(2000);
            while (true)
            {
                _project.Deadline(60);
                if (!_instance.ActiveTab.FindElementByAttribute("button", "innertext", "Retry", "regexp", 0).IsVoid)
                {
                    _project.log("pageFuckedUp Retry...");
                    _instance.HeClick(("button", "innertext", "Retry", "regexp", 0), emu:1);
                    Thread.Sleep(5000);
                    continue;
                }
                break;
            }
        }

        public string GetState()
        {
            _project.Deadline();
            _instance.Go($"https://x.com/home");
            var status = "";

            while (string.IsNullOrEmpty(status))
            {
                Thread.Sleep(5000);
                _project.Deadline(60);
                if (_instance.ActiveTab.URL == "https://x.com/home")
                {
                    _status = "logined";
                    goto end;
                }

                if (!_instance.ActiveTab.FindElementByAttribute("input:text", "autocomplete", "username", "text", 0).IsVoid)
                {
                    _status = "inputLogin";
                    goto end;
                }
                
                if (!_instance.ActiveTab.FindElementByName("password").IsVoid)
                {
                    _status = "inputPassword";
                    goto end;
                }
                
                if (!_instance.ActiveTab.FindElementByName("text").IsVoid)
                {
                    _status = "inputOtp";
                    goto end;
                }
                
                if (!_instance.ActiveTab.FindElementByAttribute("button", "data-testid", "apple_sign_in_button", "text", 0).IsVoid)
                {
                    _status = "defaultPade";
                    goto end;
                }
            }
            end:
            _log.Send(_status);
            _project.Var("xStatus", _status);
            return _status;
        }

        public void LoginWithToken(string token = null)
        {
            if (string.IsNullOrEmpty(token)) token = _token;
            string jsCode = _project.ExecuteMacro($"document.cookie = \"auth_token={token}; domain=.x.com; path=/; expires=${DateTimeOffset.UtcNow.AddYears(1).ToString("R")}; Secure\";\r\nwindow.location.replace(\"https://x.com\")");
            _instance.ActiveTab.MainDocument.EvaluateScript(jsCode);
            
            _log.Send($"token {_token} has been applied");
            _instance.F5();
            Thread.Sleep(3000);
        }

        private string LoginState(bool log = false)
        {
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

                else if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext", @"Caution:\s+This\s+account\s+is\s+temporarily\s+restricted", "regexp", 0).IsVoid)
                    status = "restricted";
                else if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext", @"Account\s+suspended\s+X\s+suspends\s+accounts\s+which\s+violate\s+the\s+X\s+Rules", "regexp", 0).IsVoid)
                    status = "suspended";
                else if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext", @"Log\ in", "regexp", 0).IsVoid || 
                         !_instance.ActiveTab.FindElementByAttribute("a", "data-testid", "loginButton", "regexp", 0).IsVoid)
                    status = "login";
                else if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext", "erify\\ your\\ email\\ address", "regexp", 0).IsVoid ||
                         !_instance.ActiveTab.FindElementByAttribute("div", "innertext", "We\\ sent\\ your\\ verification\\ code.", "regexp", 0).IsVoid)
                    status = "emailCapcha";
                else if (!_instance.ActiveTab.FindElementByAttribute("button", "data-testid", "SideNav_AccountSwitcher_Button", "regexp", 0).IsVoid)
                {
                    var check = _instance.ActiveTab.FindElementByAttribute("button", "data-testid", "SideNav_AccountSwitcher_Button", "regexp", 0)
                        .FirstChild.FirstChild.GetAttribute("data-testid");
                    if (check == $"UserAvatar-Container-{_login}") 
                        status = "ok";
                    else
                    {
                        status = "mixed";
                        _project.log($"!W {status}. Detected  [{check}] instead [UserAvatar-Container-{_login}] {DateTime.Now - start}");
                    }
                }
                else if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Something\\ went\\ wrong.\\ Try\\ reloading.", "regexp", 0).IsVoid)
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
            string jsCode = _project.ExecuteMacro($"document.cookie = \"auth_token={token}; domain=.x.com; path=/; expires=${DateTimeOffset.UtcNow.AddYears(1).ToString("R")}; Secure\";\r\nwindow.location.replace(\"https://x.com\")");
            _instance.ActiveTab.MainDocument.EvaluateScript(jsCode);
        }

        public void GetCt0FromToken()
        {
            if (string.IsNullOrEmpty(_token))
                throw new Exception("No auth_token");

            string[] headers = {
                $"cookie: auth_token={_token}",
                $"user-agent: {_project.Profile.UserAgent}"
            };
    
            _project.GET("https://api.twitter.com/1.1/account/verify_credentials.json", "+", headers);
    
            var cookJson = _instance.GetCookies(".");//new Cookies(_project, _instance).Get(".");
            JArray parsed = JArray.Parse(cookJson);
    
            for (int i = 0; i < parsed.Count; i++)
            {
                if (parsed[i]["name"].ToString() == "ct0")
                {
                    _ct0 = parsed[i]["value"].ToString();
                    _project.DbUpd($"ct0 = '{_ct0}'", "_twitter");
                    _log.Send($"ct0 extracted: {_ct0.Length} chars");
                    break;
                }
            }
        }

        public void TokenGet()
        {
            var cookJson = _instance.GetCookies(".");//new Cookies(_project, _instance).Get(".");
            JArray toParse = JArray.Parse(cookJson);
    
            string token = "";
            string ct0 = "";
            string guest_id = "";
            string kdt= "";
            string twid = "";

            
    
            for (int i = 0; i < toParse.Count; i++)
            {
                string cookieName = toParse[i]["name"].ToString();
        
                if (cookieName == "auth_token")
                    token = toParse[i]["value"].ToString();
        
                if (cookieName == "ct0")
                    ct0 = toParse[i]["value"].ToString();
                
                if (cookieName == "guest_id")
                    guest_id = toParse[i]["value"].ToString();
                
                if (cookieName == "kdt")
                    kdt = toParse[i]["value"].ToString();
                
                if (cookieName == "twid")
                    twid = toParse[i]["value"].ToString();
                
        
                if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(ct0))
                    break;
            }

            _token = token;
            _ct0 = ct0;
    
            _project.DbUpd($"token = '{token}', ct0 = '{ct0}'", "_twitter");
    
            _log.Send($"Tokens extracted: auth_token length={token.Length}, ct0 length={ct0.Length}");
        }

        public string LoginWithCredentials()
        {
            var err = "";
            if (_instance.ActiveTab.FindElementByAttribute("input:text", "autocomplete", "username", "text", 0).IsVoid)
            {
                DateTime deadline = DateTime.Now.AddSeconds(60);

                _instance.ActiveTab.Navigate("https://x.com/", "");
                _idle.Sleep();
                _instance.HeClick(("button", "innertext", "Accept\\ all\\ cookies", "regexp", 0), deadline: 1, thr0w: false);
                _instance.HeClick(("button", "data-testid", "xMigrationBottomBar", "regexp", 0), deadline: 0, thr0w: false);
                _instance.HeClick(("a", "data-testid", "login", "regexp", 0));
            }

            _instance.JsSet("[autocomplete='username']", _login);
            _idle.Sleep();
            _instance.SendText("{ENTER}", 15);
            
            var toast = CatchToast();
            if (toast.Contains("Could not log you in now."))
                return toast;

            err = CatchErr();
            if (err != "") return err;
            
            _instance.JsSet("[name='password']", _pass);
            _idle.Sleep();
            _instance.SendText("{ENTER}", 15);
            _idle.Sleep();
            
            err = CatchErr();
            if (err != "") return err;
            
            var codeOTP = OTP.Offline(_2fa);
            _instance.JsSet("[name='text']", codeOTP);
            _idle.Sleep();
            _instance.SendText("{ENTER}", 15);
            _idle.Sleep();
            
            err = CatchErr();
            if (err != "") return err;

            _instance.HeClick(("button", "innertext", "Accept\\ all\\ cookies", "regexp", 0), deadline: 1, thr0w: false);
            _instance.HeClick(("button", "data-testid", "xMigrationBottomBar", "regexp", 0), deadline: 0, thr0w: false);
            TokenGet();
            return "ok";
        }

        private string CatchErr()
        {
            if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Sorry, we could not find your account')]", 0).IsVoid) 
                return "NotFound";
            if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Wrong password!')]", 0).IsVoid)
                return "WrongPass";
            if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Your account is suspended')]", 0).IsVoid)
                return "Suspended";
            if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Oops,\\ something\\ went\\ wrong.\\ Please\\ try\\ again\\ later.", "regexp", 0).IsVoid) 
                return "SomethingWentWrong";
            if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext", "Suspicious\\ login\\ prevented", "regexp", 0).IsVoid) 
                return "SuspiciousLogin";
            return "";
        }

        private string CatchToast()
        {
            var err = "";
            try
            {
                err = _instance.HeGet(("div", "data-testid", "toast", "regexp", 0), deadline:2);
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
                if (!string.IsNullOrEmpty(_token) && !string.IsNullOrEmpty(_ct0))
                {
                    try
                    {
                        bool isTokenValid = TokenValidate();
                        _log.Send($"Token API Check: {(isTokenValid ? "Valid" : "Invalid")}");
                        
                        if (isTokenValid)
                        {
                            TokenSet();
                            tokenUsed = true;
                            Thread.Sleep(5000);
                        }
                        else
                        {
                            tokenUsed = true;
                            status = LoginWithCredentials();
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Token validation error: {ex.Message}");
                        tokenUsed = true;
                        status = LoginWithCredentials();
                    }
                }
                else
                {
                    tokenUsed = true;
                    status = LoginWithCredentials();
                }
            }
            else if (status == "login" && tokenUsed)
            {
                status = LoginWithCredentials();
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
            if (status == "restricted" || status == "suspended" || status == "emailCapcha" || 
                status == "proxyTrouble" || status == "WrongPass" || status == "Suspended" || 
                status == "NotFound" || status.Contains("Could not log you in now."))
            {
                return status;
            }
            else if (status == "ok")
            {
                _instance.HeClick(("button", "innertext", "Accept\\ all\\ cookies", "regexp", 0), deadline: 0, thr0w: false);
                _instance.HeClick(("button", "data-testid", "xMigrationBottomBar", "regexp", 0), deadline: 0, thr0w: false);
                TokenGet();
                return status;
            }
            else
            {
                _log.Send($"unknown {status}");
            }

            goto check;
        }

        public void Auth()
        {
            _project.Deadline();
            _instance.HeClick(("button", "innertext", "Accept\\ all\\ cookies", "regexp", 0), deadline: 0, thr0w: false);
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
                    _instance.HeSet(("input:text", "data-testid", "ocfEnterTextTextInput", "text", 0), OTP.Offline(_2fa));
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
            else 
                goto check;
        }

        public string AuthState()
        {
            string state = "undefined";

            if (_instance.ActiveTab.URL.Contains("oauth/authorize"))
            {
                if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'The request token for this page is invalid')]", 0).IsVoid)
                    return "InvalidRequestToken";
                if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'This account is suspended')]", 0).IsVoid)
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

            if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Sorry, we could not find your account')]", 0).IsVoid) 
                state = "NotFound";
            else if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Your account is suspended')]", 0).IsVoid) 
                state = "Suspended";
            else if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Wrong password!')]", 0).IsVoid)
                state = "WrongPass";
            else if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Oops,\\ something\\ went\\ wrong.\\ Please\\ try\\ again\\ later.", "regexp", 0).IsVoid) 
                state = "SomethingWentWrong";
            else if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext", "Suspicious\\ login\\ prevented", "regexp", 0).IsVoid) 
                state = "SuspiciousLogin";
            else if (!_instance.ActiveTab.FindElementByAttribute("input:text", "autocomplete", "username", "text", 0).IsVoid) 
                state = "InputLogin";
            else if (!_instance.ActiveTab.FindElementByAttribute("input:password", "autocomplete", "current-password", "text", 0).IsVoid) 
                state = "InputPass";
            else if (!_instance.ActiveTab.FindElementByAttribute("input:text", "data-testid", "ocfEnterTextTextInput", "text", 0).IsVoid) 
                state = "InputOTP";
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
            
            _instance.JsClick("[data-testid='SideNav_NewTweet_Button']");
            _instance.JsSet("[data-testid='tweetTextarea_0']", title);
            _idle.Sleep();

            int tIndex = 1;
            foreach (var add in tweets)
            {
                _instance.JsClick("[data-testid='addButton']");
                _idle.Sleep();
                _instance.JsSet($"[data-testid='tweetTextarea_{tIndex}']", add);
                _idle.Sleep();
                tIndex++;
            }
            
            _instance.JsClick("[data-testid='tweetButton']");
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

            if (FlwButton.Contains("Follow"))
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

        public string GetCurrentEmail()
        {
            _instance.Go("https://x.com/settings/email");
            try
            {
                _instance.HeSet(("current_password", "name"), _pass, deadline: 1);
                _instance.HeClick(("button", "innertext", "Confirm", "regexp", 0));
            }
            catch { }

            string email = _instance.HeGet(("current_email", "name"), atr:"value");
            return email.ToLower();
        }

        #endregion

        #region Intent Links

        public void FollowByLink(string screen_name)
        {
            Tab tab = _instance.NewTab("twitter");
            _instance.Go($"https://x.com/intent/follow?screen_name={screen_name}");
            _idle.Sleep();
            _instance.HeGet(("button", "data-testid", "confirmationSheetConfirm", "regexp", 0));
            _instance.JsClick("[data-testid='confirmationSheetConfirm']");
            _idle.Sleep();
            tab.Close();
        }

        public void QuoteByLink(string tweeturl)
        {
            Tab tab = _instance.NewTab("twitter");
            string text = Uri.EscapeDataString(tweeturl);
            _instance.Go($"https://x.com/intent/post?text={text}");
            _idle.Sleep();
            _instance.HeGet(("button", "data-testid", "confirmationSheetConfirm", "regexp", 0));
            _instance.JsClick("[data-testid='confirmationSheetConfirm']");
            _idle.Sleep();
            tab.Close();
        }

        public void RetweetByLink(string tweet_id)
        {
            Tab tab = _instance.NewTab("twitter");
            _instance.Go($"https://x.com/intent/retweet?tweet_id={tweet_id}");
            _idle.Sleep();
            _instance.HeGet(("button", "data-testid", "confirmationSheetConfirm", "regexp", 0));
            _instance.JsClick("[data-testid='confirmationSheetConfirm']");
            _idle.Sleep();
            tab.Close();
        }

        public void LikeByLink(string tweet_id)
        {
            Tab tab = _instance.NewTab("twitter");
            _instance.Go($"https://x.com/intent/like?tweet_id={tweet_id}");
            _idle.Sleep();
            _instance.HeGet(("button", "data-testid", "confirmationSheetConfirm", "regexp", 0));
            _instance.JsClick("[data-testid='confirmationSheetConfirm']");
            _idle.Sleep();
            tab.Close();
        }

        public void ReplyByLink(string tweet_id, string text)
        {
            Tab tab = _instance.NewTab("twitter");
            string escapedText = Uri.EscapeDataString(text);
            _instance.Go($"https://x.com/intent/post?in_reply_to={tweet_id}&text={escapedText}");
            _idle.Sleep();
            _instance.HeGet(("button", "data-testid", "tweetButton", "regexp", 0));
            _instance.JsClick("[data-testid='tweetButton']");
            _idle.Sleep();
            tab.Close();
        }

        #endregion

        #region Import

        public Dictionary<string, string> ParseNewCredentials(string data, bool toDb = true)
        {
            var creds = ParseNewCred(data);
            if (toDb) _project.DicToDb(creds, "_twitter");
            return creds;
        }

        public static Dictionary<string, string> ParseNewCred(string data)
        {
            var separator = data.Contains(":") ? ':' : ';';
            var creds = new Dictionary<string, string>();
            var dataparts = data.Split(separator);
            var list = dataparts.ToList();

            foreach (var part in dataparts)
            {
                if (part.Contains("@"))
                {
                    creds.Add("email", part);
                    list.Remove(part);
                }
    
                if (part.Length == 160)
                {
                    creds.Add("ct0", part);
                    list.Remove(part);
                }
    
                if (part.Length == 40)
                {
                    creds.Add("token", part);
                    list.Remove(part);
                }
            }

            creds.Add("login", list[0]);
            list.RemoveAt(0);

            creds.Add("password", list[0]);
            list.RemoveAt(0);

            foreach (var part in list.ToList())
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(part, "^[A-Z2-7]{16}$"))
                {
                    creds.Add("otpsecret", part);
                    list.Remove(part);
                    break;
                }
            }

            if (list.Count > 0)
            {
                creds.Add("emailpassword", list[0]);
            }
            
            return creds;
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
    
    
    public class __Discord
    {
        #region Members & constructor
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _log;
        private readonly Utilities.Sleeper _idle;
        private readonly bool _enableLog;
        private string _status;
        private string _token;
        private string _login;
        private string _pass;
        private string _2fa;
        
        public __Discord(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _enableLog = log;
            _log = new Logger(project, log: _enableLog, classEmoji: "👾");
            _idle = new Utilities.Sleeper(1337, 2078);
            LoadCreds();
        }
        #endregion
        #region Authentication
        
        public string Load(bool log = false)
        {
            string state = null;
            var emu = _instance.UseFullMouseEmulation;
            _instance.UseFullMouseEmulation = false;
            
            bool isTokenValid = false;
            if (!string.IsNullOrEmpty(_token))
            {
                try 
                {
                    isTokenValid = TokenValidate();
                    _log.Send($"Token API Check: {(isTokenValid ? "Valid" : "Invalid")}");
                }
                catch (Exception ex)
                {
                    _log.Warn($"Token validation error: {ex.Message}");
                    isTokenValid = false;
                }
            }
            else
            {
                 _log.Send("No token to validate, will use credentials.");
            }

            bool tokenUsed = false;
            bool credentialsUsed = false;

            _instance.Go("https://discord.com/channels/@me");
            _project.Deadline();
            
        start:
            _project.Deadline(60);
            state = GetState();
            
            _log.Send($"Page state detected: {state}, tokenValid={isTokenValid}, tokenUsed={tokenUsed}");

            if (isTokenValid && !tokenUsed && state == "input_credentials")
            {
                _log.Send("API confirmed token is valid. Attempting injection...");
                TokenSet();
                tokenUsed = true;
                goto start;
            } 
            else if (state == "appDetected") 
            {
                 _log.Send("appDetected ");
                _instance.HeClick(("span", "innertext", "Continue\\ in\\ Browser", "regexp", 0));
                goto start;
            }

            switch (state){
                case "input_credentials":
                    _log.Send($"Using credentials (Token valid: {isTokenValid})");
                    credentialsUsed = true;
                    InputCredentials();
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
                    
                    if (credentialsUsed || !isTokenValid)
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
        public string GetState(bool log = false)
        {
            string state = null;
            _project.Deadline();
            while (string.IsNullOrEmpty(state))
            {
                _project.Deadline(180);
                _idle.Sleep();
            
                if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Continue\\ in\\ Browser", "regexp", 0).IsVoid) 
                    state = "appDetected";
                else if (!_instance.ActiveTab.FindElementByAttribute("section", "aria-label", "User\\ area", "regexp", 0).IsVoid) 
                    state = "logged";
                else if (!_instance.ActiveTab.FindElementByAttribute("div", "innertext", "Are\\ you\\ human\\?", "regexp", 0).IsVoid) 
                    state = "capctha";
                else if (!_instance.ActiveTab.FindElementByAttribute("input:text", "autocomplete", "one-time-code", "regexp", 0).IsVoid) 
                    state = "input_otp";
                else if (_instance.ActiveTab.FindElementByAttribute("div", "class", "helperTextContainer__", "regexp", 0).InnerText != "") 
                    state = _instance.ActiveTab.FindElementByAttribute("div", "class", "helperTextContainer__", "regexp", 0).InnerText;
                else if (!_instance.ActiveTab.FindElementByAttribute("input:text", "aria-label", "Email or Phone Number", "text", 0).IsVoid) 
                    state = "input_credentials";
                
            }
            return state;
            
        }

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
            var stats = new Traffic(_project,_instance).FindTrafficElement("https://discord.com/api/v9/science",  reload:true).RequestHeaders;
            string patern = @"(?<=uthorization:\ ).*";
            string token = System.Text.RegularExpressions.Regex.Match(stats, patern).Value;
            _log.Send($"Token extracted: length={token?.Length ?? 0}, valid={!string.IsNullOrEmpty(token)}");
            if (saveToDb) _project.DbUpd($"token = '{token}', status = 'ok'", "_discord");
            return token;
        }
        private void InputCredentials()
        {
            _instance.HeSet(("input:text", "aria-label", "Email or Phone Number", "text", 0), _login);
            _instance.HeSet(("input:password", "aria-label", "Password", "text", 0), _pass);
            _instance.HeClick(("button", "type", "submit", "regexp", 0));
        }
        #endregion
        #region Stats & Info UI
        
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


        #region Api
        
        private string[] BuildHeaders()
        {
            string[] headers = {
                $"Authorization : {_token}",
                "accept: application/json",
                "accept-encoding: ",
                "accept-language: en-US,en;q=0.9",
                "origin: https://discord.com",
                "referer: https://discord.com/channels/@me",
                "sec-ch-ua-mobile: ?0",
                "sec-fetch-dest: empty",
                "sec-fetch-mode: cors",
                "sec-fetch-site: same-origin",
                $"user-agent: {_project.Profile.UserAgent}",
                "x-discord-locale: en-US",
            };
            return headers;
        }

        public Dictionary<string, string> GetMe(bool updateDb = false)
        {
            _idle.Sleep();
            string response = _project.GET("https://discord.com/api/v9/users/@me", "+",BuildHeaders());
            if (response.Contains("{\"message\":")) 
                throw new Exception(response);
            var dict = response.JsonToDic(ignoreEmpty:true);
            if  (updateDb) 
                _project.JsonToDb(response, "_discord", log:_enableLog);
            return dict;
        }
        
        public bool TokenValidate(string token = null)
        {
            if( string.IsNullOrEmpty(token)) token = _token;
            _idle.Sleep();
            string response = _project.GET("https://discord.com/api/v9/users/@me", "+",BuildHeaders(), parse:true);
            if (response.Contains("401: Unauthorized")) return false;
            if (response.Contains("username")) return true;
            throw new Exception($"uncknown response: {response}");
        }
        
        public List<string> GetRolesId(string guildId)
        {
            var myUserId = _project.DbGet("_id", "__discord");
            _idle.Sleep();
            string response = _project.GET($"https://discord.com/api/v9/guilds/{guildId}/members/{myUserId}", "+",BuildHeaders(), parse:true);

            var roles = new List<string>();
            var json = _project.Json;
            
            if (json.roles == null)
            {
                _log.Warn($"API error: {response}");
                return new List<string>();
            }
            
            var rolesCnt = json.roles.Count;
            for(int i = 0; i < rolesCnt ; i++)
            {
                var roleId = json.roles[i];
                roles.Add(roleId);
            }
            return roles;
        }

        public Dictionary<string, string> GetRolesNamesForGuild(string guildId)
        {
            _idle.Sleep();
            string response = _project.GET($"https://discord.com/api/v9/guilds/{guildId}/roles", "+", BuildHeaders(),  parse:true);

            var roles = new Dictionary<string, string>();
            var json = _project.Json;
            var rolesCnt = json.Count;

            for(int i = 0; i < rolesCnt ; i++)
            {
                string roleId = json[i].id.ToString();
                var roleName = json[i].name;
                roles.Add(roleId,roleName);
            }
            return roles;
        }
        
        public List<string> GetRolesNames(string guildId)
        {
            var rolesIds = GetRolesId(guildId);
            var rolesNames = GetRolesNamesForGuild(guildId);  
    
            var namedRoles = new List<string>();
            foreach (var roleId in rolesIds)
            {
                if (rolesNames.ContainsKey(roleId))  // Безопаснее
                {
                    namedRoles.Add(rolesNames[roleId]);
                }
                else
                {
                    _project.warn($"Role ID {roleId} not found in guild roles");
                }
            }
            return namedRoles;
        }
        
        public Dictionary<string, string> GetServers(string guildId)
        {
            _idle.Sleep();
            string response = _project.GET("https://discord.com/api/v9/users/@me/guilds", "+",BuildHeaders(), parse:true);

            var servers = new Dictionary<string, string>();
            var json = _project.Json;
            var rolesCnt = json.Count;

            for(int i = 0; i < rolesCnt ; i++)
            {
                string id = json[i].id.ToString();
                var name = json[i].name;
                servers.Add(id,name);
            }
            return servers;
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
    public static class Test
    {
      
        public static string GetFileNameFromUrl(this string input, bool withExtension = false)
        {
            try
            {
                // Пробуем найти URL в строке
                var urlMatch = Regex.Match(input, @"(?:src|href)=[""']?([^""'\s>]+)", RegexOptions.IgnoreCase);
                var url = urlMatch.Success ? urlMatch.Groups[1].Value : input;

                // Извлекаем последний сегмент (имя файла)
                var fileMatch = Regex.Match(url, @"([^/\\?#]+)(?:\?[^/]*)?$");
                if (fileMatch.Success)
                {
                    var fileName = fileMatch.Groups[1].Value;
            
                    // Если нужно с расширением - возвращаем как есть
                    if (withExtension)
                    {
                        return fileName;
                    }
            
                    // Удаляем расширение
                    return Regex.Replace(fileName, @"\.[^.]+$", "");
                }

                return input;
            }
            catch
            {
                return input;
            }
        }
        
    }

    
}