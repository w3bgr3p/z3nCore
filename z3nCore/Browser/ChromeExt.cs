using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;


namespace z3nCore
{
    public class Extension
    {

        protected readonly IZennoPosterProjectModel _project;
        protected readonly Instance _instance;
        private readonly Logger _logger;
        
        private const string EXT_ID = "pbgjpgbpljobkekbhnnmlikbbfhbhmem";
        private const string CRX = "One-Click-Extensions-Manager.crx";

        private const string URL_STORE = "https://chromewebstore.google.com/detail/one-click-extensions-mana/pbgjpgbpljobkekbhnnmlikbbfhbhmem";
        private const string URL_POPUP = "chrome-extension://pbgjpgbpljobkekbhnnmlikbbfhbhmem/index.html";
        public Extension(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project;
            
            _logger = new Logger(project,log);
            _logger.Send("Ext initialized (without instance)");
        }
        public Extension(IZennoPosterProjectModel project, Instance instance,  bool log = false)
        {
            _project = project;
            _instance = instance;
            _logger = new Logger(project,log);
            _logger.Send("Ext initialized (with instance)");
        }
        public string GetVer(string extId)
        {
            _logger.Send($"GetVer started for extId: {extId}");
            string securePrefsPath = _project.Variables["pathProfileFolder"].Value + @"\Default\Secure Preferences";
            _logger.Send($"Secure Preferences path: {securePrefsPath}");
            
            string json = File.ReadAllText(securePrefsPath);
            _logger.Send("Secure Preferences file read successfully");
            
            JObject jObj = JObject.Parse(json);
            JObject settings = (JObject)jObj["extensions"]?["settings"];

            if (settings == null)
            {
                _logger.Send("ERROR: extensions.settings section not found");
                throw new Exception("Секция extensions.settings не найдена");
            }
            _logger.Send("extensions.settings section found");

            JObject extData = (JObject)settings[extId];
            if (extData == null)
            {
                _logger.Send($"ERROR: Extension with ID {extId} not found");
                throw new Exception($"Расширение с ID {extId} не найдено");
            }
            _logger.Send($"Extension data found for {extId}");

            string version = (string)extData["manifest"]?["version"];
            if (string.IsNullOrEmpty(version))
            {
                _logger.Send($"ERROR: Version not found for extension {extId}");
                throw new Exception($"Версия для расширения {extId} не найдена");
            }
            _logger.Send($"GetVer completed. Version: {version}");
            return version;
        }
        public bool InstallFromStore(string url, bool log = false)
        {
            _logger.Send($"InstallFromStore: {url}");
            _instance.ActiveTab.Navigate(url, "");
            if (_instance.ActiveTab.IsBusy) _instance.ActiveTab.WaitDownloading();
            try
            {
                _instance.HeGet(("button", "innertext", "Remove\\ from\\ Chrome", "regexp", 0), deadline:2);
                _logger.Send("already installed");
                try {
                    _instance.HeClick(("button", "innertext", "Enable\\ now", "regexp", 0),deadline:0);
                    _logger.Send("enabled");
                }
                catch
                {
                    
                }

                return false;
            }
            catch
            {
                
            }
            _instance.HeClick(("button", "innertext", "Add\\ to\\ Chrome", "regexp", 0));
            Thread.Sleep(1000);
            ZennoLab.Emulation.Emulator.SendKey(_instance.ActiveTab.Handle, System.Windows.Forms.Keys.Tab, ZennoLab.Emulation.KeyboardEvent.Down);
            Thread.Sleep(1000);
            ZennoLab.Emulation.Emulator.SendKey(_instance.ActiveTab.Handle, System.Windows.Forms.Keys.Enter, ZennoLab.Emulation.KeyboardEvent.Down);
            return true;
        }
        public bool InstallFromCrx(string extId, string fileName, bool log = false)
        {
            _logger.Send($"InstallFromCrx started for extId: {extId}, fileName: {fileName}");
            string path = $"{_project.Path}.crx\\{fileName}";
            _logger.Send($"CRX path: {path}");

            if (!File.Exists(path))
            {
                _logger.Send($"ERROR: File not found: {path}");
                throw new FileNotFoundException($"CRX file not found: {path}");
            }
            _logger.Send("CRX file exists");

            var extListString = string.Join("\n", _instance.GetAllExtensions().Select(x => $"{x.Name}:{x.Id}"));
            _logger.Send($"Current extensions list:\n{extListString}");
            
            if (!extListString.Contains(extId))
            {
                _logger.Send($"Extension {extId} not found, installing from {path}");
                _instance.InstallCrxExtension(path);
                _logger.Send($"Extension {extId} installed successfully");
                return true;
            }
            _logger.Send($"Extension {extId} already installed, skipping");
            return false;
        }
        public bool Switch( string toUse = "", bool log = false)
        {
            _logger.Send($"Switch started. Extensions to use: {toUse}");
            bool switched = false;
            
            _logger.Send($"Browser type is {_instance.BrowserType.ToString()}");
            if (_instance.BrowserType.ToString() == "Chromium")
            {
                InstallFromCrx(EXT_ID, CRX, log);
            }
            if (_instance.BrowserType.ToString() == "ChromiumFromZB")
            {
                InstallFromStore(URL_STORE,log);
            }

            if (_instance.BrowserType.ToString() == "Chromium" || _instance.BrowserType.ToString() == "ChromiumFromZB")               
            {
                var em = _instance.UseFullMouseEmulation;
                _logger.Send($"Current mouse emulation setting: {em}");

                int i = 0; string extStatus = "enabled";

                _logger.Send("Navigating to extension manager page");
                while (_instance.ActiveTab.URL != URL_POPUP)
                {
                    _instance.ActiveTab.Navigate(URL_POPUP, "");
                    _instance.CloseExtraTabs();
                    _logger.Send($"Current URL: {_instance.ActiveTab.URL}");
                }

                while (!_instance.ActiveTab.FindElementByAttribute("button", "class", "ext-name", "regexp", i).IsVoid)
                {
                    string extName = Regex.Replace(_instance.ActiveTab.FindElementByAttribute("button", "class", "ext-name", "regexp", i).GetAttribute("innertext"), @" Wallet", "");
                    string outerHtml = _instance.ActiveTab.FindElementByAttribute("li", "class", "ext\\ type-normal", "regexp", i).GetAttribute("outerhtml");
                    string extId = Regex.Match(outerHtml, @"extension-icon/([a-z0-9]+)").Groups[1].Value;
                    if (outerHtml.Contains("disabled")) extStatus = "disabled";
                    
                    _logger.Send($"Extension [{i}]: Name={extName}, ID={extId}, Status={extStatus}");
                    
                    if ((toUse.Contains(extName) || toUse.Contains(extId)) && extStatus == "disabled")
                    {
                        _instance.HeClick(("button", "class", "ext-name", "regexp", i));
                        switched = true;
                        _logger.Send($"Extension {extName} enabled");
                    }
                    
                    if ((toUse.Contains(extName) || toUse.Contains(extId)) && extStatus == "enabled")
                    {
                        _logger.Send($"Extension {extName} ({extId}) is enabled");
                        switched = true;
                    }
                    
                    if ((!toUse.Contains(extName) && !toUse.Contains(extId)) && extStatus == "enabled")
                    {
                        _instance.HeClick(("button", "class", "ext-name", "regexp", i));
                        _logger.Send($"Extension {extName} disabled");
                    }
                    i++;
                }
                _instance.CloseExtraTabs();
                _instance.UseFullMouseEmulation = em;
                _logger.Send($"Switch completed. Enabled extensions: {toUse}");
            }
            else
            {
                _logger.Send($"Browser type is not Chromium: {_instance.BrowserType}");
            }
            _logger.Send($"Switch result: {switched}");
            return switched;
        }
        public void Rm(string[] ExtToRemove)
        {
            _logger.Send($"Rm started. Extensions to remove: {(ExtToRemove != null ? string.Join(", ", ExtToRemove) : "null")}");
            if (ExtToRemove != null && ExtToRemove.Length > 0)
                foreach (string ext in ExtToRemove)
                {
                    try 
                    { 
                        _logger.Send($"Attempting to uninstall extension: {ext}");
                        _instance.UninstallExtension(ext);
                        _logger.Send($"Extension {ext} uninstalled successfully");
                    } 
                    catch (Exception ex)
                    { 
                        _logger.Send($"ERROR: Failed to uninstall extension {ext}. Exception: {ex.Message}");
                    }
                }
            else
            {
                _logger.Send("No extensions to remove");
            }
            _logger.Send("Rm completed");
        }
        
    }
    
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

        public bool Switch( string toUse = "", bool log = false)
        {
            _logger.Send($"switching extentions  {toUse}");
            bool switched = false; 
            if (_instance.BrowserType.ToString() == "Chromium")
            {
               
                string fileName = $"One-Click-Extensions-Manager.crx";
                var managerId = "pbgjpgbpljobkekbhnnmlikbbfhbhmem";
                
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
                    
                    // Включение
                    if ((toUse.Contains(extName) || toUse.Contains(extId)) && extStatus == "disabled")
                    {
                        _instance.HeClick(("button", "class", "ext-name", "regexp", i));
                        switched = true;
                    }
                    // Отключение
                    if ((!toUse.Contains(extName) && !toUse.Contains(extId)) && extStatus == "enabled")
                    {
                        _instance.HeClick(("button", "class", "ext-name", "regexp", i));
                    }
                    i++;
                }
                _instance.CloseExtraTabs();
                _instance.UseFullMouseEmulation = em;
                _logger.Send($"Enabled  {toUse}");
            }
            return switched;
        }
        public void Rm(string[] ExtToRemove)
        {
            if (ExtToRemove != null && ExtToRemove.Length > 0)
                foreach (string ext in ExtToRemove)
                    try { _instance.UninstallExtension(ext); } catch { }
        }
        
    }
}
