
// ============================================
// CORE SERVICES
// ============================================

namespace z3nCore.Services
{
    using z3nCore.Interfaces;
    using ZennoLab.CommandCenter;
    using ZennoLab.InterfacesLibrary.ProjectModel;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;

    // Сервис управления переменными и конфигурацией
    public class ConfigurationService
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;

        public ConfigurationService(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project;
            _logger = new Logger(project, log: log, classEmoji: "⚙️");
        }

        public void InitializeVariables(string author = "")
        {
            DisableLogs();
            SetSessionVariables();
            SetProjectVariables();
            CheckRequiredVariables();
            InitializeSAFU();
            DisplayLogo(author);
        }

        private void DisableLogs()
        {
            string currentProcessPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string processDir = Path.GetDirectoryName(currentProcessPath);
            string pathLogs = Path.Combine(processDir, "Logs");

            try
            {
                if (Directory.Exists(pathLogs))
                {
                    Directory.Delete(pathLogs, true);
                    using (var process = new System.Diagnostics.Process())
                    {
                        process.StartInfo.FileName = "cmd.exe";
                        process.StartInfo.Arguments = $"/c mklink /d \"{pathLogs}\" \"NUL\"";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.Start();
                        process.WaitForExit();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to disable logs: {ex.Message}");
            }
        }

        private void SetSessionVariables()
        {
            string sessionId = _project.SetSessionId();
            string projectName = _project.ProjectName();
            string projectTable = _project.ProjectTable();
            
            if (_project.Var("captchaModule") != "")
                _project.CaptchaModule();
        }

        private void SetProjectVariables()
        {
            _project.Range();
        }

        private void CheckRequiredVariables()
        {
            string[] requiredVars = { "cfgAccRange" };
            foreach (string var in requiredVars)
            {
                if (string.IsNullOrEmpty(_project.Variables[var].Value))
                {
                    throw new Exception($"Required variable {var} is null or empty");
                }
            }
        }

        private void InitializeSAFU()
        {
            SAFU.Initialize(_project);
        }

        private void DisplayLogo(string author)
        {
            var versions = GetVersions();
            string dllVer = versions[0];
            string zpVer = versions[1];
            
            if (!string.IsNullOrEmpty(author)) 
                author = $" script author: @{author}";
            
            string logo = $@"using ZennoPoster v{zpVer}; 
             using z3nCore v{dllVer}
            ┌by─┐					
            │    w3bgr3p;		
            └─→┘
                        ► init {_project.ProjectName()} ░ ▒ ▓ █  {author}";
            
            _project.SendInfoToLog(logo, true);
        }

        private string[] GetVersions()
        {
            string currentProcessPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            string processDir = Path.GetDirectoryName(currentProcessPath);
            string path_dll = Path.Combine(processDir, "ExternalAssemblies", "z3nCore.dll");
            string dllVer = System.Diagnostics.FileVersionInfo.GetVersionInfo(path_dll).FileVersion;
            string zpVer = processDir.Split('\\')[5];
            return new[] { dllVer, zpVer };
        }
    }

    // Сервис управления браузером
    public class BrowserService : IBrowserManager
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;

        public BrowserService(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _logger = new Logger(project, log: log, classEmoji: "🌐");
        }

        public void LaunchBrowser(string cfgBrowser = null)
        {
            string acc0 = _project.Var("acc0");
            if (string.IsNullOrEmpty(acc0))
                throw new ArgumentException("acc0 can't be null or empty");

            string pathProfile = Path.Combine(
                _project.Var("profiles_folder"),
                "accounts", 
                "profilesFolder", 
                acc0
            );
            _project.Var("pathProfileFolder", pathProfile);

            if (string.IsNullOrEmpty(cfgBrowser))
                cfgBrowser = _project.Var("cfgBrowser");

            var browserType = ParseBrowserType(cfgBrowser);
            LaunchWithSettings(browserType, pathProfile);
        }

        private ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType ParseBrowserType(string cfgBrowser)
        {
            switch (cfgBrowser)
            {
                case "WithoutBrowser":
                    return ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.WithoutBrowser;
                case "ZennoBrowser":
                    return ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.ChromiumFromZB;
                case "Chromium":
                    return ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.Chromium;
                default:
                    throw new Exception($"Unknown browser config: {cfgBrowser}");
            }
        }

        private void LaunchWithSettings(ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType browserType, string pathProfile)
        {
            if (browserType == ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.WithoutBrowser)
            {
                _instance.Launch(browserType, false);
            }
            else
            {
                var settings = (ZennoLab.CommandCenter.Classes.BuiltInBrowserLaunchSettings)
                    ZennoLab.CommandCenter.Classes.BrowserLaunchSettingsFactory.Create(browserType);
                settings.CachePath = pathProfile;
                settings.ConvertProfileFolder = true;
                settings.UseProfile = true;
                _instance.Launch(settings);
            }
        }

        public void SetBrowser(bool strictProxy = true, string cookies = null, bool log = false)
        {
            string acc0 = _project.Var("acc0");
            if (string.IsNullOrEmpty(acc0))
                throw new ArgumentException("acc0 can't be null or empty");

            _project.Variables["instancePort"].Value = _instance.Port.ToString();
            _logger.Send($"Init browser in port: {_instance.Port}");

            SetupDisplay();
            SetupProxy(strictProxy);
            SetupCookies(cookies, acc0);
            PerformBrowserScan();
        }

        private void SetupDisplay()
        {
            string webGlData = _project.SqlGet("webgl", "_instance");
            if (!string.IsNullOrEmpty(webGlData))
            {
                ApplyWebGLSettings(webGlData);
            }
            else
            {
                _logger.Warn("WebGL string is empty. Antifraud systems may detect this.");
            }

            _instance.SetWindowSize(1280, 720);
            _project.Profile.AcceptLanguage = "en-US,en;q=0.9";
            _project.Profile.Language = "EN";
            _project.Profile.UserAgentBrowserLanguage = "en-US";
            _instance.UseMedia = false;
        }

        private void ApplyWebGLSettings(string webGl)
        {
            var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(webGl);
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
                string value = GetWebGLValue(jsonObject, pair.Value);
                if (!string.IsNullOrEmpty(value))
                {
                    var preference = (ZennoLab.InterfacesLibrary.Enums.Browser.WebGLPreference)
                        Enum.Parse(typeof(ZennoLab.InterfacesLibrary.Enums.Browser.WebGLPreference), pair.Key);
                    _instance.WebGLPreferences.Set(preference, value);
                }
            }
        }

        private string GetWebGLValue(Newtonsoft.Json.Linq.JObject jsonObject, string key)
        {
            var paths = new[] { "default", "webgl", "webgl2" };
            foreach (var path in paths)
            {
                var value = jsonObject["parameters"]?[path]?[key]?.ToString();
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
            return "";
        }

        private void SetupProxy(bool strictProxy)
        {
            bool goodProxy = new NetHttp(_project, false).ProxySet(_instance);
            if (strictProxy && !goodProxy)
                throw new Exception("Bad proxy");
        }

        private void SetupCookies(string cookies, string acc0)
        {
            string cookiePath = Path.Combine(
                _project.Var("profiles_folder"),
                "accounts",
                "cookies",
                acc0 + ".json"
            );
            _project.Var("pathCookies", cookiePath);

            if (cookies != null)
            {
                _instance.SetCookie(cookies);
            }
            else
            {
                TryLoadCookiesFromDatabase(cookiePath);
            }
        }

        private void TryLoadCookiesFromDatabase(string cookiePath)
        {
            try
            {
                string cookies = _project.SqlGet("cookies", "_instance");
                _instance.SetCookie(cookies);
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to set cookies from database: {ex.Message}");
                TryLoadCookiesFromFile(cookiePath);
            }
        }

        private void TryLoadCookiesFromFile(string cookiePath)
        {
            try
            {
                string cookies = File.ReadAllText(cookiePath);
                _instance.SetCookie(cookies);
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to set cookies from file {cookiePath}: {ex.Message}");
            }
        }

        private void PerformBrowserScan()
        {
            if (_project.Var("skipBrowserScan") != "True")
            {
                var bs = new BrowserScan(_project, _instance);
                if (bs.GetScore().Contains("time"))
                    bs.FixTime();
            }
        }

        public string LoadSocials(string requiredSocial)
        {
            if (_instance.BrowserType != ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.Chromium)
                return "noBrowser";

            var socials = requiredSocial.Split(',');
            var socialLoaders = GetSocialLoaders();
            
            foreach (var social in socials)
            {
                if (!socialLoaders.ContainsKey(social))
                    continue;

                LoadSocialWithRetries(social, socialLoaders[social]);
            }

            _logger.Send($"Socials loaded: [{requiredSocial}]");
            _instance.CloseExtraTabs(true);
            return requiredSocial;
        }

        private Dictionary<string, Action> GetSocialLoaders()
        {
            return new Dictionary<string, Action>
            {
                { "Google", () => new Google(_project, _instance, true).Load() },
                { "Twitter", () => new X(_project, _instance, true).Load() },
                { "Discord", () => new Discord(_project, _instance, true).DSload() },
                { "GitHub", () => new GitHub(_project, _instance, true).Load() }
            };
        }

        private void LoadSocialWithRetries(string social, Action loader)
        {
            const int maxAttempts = 3;
            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    loader();
                    return;
                }
                catch (Exception ex)
                {
                    _instance.CloseAllTabs();
                    if (i == maxAttempts - 1)
                        throw new Exception($"[{social}] failed: {ex.Message}");
                }
            }
        }

        public string LoadWallets(string walletsToUse)
        {
            if (_instance.BrowserType != ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.Chromium)
                return "noBrowser";

            string key = !string.IsNullOrEmpty(_project.Var("accRnd")) ? Rnd.Seed() : null;
            _project.Var("refSeed", key);

            var wallets = walletsToUse.Split(',');
            var walletLoaders = GetWalletLoaders(key);

            LoadWalletsWithRetries(wallets, walletLoaders);
            
            _instance.CloseExtraTabs(true);
            return walletsToUse;
        }

        private Dictionary<string, Action> GetWalletLoaders(string key)
        {
            return new Dictionary<string, Action>
            {
                { "Backpack", () => _project.Var("addressSol", 
                    new BackpackWallet(_project, _instance, key: key, log: false).Launch()) },
                { "Zerion", () => _project.Var("addressEvm", 
                    new ZerionWallet(_project, _instance, key: key, log: false).Launch()) },
                { "Keplr", () => new KeplrWallet(_project, _instance, log: false).Launch() }
            };
        }

        private void LoadWalletsWithRetries(string[] wallets, Dictionary<string, Action> walletLoaders)
        {
            const int maxAttempts = 3;
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                try
                {
                    foreach (var wallet in wallets)
                    {
                        if (walletLoaders.ContainsKey(wallet))
                            walletLoaders[wallet]();
                    }
                    break;
                }
                catch (Exception ex)
                {
                    _instance.CloseAllTabs();
                    _logger.Warn($"Wallet loading failed: {ex.Message}");
                    attempts++;
                    if (attempts >= maxAttempts)
                        throw;
                }
            }
        }
    }

    // Сервис управления аккаунтами
    public class AccountService : IAccountManager
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;

        public AccountService(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project;
            _logger = new Logger(project, log: log, classEmoji: "👤");
        }

        public string GetAccByMode()
        {
            string mode = _project.Var("wkMode");
            _logger.Send($"Mode: {mode}. Choosing from: {string.Join(", ", _project.Lists["accs"])}");

            switch (mode)
            {
                case "Cooldown":
                    return GetCooldownAccount();
                case "Oldest":
                    return GetOldestAccount();
                case "NewRandom":
                    return GetNewRandomAccount();
                default:
                    throw new Exception($"Unknown mode: {mode}");
            }
        }

        private string GetCooldownAccount()
        {
            if (!ChooseSingleAcc())
            {
                SetTimeToChill();
                throw new Exception("TimeToChill");
            }
            return _project.Var("acc0");
        }

        private string GetOldestAccount()
        {
            if (!ChooseSingleAcc(true))
            {
                SetTimeToChill();
                throw new Exception("TimeToChill");
            }
            return _project.Var("acc0");
        }

        private string GetNewRandomAccount()
        {
            string toSet = _project.DbGetRandom("proxy, webgl", "_instance", log: false, acc: true);
            string acc0 = toSet.Split('|')[0];
            
            _project.Var("accRnd", Rnd.RndHexString(64));
            _project.Var("acc0", acc0);
            _project.Var("pathProfileFolder", 
                Path.Combine(_project.Var("profiles_folder"), "accounts", "profilesFolder", _project.Var("accRnd")));
            
            return acc0;
        }

        private void SetTimeToChill()
        {
            _project.Var("acc0", null);
            _project.Var("TimeToChill", "True");
        }

        public bool ChooseSingleAcc(bool oldest = false)
        {
            var listAccounts = _project.Lists["accs"];

            if (listAccounts.Count == 0)
            {
                _project.Variables["noAccsToDo"].Value = "True";
                _project.SendToLog("♻ noAccountsAvailable", 
                    ZennoLab.InterfacesLibrary.Enums.Log.LogType.Info, 
                    true, 
                    ZennoLab.InterfacesLibrary.Enums.Log.LogColor.Turquoise);
                return false;
            }

            int index = oldest ? 0 : new Random().Next(0, listAccounts.Count);
            string acc0 = listAccounts[index];
            
            _project.Var("acc0", acc0);
            listAccounts.RemoveAt(index);
            
            _logger.Send($"Chosen: [acc{acc0}] Left: [{listAccounts.Count}]");
            return true;
        }

        public void FilterAccList(HashSet<string> allAccounts)
        {
            var reqSocials = _project.Var("requiredSocial");

            if (!string.IsNullOrEmpty(reqSocials))
            {
                string[] demanded = reqSocials.Split(',');
                _logger.Send($"Filtering by socials: [{string.Join(", ", demanded)}]");

                foreach (string social in demanded)
                {
                    FilterBySocial(allAccounts, social.Trim());
                }
            }

            _project.Lists["accs"].Clear();
            _project.Lists["accs"].AddRange(allAccounts);
            _logger.Send($"Final list: [{string.Join("|", allAccounts)}]");
        }

        private void FilterBySocial(HashSet<string> allAccounts, string social)
        {
            string tableName = $"projects_{social.ToLower()}";
            string whereClause = "status LIKE '%suspended%' OR status LIKE '%restricted%' " +
                               "OR status LIKE '%ban%' OR status LIKE '%CAPTCHA%' " +
                               "OR status LIKE '%applyed%' OR status LIKE '%Verify%'";
            
            var notOK = _project.SqlGet("id", tableName, log: false, where: whereClause)
                .Split('\n')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x));
            
            allAccounts.ExceptWith(notOK);
            _logger.Send($"After {social} filter: [{string.Join("|", allAccounts)}]");
        }

        public void MakeAccList(List<string> dbQueries)
        {
            if (HandleForcedAccount())
                return;

            var allAccounts = GatherAccountsFromQueries(dbQueries);

            if (allAccounts.Count == 0)
            {
                _project.Variables["noAccsToDo"].Value = "True";
                _logger.Send($"♻ No accounts available by {dbQueries.Count} queries");
                return;
            }

            _logger.Send($"Initial accounts: [{string.Join(", ", allAccounts)}]");
            FilterAccList(allAccounts);
        }

        private bool HandleForcedAccount()
        {
            var forced = _project.Variables["acc0Forced"].Value;
            if (!string.IsNullOrEmpty(forced))
            {
                _project.Lists["accs"].Clear();
                _project.Lists["accs"].Add(forced);
                _logger.Send($"Manual mode with {forced}");
                return true;
            }
            return false;
        }

        private HashSet<string> GatherAccountsFromQueries(List<string> dbQueries)
        {
            var allAccounts = new HashSet<string>();
            
            foreach (var query in dbQueries)
            {
                try
                {
                    var accsByQuery = _project.DbQ(query, log: false).Trim();
                    if (!string.IsNullOrWhiteSpace(accsByQuery))
                    {
                        var accounts = accsByQuery.Split('\n')
                            .Select(x => x.Trim().TrimStart(','));
                        allAccounts.UnionWith(accounts);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Query failed: {query}. Error: {ex.Message}");
                }
            }
            
            return allAccounts;
        }
    }

    // Сервис управления отчетами
    public class ReportService : IReportManager
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;
        private readonly Reporter _reporter;

        public ReportService(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _logger = new Logger(project, log: log, classEmoji: "📊");
            _reporter = new Reporter(project, instance, log);
        }

        public string ErrorReport(bool toTg = false, bool toDb = false, bool screenshot = false)
        {
            return _reporter.ErrorReport(toTg, toDb, screenshot);
        }

        public string SuccessReport(bool log = false, bool toTg = false)
        {
            return _reporter.SuccessReport(log, toTg);
        }

        public void ToTelegram(string reportString)
        {
            _reporter.ToTelegram(reportString);
        }
    }

    // Сервис управления сессией
    public class SessionService : ISessionManager
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;

        public SessionService(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _logger = new Logger(project, log: log, classEmoji: "🔄");
        }

        public void FinishSession()
        {
            string acc0 = _project.Var("acc0");
            string accRnd = _project.Var("accRnd");

            SaveSuccessReport(acc0);
            SaveCookiesIfNeeded(acc0, accRnd);
            ClearAccountState();
        }

        private void SaveSuccessReport(string acc0)
        {
            if (!string.IsNullOrEmpty(acc0))
            {
                try
                {
                    new Reporter(_project, _instance).SuccessReport(true, true);
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Failed to save success report: {ex.Message}");
                }
            }
        }

        private void SaveCookiesIfNeeded(string acc0, string accRnd)
        {
            if (ShouldSaveCookies(acc0, accRnd))
            {
                new Cookies(_project, _instance).Save("all", _project.Var("pathCookies"));
            }
        }

        private bool ShouldSaveCookies(string acc0, string accRnd)
        {
            try
            {
                return _instance.BrowserType == ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.Chromium &&
                       !string.IsNullOrEmpty(acc0) &&
                       string.IsNullOrEmpty(accRnd);
            }
            catch
            {
                return false;
            }
        }

        public void ClearAccountState()
        {
            string acc0 = _project.Var("acc0");
            if (!string.IsNullOrEmpty(acc0))
            {
                _project.GVar($"acc{acc0}", "");
            }
            _project.Var("acc0", "");
        }
    }
}

