using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Globalization;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.Enums.Browser;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Diagnostics;
using ZennoLab.InterfacesLibrary.Enums.Log;
namespace z3nCore
{
    
    public class Init
    {
        #region Fields & Constructor
        
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;
        private readonly bool _log;
        private static readonly object _disableLogsLock = new object();
        private static readonly object _randomLock = new object();
        private static readonly Random _random = new Random();

        public Init(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _log = log;
            _logger = new Logger(project, log: _log, classEmoji: "►");
            _instance = instance;
        }
        
        #endregion

        #region Public API - Main Entry Points

        
        public void PrepareInstance(string browserToLaunch = null, bool getscore = false)
        {
            if (_project.Var("wkMode") == "UpdBalance")
            {
                _instance.Launch(ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.WithoutBrowser, false);
                return;
            }
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

            SetInstance:
            try 
            {
                if (browser && _project.Variables["acc0"].Value != "") //if browser					
                    SetBrowser(getscore:getscore);	
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
        }
        
        public bool RunProject(List<string> additionalVars = null, bool add = true)
        {
            string pathZp = _project.Var("projectScript");
            var vars = new List<string> {
                "acc0", "accRnd", "cfgChains", "cfgRefCode", "cfgDelay", "cfgLog", "cfgPin", "cfgToDo", "cfgAccRange",
                "DBmode", "DBpstgrPass", "DBpstgrUser", "DBsqltPath", "failReport", "humanNear",          
                "instancePort", "ip", "lastQuery", "pathCookies", "projectName", "projectTable", 
                "projectScript", "proxy", "requiredSocial", "requiredWallets", "toDo", "varSessionId", "wkMode",
            };

            if (additionalVars != null)
            {
                if (add)
                {
                    foreach (var varName in additionalVars)
                        if (!vars.Contains(varName)) vars.Add(varName);
                }
                else
                {
                    vars = additionalVars;
                }
            }

            _logger.Send($"Execute project: path={pathZp}, vars={vars.Count}");
            
            var mapVars = new List<Tuple<string, string>>();
            if (vars != null)
                foreach (var v in vars)
                    mapVars.Add(new Tuple<string, string>(v, v)); 
            return _project.ExecuteProject(pathZp, mapVars, true, true, true); 
        }
        
        public string LoadWallets(string walletsToUse)
        {
            if (_instance.BrowserType != BrowserType.Chromium) return "noBrowser";
            
            int exCnt = 0;
            string key = !string.IsNullOrEmpty(_project.Var("accRnd")) ? Rnd.Seed() : null;
            _project.Var("refSeed", key);

            string[] wallets = walletsToUse.Split(',');
            Dictionary<string, Action> walletActions = new Dictionary<string, Action>
            {
                { "Backpack", () => _project.Var("addressSol", new BackpackWallet(_project, _instance, key: key, log: false).Launch()) },
                { "Zerion", () => _project.Var("addressEvm", new ZerionWallet(_project, _instance, key: key, log: false).Launch()) },
                { "Keplr", () => new Wallets.Keplr(_project, _instance, log: false).Launch() }
            };

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    foreach (var wallet in wallets)
                    {
                        if (walletActions.ContainsKey(wallet))
                        {
                            walletActions[wallet]();
                        }
                    }
                    break;
                }
                catch (Exception ex)
                {
                    _instance.CloseAllTabs();
                    exCnt++;
                    _logger.Warn($"LoadWallets failed: wallet={string.Join(",", wallets)}, attempt={exCnt}/3, error={ex.Message}");
                    if (exCnt > 3) throw;
                }
            }

            _instance.CloseExtraTabs(true);
            return walletsToUse;
        }
        
        #endregion

        #region Project Initialization

        public void InitVariables(string author = "")
        {
            DisableLogs();
            _SAFU();
                //if (string.IsNullOrEmpty()
            string fileName = System.IO.Path.GetFileName(_project.Variables["projectScript"].Value);
            
            string sessionId = _project.SetSessionId();
            string projectName = _project.ProjectName();
            string projectTable = _project.ProjectTable();
            if (_project.Var("captchaModule") != "") _project.CaptchaModule();
            string dllTitle = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyTitleAttribute>()
                ?.Title ?? "z3nCore";
            
            string[] vars = { "cfgAccRange", };
            CheckVariables(vars);
            
            _project.Range();
            SAFU.Initialize(_project);
            Logo(author, dllTitle, projectName);
        }

        private void DisableLogs_()
        {
            string currentProcessPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string processDir = Path.GetDirectoryName(currentProcessPath);
            string pathLogs = Path.Combine(processDir, "Logs");
            
            try
            {
                lock (_disableLogsLock)
                {
                    if (Directory.Exists(pathLogs))
                    {
                        Directory.Delete(pathLogs, true);
                        using (Process process = new Process())
                        {
                            process.StartInfo.FileName = "cmd.exe";
                            process.StartInfo.Arguments = $"/c mklink /d \"{pathLogs}\" \"NUL\"";
                            process.StartInfo.UseShellExecute = false;
                            process.StartInfo.CreateNoWindow = true;
                            process.StartInfo.RedirectStandardOutput = true;
                            process.StartInfo.RedirectStandardError = true;

                            process.Start();
                            string output = process.StandardOutput.ReadToEnd();
                            string error = process.StandardError.ReadToEnd();
                            process.WaitForExit();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex.Message);
            }
        }

        private void Logo(string author, string dllTitle, string projectName)
        {
            var v = GetVersions();
            string DllVer = v[0];
            string ZpVer = v[1];
            
            if (author != "") author = $" script author: @{author}";
            string frameworkVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            
            string logo = $@"using ZennoPoster v{ZpVer} && {frameworkVersion}; 
             using {dllTitle} v{DllVer}  
            ┌by─┐					
            │    w3bgr3p;		
            └─→┘
                        ► init {projectName} ░ ▒ ▓ █  {author}";
            _project.SendInfoToLog(logo, true);
        }

        private void CheckVariables(string[] vars)
        {
            foreach (string var in vars)
            {
                try
                {
                    if (string.IsNullOrEmpty(_project.Variables[var].Value))
                    {
                        throw new Exception($"!E {var} is null or empty");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex.Message);
                    throw;
                }
            }
        }

        private void _SAFU()
        {
            string tempFilePath = _project.Path + "_SAFU.zp";
            
            var mapVars = new List<Tuple<string, string>>();
            mapVars.Add(new Tuple<string, string>("acc0", "acc0"));
            mapVars.Add(new Tuple<string, string>("cfgPin", "cfgPin"));
            mapVars.Add(new Tuple<string, string>("DBpstgrPass", "DBpstgrPass"));
            try { _project.ExecuteProject(tempFilePath, mapVars, true, true, true); }
            catch (Exception ex) { _logger.Warn(ex.Message); }
        }

        private void BuildNewDatabase()
        {
            if (_project.Var("cfgBuildDb") != "True") return;

            string filePath = Path.Combine(_project.Path, "DbBuilder.zp");
            if (File.Exists(filePath))
            {
                _project.Var("projectScript", filePath);
                _project.Var("wkMode", "Build");
                _project.Var("cfgAccRange", _project.Var("rangeEnd"));
                
                var vars = new List<string> {
                    "cfgLog", "cfgPin", "cfgAccRange", "DBmode", "DBpstgrPass", "DBpstgrUser", 
                    "DBsqltPath", "debug", "lastQuery", "wkMode",
                };
                _project.RunZp(vars);
            }
            else
            {
                _logger.Warn($"file {filePath} not found. Last version can be downloaded by link \nhttps://raw.githubusercontent.com/w3bgrep/z3nFarm/master/DbBuilder.zp");
            }
        }
        
        #endregion

        #region Browser & Instance Management
        private string LaunchBrowser(string cfgBrowser = null)
        {
            string acc0 = _project.Var("acc0");
            if (string.IsNullOrEmpty(acc0)) 
                throw new ArgumentException("acc0 can't be null or empty");
            var pathProfile = _project.PathProfileFolder();
            _project.Var("pathProfileFolder", pathProfile);
            
            if (string.IsNullOrEmpty(cfgBrowser))
                cfgBrowser = _project.Var("cfgBrowser");
            var browser = BrowserType.Chromium;

            switch (cfgBrowser)
            {
                case "WithoutBrowser":
                    browser = BrowserType.WithoutBrowser;
                    break;
                case "ZB":
                    break;
                case "Chromium":
                    break;	
                
                default:
                    _logger.Warn($"unknown browser config {cfgBrowser}");
                    throw new Exception($"unknown browser config {cfgBrowser}");
            }
            
            int pid = 0;
            int port = 0;

            if (cfgBrowser == "WithoutBrowser")
            {
                _instance.Launch(BrowserType.WithoutBrowser, false);
            }
            else if (cfgBrowser == "ZB")
            {
                var path = Path.Combine(_project.Path,".internal","_launchZB.zp");
                if (!File.Exists(path)) throw new Exception($"file {path} is required to rub ZB");
                var launchTime = DateTime.Now; // ИЛИ ВАРИАНТ 2: Запоминаем время перед запуском
                _project.RunZp(path);
                pid = Utilities.ProcAcc.FindFirstNewPid(acc0, launchTime); // ИЛИ ВАРИАНТ 2: Поиск по времени запуска
                port = _instance.Port;
            }
            else if (cfgBrowser == "Chromium")
            {
               
                var pidsBeforeLaunch = Utilities.ProcAcc.GetPidSnapshot();  // ВАРИАНТ 1: Используем снимок PID до запуска
                
                ZennoLab.CommandCenter.Classes.BuiltInBrowserLaunchSettings settings = 
                    (ZennoLab.CommandCenter.Classes.BuiltInBrowserLaunchSettings)
                    ZennoLab.CommandCenter.Classes.BrowserLaunchSettingsFactory.Create(browser);
                settings.CachePath = pathProfile; 
                settings.ConvertProfileFolder = true;
                settings.UseProfile = true;
                _instance.Launch(settings);
                
                pid = Utilities.ProcAcc.GetNewlyLaunchedPid(acc0, pidsBeforeLaunch); // ВАРИАНТ 1: Быстрый поиск среди новых процессов
                
                if (pid == 0)
                {
                    _logger.Send("PID search fallback: fast method failed, using slow search");
                    pid = Utilities.ProcAcc.GetNewest(acc0);
                }
        
                port = _instance.Port;
            }
            _project.Variables["instancePort"].Value = $"port: {port}, pid: {pid}";
            _logger.Send($"Browser launched: type={cfgBrowser}, port={port}, pid={pid}, acc={acc0}");
            BindPid(pid,port);
            return pid.ToString();
        }

        private void SetBrowser(bool strictProxy = true, string cookies = null, bool getscore = false, bool log = false)
        {
            string acc0 = _project.Var("acc0");
            if (string.IsNullOrEmpty(acc0)) throw new ArgumentException("acc0 can't be null or empty");
            
            string instanceType = "WithoutBrowser";
            try
            {
                instanceType = _instance.BrowserType.ToString();
            }
            finally
            {
                
            }
            
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
            //_project.Var("skipBrowserScan") != "True"
            
            if (getscore)
            {
                var bs = new BrowserScan(_project, _instance);
                if (bs.GetScore().Contains("time")) bs.FixTime();
            }
        }
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
        private bool ProxySet( string proxyString = null)
        {
            
            if (string.IsNullOrWhiteSpace(proxyString)) 
                proxyString = _project.DbGet("proxy", "_instance");
            if (string.IsNullOrWhiteSpace(proxyString))
                throw new ArgumentException(proxyString);
            
            string ipLocal = _project.GET("http://api.ipify.org/", null);
            string ipProxified = _project.GET("http://api.ipify.org/", proxyString, useNetHttp:true);

            if (string.IsNullOrEmpty(ipProxified) || !System.Net.IPAddress.TryParse(ipProxified, out _))
            {
                _logger.Warn($"Proxy check failed: proxy={proxyString}, ip={ipProxified}");
                return false;
            }
            if (ipProxified != ipLocal)
            {
                _instance.SetProxy(proxyString, true, true, true, true);
                _logger.Send($"Proxy set: ip={ipProxified}, local={ipLocal}");
                return true;
            }
            _logger.Warn($"Proxy matches local: proxy={proxyString}, ip={ipProxified}");
            return false;
        } 
        #endregion
        

        #region Filters & Validation

        private void BlockchainFilter()
        {
            //gasprice
            if (!string.IsNullOrEmpty(_project.Var("acc0Forced"))) return;
            string[] chains = _project.Var("gateOnchainChain").Split(',');
            
            if (_project.Var("gateOnchainMaxGas") != "")
            {
                decimal maxGas = decimal.Parse(_project.Var("gateOnchainMaxGas"));
                foreach (string chain in chains)
                {
                    decimal gas = W3bTools.GasPrice(Rpc.Get(chain));
                    if (gas >= maxGas) 
                        goto native;
                }
                throw new Exception($"gas is over the limit: {maxGas} on {_project.Var("gateOnchainChain")}");
            }
            
            native:
            //native
            if (_project.Var("gateOnchainMinNative") != "")
            {
                decimal minNativeInUsd = decimal.Parse(_project.Var("gateOnchainMinNative"));
                string tiker;
                string address = _project.SqlGet("evm_pk", "_addresses", log: false);

                decimal native = 0;
                foreach (string chain in chains)
                {
                    native = _project.EvmNative(Rpc.Get(chain), address);
                    _logger.Send($"{native}");
                    switch (chain)
                    {
                        case "bsc":
                        case "opbnb":
                            tiker = "BNB";
                            break;
                        case "solana":
                            address = _project.SqlGet("sol", "public_blockchain", log: false);
                            native = W3bTools.SolNative(Rpc.Get(chain), address);
                            tiker = "SOL";
                            break;
                        case "avalanche":
                            tiker = "AVAX";
                            break;
                        default:			
                            tiker = "ETH";
                            break;
                    }
                    var required = _project.Var("nativeBy") == "native" ? minNativeInUsd : _project.UsdToToken(minNativeInUsd, tiker, "OKX");
                    if (native >= required) { 			
                        _logger.Send($"Balance check passed: address={address}, chain={chain}, native={native}, required={required}", color: LogColor.Gray);
                        return;
                    }
                    _logger.Warn($"Insufficient balance: address={address}, chain={chain}, native={native}, required={required}{tiker}");
                }
                var toDb = $"- {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ} 0\n";
                toDb += $"no balance required: [{minNativeInUsd}] in chains {_project.Var("gateOnchainChain")}";
                _project.DbUpd($"status = 'lowBalance', last = '{toDb}', daily = '{Time.Cd(60)}'");
                throw new Exception(toDb);
            }
        }

        private void SocialFilter()
        {
            if (string.IsNullOrEmpty(_project.Var("requiredSocial"))) return;
            
            var requiredSocials = _project.Var("requiredSocial").Split(',').ToList();
            var badList = new List<string>{
                "suspended",
                "restricted",
                "ban",
                "CAPTCHA",
                "applied",
                "Verify"};
            
            foreach (var social in requiredSocials)
            {
                var tableName = "_" + social.ToLower().Trim();
                var status = _project.SqlGet("status", tableName, log: true);
                foreach (string word in badList)
                {
                    if (status.Contains(word))
                    {
                        string exMsg = $"Social filter failed: social={social}, acc={_project.Var("acc0")}, status={status}";
                        _logger.Warn(exMsg);
                        throw new Exception(exMsg);
                    }
                }
            }
        }
        
        #endregion

        #region Utilities & Helpers

        private string[] GetVersions()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var referencedAssembly = typeof(z3nCore.Init).Assembly;
            var executingVersion = executingAssembly.GetName().Version.ToString();
            var referencedVersion = referencedAssembly.GetName().Version.ToString();

            if (executingVersion != referencedVersion)
            {
                string errorMessage = $"Version mismatch detected! " +
                                      $"Executing assembly ({executingAssembly.Location}) version: {executingVersion}, " +
                                      $"Referenced assembly ({referencedAssembly.Location}) version: {referencedVersion}. " +
                                      $"Ensure both assemblies are the same version.";
                _logger.Warn(errorMessage);
            }
            
            string currentProcessPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string processDir = Path.GetDirectoryName(currentProcessPath);
            string DllVer = referencedVersion;
            string ZpVer = processDir.Split('\\')[5];
            
            return new[] { DllVer, ZpVer };
        }

        private static string Quote(string name)
        {
            return $"\"{name.Replace("\"", "\"\"")}\"";
        }
        
        private void DisableLogs(bool aggressive = false)
        {
            string currentProcessPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string processDir = Path.GetDirectoryName(currentProcessPath);
            string pathLogs = Path.Combine(processDir, "Logs");
            
            lock (_disableLogsLock)
            {
                // Проверяем, не выполнена ли операция уже
                if (IsLogsAlreadyDisabled(pathLogs))
                {
                    return; // Операция уже выполнена
                }
                
                if (aggressive)
                {
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        if (IsLogsAlreadyDisabled(pathLogs))
                            return; // Проверяем после каждой попытки
                            
                        TryDisableWithStrategy(pathLogs, true);
                        System.Threading.Thread.Sleep(50 * (attempt + 1));
                    }
                }
                else
                {
                    TryDisableWithStrategy(pathLogs, false);
                }
            }
        }

        private bool IsLogsAlreadyDisabled(string pathLogs)
        {
            try
            {
                // Проверка 1: Путь не существует (может быть уже удален)
                if (!Directory.Exists(pathLogs) && !File.Exists(pathLogs))
                {
                    return false; // Ничего нет, можно продолжать
                }
                
                // Проверка 2: Это файл-блокировщик (fallback уже сработал)
                if (File.Exists(pathLogs) && !Directory.Exists(pathLogs))
                {
                    return true; // Это файл, значит блокировка уже установлена
                }
                
                // Проверка 3: Это символическая ссылка
                if (Directory.Exists(pathLogs))
                {
                    var dirInfo = new DirectoryInfo(pathLogs);
                    if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        return true; // Это симлинк, операция уже выполнена
                    }
                }
                
                // Проверка 4: Существуют lock-файлы
                if (File.Exists(pathLogs + ".lock"))
                {
                    return true; // Lock-файл существует
                }
                
                return false; // Это обычная папка, нужно выполнять операцию
            }
            catch
            {
                return false; // При ошибке считаем, что нужно выполнять операцию
            }
        }

        private void TryDisableWithStrategy(string pathLogs, bool useAggressiveCommands)
        {
            try
            {
                // Повторная проверка перед выполнением
                if (IsLogsAlreadyDisabled(pathLogs))
                {
                    return;
                }
                
                if (useAggressiveCommands)
                {
                    // Только если это обычная папка
                    if (Directory.Exists(pathLogs) && !IsSymbolicLink(pathLogs))
                    {
                        ExecuteCommand(string.Format("rd /s /q \"{0}\" 2>nul", pathLogs));
                    }
                    
                    // Создаем симлинк только если его еще нет
                    if (!Directory.Exists(pathLogs))
                    {
                        ExecuteCommand(string.Format("mklink /d \"{0}\" \"NUL\" 2>nul", pathLogs));
                    }
                }
                else
                {
                    // Удаляем только если это обычная папка
                    if (Directory.Exists(pathLogs) && !IsSymbolicLink(pathLogs))
                    {
                        foreach (string file in Directory.GetFiles(pathLogs, "*", SearchOption.AllDirectories))
                        {
                            try { File.SetAttributes(file, FileAttributes.Normal); } catch { }
                        }
                        foreach (string dir in Directory.GetDirectories(pathLogs, "*", SearchOption.AllDirectories))
                        {
                            try { File.SetAttributes(dir, FileAttributes.Normal); } catch { }
                        }
                        Directory.Delete(pathLogs, true);
                    }
                    
                    // Создаем симлинк только если путь свободен
                    if (!Directory.Exists(pathLogs) && !File.Exists(pathLogs))
                    {
                        ExecuteCommand(string.Format("mklink /d \"{0}\" \"NUL\"", pathLogs));
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    _logger.Warn("Primary strategy failed, using fallback: " + ex.Message);
                }
                catch { }
                
                // Fallback только если еще ничего не заблокировано
                if (!IsLogsAlreadyDisabled(pathLogs))
                {
                    try 
                    { 
                        // Удаляем папку если она есть
                        if (Directory.Exists(pathLogs) && !IsSymbolicLink(pathLogs))
                        {
                            Directory.Delete(pathLogs, true);
                        }
                        
                        // Создаем файл-блокировщик
                        File.WriteAllText(pathLogs, "BLOCKED"); 
                    } 
                    catch { }
                    
                    try { File.SetAttributes(pathLogs, FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System); } catch { }
                    try { File.WriteAllText(pathLogs + ".lock", ""); } catch { }
                }
            }
        }

        private bool IsSymbolicLink(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    var dirInfo = new DirectoryInfo(path);
                    return dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private void ExecuteCommand(string command)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/c " + command;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();
                process.WaitForExit();
            }
        }
        
        #endregion
    }

    
    
    
    
    public static class AccountManager
    {
        private static List<string> ParseRangeGroups(IZennoPosterProjectModel project, string cfgAccRange)
        {
            var groups = new List<string>();
    
            // Сплитим по : (группы приоритета)
            foreach (var groupRange in cfgAccRange.Split(':'))
            {
                // Каждую группу обрабатываем через оригинальный Range()
                var rangeIds = project.Range(groupRange); // вернет List<string> ID
                // Склеиваем обратно в строку для SQL IN (...)
                groups.Add(string.Join(",", rangeIds));
            }
    
            return groups;
        }
        
        private static string ParseSingleRange(string rangeStr)
        {
            if (rangeStr.Contains(","))
            {
                // Already comma-separated list
                return rangeStr;
            }
            else if (rangeStr.Contains("-"))
            {
                // Range format like "1-100"
                var parts = rangeStr.Split('-').Select(int.Parse).ToArray();
                int start = parts[0];
                int end = parts[1];
                return string.Join(",", Enumerable.Range(start, end - start + 1));
            }
            else
            {
                // Single number
                return rangeStr;
            }
        }

        private static void GetListFromDb(this IZennoPosterProjectModel project,
            string condition,
            string sortByTaskAge = null,
            bool useRange = true,
            bool filterTwitter = false,
            bool filterDiscord = false,
            string tableName = null,
            bool debugLog = false)
        {
            if (!string.IsNullOrEmpty(project.Var("acc0")))
            {
                return;
            }
            if (string.IsNullOrEmpty(tableName)) 
                tableName = project.ProjectTable();
            
            // Parse priority groups from cfgAccRange
            List<string> rangeGroups = new List<string>();
            if (useRange)
            {
                var cfgAccRange = project.Var("cfgAccRange");
                rangeGroups = ParseRangeGroups(project, cfgAccRange);
            }
            else
            {
                rangeGroups.Add(null); // Single iteration without range
            }
            
            // Try each priority group in order
            foreach (var rangeGroup in rangeGroups)
            {
                var fullCondition = useRange
                    ? $"{condition} AND id in ({rangeGroup})"
                    : condition;

                List<string> accounts;

                if (!string.IsNullOrEmpty(sortByTaskAge))
                {
                    var selectColumns = $"id, {sortByTaskAge}";
                    var orderBy = $"CASE WHEN {sortByTaskAge} = '' OR {sortByTaskAge} IS NULL THEN '9999-12-31' ELSE {sortByTaskAge} END ASC";
                    var query = $"SELECT {selectColumns} FROM {tableName} WHERE {fullCondition} ORDER BY {orderBy}";

                    var rawData = project.DbQ(query, log: debugLog);

                    var accountsWithDates = rawData.Split('·')
                        .Where(row => !string.IsNullOrWhiteSpace(row))
                        .Select(row =>
                        {
                            var parts = row.Split('|');
                            var id = parts[0];
                            var dateStr = parts.Length > 1 ? parts[1] : "";

                            DateTime date;
                            if (!DateTime.TryParseExact(dateStr,
                                    "yyyy-MM-ddTHH:mm:ss.fffZ",
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                    out date))
                            {
                                date = DateTime.MaxValue;
                            }

                            return new { Id = id, Date = date };
                        })
                        .OrderBy(x => x.Date)
                        .Select(x => x.Id)
                        .ToList();

                    accounts = accountsWithDates;
                }
                else
                {
                    accounts = project.DbGetLines("id",tableName: tableName, log: debugLog, where: fullCondition)
                        .Where(acc => !string.IsNullOrWhiteSpace(acc))
                        .ToList();
                }

                if (!accounts.Any())
                {
                    // No accounts found in this group, try next priority group
                    continue;
                }

                project.ListSync("accs", accounts);

                if (filterTwitter)
                {
                    if (!project.FilterBySocial("twitter", condition))
                        continue; // Try next group
                }

                if (filterDiscord)
                {
                    if (!project.FilterBySocial("discord", condition))
                        continue; // Try next group
                }

                accounts = project.Lists["accs"].ToList();

                if (!accounts.Any())
                {
                    // After filtering, no accounts left, try next group
                    continue;
                }
                
                // Successfully found accounts, exit
                return;
            }
            
            project.warn($"No accounts found by condition\n{condition.Replace("\n", " ")} in range {project.Var("cfgAccRange")}");
        }

        public static void ChooseAccountByCondition(this IZennoPosterProjectModel project,
            string condition,
            string sortByTaskAge = null,
            bool useRange = true,
            bool filterTwitter = false,
            bool filterDiscord = false,
            string tableName = null,
            bool debugLog = false)
        {
            if (!string.IsNullOrEmpty(project.Var("acc0Forced")))
            {
                project.Var("acc0", project.Var("acc0Forced"));
                return;
            }

            // Get filtered list from DB
            project.GetListFromDb(condition, sortByTaskAge, useRange, filterTwitter, filterDiscord,tableName ,debugLog);

            var accounts = project.Lists["accs"].ToList();

            if (!accounts.Any())
            {
                project.warn($"Account selection failed: condition={condition}, found=0 in all priority groups", true);
                return;
            }

            var acc0 = !string.IsNullOrEmpty(sortByTaskAge)
                ? accounts.First()
                : project.RndFromList("accs", true);

            project.Var("acc0", acc0);

            if (!string.IsNullOrEmpty(sortByTaskAge))
                project.Lists["accs"].Remove(acc0);

            var left = project.Lists["accs"].Count;
            project.DbUpd($"status = 'working...'");
            project.SendToLog($"Account selected: acc={acc0}, remaining={left}, condition={condition}, range={project.Var("cfgAccRange")}", LogType.Info, true, LogColor.Gray);
        }

        public static void ChooseAndRunByCondition(this IZennoPosterProjectModel project,Instance instance, 
            string condition, 
            bool browser = false,
            string sortByTaskAge = null, 
            bool useRange = true, 
            bool filterTwitter = false, 
            bool filterDiscord = false, 
            string tableName = null, 
            bool debugLog = false)
        {
            
            while (true)
            {
                try{
                    project.ChooseAccountByCondition(condition,sortByTaskAge,useRange,filterTwitter,filterDiscord,tableName,debugLog); 

                    if (string.IsNullOrEmpty(project.Var("acc0"))) 
                        throw new Exception("");
                    
                    var browserMode = browser ? "Chromium" : "WithoutBrowser";
                    //run
                    project.RunBrowser(instance,browserMode);
                    return;
		
                }
                catch (Exception ex)
                {

                    bool thrw = !ex.Message.Contains("Браузер не может быть запущен в указанной папке");
                    
                    project.warn(ex,thrw);
                    continue;
                }

            }


        }

        private static bool FilterBySocial(this IZennoPosterProjectModel project, string socialName, string originalCondition = "")
        {
            var accs = project.ListSync("accs");
            var filtered = new HashSet<string>(
                project.DbGetLines("id", $"_{socialName.ToLower()}", where: @"status = 'ok'")
            );

            var combined = accs
                .Where(acc => filtered.Contains(acc))
                .ToList();
            
            project.ListSync("accs", combined);
            
            if (!combined.Any())
            {
                var conditionInfo = string.IsNullOrEmpty(originalCondition) ? "" : $", condition={originalCondition}";
                project.warn($"Social filter failed: social={socialName}, found=0{conditionInfo}");
                return false;
            }
            return true;
        }
    }

    public static partial class ProjectExtensions
    {
        
        public static void InitVariables(this IZennoPosterProjectModel project,Instance instance, string author = "w3bgr3p")
        {
            new Init(project, instance).InitVariables(author);
        }
        public static void RunBrowser(this IZennoPosterProjectModel project, Instance instance, string browserToLaunch = "Chromium", bool debug = false)
        {
            var browser = instance.BrowserType;
            var brw = new Init(project,instance, false);
            if (browser !=  ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.Chromium && browser !=  ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.ChromiumFromZB)
            {	
                brw.PrepareInstance(browserToLaunch);
            }
        }
        
        
        
    }

}