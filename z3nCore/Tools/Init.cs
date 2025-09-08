using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.Enums.Browser;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class Init
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;
        //protected readonly bool _logShow;
        //protected readonly string _pass;
        //private readonly Sql _sql;

        public Init(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            //_sql = new Sql(_project);
            //_logShow = log;
            _logger = new Logger(project, log: log, classEmoji: "►");
            _instance = instance;
        }
        public Init(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project;
            //_logShow = log;
            _logger = new Logger(project, log: log, classEmoji: "►");
            //_sql = new Sql(_project);
        }

        public void InitVariables(string author = "")
        {
            new FS(_project).DisableLogs();


            string fileName = System.IO.Path.GetFileName(_project.Variables["projectScript"].Value);

            string sessionId = (DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString();
            string projectName = fileName.Split('.')[0];
            string version = Assembly.GetExecutingAssembly()
               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
               ?.InformationalVersion ?? "Unknown";
            string dllTitle = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyTitleAttribute>()
                ?.Title ?? "Unknown";

            

            _project.Variables["projectName"].Value = projectName;
            _project.Variables["varSessionId"].Value = sessionId;
            try { _project.Variables["nameSpace"].Value = dllTitle; } catch { }

            string[] vars = { "cfgAccRange", };
            CheckVariables(vars);

            _project.Variables["projectTable"].Value = "__" + projectName;

            _project.Range();
            SAFU.Initialize(_project);
            Logo(author, dllTitle, projectName);

        }
        private void Logo(string author, string dllTitle, string projectName)
        {
            string version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "Unknown";
            if (author != "") author = $" script author: @{author}";
            string logo = $@"using {dllTitle} v{version};
            ┌by─┐					
            │    w3bgrep			
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
                    _project.L0g(ex.Message);
                    throw;
                }
            }
        }
        private void SetBrowser(bool strictProxy = true, string cookies = null, bool log = false)
        {
            _project.Variables["instancePort"].Value = _instance.Port.ToString();
            _logger.Send($"init browser in port: {_instance.Port}");

            string webGlData = _project.SqlGet("webgl", "_instance");
            SetDisplay(webGlData);

            bool goodProxy = new NetHttp(_project, log).ProxySet(_instance);
            if (strictProxy && !goodProxy) throw new Exception($"!E bad proxy");

            string cookiePath = $"{_project.Variables["profiles_folder"].Value}accounts\\cookies\\{_project.Variables["acc0"].Value}.json";
            _project.Variables["pathCookies"].Value = cookiePath;

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
            if (string.IsNullOrEmpty(cfgBrowser))
             cfgBrowser = _project.Var("cfgBrowser");
            var browser = ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.Chromium;

            switch (cfgBrowser)
            {
                case "Chromium":
                    break;
                case "WithoutBrowser":
                    browser = ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.WithoutBrowser;
                    break;
                case "ZennoBrowser":
                    browser = ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.ChromiumFromZB;
                    break;	
                default:
                    _project.SendWarningToLog($"unknown browser config {cfgBrowser}");
                    throw new Exception ($"unknown browser config {cfgBrowser}");
            }


            ZennoLab.CommandCenter.Classes.BuiltInBrowserLaunchSettings settings = 
                (ZennoLab.CommandCenter.Classes.BuiltInBrowserLaunchSettings)
                ZennoLab.CommandCenter.Classes.BrowserLaunchSettingsFactory.Create(browser);
            settings.CachePath = _project.Var("pathProfileFolder"); 
            settings.ConvertProfileFolder = true;
            settings.UseProfile = true;

            _instance.Launch(settings);

            if (_instance.ActiveTab.IsNull || _instance.ActiveTab.IsVoid) {
                _project.SendErrorToLog($"Can't start {cfgBrowser} from {settings.CachePath}", false);
                throw new Exception($"Can't start {cfgBrowser} from {settings.CachePath}");
            }

        }
        
        public void PrepareInstance()
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
        public void FilterAccList(List<string> dbQueries, bool log = false, bool filterSocials = true)
        {
            if (!string.IsNullOrEmpty(_project.Variables["acc0Forced"].Value))
            {
                _project.Lists["accs"].Clear();
                _project.Lists["accs"].Add(_project.Variables["acc0Forced"].Value);
                _logger.Send($@"manual mode on with {_project.Variables["acc0Forced"].Value}");
                return;
            }

            var allAccounts = new HashSet<string>();
            foreach (var query in dbQueries)
            {
                try
                {
                    var accsByQuery = _project.DbQ(query).Trim();
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


            if (filterSocials)
            {
                if (!string.IsNullOrEmpty(_project.Variables["requiredSocial"].Value))
                {
                    string[] demanded = _project.Variables["requiredSocial"].Value.Split(',');
                    _logger.Send($"Filtering by socials: [{string.Join(", ", demanded)}]");

                    foreach (string social in demanded)
                    {
                        string tableName = $"private_{social.Trim().ToLower()}";

                        var notOK = _project.SqlGet("id", tableName, where: "status NOT LIKE '%ok%'")//DbQ($"SELECT acc0 FROM {_tableName} WHERE status NOT LIKE '%ok%'", log)
                            .Split('\n')
                            .Select(x => x.Trim())
                            .Where(x => !string.IsNullOrEmpty(x));
                        allAccounts.ExceptWith(notOK);
                        _logger.Send($"After {social} filter: [{string.Join("|", allAccounts)}]");
                    }
                }
            }

            _project.Lists["accs"].Clear();
            _project.Lists["accs"].AddRange(allAccounts);
            _logger.Send($"final list [{string.Join("|", _project.Lists["accs"])}]");
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
                _project.GlobalNull();
                _logger.Send(ex.Message, thr0w: true);
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
            string key = !string.IsNullOrEmpty(_project.Var("accRnd")) ? new Rnd().Seed() : null;
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

    }
}
