using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using static System.Net.Mime.MediaTypeNames;


namespace z3nCore
{
    public class ChromeExt
    {

        protected readonly IZennoPosterProjectModel _project;
        protected bool _logShow = false;
        protected readonly Instance _instance;
        private readonly Logger _logger;

        public ChromeExt(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project;
            if (!log) _logShow = _project.Var("debug") == "True";
            _logger = new Logger(project);

        }
        public ChromeExt(IZennoPosterProjectModel project, Instance instance,  bool log = false)
        {
            _project = project;
            _instance = instance;
            if (!log) _logShow = _project.Var("debug") == "True";
            _logger = new Logger(project);

        }

        public string GetVer(string extId)
        {
            string securePrefsPath = _project.Variables["pathProfileFolder"].Value + @"\Default\Secure Preferences";
            string json = File.ReadAllText(securePrefsPath);
            JObject jObj = JObject.Parse(json);
            JObject settings = (JObject)jObj["extensions"]?["settings"];

            if (settings == null)
            {
                throw new Exception("Секция extensions.settings не найдена");
            }

            JObject extData = (JObject)settings[extId];
            if (extData == null)
            {
                throw new Exception($"Расширение с ID {extId} не найдено");
            }

            string version = (string)extData["manifest"]?["version"];
            if (string.IsNullOrEmpty(version))
            {
                throw new Exception($"Версия для расширения {extId} не найдена");
            }
            return version;
        }

        public bool Install(string extId, string fileName, bool log = false)
        {
            string path = $"{_project.Path}.crx\\{fileName}";

            if (!File.Exists(path))
            {
                _logger.Send($"File not found: {path}");
                throw new FileNotFoundException($"CRX file not found: {path}");
            }

            var extListString = string.Join("\n", _instance.GetAllExtensions().Select(x => $"{x.Name}:{x.Id}"));
            if (!extListString.Contains(extId))
            {
                _logger.Send($"installing {path}");
                _instance.InstallCrxExtension(path);
                return true;
            }
            return false;
        }

        public void Switch( string toUse = "", bool log = false)
        {
            _logger.Send($"switching extentions  {toUse}");

            if (_instance.BrowserType.ToString() == "Chromium")
            {
                //var wlt = new Wlt( _project, _instance, log);
                string fileName = $"One-Click-Extensions-Manager.crx";
                var managerId = "pbgjpgbpljobkekbhnnmlikbbfhbhmem";
                //Install(_project, managerId, fileName);
                Install(managerId, fileName, log);

                var em = _instance.UseFullMouseEmulation;

                int i = 0; string extStatus = "enabled";

                while (_instance.ActiveTab.URL != "chrome-extension://pbgjpgbpljobkekbhnnmlikbbfhbhmem/index.html")
                {
                    _instance.ActiveTab.Navigate("chrome-extension://pbgjpgbpljobkekbhnnmlikbbfhbhmem/index.html", "");
                    _instance.CloseExtraTabs();
                    _logger.Send($"URL is correct {_instance.ActiveTab.URL}");
                }

                while (!_instance.ActiveTab.FindElementByAttribute("button", "class", "ext-name", "regexp", i).IsVoid)
                {
                    string extName = Regex.Replace(_instance.ActiveTab.FindElementByAttribute("button", "class", "ext-name", "regexp", i).GetAttribute("innertext"), @" Wallet", "");
                    string outerHtml = _instance.ActiveTab.FindElementByAttribute("li", "class", "ext\\ type-normal", "regexp", i).GetAttribute("outerhtml");
                    string extId = Regex.Match(outerHtml, @"extension-icon/([a-z0-9]+)").Groups[1].Value;
                    if (outerHtml.Contains("disabled")) extStatus = "disabled";
                    if (toUse.Contains(extName) && extStatus == "disabled" || toUse.Contains(extId) && extStatus == "disabled" || !toUse.Contains(extName) && !toUse.Contains(extId) && extStatus == "enabled")
                        _instance.HeClick(("button", "class", "ext-name", "regexp", i));
                    i++;
                }

                _instance.CloseExtraTabs();
                _instance.UseFullMouseEmulation = em;
                _logger.Send($"Enabled  {toUse}");
            }

        }
        public void Rm(string[] ExtToRemove)
        {
            if (ExtToRemove != null && ExtToRemove.Length > 0)
                foreach (string ext in ExtToRemove)
                    Rm(ext);
        }
        public void Rm(string ExtToRemove)
        {
            try { _instance.UninstallExtension(ExtToRemove); } catch { }
        }
        
    }
}
