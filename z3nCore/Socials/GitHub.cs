using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class GitHub
    {
        protected readonly IZennoPosterProjectModel _project;
        protected readonly Instance _instance;
        private readonly Logger _logger;

        protected readonly bool _logShow;


        protected string _status;
        protected string _login;
        protected string _pass;
        protected string _2fa;
        protected string _mail;

        public GitHub(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {

            _project = project;
            _instance = instance;

            _logger = new Logger(project, log: log, classEmoji: "GITHUB");
            LoadCreds();

        }
        public void LoadCreds()
        {
            string[] creds = _project.SqlGet("status, login, password,  otpsecret, email, cookies", "_github").Split('|');
            try { _status = creds[0].Trim(); _project.Variables["github_status"].Value = _status; } catch (Exception ex) { _logger.Send(ex.Message); }
            try { _login = creds[1].Trim(); _project.Variables["github_login"].Value = _login; } catch (Exception ex) { _logger.Send(ex.Message); }
            try { _pass = creds[2].Trim(); _project.Variables["github_pass"].Value = _pass; } catch (Exception ex) { _logger.Send(ex.Message); }
            try { _2fa = creds[3].Trim(); _project.Variables["github_code"].Value = _2fa; } catch (Exception ex) { _logger.Send(ex.Message); }
            try { _mail = creds[4].Trim(); _project.Variables["github_mail"].Value = _mail; } catch (Exception ex) { _logger.Send(ex.Message); }

            if (string.IsNullOrEmpty(_login) || string.IsNullOrEmpty(_pass))
                throw new Exception($"invalid credentials login:[{_login}] pass:[{_pass}]");
        }

        public void InputCreds()
        {
            string allert = null;
            _instance.HeSet(("login_field", "id"), _mail);
            _instance.HeSet(("password", "id"), _pass);
            _instance.HeClick(("input:submit", "name", "commit", "regexp", 0), emu: 1);
            allert = _instance.HeGet(("div", "class", "js-flash-alert", "regexp", 0), thr0w: false);
            if (allert != null) throw new Exception(allert);
            try { _instance.HeSet(("app_totp", "id"), OTP.Offline(_2fa)); } catch { }
        }

        public void Go()
        {
            //Tab tab = _instance.NewTab("github");
            //if (tab.IsBusy) tab.WaitDownloading();
            _instance.Go("https://github.com/login");
            _instance.HeClick(("button", "innertext", "Accept", "regexp", 0), deadline: 2, thr0w: false);
        }
        public void Verify2fa()
        {
            _instance.HeClick(("button", "innertext", "Verify\\ 2FA\\ now", "regexp", 0), deadline: 3);
            Thread.Sleep(20000);
            _instance.HeSet(("app_totp", "id"), OTP.Offline(_2fa));
            _instance.HeClick(("button", "class", "btn-primary\\ btn\\ btn-block", "regexp", 0), emu: 1);
            _instance.HeClick(("a", "innertext", "Done", "regexp", 0), emu: 1);

        }
        public string Load()
        {
            _project.Deadline();
            Go();
        check:
            _project.Deadline(60);
            string current = string.Empty;

            try { current = Current(); }
            catch (Exception ex) { _logger.Send(ex.Message); }

            if (string.IsNullOrEmpty(current)) InputCreds();

            try { Verify2fa(); } catch { }

            if (string.IsNullOrEmpty(current))
                goto check;

            if (!current.ToLower().Contains(_login.ToLower()))
            {
                _instance.CloseAllTabs();
                _instance.ClearCookie("github.com");
                _instance.ClearCache("github.com");
                throw new Exception($"!Wrong acc: [{current}]. Expected: [{_login}]");
            }
            SaveCookies();
            return current;

        }
        public string Current()
        {
            _instance.HeClick(("img", "class", "avatar\\ circle", "regexp", 0), deadline: 3, delay: 2);
            string current = _instance.HeGet(("div", "aria-label", "User navigation", "text", 0));
            _instance.HeClick(("a", "class", "AppHeader-logo\\ ml-1\\ ", "regexp", 0));
            return current;
        }
        public void ChangePass(string password = null)
        {
            _instance.HeClick(("forgot-password", "id"));
            _instance.HeSet(("email_field", "id"), _mail);
            _project.Deadline();
            int i = 0;

        cap:
            _project.Deadline(108);

            _project.CapGuru();
            _instance.HeClick(("commit", "name"));

            try
            {
                _instance.HeGet(("p", "innertext", "Check\\ your\\ email\\ for\\ a\\ link", "regexp", 0));
            }
            catch
            {
                _project.L0g($"!W captcha notSolved. iteration {i}");
                goto cap;
            }

            Thread.Sleep(8000);

            string resetUrl = new FirstMail(_project, true).GetLink(_mail);
            _instance.ActiveTab.Navigate(resetUrl, "");
            try { _instance.HeSet(("otp", "id"), OTP.Offline(_project.Var("github_code"))); } catch { }

            _instance.HeSet(("password", "id"), _project.Variables["github_pass"].Value);
            _instance.HeSet(("password_confirmation", "id"), _pass);

            _instance.HeClick(("commit", "name"));


        }
        public void SaveCookies()
        {
            string gCookies = new Cookies(_project, _instance).Get(".");
            _project.DbUpd($"cookies = '{gCookies}'", "projects_github");
        }
    }
}
