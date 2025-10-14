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

        public Init(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _log = log;
            _logger = new Logger(project, log: _log, classEmoji: "►");
            _instance = instance;
        }
        
        #endregion

        #region Public API - Main Entry Points

        public void InitProject(string author = "w3bgr3p", string[] customQueries = null)
        {
            //_SAFU();
            InitVariables(author);
            BuildNewDatabase();

            _project.TblPrepareDefault(log: _log);
            var allQueries = ToDoQueries();
            
            if (customQueries != null)
                foreach (var query in customQueries) 
                    allQueries.Add(query);

            if (allQueries.Count > 0) 
                MakeAccList(allQueries);
            else 
                _logger.Send($"unsupported SQLFilter: [{_project.Variables["wkMode"].Value}]", thrw: true);
        }
        
        public void PrepareProject(bool log = false) 
        {
            bool forced = !string.IsNullOrEmpty(_project.Var("acc0Forced"));
            
            string acc;
            
            if (forced) { 
                if (log) _logger.Send($"Using forced account: {_project.Var("acc0Forced")}");
                _project.Var("acc0", _project.Var("acc0Forced")); 
                acc = _project.Var("acc0Forced");
                _project.GSetAcc(force: true); 
                goto run; 
            }
            if (_project.Var("wkMode") == "UpdBalance")
            {
                return;
            }
    
            getAcc:
            
            _logger.Send("BusyList: " + string.Join(" | ", _project.GGetBusyList()));
            
            acc = "";
            try 
            { 
                acc = GetAccByMode();
                if (string.IsNullOrEmpty(acc))
                {
                    _logger.Warn("accounts list is empty");
                    return;
                }
                var currentState = _project.GVar($"acc{acc}");
                _logger.Send($"trying to set [{currentState} => check]");
                if (currentState == "")
                {
                    _project.GVar($"acc{acc}", "check");
                }
                else
                {
                    _logger.Send($"acc{acc} busy with {currentState}");
                    goto getAcc; 
                }
            } 
            catch (Exception ex) 
            { 
                _project.SendWarningToLog(ex.Message, true); 
                throw; 
            }
    
            try 
            { 
                BlockchainFilter(); 
                SocialFilter(); 
            } 
            catch (Exception ex)
            { 
                _project.SendWarningToLog(ex.Message);
                _project.GVar($"acc{acc}", "");
                if (log) _project.SendWarningToLog($"acc{acc} Filter failed, resetting account and retrying");
                goto getAcc; 
            }
    
            run: 
            try { 
                if (log) _logger.Send("Preparing instance");
                PrepareInstance(); 
                if (log) _logger.Send("Instance prepared successfully");
            } catch (Exception ex) 
            { 
                _project.GVar($"acc{acc}");
                _project.SendWarningToLog(ex.Message); 
                if (log) _project.SendWarningToLog("Instance preparation failed, resetting account and retrying");
                goto getAcc; 
            }

            var pn = _project.ProjectName();
            _project.GVar($"acc{acc}", pn);
            _logger.Send($"running {pn} with acc{acc}.  [{_project.Lists["accs"].Count}] accounts left in que", show: true);
        }
        
        public void PrepareInstance(string browserToLaunch = null)
        {
            if (_project.Var("wkMode") == "UpdBalance")
            {
                _instance.Launch(ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.WithoutBrowser, false);
                return;
            }
            
            LaunchBrowser(browserToLaunch);
            int exCnt = 0;
            string browserType = _instance.BrowserType.ToString();
            bool browser = browserType == "Chromium";

            SetInstance:
            try 
            {
                if (browser && _project.Variables["acc0"].Value != "") //if browser					
                    SetBrowser();	
                else
                    new NetHttp(_project, false).CheckProxy();
            }
            catch (Exception ex)
            {
                _instance.CloseAllTabs();
                _project.log($"!W launchInstance Err {ex.Message}");
                exCnt++;
                if (exCnt > 3)
                {
                    _project.GVar($"acc{_project.Variables["acc0"].Value}", "");
                    throw;
                }
                goto SetInstance;
            }
            _instance.CloseExtraTabs(true);

            foreach (string task in _project.Variables["cfgToDo"].Value.Split(','))
                _project.Lists["toDo"].Add(task.Trim());
            
            _logger.Send($"{browserType} started in {_project.Age<string>()} ");
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

            _logger.Send($"running {pathZp}");
            
            var mapVars = new List<Tuple<string, string>>();
            if (vars != null)
                foreach (var v in vars)
                    mapVars.Add(new Tuple<string, string>(v, v)); 
            return _project.ExecuteProject(pathZp, mapVars, true, true, true); 
        }

        public string LoadSocials(string requiredSocial)
        {
            if (_instance.BrowserType != BrowserType.Chromium) return "noBrowser";
            
            int exCnt = 0;
            string[] socials = requiredSocial.Split(',');
            Dictionary<string, Action> socialActions = new Dictionary<string, Action>
            {
                { "Google", () => new Google(_project, _instance, true).Load() },
                { "Twitter", () => new X(_project, _instance, true).Load() },
                { "Discord", () => new Discord(_project, _instance, true).DSload() },
                { "GitHub", () => new GitHub(_project, _instance, true).Load() }
            };

            foreach (var social in socials)
            {
                if (!socialActions.ContainsKey(social)) continue;

                bool success = false;
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        socialActions[social]();
                        success = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        _instance.CloseAllTabs();
                        exCnt++;
                        if (exCnt >= 3) throw new Exception($"[{social}] !W:{ex.Message}");
                    }
                }
                if (!success) throw new Exception($"!W: {social} load filed");
            }
            _logger.Send($"Socials loaded: [{requiredSocial}]");
            
            _instance.CloseExtraTabs(true);
            return requiredSocial;
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
                    _project.log($"!W {ex.Message}");
                    exCnt++;
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
                _project.SendWarningToLog(ex.Message);
            }
        }

        private void Logo(string author, string dllTitle, string projectName)
        {
            var v = GetVersions();
            string DllVer = v[0];
            string ZpVer = v[1];
            
            if (author != "") author = $" script author: @{author}";
            string logo = $@"using ZennoPoster v{ZpVer}; 
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
                    _project.log(ex.Message);
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
            catch (Exception ex) { _project.SendWarningToLog(ex.Message, true); }
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
                _project.SendWarningToLog($"file {filePath} not found. Last version can be downloaded by link \nhttps://raw.githubusercontent.com/w3bgrep/z3nFarm/master/DbBuilder.zp");
            }
        }
        
        #endregion

        #region Browser & Instance Management

        private void LaunchBrowser(string cfgBrowser = null)
        {
            string acc0 = _project.Var("acc0");
            if (string.IsNullOrEmpty(acc0)) 
                throw new ArgumentException("acc0 can't be null or empty");
            var pathProfile = _project.PathProfileFolder();
            //string pathProfile = Path.Combine(_project.Var("profiles_folder"), "accounts", "profilesFolder", acc0);
            _project.Var("pathProfileFolder", pathProfile);
            
            if (string.IsNullOrEmpty(cfgBrowser))
                cfgBrowser = _project.Var("cfgBrowser");
            var browser = ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.Chromium;

            switch (cfgBrowser)
            {
                case "WithoutBrowser":
                    browser = ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.WithoutBrowser;
                    break;
                case "ZennoBrowser":
                    browser = ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.ChromiumFromZB;	
                    break;
                case "Chromium":
                    break;	
                default:
                    _project.SendWarningToLog($"unknown browser config {cfgBrowser}");
                    throw new Exception($"unknown browser config {cfgBrowser}");
            }
            
            if (cfgBrowser == "WithoutBrowser")
                _instance.Launch(ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.WithoutBrowser, false);
            else
            {
                ZennoLab.CommandCenter.Classes.BuiltInBrowserLaunchSettings settings = 
                    (ZennoLab.CommandCenter.Classes.BuiltInBrowserLaunchSettings)
                    ZennoLab.CommandCenter.Classes.BrowserLaunchSettingsFactory.Create(browser);
                settings.CachePath = pathProfile; 
                settings.ConvertProfileFolder = true;
                settings.UseProfile = true;
                _instance.Launch(settings);
            }
        }

        private void SetBrowser(bool strictProxy = true, string cookies = null, bool log = false)
        {
            string acc0 = _project.Var("acc0");
            if (string.IsNullOrEmpty(acc0)) throw new ArgumentException("acc0 can't be null or empty");
            
            _project.Variables["instancePort"].Value = _instance.FormTitle.Replace("Instance ","");;
            _logger.Send($"init {_instance.FormTitle}");

            string webGlData = _project.SqlGet("webgl", "_instance");
            SetDisplay(webGlData);

            bool goodProxy = new NetHttp(_project, log).ProxySet(_instance);
            if (strictProxy && !goodProxy) throw new Exception($"!E bad proxy");
            var cookiePath = _project.PathCookies();
            //string cookiePath = Path.Combine(_project.Var("profiles_folder"), "accounts", "cookies", acc0 + ".json");
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
                    _logger.Send($"!W Fail to set cookies from file {cookiePath}");
                    try
                    {
                        _logger.Send($"!E Fail to set cookies from db Err. {Ex.Message}");
                        cookies = File.ReadAllText(cookiePath);
                        _instance.SetCookie(cookies);
                    }
                    catch (Exception E)
                    {
                        _logger.Send($"!W Fail to set cookies from file {cookiePath} {E.Message}");
                    }
                }

            if (_project.Var("skipBrowserScan") != "True")
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
        
        #endregion

        #region Account Management

        private List<string> ToDoQueries(string toDo = null, string defaultRange = null, string defaultDoFail = null, string customCondition = null)
        {
            string tableName = _project.ProjectTable();
            string nowIso = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            if (string.IsNullOrEmpty(toDo)) 
                toDo = _project.Variables["cfgToDo"].Value;
            var taskIds = toDo.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();

            string customTask = _project.Var("customTask");
            if (!string.IsNullOrEmpty(customTask))
                taskIds.Add(customTask);

            if (string.IsNullOrEmpty(customCondition)) 
                customCondition = _project.Variables["customCondition"].Value;

            var queries = new List<(string taskId, string query)>();
            foreach (string taskId in taskIds)
            {
                string range = defaultRange ?? _project.Variables["range"].Value;
                string doFail = defaultDoFail ?? _project.Variables["doFail"].Value;
                string failCondition = doFail != "True" ? "AND status NOT LIKE '%fail%'" : "";
                string customConditionPart = "";
                if (!string.IsNullOrEmpty(customCondition)) 
                    customConditionPart = "\n	AND " + customCondition;

                _project.ClmnAdd(taskId, tableName);

                string query = $@"SELECT {Quote("id")} 
                FROM {Quote(tableName)} 
                WHERE {Quote("id")} in ({range}) {failCondition} 
                AND {Quote("status")} NOT LIKE '%skip%' 
                AND ({Quote(taskId)} < '{nowIso}' OR {Quote(taskId)} = ''){customConditionPart}";

                queries.Add((taskId, query));
            }

            var result = queries
                .OrderBy(x => DateTime.TryParseExact(x.taskId, "yyyy-MM-ddTHH:mm:ss.fffZ", 
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, 
                    out DateTime parsed) ? parsed : DateTime.MinValue)
                .Select(x => x.query)
                .ToList();
            
            return result;
        }

        private void MakeAccList(List<string> dbQueries)
        {
            if (!string.IsNullOrEmpty(_project.Variables["acc0Forced"].Value))
            {
                var forced = _project.Variables["acc0Forced"].Value;
                _project.Lists["accs"].Clear();
                _project.Lists["accs"].Add(forced);
                _logger.Send($@"manual mode on with {forced}");
                return;
            }

            var allAccounts = new HashSet<string>();
            foreach (var query in dbQueries)
            {
                try
                {
                    var accsByQuery = _project.DbQ(query, log: _log).Trim();
                    if (!string.IsNullOrWhiteSpace(accsByQuery))
                    {
                        var accounts = accsByQuery.Split('·').Select(x => x.Trim().TrimStart(','));
                        allAccounts.UnionWith(accounts);
                    }
                }
                catch
                {
                    _logger.Send($"{query}");
                }
            }

            if (allAccounts.Count == 0)
            {
                _project.Variables["noAccsToDo"].Value = "True";
                _logger.Send($"♻ noAccountsAvailable by {dbQueries.Count} querie(s)");
                return;
            }
            _logger.Send($"Initial accounts: [{string.Join(", ", allAccounts)}]");
            FilterAccList(allAccounts);
        }

        private void FilterAccList(HashSet<string> allAccounts)
        {
            var reqSocials = _project.Var("requiredSocial");

            if (!string.IsNullOrEmpty(reqSocials))
            {
                string[] demanded = reqSocials.Split(',');
                _logger.Send($"Filtering by socials: [{string.Join(", ", demanded)}]");

                foreach (string social in demanded)
                {
                    string tableName = $"projects_{social.Trim().ToLower()}";
                    var notOK = _project.SqlGet($"id", tableName, log: _log, where: "status LIKE '%suspended%' OR status LIKE '%restricted%' OR status LIKE '%ban%' OR status LIKE '%CAPTCHA%' OR status LIKE '%applyed%' OR status LIKE '%Verify%'")
                        .Split('·')
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrEmpty(x));
                    allAccounts.ExceptWith(notOK);
                    _logger.Send($"After {social} filter: [{string.Join("|", allAccounts)}]");
                }
            }
            _project.Lists["accs"].Clear();
            _project.Lists["accs"].AddRange(allAccounts);
            _logger.Send($"final list [{string.Join("|", allAccounts)}]");
        }

        private bool ChooseSingleAcc(bool oldest = false)
        {
            var listAccounts = _project.Lists["accs"];
            if (listAccounts.Count == 0)
            {
                _project.Variables["noAccsToDo"].Value = "True";
                var busy = _project.GGetBusyList();
                _logger.Warn($"♻ noAccountsAvailable for work\nBusyList: {string.Join(" | ", busy)}",show:true, color: LogColor.Turquoise);
                return false;
            }

            int randomAccount = oldest ? 0 : new Random().Next(0, listAccounts.Count);
            string acc0 = listAccounts[randomAccount];
            _project.Var("acc0", acc0);
            listAccounts.RemoveAt(randomAccount);
            _logger.Send($"chosen: [acc{acc0}] left: [{listAccounts.Count}]");
            return true;
        }

        private string GetAccByMode()
        {
            string mode = _project.Var("wkMode");
            
            _logger.Send($"Mode: {mode}. Choosing new acc from: " + string.Join(", ", _project.Lists["accs"]));
            
            switch (mode)
            {
                case "UpdBalance":
                    _project.Var("acc0", 0);
                    _project.Var("cfgBrowser", "WithoutBrowser");
                    return _project.Var("acc0");
                case "Cooldown":
                    if (!ChooseSingleAcc())
                    {
                        _project.Var("acc0", string.Empty);
                        _project.Var("TimeToChill", "True");
                    }
                    break;
                case "Oldest":
                    if (!ChooseSingleAcc(true))
                    {
                        _project.Var("acc0", string.Empty);
                        _project.Var("TimeToChill", "True");
                    }
                    break;
                case "NewRandom":
                    string toSet = _project.DbGetRandom("proxy, webgl", "_instance", log: false, acc: true);
                    string acc0 = toSet.Split('¦')[0];
                    _project.Var("accRnd", Rnd.RndHexString(64));
                    _project.Var("acc0", acc0);
                    _project.Var("pathProfileFolder", Path.Combine(_project.Var("profiles_folder"), "accounts", "profilesFolder", _project.Var("accRnd")));
                    break;
                default:
                    throw new Exception($"Unknown mode: {mode}");
            }

            if (string.IsNullOrEmpty(_project.Var("acc0"))) //Default
            {
                _logger.Send($"acc0 is empty. Check {_project.Var("wkMode")} conditions maiby it's TimeToChill", thrw: true);
            }
            return _project.Var("acc0");
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
                    _project.SendInfoToLog($"{native}");
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
                        _logger.Send($"{address} have sufficient [{native}] native in {chain}", color: LogColor.Gray);
                        return;
                    }
                    _logger.Warn($"no balance required: [{required}${tiker}] in  {chain}. native is [{native}] for {address}");
                }
                var toDb = $"- {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ} 0\n";
                toDb += $"no balance required: [{minNativeInUsd}] in chains {_project.Var("gateOnchainChain")}";
                _project.DbUpd($"status = 'lowBalance', last = '{toDb}' daily = '{Time.Cd(60)}'");
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
                        string exMsg = $"{social} of {_project.Var("acc0")}: [{status}]";
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
                _project.SendWarningToLog(errorMessage);
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
                    _project.SendWarningToLog("Primary strategy failed, using fallback: " + ex.Message);
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

    
    
    public static partial class ProjectExtensions
    {
        public static void LaunchBrowser(this IZennoPosterProjectModel project, Instance instance, string browserToLaunch)
        {
            new  Init(project, instance).PrepareInstance(browserToLaunch);
        }

    }

}
