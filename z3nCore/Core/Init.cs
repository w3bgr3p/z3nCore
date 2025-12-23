using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using z3nCore.Utilities;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.Enums.Browser;
using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class Init
    {
        #region Fields & Constructor
        
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;
        private readonly bool _log;

        public Init(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _log = log;
            _logger = new Logger(project, log: _log, classEmoji: "►");
            _instance = instance;
        }
        
        #endregion

        #region Public API - Main Entry Points
        
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
            LogDisabler.DisableLogs();
            _SAFU();
            
            string fileName = Path.GetFileName(_project.Variables["projectScript"].Value);
            
            string sessionId = _project.SetSessionId();
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
        private void Logo(string author, string dllTitle, string projectName)
        {
            var v = GetVersions();
            string dllVer = v[0];
            string zpVer = v[1];
            
            if (author != "") author = $" script author: @{author}";
            string frameworkVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            
            string logo = $@"using ZennoPoster v{zpVer} && {frameworkVersion}; 
             using {dllTitle} v{dllVer}  
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

        #region Filters & Validation

        private void BlockchainFilter()
        {
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
            
            string currentProcessPath = Process.GetCurrentProcess().MainModule.FileName;
            string processDir = Path.GetDirectoryName(currentProcessPath);
            string DllVer = referencedVersion;
            string ZpVer = processDir.Split('\\')[5];
            
            return new[] { DllVer, ZpVer };
        }

        
        #endregion
    }
    
}


namespace z3nCore //ProjectExtensions
{
    public static partial class ProjectExtensions
    {
        public static void InitVariables(this IZennoPosterProjectModel project, Instance instance, string author = "w3bgr3p")
        {
            new Init(project, instance).InitVariables(author);
        }
        
        public static void RunBrowser(this IZennoPosterProjectModel project, Instance instance, string browserToLaunch = "Chromium", bool debug = false, bool fixTimezone = false,bool useLegacy = true)
        {
            var browser = instance.BrowserType;
            var brw = new InstanceManager(project, instance, debug);
            
            
            if (browser != BrowserType.Chromium && browser != BrowserType.ChromiumFromZB)
            {	
                //brw.PrepareInstance(browserToLaunch);
                brw.Initialize(browserToLaunch, fixTimezone, useLegacy:useLegacy);
            }
        }
        
        public static void Finish(this IZennoPosterProjectModel project, Instance instance,bool useLegacy = true)
        {
            new Disposer(project, instance).FinishSession(useLegacy);
        }
        
        public static string ReportError(this IZennoPosterProjectModel project, Instance instance, 
            bool toLog = true, bool toTelegram = false, bool toDb = false, bool screenshot = false)
        {
            return new Disposer(project, instance).ErrorReport(toLog, toTelegram, toDb, screenshot);
        }
        
        public static string ReportSuccess(this IZennoPosterProjectModel project, Instance instance,
            bool toLog = true, bool toTelegram = false, bool toDb = false, string customMessage = null)
        {
            return new Disposer(project, instance).SuccessReport(toLog, toTelegram, toDb, customMessage);
        }
    }
}