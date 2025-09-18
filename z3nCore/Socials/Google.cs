using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class Google
    {
        protected readonly IZennoPosterProjectModel _project;
        protected readonly Instance _instance;
        private readonly Logger _logger;

        protected readonly bool _logShow;


        protected string _status;
        protected string _login;
        protected string _pass;
        protected string _2fa;
        protected string _recoveryMail;
        protected string _recoveryCodes;
        protected string _cookies;
        protected bool _cookRestored = false;

        public Google(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {

            _project = project;
            _instance = instance;

            _logger = new Logger(project, log: log, classEmoji: "G");
            DbCreds();

        }
        private void DbCreds()
        {
            string[] creds = _project.SqlGet("status, login, password, otpsecret, recoveryemail, otpbackup, cookies", "_google").Split('|');
            try { _status = creds[0]; _project.Variables["googleSTATUS"].Value = _status; } catch (Exception ex) { _logger.Send(ex.Message); }
            try { _login = creds[1]; _project.Variables["googleLOGIN"].Value = _login; } catch (Exception ex) { _logger.Send(ex.Message); }
            try { _pass = creds[2]; _project.Variables["googlePASSWORD"].Value = _pass; } catch (Exception ex) { _logger.Send(ex.Message); }
            try { _2fa = creds[3]; _project.Variables["google2FACODE"].Value = _2fa; } catch (Exception ex) { _logger.Send(ex.Message); }
            try { _recoveryMail = creds[4]; _project.Variables["googleSECURITY_MAIL"].Value = _recoveryMail; } catch (Exception ex) { _logger.Send(ex.Message); }
            try { _recoveryCodes = creds[5]; _project.Variables["googleBACKUP_CODES"].Value = _recoveryCodes; } catch (Exception ex) { _logger.Send(ex.Message); }
            try { _cookies = creds[6]; _project.Variables["googleCOOKIES"].Value = _cookies; } catch (Exception ex) { _logger.Send(ex.Message); }
        }
        public string Load(bool log = false, bool cookieBackup = true)
        {
            if (!_instance.ActiveTab.URL.Contains("google")) _instance.Go("https://myaccount.google.com/");
            check:
            Thread.Sleep(1000);
            string state = State();

            _project.Var("googleSTATUS", state);

            _logger.Send(state);
            switch (state)
            {
                case "ok":
                    if (cookieBackup) SaveCookies();
                    return state;

                case "!WrongAcc":

                    _instance.CloseAllTabs();
                    _instance.ClearCookie("google.com");
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
        public string State(bool log = false)
        {
        check:
            var state = "";

            if (!_instance.ActiveTab.FindElementByAttribute("a", "href", "https://accounts.google.com/SignOutOptions\\?", "regexp", 0).IsVoid)
                state = "signedIn";

            else if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Confirm you’re not a robot')]", 0).IsVoid)
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

            else if (!_instance.ActiveTab.FindElementByXPath("//*[contains(text(), 'Google needs to verify it’s you. Please sign in again to continue')]", 0).IsVoid)
                state = "signInAgain";

            else state = "undefined";

            //_logger.Send(state);

            switch (state)
            {
                case "signedIn":
                    var currentAcc = _instance.HeGet(("a", "href", "https://accounts.google.com/SignOutOptions\\?", "regexp", 0), atr: "aria-label").Split('\n')[1];
                    if (currentAcc.ToLower().Contains(_login.ToLower()))
                    {
                        _logger.Send($"{currentAcc} is Correct. Login done");
                        state = "ok";
                    }

                    else
                    {
                        _logger.Send($"!W {currentAcc} is InCorrect. MustBe {_login}");
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
        public string GAuth(bool log = false)
        {
            try
            {
                var userContainer = _instance.HeGet(("div", "data-authuser", "0", "regexp", 0));
                _logger.Send($"container:{userContainer} catched");
                if (userContainer.IndexOf(_login, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _logger.Send($"correct user found: {_login}");
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
                        catch  { }
                        return "SUCCESS. without confirmation";
                    }
                }
                else
                {
                    _logger.Send($"!Wrong account [{userContainer}]. Expected: {_login}. Cleaning");
                    _instance.CloseAllTabs();
                    _instance.ClearCookie("google.com");
                    _instance.ClearCookie("google.com");
                    return "FAIL. Wrong account";
                }
            }
            catch
            {
                return "FAIL. No loggined Users Found";
            }
        }
        public void SaveCookies()
        {
            _instance.Go("youtube.com");
            if (_instance.ActiveTab.IsBusy) _instance.ActiveTab.WaitDownloading();
            //Thread.Sleep(5000);
            _instance.Go("https://myaccount.google.com/");
            string gCookies = new Cookies(_project, _instance).Get(".");
            _project.DbUpd($"status = 'ok', cookies = '{gCookies}'", "_google");
        }
        public void ParseSecurity()
        {
            if (!_instance.ActiveTab.URL.Contains("https://myaccount.google.com/security"))
                _instance.HeClick(("a", "href", "https://myaccount.google.com/security", "regexp", 1));

            var status2fa = _instance.HeGet(("a", "aria-label", "2-Step Verification", "text", 0), "last").Split('\n');
            var statusPassword = _instance.HeGet(("a", "aria-label", "Password", "text", 0), "last").Split('\n');
            var statusSkipPassword = _instance.HeGet(("a", "aria-label", "Skip password when possible", "text", 0), "last").Split('\n');
            var statusAuthenticator = _instance.HeGet(("a", "aria-label", "Authenticator", "text", 0), "last").Split('\n');
            var statusRecoveryPhone = _instance.HeGet(("a", "aria-label", "Recovery phone", "text", 0), "last").Split('\n');
            var statusRecoveryEmail = _instance.HeGet(("a", "aria-label", "Recovery email", "text", 0), "last").Split('\n');
            var statusBackupCodes = _instance.HeGet(("a", "aria-label", "Backup codes", "text", 0), "last").Split('\n');

            string todb = $@"{status2fa[0]} [{status2fa[1]}]
	            {statusPassword[0]} [{statusPassword[1]}]
	            {statusSkipPassword[0]} [{statusSkipPassword[1]}]   
	            {statusAuthenticator[0]} [{statusAuthenticator[1]}]
	            {statusRecoveryPhone[0]} [{statusRecoveryPhone[1]}]
	            {statusRecoveryEmail[0]} [{statusRecoveryEmail[1]}]
	            {statusBackupCodes[0]} [{statusBackupCodes[1]}]";


            _project.DbUpd($"security = '{todb}'", "__google");

        }

    }
}
