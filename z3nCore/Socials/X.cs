using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Newtonsoft.Json.Linq;



namespace z3nCore
{
    public class X
    {
        protected readonly IZennoPosterProjectModel _project;
        protected readonly Instance _instance;
        private readonly Logger _logger;

        protected readonly bool _logShow;
        protected readonly Sql _sql;

        protected string _status;
        protected string _token;
        protected string _login;
        protected string _pass;
        protected string _2fa;
        protected string _email;
        protected string _email_pass;

        public X(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {

            _project = project;
            _instance = instance;
            _logShow = log;
            _logger = new Logger(project, log: log, classEmoji: "X");

            LoadCreds();

        }
        public void LoadCreds()
        {
            string[] creds = _project.SqlGet(" status, token, login, password, otpsecret, email, emailpass", "_twitter").Split('|');
            try { _status = creds[0].Trim(); _project.Variables["twitterSTATUS"].Value = _status; } catch (Exception ex) { _logger.Send(ex.Message); }
            try { _token = creds[1].Trim(); _project.Variables["twitterTOKEN"].Value = _token; } catch (Exception ex) { _logger.Send(ex.Message); }
            try { _login = creds[2].Trim(); _project.Variables["twitterLOGIN"].Value = _login; } catch (Exception ex) { _logger.Send(ex.Message); }
            try { _pass = creds[3].Trim(); _project.Variables["twitterPASSWORD"].Value = _pass; } catch (Exception ex) { _logger.Send(ex.Message); }
            try { _2fa = creds[4].Trim(); _project.Variables["twitterCODE2FA"].Value = _2fa; } catch (Exception ex) { _logger.Send(ex.Message); }
            try { _email = creds[5].Trim(); _project.Variables["twitterEMAIL"].Value = _email; } catch (Exception ex) { _logger.Send(ex.Message); }
            try { _email_pass = creds[6].Trim(); _project.Variables["twitterEMAIL_PASSWORD"].Value = _email_pass; } catch (Exception ex) { _logger.Send(ex.Message); }

            if (string.IsNullOrEmpty(_login) || string.IsNullOrEmpty(_pass))
                throw new Exception($"invalid credentials login:[{_login}] pass:[{_pass}]");
        }


        private string XcheckState(bool log = false)
        {
            log = _project.Variables["debug"].Value == "True";
            DateTime start = DateTime.Now;
            DateTime deadline = DateTime.Now.AddSeconds(60);
            string login = _project.Variables["twitterLOGIN"].Value;
            _instance.ActiveTab.Navigate($"https://x.com/{login}", "");
            var status = "";

            while (string.IsNullOrEmpty(status))
            {
                Thread.Sleep(5000);
                _project.L0g($"{DateTime.Now - start}s check... URLNow:[{_instance.ActiveTab.URL}]");
                if (DateTime.Now > deadline) throw new Exception("timeout");

                else if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext", @"Caution:\s+This\s+account\s+is\s+temporarily\s+restricted", "regexp", 0).IsVoid)
                    status = "restricted";
                else if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext", @"Account\s+suspended\s+X\s+suspends\s+accounts\s+which\s+violate\s+the\s+X\s+Rules", "regexp", 0).IsVoid)
                    status = "suspended";
                else if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext", @"Log\ in", "regexp", 0).IsVoid || !_instance.ActiveTab.FindElementByAttribute("a", "data-testid", "loginButton", "regexp", 0).IsVoid)
                    status = "login";

                else if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext", "erify\\ your\\ email\\ address", "regexp", 0).IsVoid ||
                    !_instance.ActiveTab.FindElementByAttribute("div", "innertext", "We\\ sent\\ your\\ verification\\ code.", "regexp", 0).IsVoid)
                    status = "emailCapcha";
                else if (!_instance.ActiveTab.FindElementByAttribute("button", "data-testid", "SideNav_AccountSwitcher_Button", "regexp", 0).IsVoid)
                {
                    var check = _instance.ActiveTab.FindElementByAttribute("button", "data-testid", "SideNav_AccountSwitcher_Button", "regexp", 0).FirstChild.FirstChild.GetAttribute("data-testid");
                    if (check == $"UserAvatar-Container-{login}") status = "ok";
                    else
                    {
                        status = "mixed";
                        _project.L0g($"!W {status}. Detected  [{check}] instead [UserAvatar-Container-{login}] {DateTime.Now - start}");
                    }
                }
                else if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Something\\ went\\ wrong.\\ Try\\ reloading.", "regexp", 0).IsVoid)
                {
                    _instance.ActiveTab.MainDocument.EvaluateScript("location.reload(true)");
                    Thread.Sleep(3000);
                    continue;
                }
            }
            _project.L0g($"{status} {DateTime.Now - start}");
            return status;
        }
        public string Xload(bool log = false)
        {
            bool tokenUsed = false;
            DateTime deadline = DateTime.Now.AddSeconds(60);
        check:

            if (DateTime.Now > deadline) throw new Exception("timeout");

            var status = XcheckState(log: true);
            try { _project.Var("twitterSTATUS", status); } catch  { }
            if (status == "login" && !tokenUsed)
            {
                TokenSet();
                tokenUsed = true;
                Thread.Sleep(3000);
            }
            else if (status == "login" && tokenUsed)
            {
                var login = Login();
                _project.L0g($"{login}");
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
            if (status == "restricted" || status == "suspended" || status == "emailCapcha")
            {

                _sql.Upd($"status = '{status}'", "twitter");
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
                _project.L0g($"unknown {status}");
            goto check;
        }

        private void TokenSet()
        {
            var token = _project.Variables["twitterTOKEN"].Value;
            string jsCode = _project.ExecuteMacro($"document.cookie = \"auth_token={token}; domain=.x.com; path=/; expires=${DateTimeOffset.UtcNow.AddYears(1).ToString("R")}; Secure\";\r\nwindow.location.replace(\"https://x.com\")");
            _instance.ActiveTab.MainDocument.EvaluateScript(jsCode);
        }
        private string TokenGet()
        {
            //var cookJson = _instance.GetCookies(_project,".");
            var cookJson = new Cookies(_project, _instance).Get(".");//_instance.GetCookies(_project, ".");

            JArray toParse = JArray.Parse(cookJson);
            int i = 0; var token = "";
            while (token == "")
            {
                if (toParse[i]["name"].ToString() == "auth_token") token = toParse[i]["value"].ToString();
                i++;
            }
            _project.Variables["twitterTOKEN"].Value = token;
            _sql.Upd($"token = '{token}'", "private_twitter");
            return token;
        }

        private string Login()
        {
            DateTime deadline = DateTime.Now.AddSeconds(60);
            var login = _project.Variables["twitterLOGIN"].Value;

            _instance.ActiveTab.Navigate("https://x.com/", ""); Thread.Sleep(2000);
            _instance.HeClick(("button", "innertext", "Accept\\ all\\ cookies", "regexp", 0), deadline: 1, thr0w: false);
            _instance.HeClick(("button", "data-testid", "xMigrationBottomBar", "regexp", 0), deadline: 0, thr0w: false);
            _instance.HeClick(("a", "data-testid", "login", "regexp", 0));
            _instance.HeSet(("input:text", "autocomplete", "username", "text", 0), login, deadline: 30);
            _instance.HeClick(("span", "innertext", "Next", "regexp", 1), "clickOut");

            if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Sorry, we could not find your account')]", 0).IsVoid) return "NotFound";

            _instance.HeSet(("password", "name"), _project.Variables["twitterPASSWORD"].Value);


            _instance.HeClick(("button", "data-testid", "LoginForm_Login_Button", "regexp", 0), "clickOut");

            if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Wrong password!')]", 0).IsVoid) return "WrongPass";

            var codeOTP = OTP.Offline(_project.Variables["twitterCODE2FA"].Value);
            _instance.HeSet(("text", "name"), codeOTP);


            _instance.HeClick(("span", "innertext", "Next", "regexp", 1), "clickOut");

            if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Your account is suspended')]", 0).IsVoid) return "Suspended";
            if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Oops,\\ something\\ went\\ wrong.\\ Please\\ try\\ again\\ later.", "regexp", 0).IsVoid) return "SomethingWentWrong";
            if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext", "Suspicious\\ login\\ prevented", "regexp", 0).IsVoid) return "SuspiciousLogin";

            _instance.HeClick(("button", "innertext", "Accept\\ all\\ cookies", "regexp", 0), deadline: 1, thr0w: false);
            _instance.HeClick(("button", "data-testid", "xMigrationBottomBar", "regexp", 0), deadline: 0, thr0w: false);
            TokenGet();
            return "ok";
        }
        public string Load(bool log = false)
        {
            bool tokenUsed = false;
            DateTime deadline = DateTime.Now.AddSeconds(60);
        check:

            if (DateTime.Now > deadline) throw new Exception("timeout");

            var status = XcheckState(log: true);
            try { _project.Var("twitterSTATUS", status); } catch  { }

            if (status == "login" && !tokenUsed)
            {
                TokenSet();
                tokenUsed = true;
                Thread.Sleep(3000);
            }
            else if (status == "login" && tokenUsed)
            {
                var login = Login();
                _project.L0g($"{login}");
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
            if (status == "restricted" || status == "suspended" || status == "emailCapcha")
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
                _logger.Send($"unknown {status}");
            goto check;
        }
        public void Auth()
        {
            _project.Deadline();
            _instance.HeClick(("button", "innertext", "Accept\\ all\\ cookies", "regexp", 0), deadline: 0, thr0w: false);
            _instance.HeClick(("button", "data-testid", "xMigrationBottomBar", "regexp", 0), deadline: 0, thr0w: false);

        check:
            _project.Deadline(60);
            string state = State();

            _project.L0g(state);

            switch (state)
            {
                case "NotFound":
                case "Suspended":
                case "SuspiciousLogin":
                case "WrongPass":
                    _sql.Upd($"status = '{state}'", "twitter");
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
                    _logger.Send($"unknown state [{state}]");
                    break;

            }

            if (!_instance.ActiveTab.URL.Contains("x.com") && !_instance.ActiveTab.URL.Contains("twitter.com"))
                _project.L0g("auth done");
            else goto check;
        }
        public string State()
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

            if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Sorry, we could not find your account')]", 0).IsVoid) state = "NotFound";
            else if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Your account is suspended')]", 0).IsVoid) state = "Suspended";
            else if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Wrong password!')]", 0).IsVoid) state = "WrongPass";
            else if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Oops,\\ something\\ went\\ wrong.\\ Please\\ try\\ again\\ later.", "regexp", 0).IsVoid) state = "SomethingWentWrong";
            else if (!_instance.ActiveTab.FindElementByAttribute("*", "innertext", "Suspicious\\ login\\ prevented", "regexp", 0).IsVoid) state = "SuspiciousLogin";



            else if (!_instance.ActiveTab.FindElementByAttribute("input:text", "autocomplete", "username", "text", 0).IsVoid) state = "InputLogin";
            else if (!_instance.ActiveTab.FindElementByAttribute("input:password", "autocomplete", "current-password", "text", 0).IsVoid) state = "InputPass";
            else if (!_instance.ActiveTab.FindElementByAttribute("input:text", "data-testid", "ocfEnterTextTextInput", "text", 0).IsVoid) state = "InputOTP";


            else if (!_instance.ActiveTab.FindElementByAttribute("a", "data-testid", "login", "regexp", 0).IsVoid) state = "ClickLogin";
            else if (!_instance.ActiveTab.FindElementByAttribute("li", "data-testid", "UserCell", "regexp", 0).IsVoid) state = "CheckUser";
            return state;
        }

        public void UpdXCreds(Dictionary<string, string> data)
        {
            if (data.ContainsKey("CODE2FA"))
            {
                _project.L0g($"CODE2FA raw value: {data["CODE2FA"]}"); // Логируем исходное значение
            }

            var fields = new Dictionary<string, string>
            {
                { "LOGIN", data.ContainsKey("LOGIN") ? data["LOGIN"].Replace("'", "''") : "" },
                { "PASSWORD", data.ContainsKey("PASSWORD") ? data["PASSWORD"].Replace("'", "''") : "" },
                { "EMAIL", data.ContainsKey("EMAIL") ? data["EMAIL"].Replace("'", "''") : "" },
                { "EMAIL_PASSWORD", data.ContainsKey("EMAIL_PASSWORD") ? data["EMAIL_PASSWORD"].Replace("'", "''") : "" },
                { "TOKEN", data.ContainsKey("TOKEN") ? (data["TOKEN"].Contains('=') ? data["TOKEN"].Split('=').Last().Replace("'", "''") : data["TOKEN"].Replace("'", "''")) : "" },
                { "CODE2FA", data.ContainsKey("CODE2FA") ? (data["CODE2FA"].Contains('/') ? data["CODE2FA"].Split('/').Last().Replace("'", "''") : data["CODE2FA"].Replace("'", "''")) : "" },
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
                otpbackup = '{fields["RECOVERY_SEED"]}'", "_twitter", log:_logShow);
            }
            catch (Exception ex)
            {
                _project.L0g($"!W{ex.Message}");
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
            string id = main?["identifier"]?.ToString() ?? "";
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
                id = '{id}',
                username = '{username}',
                description = '{description}',
                givenname = '{givenName}',
                homelocation = '{homeLocation}',
                ava = '{ava}',
                banner = '{banner}',
                followers = '{followers}',
                following = '{following}',
                tweets = '{tweets}',
                ","__twitter", log:_logShow);
            try
            {
                var toFill = _project.Lists["editProfile"];
                toFill.Clear();

                if (description == "") toFill.Add("description");
                if (homeLocation == "") toFill.Add("homeLocation");
                if (ava == "https://abs.twimg.com/sticky/default_profile_images/default_profile_400x400.png") toFill.Add("ava");
                if (banner == "https://abs.twimg.com/sticky/default_profile_images/default_profile_normal.png") toFill.Add("banner");

            }
            catch { }

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
            catch { }
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
                        birth = '{birth}',
                        ","__twitter", log:_logShow);


            try
            {
                email = email.ToLower();
                var emails = _project.DbGet("gmail, icloud, firstmail", "_mail").ToLower();
                var address = _project.DbGet("evm_pk","_addresses").ToLower();
                var toFill = _project.Lists["editSecurity"];
                toFill.Clear();

                if (!emails.Contains(email) || !email.Contains(address)) toFill.Add("email");

            }
            catch { }
        }
    }
}
