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
        


        public void Initialize(string browserToLaunch = null, bool fixTimezone = false, bool useLegacy = true, bool useZpprofile = false, bool useFolder = true)
        {
            _logger.Send($"[DIAG] Initialize START. Args: browserToLaunch='{browserToLaunch}', fixTimezone={fixTimezone}, useLegacy={useLegacy}");
            try
            {
                LaunchBrowser(browserToLaunch, useZpprofile, useFolder);
            }
            catch (Exception e)
            {
                _logger.Warn($"[DIAG] LaunchBrowser threw exception: {e.Message}");
                _logger.Warn(e.Message);
                throw;
            }
            
            int exCnt = 0;
            string browserType = _instance.BrowserType.ToString();
            _logger.Send($"[DIAG] Browser launched. _instance.BrowserType='{browserType}'");

            bool browser = browserType == "Chromium";
            _logger.Send($"[DIAG] Flag: browser={browser} (is Chromium)");

            if (useLegacy)
            {
                SetInstance:
                try 
                {
                    string acc0Val = _project.Variables["acc0"].Value;
                    _logger.Send($"[DIAG] Legacy path. Attempt={exCnt}. acc0='{acc0Val}'");

                    if (browser && acc0Val != "")
                    {
                        _logger.Send($"[DIAG] Condition met (browser && acc0!=''). Calling SetBrowser(fixTimezone: {fixTimezone})");
                        SetBrowser(fixTimezone: fixTimezone);   
                    }
                    else
                    {
                        _logger.Send($"[DIAG] Condition else. Calling ProxySet()");
                        ProxySet();
                    }
                }
                catch (Exception ex)
                {
                    _instance.CloseAllTabs();
                    exCnt++;
                    string currentAcc = _project.Variables["acc0"].Value;
                    _logger.Warn($"SetInstance failed: attempt={exCnt}/3, acc={currentAcc}, error={ex.Message}");
                    _logger.Send($"[DIAG] Exception details: {ex.ToString()}");
                    
                    if (exCnt > 3)
                    {
                        _logger.Send($"[DIAG] Max attempts reached. Clearing GVar acc{currentAcc}");
                        _project.GVar($"acc{currentAcc}", "");
                        throw;
                    }
                    goto SetInstance;
                }
                _instance.CloseExtraTabs(true);
                return;
            }

            _logger.Send($"[DIAG] Non-Legacy path. Calling _SetBrowser(fixTimezone: {fixTimezone})");
            _SetBrowser(fixTimezone: fixTimezone);  
            
           
        }

        private string LaunchBrowser(string cfgBrowser = null, bool useZpprofile = true, bool useFolder = true)
        {
            string acc0 = _project.Var("acc0");
            _logger.Send($"[DIAG] LaunchBrowser START. acc0='{acc0}', cfgBrowser='{cfgBrowser}', useZpprofile={useZpprofile}, useFolder={useFolder}");

            if (string.IsNullOrEmpty(acc0)) 
                throw new ArgumentException("acc0 can't be null or empty");
            
            var pathToProfileFolder = _project.PathProfileFolder();
            var pathToZpprofile = pathToProfileFolder + ".zpprofile";
            
            _logger.Send($"Profile path: {pathToProfileFolder}, exists: {Directory.Exists(pathToProfileFolder)}");
            _logger.Send($"[DIAG] ZPProfile path: '{pathToZpprofile}', exists: {File.Exists(pathToZpprofile)}");
            

            if (useZpprofile && File.Exists(pathToZpprofile))
            {
                
                _logger.Send($"Profile path: {pathToZpprofile}, exists: {File.Exists(pathToZpprofile)}");
                try
                {
                    _logger.Send($"[DIAG] Loading profile from '{pathToZpprofile}'");
                    _project.Profile.Load(pathToZpprofile, true);
                }
                catch (Exception ex)
                {
                    _project.warn(ex);
                    useZpprofile = false;
                }
                
            }
            else 
                useZpprofile = false;

            if (Directory.Exists(pathToProfileFolder) && useFolder)
            {
                var size = new DirectoryInfo(pathToProfileFolder).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
                _logger.Send($"Profile size: {size / 1024 / 1024}MB");
            }
            
            _project.Var("pathProfileFolder", pathToProfileFolder);
            
            
            if (cfgBrowser == null) cfgBrowser = _project.Var("cfgBrowser");
            _logger.Send($"[DIAG] Resolved cfgBrowser='{cfgBrowser}'");
            
            
            int pid = 0;
            int port = 0;

            if (cfgBrowser == "WithoutBrowser"|| cfgBrowser == "")
            {
                _logger.Send("[DIAG] Launching WithoutBrowser");
                _instance.Launch(BrowserType.WithoutBrowser, true);
            }
            else if (cfgBrowser == "Chromium")
            {
                var pidsBeforeLaunch = Utilities.ProcAcc.GetPidSnapshot();
                _logger.Send($"[DIAG] PIDs before launch count: {pidsBeforeLaunch.Count}");

                if (useFolder)
                {
                    _logger.Send($"[DIAG] Launching UpFromFolder: '{pathToProfileFolder}'");
                    _instance.UpFromFolder(pathToProfileFolder , useZpprofile);
                }
                else
                {
                    _logger.Send($"[DIAG] Launching standard Chromium");
                    _instance.Launch(BrowserType.Chromium, true);
                }
                pid = Utilities.ProcAcc.GetNewlyLaunchedPid(acc0, pidsBeforeLaunch);
                _logger.Send($"[DIAG] GetNewlyLaunchedPid result: {pid}");

                if (pid == 0)
                {
                    _logger.Send("PID search fallback: fast method failed, using slow search");
                    pid = Utilities.ProcAcc.GetNewest(acc0);
                    _logger.Send($"[DIAG] GetNewest result: {pid}");
                }
                port = _instance.Port;
            }
            _project.Variables["instancePort"].Value = $"port: {port}, pid: {pid}";
            _logger.Send($"Browser launched: type={cfgBrowser}, port={port}, pid={pid}, acc={acc0}");
            
            _logger.Send($"[DIAG] Calling BindPid({pid}, {port})");
            BindPid(pid, port);
            return pid.ToString();
        }

        #region Obsolete
        private void SetDisplay(string webGl)
        {
            _logger.Send($"[DIAG] SetDisplay. webGl length: {webGl?.Length ?? 0}");
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
            _logger.Send($"[DIAG] SetBrowser START. acc0='{acc0}', strictProxy={strictProxy}, cookies input='{cookies}', fixTimezone={fixTimezone}");

            if (string.IsNullOrEmpty(acc0)) throw new ArgumentException("acc0 can't be null or empty");
            
            string instanceType = "WithoutBrowser";
            try
            {
                instanceType = _instance.BrowserType.ToString();
            }
            finally { }
            _logger.Send($"[DIAG] Instance Type: {instanceType}");
            
            if (instanceType == "Chromium")
            {
                string webGlData = _project.SqlGet("webgl", "_instance");
                _logger.Send($"[DIAG] Fetched WebGL from SQL. Length: {webGlData?.Length ?? 0}");
                SetDisplay(webGlData);
                
                bool goodProxy = ProxySet();
                _logger.Send($"[DIAG] ProxySet result: {goodProxy}");

                if (strictProxy && !goodProxy) throw new Exception($"!E bad proxy");
                
                var cookiePath = _project.PathCookies();
                _project.Var("pathCookies", cookiePath);
                _logger.Send($"[DIAG] Cookie path: {cookiePath}");

                if (cookies != null) 
                    _instance.SetCookie(cookies);
                else
                    try
                    {
                        cookies = _project.SqlGet("cookies", "_instance");
                        _logger.Send($"[DIAG] Cookies from SQL length: {cookies?.Length ?? 0}");
                        _instance.SetCookie(cookies);
                    }
                    catch (Exception Ex)
                    {
                        _logger.Warn($"Cookies set failed: source=database, path={cookiePath}, error={Ex.Message}");
                        try
                        {
                            cookies = File.ReadAllText(cookiePath);
                            _logger.Send($"[DIAG] Cookies from File length: {cookies?.Length ?? 0}");
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
                _logger.Send("[DIAG] Fixing timezone via BrowserScan");
                var bs = new BrowserScan(_project, _instance);
                if (bs.GetScore().Contains("time")) bs.FixTime();
            }
        }
        #endregion
        private void _SetBrowser(bool strictProxy = true, string restoreFrom = "folder", bool fixTimezone = false)
        {
            string acc0 = _project.Var("acc0");
            _logger.Send($"[DIAG] _SetBrowser START. acc0='{acc0}', strictProxy={strictProxy}, restoreFrom='{restoreFrom}'");

            if (string.IsNullOrEmpty(acc0)) throw new ArgumentException("acc0 can't be null or empty");
            var syncer = new ProfileSync(_project, _instance,_log);
            string instanceType = "WithoutBrowser";
            try
            {
                instanceType = _instance.BrowserType.ToString();
                _logger.Send($"[DIAG] _SetBrowser: Pre-restore check. Type: {instanceType}");
                
                syncer.RestoreProfile(restoreFrom: "folder", restoreProfile: true,
                    restoreCookies: true, restoreInstance: false,
                    restoreWebgl: false, rebuildWebgl: false);
            }
            finally { }
            
            if (instanceType == "Chromium")
            {
                _logger.Send("[DIAG] _SetBrowser: Chromium path. Restoring full profile.");
                syncer.RestoreProfile(restoreFrom: "folder", restoreProfile: true,
                    restoreCookies: true, restoreInstance: true,
                    restoreWebgl: true, rebuildWebgl: false);
                DefaultSettings();

                try
                {
                    _logger.Send("[DIAG] _SetBrowser: Calling ProxySet");
                    ProxySet();
                }
                catch (Exception ex)
                {
                    _logger.Send($"[DIAG] _SetBrowser: ProxySet failed. Error: {ex.Message}");
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
            
            _logger.Send($"[DIAG] ProxySet logic. ProxyString='{proxyString}'");

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
            
            _logger.Send($"[DIAG] Proxy Check Result: ipLocal={ipLocal}, ipProxified={ipProxified}");
    
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
            _logger.Send($"[DIAG] BindPid Entering. pid={pid}, port={port}");
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
                    _logger.Send($"[DIAG] BindPid Success. Mem={memoryMb}, Time={runtimeMinutes}");
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"[DIAG] BindPid ERROR: {ex.Message}");
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

                var syncer = new ProfileSync(_project, _instance,_log);
        
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