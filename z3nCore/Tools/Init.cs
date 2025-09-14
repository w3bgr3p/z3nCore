using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;

        public Init(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;

            _logger = new Logger(project, log: log, classEmoji: "►");
            _instance = instance;
        }
        public Init(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project;

            _logger = new Logger(project, log: log, classEmoji: "►");

        }
        private void DisableLogs()
        {
            
            string currentProcessPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string processDir = Path.GetDirectoryName(currentProcessPath);

            string pathLogs = Path.Combine(processDir,"Logs");
            
            var lockobj = new object();

            try
            {
                lock (lockobj)
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
                _logger.Send(ex.Message);
            }
        }
        private void InitVariables(string author = "")
        {
            DisableLogs();
            
            string fileName = System.IO.Path.GetFileName(_project.Variables["projectScript"].Value);
            string sessionId = _project.SessionId();
            string projectName = _project.ProjectName();
            string projectTable = _project.ProjectTable();  
            
            string dllTitle = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyTitleAttribute>()
                ?.Title ?? "z3nCore";
            
            string[] vars = { "cfgAccRange", };
            CheckVariables(vars);
            
            _project.Range();
            SAFU.Initialize(_project);
            Logo(author, dllTitle, projectName);

        }
        private void Logo(string author, string dllTitle , string projectName)
        {
            
            //string currentProcessPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            //string processDir = Path.GetDirectoryName(currentProcessPath);
            //string path_ExternalAssemblies = Path.Combine(processDir,"ExternalAssemblies");
            //string path_dll = Path.Combine(path_ExternalAssemblies,$"{dllTitle}.dll");
            //string DllVer = FileVersionInfo.GetVersionInfo(path_dll).FileVersion;
            //string ZpVer = processDir.Split('\\')[5];
            var v = Versions();
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
        private void _SAFU()
        {
            string tempFilePath = _project.Path + "_SAFU.zp";
            var mapVars = new List<Tuple<string, string>>();
            mapVars.Add(new Tuple<string, string>("acc0", "acc0"));
            mapVars.Add(new Tuple<string, string>("cfgPin", "cfgPin"));
            mapVars.Add(new Tuple<string, string>("DBpstgrPass", "DBpstgrPass"));
            try { _project.ExecuteProject(tempFilePath, mapVars, true, true, true); }
            catch (Exception ex) { _project.SendWarningToLog(ex.Message); }
            
        }    
        private void BuildNewDatabase ()
        {
            if (_project.Var("cfgBuildDb") != "True") return;

            string filePath = Path.Combine(_project.Path, "DbBuilder.zp");
            if (File.Exists(filePath))
            {
                _project.Var("projectScript",filePath);
                _project.Var("wkMode","Build");
                _project.Var("cfgAccRange", _project.Var("rangeEnd"));
                
                var vars =  new List<string> {
                    "cfgLog",
                    "cfgPin",
                    "cfgAccRange",
                    "DBmode",
                    "DBpstgrPass",
                    "DBpstgrUser",
                    "DBsqltPath",
                    "debug",
                    "lastQuery",
                    "wkMode",
                };
                _project.RunZp(vars);
            }
            else
            {
                _project.SendWarningToLog($"file {filePath} not found. Last version can be downloaded by link \nhttps://raw.githubusercontent.com/w3bgrep/z3nFarm/master/DbBuilder.zp");
            }
            
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
                    _project.L0g(ex.Message);
                    throw;
                }
            }
        }
        private void SetBrowser(bool strictProxy = true, string cookies = null, bool log = false)
        {
            string acc0 = _project.Var("acc0");
            if (string.IsNullOrEmpty(acc0)) throw new ArgumentException("acc0 can't be null or empty");
            
            _project.Variables["instancePort"].Value = _instance.Port.ToString();
            _logger.Send($"init browser in port: {_instance.Port}");

            string webGlData = _project.SqlGet("webgl", "_instance");
            SetDisplay(webGlData);

            bool goodProxy = new NetHttp(_project, log).ProxySet(_instance);
            if (strictProxy && !goodProxy) throw new Exception($"!E bad proxy");
            
            string cookiePath = Path.Combine(_project.Var("profiles_folder"),"accounts","cookies",acc0 + ".json");
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
        private void LaunchBrowser(string cfgBrowser = null)
        {
            string acc0 = _project.Var("acc0");
            if (string.IsNullOrEmpty(acc0)) 
                throw new ArgumentException("acc0 can't be null or empty");
            string pathProfile = Path.Combine(_project.Var("profiles_folder"),"accounts","profilesFolder",acc0);
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
                    throw new Exception ($"unknown browser config {cfgBrowser}");
            }
            if ( cfgBrowser == "WithoutBrowser")
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
        
        private void PrepareInstance()
        {
            
            var newBrowser = new Init(_project, _instance, false);
            newBrowser.LaunchBrowser();
            
            int exCnt = 0;
            string browserType = _instance.BrowserType.ToString();
            bool browser = browserType == "Chromium";

            SetInstance:
            try 
            {
                if (browser && _project.Variables["acc0"].Value != "") //if browser					
                    newBrowser.SetBrowser();	
                else
                    new NetHttp(_project, false).CheckProxy();
            }
            catch (Exception ex)
            {
                _instance.CloseAllTabs();
                _project.L0g($"!W launchInstance Err {ex.Message}");
                exCnt++;				
                if (exCnt > 3 ) throw;
                goto SetInstance;
            }
            _instance.CloseExtraTabs(true);

            foreach(string task in 	_project.Variables["cfgToDo"].Value.Split(','))
                _project.Lists["toDo"].Add(task.Trim());
            
            _project.L0g($"{browserType} started in {_project.Age<string>()} ");
            _project.Var("varSessionId", (DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString());

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
            else _logger.Send("!W WebGL string is empty. Please parse WebGL data into the database. Otherwise, any antifraud system will fuck you up like it’s a piece of cake.");

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
                _logger.Send(ex.Message, thr0w: true);
            }

        }
        private string[] Versions()
        {
            string currentProcessPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string processDir = Path.GetDirectoryName(currentProcessPath);

            string path_ExternalAssemblies = Path.Combine(processDir,"ExternalAssemblies");
            string path_dll = Path.Combine(path_ExternalAssemblies,"z3nCore.dll");
            string DllVer = FileVersionInfo.GetVersionInfo(path_dll).FileVersion;
            string ZpVer = processDir.Split('\\')[5];
            
            return new[] { DllVer, ZpVer };
        }
        private static string Quote(string name)
        {
            return $"\"{name.Replace("\"", "\"\"")}\"";
        }
        private List<string> ToDoQueries(string toDo = null, string defaultRange = null, string defaultDoFail = null)
        {
            string tableName = _project.ProjectTable();

            var nowIso = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            if (string.IsNullOrEmpty(toDo)) 
                toDo = _project.Variables["cfgToDo"].Value;
            
            var toDoItems = new List<string>();

            foreach (string task in toDo.Split(','))
            { 
                toDoItems.Add(task.Trim());
            }
            
            
            string customTask  = _project.Var("cfgCustomTask");
            if (!string.IsNullOrEmpty(customTask))
                toDoItems.Add(customTask);
            

            var allQueries = new List<(string TaskId, string Query)>();

            foreach (string taskId in toDoItems)
            {
                string trimmedTaskId = taskId.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedTaskId))
                {
                    string range = defaultRange ?? _project.Variables["range"].Value;
                    string doFail = defaultDoFail ?? _project.Variables["doFail"].Value;
                    _project.ClmnAdd(trimmedTaskId,tableName);
                    string failCondition = (doFail != "True" ? "AND status NOT LIKE '%fail%'" : "");
                    string query = $@"SELECT {Quote("id")} 
                        FROM {Quote(tableName)} 
                        WHERE {Quote("id")} in ({range}) {failCondition} 
                        AND {Quote("status")} NOT LIKE '%skip%' 
                        AND ({Quote(trimmedTaskId)} < '{nowIso}' OR {Quote(trimmedTaskId)} = '')";
                    allQueries.Add((trimmedTaskId, query));
                }
            }

            return allQueries
                .OrderBy(x =>
                {
                    if (string.IsNullOrEmpty(x.TaskId))
                        return DateTime.MinValue;

                    if (DateTime.TryParseExact(
                            x.TaskId,
                            "yyyy-MM-ddTHH:mm:ss.fffZ",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                            out DateTime parsed))
                    {
                        return parsed;
                    }

                    return DateTime.MinValue;
                })
                .Select(x => x.Query)
                .ToList();
        }
        private void MakeAccList(List<string> dbQueries, bool log = false)
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
                    var accsByQuery = _project.DbQ(query,log:log).Trim();
                    if (!string.IsNullOrWhiteSpace(accsByQuery))
                    {
                        var accounts = accsByQuery.Split('\n').Select(x => x.Trim().TrimStart(','));
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
                _logger.Send($"♻ noAccountsAvailable by queries [{string.Join(" | ", dbQueries)}]");
                return;
            }
            _logger.Send($"Initial availableAccounts: [{string.Join(", ", allAccounts)}]");
            FilterAccList( allAccounts, log);
        }
        private void FilterAccList(HashSet<string> allAccounts,bool log=false)
        {
            var reqSocials = _project.Var("requiredSocial");

            if (!string.IsNullOrEmpty(reqSocials))
            {
                string[] demanded = reqSocials.Split(',');
                _logger.Send($"Filtering by socials: [{string.Join(", ", demanded)}]");

                foreach (string social in demanded)
                {
                    string tableName = $"projects_{social.Trim().ToLower()}";
                    var notOK = _project.SqlGet($"id", tableName, log: log, where: "status LIKE '%suspended%' OR status LIKE '%restricted%' OR status LIKE '%ban%' OR status LIKE '%CAPTCHA%' OR status LIKE '%applyed%' OR status LIKE '%Verify%'")
                        .Split('\n')
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
            string pathProfiles = _project.Var("profiles_folder");

            
            if (listAccounts.Count == 0)
            {
                _project.Variables["noAccsToDo"].Value = "True";
                _project.SendToLog($"♻ noAccoutsAvaliable", LogType.Info, true, LogColor.Turquoise);
                return false;
            }

            int randomAccount = oldest ? 0 : new Random().Next(0, listAccounts.Count) ;
            string acc0 = listAccounts[randomAccount];
            _project.Var("acc0", acc0);
            listAccounts.RemoveAt(randomAccount);
            _logger.Send($"`working with: [acc{acc0}] accs left: [{listAccounts.Count}]");
            return true;
        }
        
        private void BlockchainFilter ()
        {
            //gasprice
            if (!string.IsNullOrEmpty(_project.Var("acc0Forced")) ) return ;
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
            if( _project.Var("gateOnchainMinNative") != "")
            {
	        
                decimal minNativeInUsd = decimal.Parse(_project.Var("gateOnchainMinNative"));
                string tiker;
                string  address = _project.DbGet("evm_pk","_addresses", log:false);

                decimal native = 0;
                foreach (string chain in chains)
                {
                    native = _project.EvmNative(Rpc.Get(chain),address);
                    _project.SendInfoToLog($"{native}");
                    switch(chain)
                    {
			        
                        case "bsc":
                        case "opbnb":
                            tiker = "BNB";
                            break;
                        case "solana":
                            address = _project.DbGet("sol","public_blockchain", log:false);
                            native = W3bTools.SolNative(Rpc.Get(chain),address);
                            tiker = "SOL";
                            break;
                        case "avalance":
                            tiker = "AVAX";
                            break;
                        default:			
                            tiker = "ETH";
                            break;
                    }
                    var required = _project.Var("nativeBy") == "native" ? minNativeInUsd : _project.UsdToToken(minNativeInUsd, tiker,"OKX");
                    if (native >= required) { 			
                        _project.SendToLog($"{address} have sufficient [{native}] native in {chain}",LogType.Info ,true ,LogColor.LightBlue);
                        return ;
                    }
                    _project.SendWarningToLog($"!W no balnce required: [{required}${tiker}] in  {chain}. native is [{native}] for {address}");
                }
                _project.DbUpd($"status = '! noBalance', daily = '{Time.Cd(60)}'");
                throw new Exception($"!W no balnce required: [{minNativeInUsd}] in chains {_project.Var("gateOnchainChain")}");
            }
        }

        private void SocialFilter()
        {
            if (string.IsNullOrEmpty(_project.Var("requiredSocial"))) return ;
            
            var requiredSocials = _project.Var("requiredSocial").Split(',').ToList();
            var badList = new List<string>{
                "suspended",
                "restricted",
                "ban",
                "CAPTCHA",
                "applyed",
                "Verify"};
            
            foreach (var social in requiredSocials)
            {
                var tableName = "_" + social.ToLower().Trim();
                var status = _project.DbGet("status",tableName, log:true);
                foreach (string word in badList)
                {
                    if (status.Contains(word)){
                        string exMsg = $"{social} of {_project.Var("acc0")}: [{status}]";
                        _project.SendWarningToLog(exMsg);
                        throw new Exception(exMsg);
                    }
                }
            }


        }

        private void GetAccByMode()
        {
            _logger.Send("accsQueue: " + string.Join(", ",_project.Lists["accs"]));
            if (_project.Var("wkMode") == "Cooldown") //Cooldown
            {
                //chose:
                //bool chosen = ChooseSingleAcc();
	
                if (!ChooseSingleAcc())
                {
                    _project.Var("acc0", null);
                    _project.Var("TimeToChill", "True");

                    throw new Exception($"TimeToChill");
                }
            }

            if (_project.Var("wkMode") == "Oldest") //Cooldown
            {

                //bool chosen = ChooseSingleAcc(true);
	
                if (!ChooseSingleAcc())
                {
                    _project.Var("acc0", null);
                    _project.Var("TimeToChill", "True");
                    throw new Exception($"TimeToChill");
                }
            }


            if (_project.Var("wkMode") == "NewRandom") //DeadSouls
            {
                string toSet = new Sql(_project).GetRandom("proxy, webgl","private_profile",log:false, acc:true);
                string acc0 = toSet.Split('|')[0];
                _project.Var("accRnd", Rnd.RndHexString(64));
                _project.Var("acc0", acc0);
                _project.Var("pathProfileFolder",Path.Combine(_project.Var("profiles_folder") , "accounts", "profilesFolder" , _project.Var("accRnd")));
            }
            

            if (_project.Var("wkMode") == "UpdateToken") //UpdateToken
            {
                string toSet = new Sql(_project).GetRandom("token",log:true, acc:true, invert:true);
                string acc0 = toSet.Split('|')[0];
                _project.Var("acc0", acc0);
                
            }
            
            if (string.IsNullOrEmpty(_project.Var("acc0"))) //Default
            {
                _logger.Send($"acc0 is empty. Check {_project.Var("wkMode")} conditions maiby it's TimeToChill",thr0w:true);
            }
            
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
            //run...
            if (_instance.BrowserType != BrowserType.Chromium) return "noBrowser";
            int exCnt = 0;
            string key = !string.IsNullOrEmpty(_project.Var("accRnd")) ? Rnd.Seed() : null;
            _project.Var("refSeed", key);

            string[] wallets = walletsToUse.Split(',');
            Dictionary<string, Action> walletActions = new Dictionary<string, Action>
            {
                { "Backpack", () => _project.Var("addressSol", new BackpackWallet(_project, _instance, key: key, log: false).Launch()) },
                { "Zerion", () => _project.Var("addressEvm", new ZerionWallet(_project, _instance, key: key, log: false).Launch()) },
                { "Keplr", () => new KeplrWallet(_project, _instance, log: false).Launch() }
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
                    _project.L0g($"!W {ex.Message}");
                    exCnt++;
                    if (exCnt > 3) throw;
                }
            }

            _instance.CloseExtraTabs(true);
            return walletsToUse;
        }
        
        public void InitProject( string author = "w3bgr3p", string[] customQueries = null, bool log = false )
        {
            
            _SAFU();
            InitVariables(author);
            BuildNewDatabase();

            _project.TblPrepareDefault(log:log);
            var allQueries = ToDoQueries();
            
            if (customQueries != null)
                foreach(var query in customQueries) 
                    allQueries.Add(query);

            if (allQueries.Count > 0) 
                MakeAccList(allQueries, log: log);
            else 
                _logger.Send($"unsupported SQLFilter: [{_project.Variables["wkMode"].Value}]",thr0w:true);
            
        }
        public void PrepareProject()
        {
            
            bool forced = !string.IsNullOrEmpty(_project.Var("acc0Forced"));

            if (forced){
                _project.Var("acc0",_project.Var("acc0Forced"));
                _project.GSet(force:true);
                goto run;
            }
		
            getAcc:
            try
            {
                GetAccByMode();
                if (!_project.GSet("check")) goto getAcc;
            }
            catch (Exception ex)
            {
                _project.SendWarningToLog(ex.Message);
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
                _project.GSet("", true);
                goto getAcc;
            }

            run:
            try
            {
                PrepareInstance();
            }
            catch (Exception ex)
            {
                _project.GSet("", true);
                _project.SendWarningToLog(ex.Message);
                goto getAcc;
            }

            _project.GSet(force:true);

        }
        public bool RunProject(List<string> additionalVars = null, bool add = true )
        {
            string pathZp = _project.Var("projectScript");
            var vars =  new List<string> {
                "acc0",
                "accRnd",
                "cfgChains",
                "cfgRefCode",
                "captchaModule",
                "cfgDelay",
                "cfgLog",
                "cfgPin",
                "cfgToDo",
                "DBmode",
                "DBpstgrPass",
                "DBpstgrUser",
                "DBsqltPath",
                "failReport",
                "humanNear",          
                "instancePort", 
                "ip",
                "run",
                "lastQuery",
                "lastErr",
                "pathCookies",
                "projectName",
                "projectTable", 
                "projectScript",
                "proxy",          
                "requiredSocial",
                "requiredWallets",
                "toDo",
                "varSessionId",
                "wkMode",
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

            _logger.Send($"running {pathZp}" );
            
            var mapVars = new List<Tuple<string, string>>();
            if (vars != null)
                foreach (var v in vars)
                    mapVars.Add(new Tuple<string, string>(v, v)); 
            return _project.ExecuteProject(pathZp, mapVars, true, true, true); 
        }
        
    }
}
