
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using z3nCore.Utilities;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.Enums.Browser;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
   public class InstanceManager
    {
        #region Fields & Constructor

        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;
        private readonly bool _log;

        public InstanceManager(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            _log = log;
            _logger = new Logger(project, log: _log, classEmoji: "🔧");
        }

        #endregion

        #region Init - Profile Setup
        


        public void Initialize(string browserToLaunch = null, bool fixTimezone = false, bool useLegacy = true)
        {
            try
            {
                LaunchBrowser(browserToLaunch);
            }
            catch (Exception e)
            {
                _logger.Warn(e.Message);
                throw;
            }
            
            int exCnt = 0;
            string browserType = _instance.BrowserType.ToString();
            bool browser = browserType == "Chromium";
            if (useLegacy)
            {
                SetInstance:
                try 
                {
                    if (browser && _project.Variables["acc0"].Value != "")
                        SetBrowser(fixTimezone: fixTimezone);	
                    else
                    {
                        ProxySet();
                    }
                }
                catch (Exception ex)
                {
                    _instance.CloseAllTabs();
                    exCnt++;
                    _logger.Warn($"SetInstance failed: attempt={exCnt}/3, acc={_project.Variables["acc0"].Value}, error={ex.Message}");
                    if (exCnt > 3)
                    {
                        _project.GVar($"acc{_project.Variables["acc0"].Value}", "");
                        throw;
                    }
                    goto SetInstance;
                }
                _instance.CloseExtraTabs(true);
                return;
            }
            _SetBrowser(fixTimezone: fixTimezone);	
            
           
        }

        private string LaunchBrowser(string cfgBrowser = null, bool useZpprofile = true, bool useFolder = true)
        {
            string acc0 = _project.Var("acc0");
            if (string.IsNullOrEmpty(acc0)) 
                throw new ArgumentException("acc0 can't be null or empty");
            
            var pathToProfileFolder = _project.PathProfileFolder();
            var pathToZpprofile = pathToProfileFolder + ".zpprofile";
            
            _logger.Send($"Profile path: {pathToProfileFolder}, exists: {Directory.Exists(pathToProfileFolder)}");
            

            if (useZpprofile && File.Exists(pathToZpprofile))
            {
                
                _logger.Send($"Profile path: {pathToZpprofile}, exists: {File.Exists(pathToZpprofile)}");
                _project.Profile.Load(pathToZpprofile, true);

            }

            if (Directory.Exists(pathToProfileFolder) && useFolder)
            {
                var size = new DirectoryInfo(pathToProfileFolder).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
                _logger.Send($"Profile size: {size / 1024 / 1024}MB");
            }
            
            _project.Var("pathProfileFolder", pathToProfileFolder);
            
            
            if (cfgBrowser == null) cfgBrowser = _project.Var("cfgBrowser");
            
            
            var browser = BrowserType.Chromium;

            switch (cfgBrowser)
            {
                case "":
                case "WithoutBrowser":
                    browser = BrowserType.WithoutBrowser;
                    break;
                case "Chromium":
                    break;	
                default:
                    _logger.Warn($"unknown browser config {cfgBrowser}");
                    throw new Exception($"unknown browser config {cfgBrowser}");
            }
            
            int pid = 0;
            int port = 0;

            if (cfgBrowser == "WithoutBrowser"|| cfgBrowser == "")
            {
                _instance.Launch(BrowserType.WithoutBrowser, true);
            }
            else if (cfgBrowser == "Chromium")
            {
                var pidsBeforeLaunch = Utilities.ProcAcc.GetPidSnapshot();
                if (useFolder)
                {
                    _instance.UpFromFolder(pathToProfileFolder);
                }
                else
                {
                    _instance.Launch(BrowserType.Chromium, true);
                }
                pid = Utilities.ProcAcc.GetNewlyLaunchedPid(acc0, pidsBeforeLaunch);
                if (pid == 0)
                {
                    _logger.Send("PID search fallback: fast method failed, using slow search");
                    pid = Utilities.ProcAcc.GetNewest(acc0);
                }
                port = _instance.Port;
            }
            _project.Variables["instancePort"].Value = $"port: {port}, pid: {pid}";
            _logger.Send($"Browser launched: type={cfgBrowser}, port={port}, pid={pid}, acc={acc0}");
            BindPid(pid, port);
            return pid.ToString();
        }

        #region Obsolete
        private void SetDisplay(string webGl)
        {
            if (!string.IsNullOrEmpty(webGl))
            {
                var jsonObject = JObject.Parse(webGl);
                var mapping = new Dictionary<string, string>
                {
                    {"Renderer", "RENDERER"},
                    {"Vendor", "VENDOR"},
                    {"Version", "VERSION"},
                    {"ShadingLanguageVersion", "SHADING_LANGUAGE_VERSION"},
                    {"UnmaskedRenderer", "UNMASKED_RENDERER_WEBGL"},
                    {"UnmaskedVendor", "UNMASKED_VENDOR"},
                    {"MaxCombinedTextureImageUnits", "MAX_COMBINED_TEXTURE_IMAGE_UNITS"},
                    {"MaxCubeMapTextureSize", "MAX_CUBE_MAP_TEXTURE_SIZE"},
                    {"MaxFragmentUniformVectors", "MAX_FRAGMENT_UNIFORM_VECTORS"},
                    {"MaxTextureSize", "MAX_TEXTURE_SIZE"},
                    {"MaxVertexAttribs", "MAX_VERTEX_ATTRIBS"}
                };

                foreach (var pair in mapping)
                {
                    string value = "";
                    if (jsonObject["parameters"]["default"][pair.Value] != null) value = jsonObject["parameters"]["default"][pair.Value].ToString();
                    else if (jsonObject["parameters"]["webgl"][pair.Value] != null) value = jsonObject["parameters"]["webgl"][pair.Value].ToString();
                    else if (jsonObject["parameters"]["webgl2"][pair.Value] != null) value = jsonObject["parameters"]["webgl2"][pair.Value].ToString();
                    if (!string.IsNullOrEmpty(value)) _instance.WebGLPreferences.Set((WebGLPreference)Enum.Parse(typeof(WebGLPreference), pair.Key), value);
                }
            }
            else _logger.Send("!W WebGL string is empty. Please parse WebGL data into the database. Otherwise, any antifraud system will fuck you up like it's a piece of cake.");

            try
            {
                _instance.SetWindowSize(1280, 720);
                _project.Profile.AcceptLanguage = "en-US,en;q=0.9";
                _project.Profile.Language = "EN";
                _project.Profile.UserAgentBrowserLanguage = "en-US";
                _instance.UseMedia = false;
            }
            catch (Exception ex)
            {
                _logger.Send(ex.Message, thrw: true);
            }
        }
        private void SetBrowser(bool strictProxy = true, string cookies = null, bool fixTimezone = false)
        {
            string acc0 = _project.Var("acc0");
            if (string.IsNullOrEmpty(acc0)) throw new ArgumentException("acc0 can't be null or empty");
            
            string instanceType = "WithoutBrowser";
            try
            {
                instanceType = _instance.BrowserType.ToString();
            }
            finally { }
            
            if (instanceType == "Chromium")
            {
                string webGlData = _project.SqlGet("webgl", "_instance");
                SetDisplay(webGlData);
                
                bool goodProxy = ProxySet();
                if (strictProxy && !goodProxy) throw new Exception($"!E bad proxy");
                
                var cookiePath = _project.PathCookies();
                _project.Var("pathCookies", cookiePath);

                if (cookies != null) 
                    _instance.SetCookie(cookies);
                else
                    try
                    {
                        cookies = _project.SqlGet("cookies", "_instance");
                        _instance.SetCookie(cookies);
                    }
                    catch (Exception Ex)
                    {
                        _logger.Warn($"Cookies set failed: source=database, path={cookiePath}, error={Ex.Message}");
                        try
                        {
                            cookies = File.ReadAllText(cookiePath);
                            _instance.SetCookie(cookies);
                        }
                        catch (Exception E)
                        {
                            _logger.Warn($"Cookies set failed: source=file, path={cookiePath}, error={E.Message}");
                        }
                    }
            }
            
            if (fixTimezone)
            {
                var bs = new BrowserScan(_project, _instance);
                if (bs.GetScore().Contains("time")) bs.FixTime();
            }
        }
        #endregion
        private void _SetBrowser(bool strictProxy = true, string restoreFrom = "folder", bool fixTimezone = false)
        {
            string acc0 = _project.Var("acc0");
            if (string.IsNullOrEmpty(acc0)) throw new ArgumentException("acc0 can't be null or empty");
            var syncer = new ProfileSync(_project, _instance);
            string instanceType = "WithoutBrowser";
            try
            {
                instanceType = _instance.BrowserType.ToString();
                syncer.RestoreProfile(restoreFrom: "folder", restoreProfile: true,
                    restoreCookies: true, restoreInstance: false,
                    restoreWebgl: false, rebuildWebgl: false);
            }
            finally { }
            
            if (instanceType == "Chromium")
            {
                
                syncer.RestoreProfile(restoreFrom: "folder", restoreProfile: true,
                    restoreCookies: true, restoreInstance: true,
                    restoreWebgl: true, rebuildWebgl: false);
                DefaultSettings();

                try
                {
                    ProxySet();
                }
                catch (Exception ex)
                {
                    _project.warn(ex,strictProxy);
                }
                
            }
            
            if (fixTimezone)
            {
                var bs = new BrowserScan(_project, _instance);
                if (bs.GetScore().Contains("time")) bs.FixTime();
            }
        }

        private void DefaultSettings()
        {
            try
            {
                _instance.SetWindowSize(1280, 720);
                _instance.UseMedia = false;
            }
            catch (Exception ex)
            {
                _logger.Send(ex.Message, thrw: true);
            }
        }

        private bool ProxySet(string proxyString = null)
        {
            if (string.IsNullOrWhiteSpace(proxyString)) 
                proxyString = _project.DbGet("proxy", "_instance");
            if (string.IsNullOrWhiteSpace(proxyString))
                throw new ArgumentException("Proxy string is empty");
    
            var ipServices = new[] {
                "https://api.ipify.org/",
                "https://icanhazip.com/",
                "https://ifconfig.me/ip",
                "https://checkip.amazonaws.com/",
                "https://ident.me/"
            };
    
            string ipLocal = null;
            string ipProxified = null;
    
            foreach (var service in ipServices)
            {
                try
                {
                    ipLocal = _project.GET(service, null)?.Trim();
                    if (!string.IsNullOrEmpty(ipLocal) && System.Net.IPAddress.TryParse(ipLocal, out _))
                    {
                        ipProxified = _project.GET(service, proxyString, useNetHttp: false)?.Trim();
                        if (!string.IsNullOrEmpty(ipProxified) && System.Net.IPAddress.TryParse(ipProxified, out _))
                        {
                            break;
                        }
                    }
                }
                catch 
                {
                    continue;
                }
            }
    
            if (string.IsNullOrEmpty(ipProxified) || !System.Net.IPAddress.TryParse(ipProxified, out _))
            {
                throw new Exception($"proxy check failed: proxyString=[{proxyString}]");
            }
    
            if (ipProxified != ipLocal)
            {
                _instance.SetProxy(proxyString, true, true, true, true);
                _logger.Send($"Proxy set: ip={ipProxified}, local={ipLocal}");
                return true;
            }
            throw new Exception($"proxy check failed: proxyString=[{proxyString}]");

        }

        private void BindPid(int pid, int port)
        {
            if (pid == 0) return;
            try
            {
                string acc0 = _project.Var("acc0");
                using (var proc = Process.GetProcessById(pid))
                {
                    var memoryMb = proc.WorkingSet64 / (1024 * 1024);
                    var runtimeMinutes = (int)(DateTime.Now - proc.StartTime).TotalMinutes;
                    var name = _project.ProjectName();
                    Running.Add(pid, new List<object> { memoryMb, runtimeMinutes, port, name, acc0 });
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex.Message);
            }
        }

        #endregion

        #region Dispose - Profile Cleanup

        public void SaveProfile(bool saveCookies = true, bool saveZpProfile = true)
        {
            string acc0 = _project.Var("acc0");
            string accRnd = _project.Var("accRnd");
            
            try
            {
                bool shouldSave = _instance.BrowserType == BrowserType.Chromium &&
                                !string.IsNullOrEmpty(acc0) &&
                                string.IsNullOrEmpty(accRnd);

                if (!shouldSave)
                {
                    _logger.Send("Profile save skipped: conditions not met");
                    return;
                }

                if (saveCookies)
                {
                    string cookiesPath = _project.PathCookies();
                    _logger.Send($"Saving cookies to: '{cookiesPath}'");
                    _project.SaveAllCookies(_instance);
                    _logger.Send($"Cookies saved successfully");
                }
                
                if (saveZpProfile)
                {
                    var pathProfile = _project.PathProfileFolder();
                    _project.Profile.Save(pathProfile, true, true, true, true, true, true, true, true, true);
                    _logger.Send($"Profile saved successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Profile save failed: {ex.GetType().Name} - {ex.Message}");
            }
        }
        public void _SaveProfile(bool saveCookies = true, bool saveProfile = true, string saveTo = "folder",bool saveZpProfile = true)
        {
            string acc0 = _project.Var("acc0");
            string accRnd = _project.Var("accRnd");
    
            try
            {
                bool shouldSave = _instance.BrowserType == BrowserType.Chromium &&
                                  !string.IsNullOrEmpty(acc0) &&
                                  string.IsNullOrEmpty(accRnd);

                if (!shouldSave)
                {
                    _logger.Send("Profile save skipped: conditions not met");
                    return;
                }

                var syncer = new ProfileSync(_project, _instance);
        
                syncer.SaveProfile(
                    saveTo: saveTo,
                    saveProfile: saveProfile,
                    saveInstance: saveProfile,
                    saveCookies: saveCookies,
                    saveWebgl: saveProfile
                );
                
                if (saveZpProfile)
                {
                    var pathProfile = _project.PathProfileFolder();
                    _project.Profile.Save(pathProfile, true, true, true, true, true, true, true, true, true);
                    _logger.Send($"Profile saved successfully");
                }
        
                _logger.Send($"Profile saved via ProfileSync: saveTo={saveTo}, profile={saveProfile}, cookies={saveCookies}");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Profile save failed: {ex.GetType().Name} - {ex.Message}");
            }
        }
        public void Cleanup()
        {
            string acc0 = _project.Var("acc0");
            _logger.Send($"Starting instance cleanup: acc0='{acc0}'");
            
            try
            {
                if (!string.IsNullOrEmpty(acc0))
                {
                    _logger.Send($"Clearing global variable 'acc{acc0}'");
                    _project.GVar($"acc{acc0}", string.Empty);
                }

                _logger.Send("Clearing local variable 'acc0'");
                _project.Var("acc0", string.Empty);

                _logger.Send("Stopping instance");
                _instance.Stop();
                
                _logger.Send("Instance cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.Send($"Cleanup failed: {ex.GetType().Name} - {ex.Message}");
                
                try
                {
                    _logger.Send("Attempting emergency instance stop");
                    _instance.Stop();
                }
                catch (Exception stopEx)
                {
                    _logger.Send($"Emergency stop failed: {stopEx.GetType().Name} - {stopEx.Message}");
                }
            }
        }

        #endregion
    }
}