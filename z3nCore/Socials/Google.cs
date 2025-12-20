using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Newtonsoft.Json.Linq;
using z3nCore.Utilities;

namespace z3nCore
{
    #region Main Class
    
    public class _Google
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _log;
        
        // Подклассы
        public GoogleAPI API { get; private set; }
        public GoogleUI UI { get; private set; }
        
        /// <summary>
        /// Конструктор с Instance (полный функционал: API + UI)
        /// </summary>
        public _Google(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance), "Instance cannot be null for this constructor");
                
            _project = project;
            _instance = instance;
            _log = new Logger(project, log: log, classEmoji: "G");
            
            InitializeSubclasses();
        }
        
        /// <summary>
        /// Конструктор без Instance (только API методы)
        /// </summary>
        public _Google(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project;
            _instance = null;
            _log = new Logger(project, log: log, classEmoji: "G");
            
            InitializeSubclasses();
        }
        
        private void InitializeSubclasses()
        {
            API = new GoogleAPI(_project, _instance, _log);
            
            if (_instance != null)
            {
                UI = new GoogleUI(_project, _instance, _log, API);
            }
        }
    }
    
    #endregion
    
    #region API Subclass
    
    /// <summary>
    /// Google API методы (работают без браузера)
    /// </summary>
    public class GoogleAPI
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _log;
        private readonly Sleeper _idle;
        
        private string _login;
        
        internal GoogleAPI(IZennoPosterProjectModel project, Instance instance, Logger log)
        {
            _project = project;
            _instance = instance;
            _log = log;
            _idle = new Sleeper(1337, 2078);
            LoadCreds();
        }
        
        private void LoadCreds()
        {
            var creds = _project.DbGetColumns("login, password", "_google");
            _login = creds["login"];
        }
        
        private string MakeCookieFromDb()
        {
            var c = _project.DbGet("cookies", "_instance");
            var cookJson = c.FromBase64();
            cookJson = Cookies.ConvertCookieFormat(cookJson, "json");
            JArray toParse = JArray.Parse(cookJson);
            
            var cookiesList = new List<string>();
            
            for (int i = 0; i < toParse.Count; i++)
            {
                string domain = toParse[i]["domain"]?.ToString() ?? "";
                
                // Только Google cookies
                if (domain.Contains("google.com") || domain.Contains("youtube.com"))
                {
                    string name = toParse[i]["name"]?.ToString() ?? "";
                    string value = toParse[i]["value"]?.ToString() ?? "";
                    
                    if (!string.IsNullOrEmpty(name))
                    {
                        cookiesList.Add($"{name}={value}");
                    }
                }
            }
            
            return string.Join("; ", cookiesList) + ";";
        }
        
        /// <summary>
        /// Получить email текущего аккаунта через Google API
        /// </summary>
        public string GetCurrentEmail()
        {
            try
            {
                _idle.Sleep();
                string cookies = MakeCookieFromDb();
                
                string[] headers = new[]
                {
                    $"User-Agent: {_project.Profile.UserAgent}",
                    "Accept: application/json",
                    "Accept-Language: en-US,en;q=0.9"
                };
                
                var resp = _project.GET(
                    "https://www.googleapis.com/oauth2/v1/userinfo?alt=json",
                    "+",
                    headers,
                    cookies,
                    returnSuccessWithStatus: true
                );
                
                if (resp.StartsWith("401") || resp.StartsWith("403"))
                {
                    _log.Warn("Unauthorized - invalid cookies");
                    return "";
                }
                
                if (!resp.StartsWith("200"))
                {
                    _log.Warn($"API error: {resp.Substring(0, Math.Min(100, resp.Length))}");
                    return "";
                }
                
                int bodyStart = resp.IndexOf("\r\n\r\n");
                string body = resp.Substring(bodyStart + 4);
                
                var userInfo = body.JsonToDic();
                
                if (userInfo.ContainsKey("email"))
                {
                    string email = userInfo["email"];
                    _log.Send($"Current email: {email}");
                    return email;
                }
                
                return "";
            }
            catch (Exception ex)
            {
                _log.Warn($"Exception: {ex.Message}");
                return "";
            }
        }
        
        /// <summary>
        /// Валидация что cookies принадлежат нужному аккаунту
        /// Возвращает: "valid" | "wrong_account" | "invalid" | "error"
        /// </summary>
        public string ValidateCookiesOwnership()
        {
            try
            {
                string currentEmail = GetCurrentEmail();
                
                if (string.IsNullOrEmpty(currentEmail))
                {
                    _log.Warn("Cannot get email - cookies invalid");
                    return "invalid";
                }
                
                if (currentEmail.ToLower() != _login.ToLower())
                {
                    _log.Warn($"WRONG ACCOUNT: cookies=[{currentEmail}], DB=[{_login}]", show: true);
                    return "wrong_account";
                }
                
                _log.Send($"✓ Valid: [{currentEmail}]");
                return "valid";
            }
            catch (Exception ex)
            {
                _log.Warn($"Exception: {ex.Message}");
                return "error";
            }
        }
        
        /// <summary>
        /// Получить информацию о пользователе
        /// </summary>
        public Dictionary<string, string> GetUserInfo()
        {
            try
            {
                _idle.Sleep();
                string cookies = MakeCookieFromDb();
                
                string[] headers = new[]
                {
                    $"User-Agent: {_project.Profile.UserAgent}",
                    "Accept: application/json"
                };
                
                var resp = _project.GET(
                    "https://www.googleapis.com/oauth2/v1/userinfo?alt=json",
                    "+",
                    headers,
                    cookies
                );
                
                if (resp.StartsWith("401") || resp.StartsWith("403") || resp.StartsWith("4"))
                {
                    return new Dictionary<string, string>();
                }
                
                return resp.JsonToDic();
            }
            catch (Exception ex)
            {
                _log.Warn($"Exception: {ex.Message}");
                return new Dictionary<string, string>();
            }
        }
    }
    
    #endregion
    
    #region UI Subclass
    
    /// <summary>
    /// Google UI методы (требуют Instance)
    /// </summary>
    public class GoogleUI
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _log;
        private readonly Sleeper _idle;
        private readonly GoogleAPI _api;
        
        private string _login;
        private string _pass;
        private string _2fa;
        
        internal GoogleUI(IZennoPosterProjectModel project, Instance instance, Logger log, GoogleAPI api)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            _log = log;
            _idle = new Sleeper(1337, 2078);
            _api = api;
            
            LoadCreds();
        }
        
        private void LoadCreds()
        {
            var creds = _project.DbGetColumns("login, password, otpsecret", "_google");
            _login = creds["login"];
            _pass = creds["password"];
            _2fa = creds["otpsecret"];
        }
        
        /// <summary>
        /// Полный процесс авторизации
        /// </summary>
        public string Load(bool cookieBackup = true)
        {
            if (!_instance.ActiveTab.URL.Contains("google"))
                _instance.Go("https://myaccount.google.com/");
            
            check:
            Thread.Sleep(1000);
            string state = CheckState();
            
            _project.Var("googleSTATUS", state);
            _log.Send(state);
            
            switch (state)
            {
                case "ok":
                    if (cookieBackup) SaveCookies();
                    return state;
                    
                case "!WrongAcc":
                    _instance.CloseAllTabs();
                    _instance.ClearCookie("google.com");
                    throw new Exception(state);
                    
                case "inputLogin":
                    _instance.HeSet(("identifierId", "id"), _login);
                    _instance.HeClick(("button", "innertext", "Next", "regexp", 0));
                    goto check;
                    
                case "inputPassword":
                    _instance.HeSet(("Passwd", "name"), _pass, deadline: 5);
                    _instance.HeClick(("button", "innertext", "Next", "regexp", 0));
                    goto check;
                    
                case "inputOtp":
                    _instance.HeSet(("totpPin", "id"), OTP.Offline(_2fa));
                    _instance.HeClick(("button", "innertext", "Next", "regexp", 0));
                    goto check;
                    
                case "addRecoveryPhone":
                    _instance.HeClick(("button", "innertext", "Cancel", "regexp", 0));
                    _instance.HeClick(("button", "innertext", "Skip", "regexp", 0), deadline: 5, thr0w: false);
                    goto check;
                    
                case "setHome":
                    _instance.HeClick(("button", "innertext", "Skip", "regexp", 0));
                    goto check;
                    
                case "signInAgain":
                    _instance.ClearShit("google.com");
                    Thread.Sleep(3000);
                    _instance.Go("https://myaccount.google.com/");
                    goto check;
                    
                case "CAPTCHA":
                    throw new Exception("gCAPTCHA");
                    
                case "phoneVerify":
                case "badBrowser":
                    _project.DbUpd($"status = '{state}'", "_google");
                    throw new Exception(state);
                    
                default:
                    return state;
            }
        }
        
        /// <summary>
        /// Проверка текущего состояния авторизации
        /// </summary>
        private string CheckState()
        {
            check:
            var state = "";
            
            if (!_instance.ActiveTab.FindElementByAttribute("a", "href", "https://accounts.google.com/SignOutOptions\\?", "regexp", 0).IsVoid)
                state = "signedIn";
            else if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Confirm you're not a robot')]", 0).IsVoid)
                state = "CAPTCHA";
            else if (!_instance.ActiveTab.FindElementByAttribute("div", "innertext", "Enter\\ a\\ phone\\ number\\ to\\ get\\ a\\ text\\ message\\ with\\ a\\ verification\\ code.", "regexp", 0).IsVoid)
                state = "PhoneVerify";
            else if (!_instance.ActiveTab.FindElementByAttribute("div", "innertext", "Try\\ using\\ a\\ different\\ browser.", "regexp", 0).IsVoid)
                state = "BadBrowser";
            else if ((!_instance.ActiveTab.FindElementByAttribute("input:email", "fulltagname", "input:email", "text", 0).IsVoid) &&
                    (_instance.ActiveTab.FindElementByAttribute("input:email", "fulltagname", "input:email", "text", 0).GetAttribute("value") == ""))
                state = "inputLogin";
            else if ((!_instance.ActiveTab.FindElementByAttribute("input:password", "fulltagname", "input:password", "text", 0).IsVoid) &&
                    _instance.ActiveTab.FindElementByAttribute("input:password", "fulltagname", "input:password", "text", 0).GetAttribute("value") == "")
                state = "inputPassword";
            else if ((!_instance.ActiveTab.FindElementById("totpPin").IsVoid) &&
                    _instance.ActiveTab.FindElementById("totpPin").GetAttribute("value") == "")
                state = "inputOtp";
            else if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Add a recovery phone')]", 0).IsVoid)
                state = "addRecoveryPhone";
            else if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Set a home address')]", 0).IsVoid)
                state = "setHome";
            else if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Your account has been disabled')]", 0).IsVoid)
                state = "Disabled";
            else if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Google needs to verify it's you. Please sign in again to continue')]", 0).IsVoid)
                state = "signInAgain";
            else
                state = "undefined";
            
            switch (state)
            {
                case "signedIn":
                    var currentAcc = _instance.HeGet(("a", "href", "https://accounts.google.com/SignOutOptions\\?", "regexp", 0), atr: "aria-label").Split('\n')[1];
                    if (currentAcc.ToLower().Contains(_login.ToLower()))
                    {
                        _log.Send($"{currentAcc} is Correct. Login done");
                        state = "ok";
                    }
                    else
                    {
                        _log.Send($"!W {currentAcc} is InCorrect. MustBe {_login}");
                        state = "!WrongAcc";
                    }
                    break;
                    
                case "undefined":
                    _instance.HeClick(("a", "class", "h-c-header__cta-li-link\\ h-c-header__cta-li-link--primary\\ button-standard-mobile", "regexp", 1), deadline: 1, thr0w: false);
                    goto check;
                    
                default:
                    break;
            }
            
            _project.Var("googleSTATUS", state);
            return state;
        }
        
        /// <summary>
        /// Сохранить cookies в БД
        /// </summary>
        public void SaveCookies()
        {
            _instance.Go("youtube.com");
            if (_instance.ActiveTab.IsBusy) _instance.ActiveTab.WaitDownloading();
            _instance.Go("https://myaccount.google.com/");
            
            _project.SaveDomainCookies(_instance, tableName: "_google");
            _log.Send("Cookies saved to DB");
        }
        
        /// <summary>
        /// OAuth авторизация на стороннем сайте
        /// </summary>
        public string GAuth()
        {
            try
            {
                var userContainer = _instance.HeGet(("div", "data-authuser", "0", "regexp", 0));
                _log.Send($"container:{userContainer} catched");
                
                if (userContainer.IndexOf(_login, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _log.Send($"correct user found: {_login}");
                    _instance.HeClick(("div", "data-authuser", "0", "regexp", 0), delay: 3);
                    Thread.Sleep(5000);
                    
                    if (!_instance.ActiveTab.FindElementByAttribute("div", "data-authuser", "0", "regexp", 0).IsVoid)
                    {
                        while (true) _instance.HeClick(("div", "data-authuser", "0", "regexp", 0), "clickOut", deadline: 5, delay: 3);
                    }
                    
                    Continue:
                    try
                    {
                        _instance.HeClick(("button", "innertext", "Continue", "regexp", 0), deadline: 5, delay: 1);
                        return "SUCCESS with continue";
                    }
                    catch
                    {
                        try
                        {
                            _instance.HeSet(("totpPin", "id"), OTP.Offline(_2fa), deadline: 1);
                            _instance.HeClick(("button", "innertext", "Next", "regexp", 0));
                            goto Continue;
                        }
                        catch { }
                        return "SUCCESS. without confirmation";
                    }
                }
                else
                {
                    _log.Send($"!Wrong account [{userContainer}]. Expected: {_login}. Cleaning");
                    _instance.CloseAllTabs();
                    _instance.ClearCookie("google.com");
                    return "FAIL. Wrong account";
                }
            }
            catch
            {
                return "FAIL. No loggined Users Found";
            }
        }
    }
    
    #endregion
    
    #region Batch Operations
    
    public static class GoogleBatch
    {
        /// <summary>
        /// Валидация cookies для всех Google аккаунтов
        /// </summary>
        public static void ValidateAllCookies(IZennoPosterProjectModel project, string tableName = "_google")
        {
            int startRange = project.Int("rangeStart");
            int endRange = project.Int("rangeEnd");
            
            project.log("=== Validating Google cookies ownership ===");
            
            for (int i = startRange; i <= endRange; i++)
            {
                project.Var("acc0", i);
                Thread.Sleep(1000);
                
                var login = project.DbGet("login", tableName);
                
                if (string.IsNullOrEmpty(login))
                {
                    project.CleanDomainInDb("google.com");
                    project.CleanDomainInDb("youtube.com");
                    continue;
                }
                
                if (string.IsNullOrEmpty(project.DbGet("cookies", "_instance")))
                {
                    project.log($"[{login}] No cookies, skip");
                    continue;
                }
                
                try
                {
                    var google = new _Google(project, log: false);
                    string result = google.API.ValidateCookiesOwnership();
                    
                    switch (result)
                    {
                        case "valid":
                            project.log($"✓ [{login}] Valid");
                            var data = google.API.GetUserInfo();
                            project.DicToDb(data, tableName);
                            break;
                            
                        case "wrong_account":
                            project.warn($"✗ [{login}] WRONG ACCOUNT - clearing cookies");
                            project.CleanDomainInDb("google.com");
                            project.CleanDomainInDb("youtube.com");
                            project.DbUpd("status = 'WrongCookies'", tableName);
                            break;
                            
                        case "invalid":
                            project.warn($"✗ [{login}] Invalid cookies");
                            project.CleanDomainInDb("google.com");
                            project.CleanDomainInDb("youtube.com");
                            project.DbUpd("status = 'InvalidCookies'", tableName);
                            break;
                            
                        case "error":
                            project.warn($"✗ [{login}] Validation error");
                            project.DbUpd("status = 'ValidationError'", tableName);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    project.warn($"[{login}] Exception: {ex.Message}");
                }
            }
        }
    }
    
    #endregion
}