// ============================================
//  ORCHESTRATOR
// ============================================

namespace z3nCore.Orchestration
{
    using z3nCore.Interfaces;
    using z3nCore.Services;
    using ZennoLab.CommandCenter;
    using ZennoLab.InterfacesLibrary.ProjectModel;
    using System;
    using System.Collections.Generic;
    using System.Linq;


    public class ProjectOrchestrator : IProjectInitializer, IProjectRunner
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly bool _showLog;
        
        // Services
        private readonly ConfigurationService _configService;
        private readonly BrowserService _browserService;
        private readonly AccountService _accountService;
        private readonly ReportService _reportService;
        private readonly SessionService _sessionService;
        private readonly Logger _logger;

        public ProjectOrchestrator(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _showLog = log;
            
            // Initialize services
            _configService = new ConfigurationService(project, log);
            _browserService = new BrowserService(project, instance, log);
            _accountService = new AccountService(project, log);
            _reportService = new ReportService(project, instance, log);
            _sessionService = new SessionService(project, instance, log);
            _logger = new Logger(project, log: log, classEmoji: "🎯");
        }

        public void InitVariables(string author = "")
        {
            _configService.InitializeVariables(author);
        }

        public void InitProject(string author = "w3bgr3p", string[] customQueries = null, bool log = false)
        {
            _logger.Send("Initializing project...");
            
            // Initialize configuration
            InitVariables(author);
            
            // Prepare database
            PrepareDatabase();
            
            // Build query list
            var allQueries = BuildQueryList(customQueries);
            
            // Prepare account list
            if (allQueries.Count > 0)
            {
                _accountService.MakeAccList(allQueries);
            }
            else
            {
                throw new Exception($"No queries to process for mode: {_project.Variables["wkMode"].Value}");
            }
            
            _logger.Send("Project initialization complete");
        }

        private void PrepareDatabase()
        {
            _project.TblPrepareDefault(log: _showLog);
            BuildNewDatabaseIfNeeded();
        }

        private void BuildNewDatabaseIfNeeded()
        {
            if (_project.Var("cfgBuildDb") != "True") return;

            string filePath = System.IO.Path.Combine(_project.Path, "DbBuilder.zp");
            if (System.IO.File.Exists(filePath))
            {
                _project.Var("projectScript", filePath);
                _project.Var("wkMode", "Build");
                _project.Var("cfgAccRange", _project.Var("rangeEnd"));

                var vars = new List<string> {
                    "cfgLog", "cfgPin", "cfgAccRange", "DBmode",
                    "DBpstgrPass", "DBpstgrUser", "DBsqltPath",
                    "debug", "lastQuery", "wkMode"
                };
                _project.RunZp(vars);
            }
            else
            {
                _logger.Warn($"File {filePath} not found. Download from: https://raw.githubusercontent.com/w3bgrep/z3nFarm/master/DbBuilder.zp");
            }
        }

        private List<string> BuildQueryList(string[] customQueries)
        {
            var allQueries = ToDoQueries();
            
            if (customQueries != null)
            {
                foreach (var query in customQueries)
                    allQueries.Add(query);
            }
            
            return allQueries;
        }

        private List<string> ToDoQueries(string toDo = null, string defaultRange = null, 
            string defaultDoFail = null, string customCondition = null)
        {
            string tableName = _project.ProjectTable();
            string nowIso = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            if (string.IsNullOrEmpty(toDo))
                toDo = _project.Variables["cfgToDo"].Value;

            var taskIds = toDo.Split(',')
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

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
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                    out DateTime parsed) ? parsed : DateTime.MinValue)
                .Select(x => x.query)
                .ToList();

            return result;
        }

        private static string Quote(string name)
        {
            return $"\"{name.Replace("\"", "\"\"")}\"";
        }

        public void PrepareProject(bool log = false)
        {
            bool forced = !string.IsNullOrEmpty(_project.Var("acc0Forced"));
            if (log) _logger.Send($"PrepareProject started. Forced mode: {forced}");

            if (forced)
            {
                HandleForcedAccount();
            }
            else
            {
                SelectAndPrepareAccount(log);
            }

            PrepareInstance();
            SetProjectState();
        }

        private void HandleForcedAccount()
        {
            _logger.Send($"Using forced account: {_project.Var("acc0Forced")}");
            _project.Var("acc0", _project.Var("acc0Forced"));
            _project.GSetAcc(force: true);
        }

        private void SelectAndPrepareAccount(bool log)
        {
            string acc = "";
            
            while (true)
            {
                _logger.Send("BusyList: " + string.Join(" | ", _project.GGetBusyList()));
                
                try
                {
                    acc = _accountService.GetAccByMode();
                    var currentState = _project.GVar($"acc{acc}");
                    
                    _logger.Send($"Trying to set [{currentState} => check]");
                    
                    if (currentState == "")
                    {
                        _project.GVar($"acc{acc}", "check");
                    }
                    else
                    {
                        continue; // Account is busy, try another
                    }
                    
                    // Apply filters
                    ApplyAccountFilters(acc);
                    break; // Success
                }
                catch (Exception ex)
                {
                    if (ex.Message == "TimeToChill")
                        throw;
                    
                    _project.SendWarningToLog(ex.Message);
                    
                    if (!string.IsNullOrEmpty(acc))
                        _project.GVar($"acc{acc}", "");
                    
                    if (log)
                        _project.SendWarningToLog($"acc{acc} Filter failed, resetting and retrying");
                }
            }
        }

        private void ApplyAccountFilters(string acc)
        {
            BlockchainFilter();
            SocialFilter();
        }

        private void BlockchainFilter()
        {
            if (!string.IsNullOrEmpty(_project.Var("acc0Forced")))
                return;

            string[] chains = _project.Var("gateOnchainChain").Split(',');

            // Check gas price
            CheckGasPrice(chains);
            
            // Check native balance
            CheckNativeBalance(chains);
        }

        private void CheckGasPrice(string[] chains)
        {
            if (_project.Var("gateOnchainMaxGas") != "")
            {
                decimal maxGas = decimal.Parse(_project.Var("gateOnchainMaxGas"));
                
                foreach (string chain in chains)
                {
                    decimal gas = W3bTools.GasPrice(Rpc.Get(chain));
                    if (gas < maxGas)
                        return; // Gas is acceptable
                }
                
                throw new Exception($"Gas is over the limit: {maxGas} on {_project.Var("gateOnchainChain")}");
            }
        }

        private void CheckNativeBalance(string[] chains)
        {
            if (_project.Var("gateOnchainMinNative") == "")
                return;

            decimal minNativeInUsd = decimal.Parse(_project.Var("gateOnchainMinNative"));
            string address = _project.DbGet("evm_pk", "_addresses", log: false);

            foreach (string chain in chains)
            {
                decimal native = GetNativeBalance(chain, address);
                string ticker = GetChainTicker(chain);
                
                var required = _project.Var("nativeBy") == "native" 
                    ? minNativeInUsd 
                    : _project.UsdToToken(minNativeInUsd, ticker, "OKX");
                
                if (native >= required)
                {
                    _logger.Send($"{address} has sufficient [{native}] native in {chain}", 
                        color: ZennoLab.InterfacesLibrary.Enums.Log.LogColor.Gray);
                    return;
                }
                
                _logger.Warn($"No balance required: [{required}${ticker}] in {chain}. Native is [{native}] for {address}");
            }
            
            _project.DbUpd($"status = '! noBalance', daily = '{Time.Cd(60)}'");
            throw new Exception($"No balance required: [{minNativeInUsd}] in chains {_project.Var("gateOnchainChain")}");
        }

        private decimal GetNativeBalance(string chain, string address)
        {
            if (chain == "solana")
            {
                address = _project.DbGet("sol", "public_blockchain", log: false);
                return W3bTools.SolNative(Rpc.Get(chain), address);
            }
            
            return _project.EvmNative(Rpc.Get(chain), address);
        }

        private string GetChainTicker(string chain)
        {
            switch (chain)
            {
                case "bsc":
                case "opbnb":
                    return "BNB";
                case "solana":
                    return "SOL";
                case "avalance":
                    return "AVAX";
                default:
                    return "ETH";
            }
        }

        private void SocialFilter()
        {
            if (string.IsNullOrEmpty(_project.Var("requiredSocial")))
                return;

            var requiredSocials = _project.Var("requiredSocial").Split(',').ToList();
            var badStatuses = new List<string> {
                "suspended", "restricted", "ban", 
                "CAPTCHA", "applyed", "Verify"
            };

            foreach (var social in requiredSocials)
            {
                var tableName = "_" + social.ToLower().Trim();
                var status = _project.DbGet("status", tableName, log: true);
                
                foreach (string badStatus in badStatuses)
                {
                    if (status.Contains(badStatus))
                    {
                        string exMsg = $"{social} of {_project.Var("acc0")}: [{status}]";
                        _logger.Warn(exMsg);
                        throw new Exception(exMsg);
                    }
                }
            }
        }

        public void PrepareInstance()
        {
            _browserService.LaunchBrowser();
            SetupBrowserWithRetries();
            PrepareToDoList();
            
            _logger.Send($"{_instance.BrowserType} started in {_project.Age<string>()}");
        }

        private void SetupBrowserWithRetries()
        {
            const int maxAttempts = 3;
            int attempts = 0;
            
            while (attempts < maxAttempts)
            {
                try
                {
                    if (_instance.BrowserType == ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.Chromium 
                        && _project.Variables["acc0"].Value != "")
                    {
                        _browserService.SetBrowser();
                    }
                    else
                    {
                        new NetHttp(_project, false).CheckProxy();
                    }
                    
                    break;
                }
                catch (Exception ex)
                {
                    _instance.CloseAllTabs();
                    _logger.Warn($"Launch instance error: {ex.Message}");
                    attempts++;
                    
                    if (attempts >= maxAttempts)
                    {
                        _project.GVar($"acc{_project.Variables["acc0"].Value}", "");
                        throw;
                    }
                }
            }
            
            _instance.CloseExtraTabs(true);
        }

        private void PrepareToDoList()
        {
            foreach (string task in _project.Variables["cfgToDo"].Value.Split(','))
            {
                _project.Lists["toDo"].Add(task.Trim());
            }
        }

        private void SetProjectState()
        {
            var acc = _project.Var("acc0");
            var projectName = _project.ProjectName();
            _project.GVar($"acc{acc}", projectName);
            _logger.Send($"Running acc{acc} with {projectName}");
        }

        public bool RunProject(List<string> additionalVars = null, bool add = true)
        {
            string pathZp = _project.Var("projectScript");
            var vars = GetProjectVariables(additionalVars, add);
            
            _logger.Send($"Running {pathZp}");
            
            var mapVars = new List<Tuple<string, string>>();
            if (vars != null)
            {
                foreach (var v in vars)
                    mapVars.Add(new Tuple<string, string>(v, v));
            }
            
            return _project.ExecuteProject(pathZp, mapVars, true, true, true);
        }

        private List<string> GetProjectVariables(List<string> additionalVars, bool add)
        {
            var baseVars = new List<string> {
                "acc0", "accRnd", "cfgChains", "cfgRefCode", "cfgDelay",
                "cfgLog", "cfgPin", "cfgToDo", "DBmode", "DBpstgrPass",
                "DBpstgrUser", "DBsqltPath", "failReport", "humanNear",
                "instancePort", "ip", "lastQuery", "pathCookies",
                "projectName", "projectTable", "projectScript", "proxy",
                "requiredSocial", "requiredWallets", "toDo", "varSessionId", "wkMode"
            };

            if (additionalVars == null)
                return baseVars;

            if (add)
            {
                foreach (var varName in additionalVars)
                {
                    if (!baseVars.Contains(varName))
                        baseVars.Add(varName);
                }
                return baseVars;
            }
            
            return additionalVars;
        }

        // Additional helper methods
        public void LoadSocials(string requiredSocial)
        {
            _browserService.LoadSocials(requiredSocial);
        }

        public void LoadWallets(string walletsToUse)
        {
            _browserService.LoadWallets(walletsToUse);
        }

        public void FinishSession()
        {
            _sessionService.FinishSession();
        }
    }
}