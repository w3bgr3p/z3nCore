using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

using System.Threading;

using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    /// <summary>
    /// Manages Zerion wallet extension operations including installation, configuration, and transaction handling
    /// </summary>
    public class Zerion_prod
    {
        #region Constants and URLs
        
        private const string EXTENSION_ID = "klghhnkeealcohjjanjjdaeeggmfmlpl";
        private const string DEFAULT_FILE_NAME = "Zerion1.21.3.crx";
        
        private readonly string _urlOnboardingTab = $"chrome-extension://{EXTENSION_ID}/popup.8e8f209b.html?windowType=tab&appMode=onboarding#/onboarding/import";
        private readonly string _urlPopup = $"chrome-extension://{EXTENSION_ID}/popup.8e8f209b.html#";
        private readonly string _urlImport = $"chrome-extension://{EXTENSION_ID}/popup.8e8f209b.html#/get-started/import";
        
        #endregion

        #region Private Fields
        
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;
        private readonly string _key;
        private readonly string _pass;
        private readonly string _fileName;
        private string _expectedAddress;
        
        #endregion

        #region Constructor
        
        public Zerion_prod(
            IZennoPosterProjectModel project, 
            Instance instance, 
            bool log = false, 
            string key = null, 
            string fileName = DEFAULT_FILE_NAME)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            _fileName = fileName;
            _key = LoadKey(key);
            _pass = SAFU.HWPass(_project);
            _logger = new Logger(project, log: log, classEmoji: "🇿");
        }
        
        #endregion

        #region Public Methods - Main Operations

        /// <summary>
        /// Launches the Zerion wallet with specified configuration
        /// </summary>
        public string Launch(
            string fileName = null, 
            bool log = false, 
            string source = null, 
            string refCode = null)
        {
            fileName = fileName ?? _fileName;
            source = source ?? "key";
            
            var originalMouseEmulation = _instance.UseFullMouseEmulation;
            _instance.UseFullMouseEmulation = false;

            try
            {
                // Install and switch to extension
                new ChromeExt(_project, _instance, log: log).Switch(EXTENSION_ID);
                new ChromeExt(_project, _instance).Install(EXTENSION_ID, fileName, log);

                // Process wallet state
                ProcessWalletState(source, refCode, log);

                // Configure wallet
                ConfigureWallet();

                // Get active address
                var address = ActiveAddress();
                _logger.Send($"Launched with: {address}", show: true);
                
                return address;
            }
            finally
            {
                _instance.CloseExtraTabs();
                _instance.UseFullMouseEmulation = originalMouseEmulation;
            }
        }

        /// <summary>
        /// Signs a transaction or message
        /// </summary>
        public bool Sign(bool log = false, int deadline = 10)
        {
            _project.Deadline();

            while (true)
            {
                _project.Deadline(deadline);
                
                try
                {
                    ParseCurrentTransaction();
                    
                    if (TryClickButton("Confirm", deadline: 1))
                        return true;
                    
                    if (TryClickButton("Sign", deadline: 1))
                        return true;
                }
                catch (Exception ex)
                {
                    _logger.Send($"!W {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Connects wallet to a dApp
        /// </summary>
        public void Connect(bool log = false)
        {
            var stateActions = new Dictionary<string, Action>
            {
                ["Add"] = () => HandleAddAction(),
                ["Close"] = () => HandleCloseAction(),
                ["Connect"] = () => HandleConnectAction(),
                ["Sign"] = () => HandleSignAction(),
                ["Sign In"] = () => HandleSignInAction()
            };

            while (true)
            {
                string action = GetCurrentAction();
                if (action == null)
                {
                    _logger.Send("No wallet tab found");
                    return;
                }

                _logger.Send($"Current action: {action}");
                _logger.Send(_instance.ActiveTab.URL.ConvertUrl(true));

                if (stateActions.ContainsKey(action))
                {
                    stateActions[action]();
                }
            }
        }

        /// <summary>
        /// Waits for transaction completion
        /// </summary>
        public bool WaitTx(int deadline = 60, bool log = false)
        {
            DateTime startTime = DateTime.Now;
            
            NavigateToHistory();

            while ((DateTime.Now - startTime).TotalSeconds <= deadline)
            {
                Thread.Sleep(2000);
                
                var status = GetTransactionStatus();
                
                switch (status)
                {
                    case "Pending":
                        continue;
                    case "Failed":
                        _instance.CloseExtraTabs();
                        return false;
                    case "Execute":
                        _instance.CloseExtraTabs();
                        return true;
                    default:
                        _logger.Send($"Unknown status: {status}");
                        continue;
                }
            }
            
            throw new Exception($"!W Deadline [{deadline}]s exceeded");
        }

        /// <summary>
        /// Gets list of claimable quests for an address
        /// </summary>
        public List<string> Claimable(string address)
        {
            var claimableIds = new List<string>();
            var http = new NetHttp(_project);
            address = address.ToLower();

            string url = $"https://dna.zerion.io/api/v1/memberships/{address}/quests";
            var headers = BuildApiHeaders();

            string response = http.GET(
                url: url,
                proxyString: "+",
                headers: headers,
                parse: false
            );

            try
            {
                JArray quests = JArray.Parse(response);
                
                foreach (var quest in quests)
                {
                    string id = quest["id"]?.ToString();
                    string claimable = quest["claimable"]?.ToString();
                    string kind = quest["kind"]?.ToString();
                    
                    if (claimable != "0" && !string.IsNullOrEmpty(id))
                    {
                        claimableIds.Add(id);
                        _project.log($"Unclaimed [{claimable}] Exp on Zerion [{kind}] [{id}]");
                    }
                }
            }
            catch (Exception ex)
            {
                _project.log($"!W Failed to parse response: {ex.Message}");
            }

            return claimableIds;
        }

        /// <summary>
        /// Gets the currently active wallet address
        /// </summary>
        public string ActiveAddress()
        {
            var href = _instance.HeGet(
                ("a", "href", $"chrome-extension://{EXTENSION_ID}/popup.8e8f209b.html#/receive\\?address=", "regexp", 0), 
                atr: "href"
            );
            
            var address = href.Replace($"chrome-extension://{EXTENSION_ID}/popup.8e8f209b.html#/receive?address=", "");
            _logger.Send($"Active address: {address}");
            
            return address;
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Extracts transaction data from a URL
        /// </summary>
        public static string TxFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL is null or empty");

            try
            {
                var uri = new Uri(url);
                var query = uri.Fragment.Contains("?") 
                    ? uri.Fragment.Split('?')[1] 
                    : uri.Query.TrimStart('?');

                string transactionJson = ExtractTransactionFromQuery(query);
                ValidateTransactionJson(transactionJson);
                
                return transactionJson;
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Failed to parse transaction from URL: {ex.Message}", 
                    ex
                );
            }
        }

        #endregion

        #region Private Methods - Wallet Operations

        private void ProcessWalletState(string source, string refCode, bool log)
        {
            bool stateProcessed = false;
            
            while (!stateProcessed)
            {
                string state = GetWalletState();
                _logger.Send($"Wallet state: {state}");

                switch (state)
                {
                    case "onboarding":
                        Import(source, refCode, log: log);
                        break;
                        
                    case "noTab":
                        _instance.Go(_urlPopup);
                        break;
                        
                    case "unlock":
                        Unlock();
                        break;
                        
                    case "overview":
                        stateProcessed = true;
                        break;
                        
                    default:
                        Thread.Sleep(500);
                        break;
                }
            }
        }

        private void Import(string source, string refCode, bool log)
        {
            string key = LoadKey(source);
            key = key.Trim().StartsWith("0x") ? key.Substring(2) : key;
            string keyType = key.KeyType();
            
            _logger.Send($"Importing {keyType}");
            _instance.Go(_urlOnboardingTab);

            // Import key based on type
            if (keyType == "keyEvm")
            {
                ImportPrivateKey(key);
            }
            else if (keyType == "seed")
            {
                ImportSeedPhrase(key);
            }

            // Complete import process
            CompleteImport(refCode);
        }

        private void ImportPrivateKey(string key)
        {
            _instance.HeClick((
                "a", 
                "href", 
                $"chrome-extension://{EXTENSION_ID}/popup.8e8f209b.html\\?windowType=tab&appMode=onboarding#/onboarding/import/private-key", 
                "regexp", 
                0
            ));
            
            _instance.ActiveTab.FindElementByName("key").SetValue(key, "Full", false);
        }

        private void ImportSeedPhrase(string key)
        {
            _instance.HeClick((
                "a", 
                "href", 
                $"chrome-extension://{EXTENSION_ID}/popup.8e8f209b.html\\?windowType=tab&appMode=onboarding#/onboarding/import/mnemonic", 
                "regexp", 
                0
            ));

            var words = key.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                _instance.ActiveTab.FindElementById($"word-{i}").SetValue(words[i], "Full", false);
            }
        }

        private void CompleteImport(string refCode)
        {
            // Import wallet
            _instance.HeClick(("button", "innertext", "Import\\ wallet", "regexp", 0));
            
            // Set password
            _instance.HeSet(("input:password", "fulltagname", "input:password", "text", 0), _pass);
            _instance.HeClick(("button", "class", "_primary", "regexp", 0));
            
            // Confirm password
            _instance.HeSet(("input:password", "fulltagname", "input:password", "text", 0), _pass);
            _instance.HeClick(("button", "class", "_primary", "regexp", 0));

            // Handle referral code if provided
            if (!string.IsNullOrWhiteSpace(refCode))
            {
                _instance.HeClick(("button", "innertext", "Enter\\ Referral\\ Code", "regexp", 0));
                _instance.HeSet(("referralCode", "name"), refCode);
                _instance.HeClick(("button", "class", "_regular", "regexp", 0));
            }

            _instance.CloseExtraTabs(true);
            _instance.Go(_urlPopup);
        }

        private void Unlock()
        {
            try
            {
                _instance.HeSet(
                    ("input:password", "fulltagname", "input:password", "text", 0), 
                    _pass, 
                    deadline: 3
                );
                _instance.HeClick(("button", "class", "_primary", "regexp", 0));
            }
            catch (Exception ex)
            {
                _logger.Send($"Unlock error: {ex.Message}");
            }

            // Check for incorrect password
            var errorElement = _instance.ActiveTab.FindElementByAttribute(
                "div", 
                "innertext", 
                "Incorrect\\ password", 
                "regexp", 
                0
            );
            
            if (!errorElement.IsVoid)
            {
                _instance.UninstallExtension(EXTENSION_ID);
                throw new Exception("Incorrect password");
            }
        }

        private void Add(string source = null, bool log = false)
        {
            string key = LoadKey(source);
            _instance.Go(_urlImport);

            _instance.HeSet(("seedOrPrivateKey", "name"), key);
            _instance.HeClick(("button", "innertext", "Import", "regexp", 0));
            _instance.HeSet(("input:password", "fulltagname", "input:password", "text", 0), _pass);
            _instance.HeClick(("button", "class", "_primary", "regexp", 0));
            
            try
            {
                _instance.HeClick(("button", "class", "_option", "regexp", 0));
                _instance.HeClick(("button", "class", "_primary", "regexp", 0));
                _instance.HeClick(("button", "class", "_primary", "regexp", 0));
            }
            catch { }
        }

        public void SwitchSource(string addressToUse = "key")
        {
            _project.Deadline();

            // Resolve address
            string targetAddress = ResolveTargetAddress(addressToUse);
            _expectedAddress = targetAddress;

            while (true)
            {
                NavigateToWalletSelect();
                
                if (TrySelectWallet(targetAddress))
                    return;
                
                _logger.Send("Address not found, adding new wallet");
                Add("seed");
                _instance.CloseExtraTabs(true);
            }
        }

        #endregion

        #region Private Methods - Helper Functions

        private string LoadKey(string key)
        {
            if (string.IsNullOrEmpty(key)) 
                key = "key";

            switch (key)
            {
                case "key":
                    key = _project.DbKey("evm");
                    break;
                case "seed":
                    key = _project.DbKey("seed");
                    break;
                default:
                    // Use provided key as-is
                    break;
            }

            if (string.IsNullOrEmpty(key))
                throw new Exception("Key is empty").Throw();

            _expectedAddress = key.ToPubEvm();
            return key;
        }

        private string GetWalletState()
        {
            Thread.Sleep(500);
            
            if (!_instance.ActiveTab.URL.Contains(EXTENSION_ID))
                return "noTab";
            
            if (_instance.ActiveTab.URL.Contains("onboarding"))
                return "onboarding";
            
            if (_instance.ActiveTab.URL.Contains("login"))
                return "unlock";
            
            if (_instance.ActiveTab.URL.Contains("overview"))
                return "overview";
            
            return "unknown";
        }

        private void ConfigureWallet()
        {
            try 
            { 
                TestnetMode(false); 
            } 
            catch 
            { 
                _logger.Send("Failed to set testnet mode");
            }
        }

        private void TestnetMode(bool testMode = false)
        {
            string currentValue = _instance.HeGet(
                ("input:checkbox", "fulltagname", "input:checkbox", "text", 0), 
                deadline: 1, 
                atr: "value"
            );
            
            bool currentMode = currentValue == "True";
            
            if (testMode != currentMode)
            {
                _instance.HeClick(("input:checkbox", "fulltagname", "input:checkbox", "text", 0));
            }
        }

        private string GetCurrentAction()
        {
            try
            {
                return _instance.HeGet(("button", "class", "_primary", "regexp", 0), "last");
            }
            catch
            {
                return null;
            }
        }

        private void HandleAddAction()
        {
            var url = _instance.HeGet(("input:url", "fulltagname", "input:url", "text", 0), atr: "value");
            _project.log($"Adding {url}");
            _instance.HeClick(("button", "class", "_primary", "regexp", 0), "last");
        }

        private void HandleCloseAction()
        {
            var site = _instance.HeGet(("div", "class", "_uitext_", "regexp", 0));
            _project.log($"Added {site}");
            _instance.HeClick(("button", "class", "_primary", "regexp", 0), "last");
        }

        private void HandleConnectAction()
        {
            var site = _instance.HeGet(("div", "class", "_uitext_", "regexp", 0));
            _project.log($"Connecting {site}");
            _instance.HeClick(("button", "class", "_primary", "regexp", 0), "last");
        }

        private void HandleSignAction()
        {
            var site = _instance.HeGet(("div", "class", "_uitext_", "regexp", 0));
            _project.log($"Signing for {site}");
            _instance.HeClick(("button", "class", "_primary", "regexp", 0), "last");
        }

        private void HandleSignInAction()
        {
            var site = _instance.HeGet(("div", "class", "_uitext_", "regexp", 0));
            _project.log($"Signing in to {site}");
            _instance.HeClick(("button", "class", "_primary", "regexp", 0), "last");
        }

        private bool TryClickButton(string buttonText, int deadline)
        {
            try
            {
                _instance.HeClick(("button", "innertext", buttonText, "regexp", 0), deadline: deadline);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void NavigateToHistory()
        {
            string historyUrl = $"chrome-extension://{EXTENSION_ID}/sidepanel.21ca0c41.html#/overview/history";
            
            if (!_instance.ActiveTab.URL.Contains(historyUrl))
            {
                var tab = _instance.NewTab("zw");
                if (tab.IsBusy) 
                    tab.WaitDownloading();
                
                _instance.ActiveTab.Navigate(historyUrl, "");
            }
        }

        private string GetTransactionStatus()
        {
            var statusDiv = _instance.HeGet(("div", "style", "padding: 0px 16px;", "regexp", 0));
            
            if (statusDiv.Contains("Pending")) return "Pending";
            if (statusDiv.Contains("Failed")) return "Failed";
            if (statusDiv.Contains("Execute")) return "Execute";
            
            return "Unknown";
        }

        private void NavigateToWalletSelect()
        {
            _instance.HeClick((
                "a", 
                "href", 
                $"chrome-extension://{EXTENSION_ID}/popup.8e8f209b.html\\#/wallet-select", 
                "regexp", 
                0
            ));
            Thread.Sleep(1000);
            
            // Wait for wallets to load
            _project.Deadline(60);
            while (_instance.ActiveTab.FindElementByAttribute("button", "class", "_wallet", "regexp", 0).IsVoid)
            {
                Thread.Sleep(500);
            }
        }

        private bool TrySelectWallet(string targetAddress)
        {
            var wallets = _instance.ActiveTab
                .FindElementsByAttribute("button", "class", "_wallet", "regexp")
                .ToList();

            foreach (HtmlElement wallet in wallets)
            {
                // Skip add wallet button
                if (wallet.InnerHtml.Contains("M18 21a2.9 2.9 0"))
                    continue;

                var walletInfo = ParseWalletInfo(wallet);
                
                _logger.Send($"[{walletInfo.masked}] checking against [{targetAddress}]");

                if (walletInfo.masked.ChkAddress(targetAddress))
                {
                    _instance.HeClick(wallet);
                    return true;
                }
            }

            return false;
        }

        private (string masked, string balance, string ens) ParseWalletInfo(HtmlElement wallet)
        {
            string masked = "";
            string balance = "";
            string ens = "";

            if (wallet.InnerText.Contains("·"))
            {
                var parts = wallet.InnerText.Split('\n')[0].Split('·');
                ens = parts[0];
                masked = parts[1].Trim();
                balance = wallet.InnerText.Split('\n')[1].Trim();
            }
            else
            {
                var lines = wallet.InnerText.Split('\n');
                masked = lines[0].Trim();
                balance = lines.Length > 1 ? lines[1] : "";
            }

            return (masked, balance, ens);
        }

        private string ResolveTargetAddress(string addressToUse)
        {
            switch (addressToUse)
            {
                case "key":
                    return _project.DbKey("evm").ToPubEvm();
                case "seed":
                    return _project.DbKey("seed").ToPubEvm();
                default:
                    throw new Exception("Supports \"key\" | \"seed\" only");
            }
        }

        private void ParseCurrentTransaction()
        {
            var url = _instance.ActiveTab.URL;
            
            try
            {
                var transactionData = ExtractTransactionFromUrl(url);
                LogTransactionDetails(transactionData);
            }
            catch
            {
                // Transaction parsing is optional for logging
            }
        }

        private dynamic ExtractTransactionFromUrl(string url)
        {
            var parts = url.Split('?').ToList();
            string data = null;

            foreach (string part in parts)
            {
                if (part.StartsWith("origin"))
                {
                    var values = part.Split('=');
                    if (values.Length > 2)
                    {
                        data = values[2].Split('&')[0].Trim();
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(data))
            {
                return JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(data);
            }

            return null;
        }

        private void LogTransactionDetails(dynamic txData)
        {
            if (txData == null) return;

            try
            {
                var gas = txData.gas?.ToString();
                var value = txData.value?.ToString();
                var sender = txData.from?.ToString();
                var recipient = txData.to?.ToString();
                var dataString = $"{txData.data}";

                if (!string.IsNullOrEmpty(gas))
                {
                    BigInteger gasWei = BigInteger.Parse(
                        "0" + gas.TrimStart('0', 'x'), 
                        NumberStyles.AllowHexSpecifier
                    );
                    decimal gasGwei = (decimal)gasWei / 1000000000m;
                    
                    _logger.Send($"Sending {dataString} to {recipient}, gas: {gasGwei} Gwei");
                }
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        private Dictionary<string, string> BuildApiHeaders()
        {
            return new Dictionary<string, string>
            {
                { "Accept", "*/*" },
                { "Accept-Language", "en-US,en;q=0.9" },
                { "Origin", "https://app.zerion.io" },
                { "Referer", "https://app.zerion.io" },
                { "Priority", "u=1, i" }
            };
        }

        private static string ExtractTransactionFromQuery(string query)
        {
            var pairs = query.Split('&');
            
            foreach (var pair in pairs)
            {
                if (pair.StartsWith("transaction="))
                {
                    return Uri.UnescapeDataString(
                        pair.Substring("transaction=".Length)
                    );
                }
            }

            throw new ArgumentException("Transaction data not found in URL");
        }

        private static void ValidateTransactionJson(string transactionJson)
        {
            if (string.IsNullOrEmpty(transactionJson))
                throw new ArgumentException("Transaction data is empty");

            var transaction = JsonConvert.DeserializeObject<Dictionary<string, string>>(transactionJson);
            
            if (transaction == null || !transaction.ContainsKey("to") || !transaction.ContainsKey("from"))
                throw new ArgumentException("Invalid transaction data: missing required fields");
        }

        #endregion
    }
}