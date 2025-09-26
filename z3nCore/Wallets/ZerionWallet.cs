
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
    public class ZerionWallet
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;

        private readonly string _key;
        private readonly string _pass;
        private readonly string _fileName;
        private string _expectedAddress;


        private readonly string _extId = "klghhnkeealcohjjanjjdaeeggmfmlpl";

        private readonly string _urlOnboardingTab = "chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/popup.8e8f209b.html?windowType=tab&appMode=onboarding#/onboarding/import";
        private readonly string _urlPopup = "chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/popup.8e8f209b.html#";
        private readonly string _urlImport = "chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/popup.8e8f209b.html#/get-started/import";


        public ZerionWallet(IZennoPosterProjectModel project, Instance instance, bool log = false, string key = null, string fileName = "Zerion1.21.3.crx")
        {
            _project = project;
            _instance = instance;
            _fileName = fileName;

            _key = KeyLoad(key);
            _pass = SAFU.HWPass(_project);
            _logger = new Logger(project, log: log, classEmoji: "🇿");

        }


        private string KeyLoad(string key)
        {
            if (string.IsNullOrEmpty(key)) key = "key";

            switch (key)
            {
                case "key":
                    key = _project.DbKey("evm");
                    break;
                case "seed":
                    key = _project.DbKey("seed");
                    break;
                default:
                    return key;
            }
            if (string.IsNullOrEmpty(key)) throw new Exception("keyIsEmpy").Throw();

            _expectedAddress = key.ToPubEvm();
            return key;
        }

        public string Launch(string fileName = null, bool log = false, string source = null, string refCode = null)
        {
            if (string.IsNullOrEmpty(fileName))
                fileName = _fileName;
            if (string.IsNullOrEmpty(source))
                source = "key";
            //string active = null;
            var em = _instance.UseFullMouseEmulation;
            _instance.UseFullMouseEmulation = false;

            new ChromeExt(_project, _instance, log: log).Switch(_extId);
            new ChromeExt(_project, _instance).Install(_extId, fileName, log);

        check:
            string state = GetState();
            _logger.Send(state);
            switch (state)
            {
                case "onboarding":
                    Import(source, refCode, log: log);
                    goto check;
                case "noTab":
                    _instance.Go(_urlPopup);
                    goto check;
                case "unlock":
                    Unlock();
                    goto check;
                case "overview":
                    //string current = GetActive();
                    //SwitchSource(source);
                    
                    break;
                default:
                    goto check;
            }

            try { TestnetMode(false); } catch { }
            var address =  ActiveAddress();
            //var address = GetActive();
            _instance.CloseExtraTabs();
            _instance.UseFullMouseEmulation = em;
            _logger.Send($"launched with: {address}",show:true);
            return address;
        }


        private void Add(string source = null, bool log = false)
        {
            string key = KeyLoad(source);
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
        public bool Sign(bool log = false,int deadline = 10)
        {
            //parseURL();
            _project.Deadline();
        scan:
            _project.Deadline(deadline);
            try
            {               
                parseURL();
                try
                {
                    _instance.HeClick(("button", "innertext", "Confirm", "regexp", 0), deadline:1);
                    return true;
                }
                catch { }
                try
                {
                    _instance.HeClick(("button", "innertext", "Sign", "regexp", 0), deadline: 1);
                    return true;
                }
                catch { }
            }
            catch (Exception ex)
            {
                _logger.Send($"!W {ex.Message}");
                throw;
            }
            goto scan;
        }

        public void Connect(bool log = false)
        {

            string action = null;
        getState:

            try
            {
                action = _instance.HeGet(("button", "class", "_primary", "regexp", 0),"last");
            }
            catch 
            {
                _logger.Send($"No Wallet tab found. 0");
                return;
            }

            _logger.Send(action);
            _logger.Send(_instance.ActiveTab.URL.ConvertUrl(true));

            switch (action)
            {
                case "Add":
                    _project.log($"adding {_instance.HeGet(("input:url", "fulltagname", "input:url", "text", 0), atr: "value")}");
                    _instance.HeClick(("button", "class", "_primary", "regexp", 0), "last");
                    goto getState;
                case "Close":
                    _project.log($"added {_instance.HeGet(("div", "class", "_uitext_", "regexp", 0))}");
                    _instance.HeClick(("button", "class", "_primary", "regexp", 0), "last");
                    goto getState;
                case "Connect":
                    _project.log($"connecting {_instance.HeGet(("div", "class", "_uitext_", "regexp", 0))}");
                    _instance.HeClick(("button", "class", "_primary", "regexp", 0), "last");
                    goto getState;
                case "Sign":
                    _project.log($"sign {_instance.HeGet(("div", "class", "_uitext_", "regexp", 0))}");
                    _instance.HeClick(("button", "class", "_primary", "regexp", 0), "last");
                    goto getState;
                case "Sign In":
                    _project.log($"sign {_instance.HeGet(("div", "class", "_uitext_", "regexp", 0))}");
                    _instance.HeClick(("button", "class", "_primary", "regexp", 0), "last");
                    goto getState;

                default:
                    goto getState;

            }


        }

        private void Import(string source = null, string refCode = null, bool log = false)
        {
            string key = KeyLoad(source);
            key = key.Trim().StartsWith("0x") ? key.Substring(2) : key;
            string keyType = key.KeyType();
            _instance.Go(_urlOnboardingTab);
            
            if (string.IsNullOrWhiteSpace(refCode)) refCode = "";

            _logger.Send(keyType);
            var inputRef = false;
            if (!string.IsNullOrEmpty(refCode)) inputRef = true;
            
            if (keyType == "keyEvm")
            {
                _instance.HeClick(("a", "href", "chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/popup.8e8f209b.html\\?windowType=tab&appMode=onboarding#/onboarding/import/private-key", "regexp", 0));
                _instance.ActiveTab.FindElementByName("key").SetValue(key, "Full", false);
            }
            else if (keyType == "seed")
            {
                _instance.HeClick(("a", "href", "chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/popup.8e8f209b.html\\?windowType=tab&appMode=onboarding#/onboarding/import/mnemonic", "regexp", 0));
                int index = 0;
                foreach (string word in key.Split(' '))
                {
                    _instance.ActiveTab.FindElementById($"word-{index}").SetValue(word, "Full", false);
                    index++;
                }
            }

            _instance.HeClick(("button", "innertext", "Import\\ wallet", "regexp", 0));
            _instance.HeSet(("input:password", "fulltagname", "input:password", "text", 0), _pass);
            _instance.HeClick(("button", "class", "_primary", "regexp", 0));
            _instance.HeSet(("input:password", "fulltagname", "input:password", "text", 0), _pass);
            _instance.HeClick(("button", "class", "_primary", "regexp", 0));
            if (inputRef)
            {
                _instance.HeClick(("button", "innertext", "Enter\\ Referral\\ Code", "regexp", 0));
                _instance.HeSet((("referralCode", "name")), refCode);
                _instance.HeClick(("button", "class", "_regular", "regexp", 0));
            }
            Thread.Sleep(1000);
            _instance.CloseExtraTabs(true);
            _instance.Go(_urlPopup);
        }

        private void Unlock(bool log = false)
        {
            try
            {
                _instance.HeSet(("input:password", "fulltagname", "input:password", "text", 0), _pass, deadline: 3);
                _instance.HeClick(("button", "class", "_primary", "regexp", 0));
            }
            catch (Exception ex)
            {
                _logger.Send(ex.Message);
            }
            if (!_instance.ActiveTab.FindElementByAttribute("div", "innertext", "Incorrect\\ password", "regexp", 0).IsVoid)
            {
                
                _instance.UninstallExtension("klghhnkeealcohjjanjjdaeeggmfmlpl");
                throw new Exception("Incorrect password");

            }
        }

        public void SwitchSource(string addressToUse = "key")
        {

            _project.Deadline();

            if (addressToUse == "key") addressToUse = _project.DbKey("evm").ToPubEvm();
            else if (addressToUse == "seed") addressToUse = _project.DbKey("seed").ToPubEvm();
            else throw new Exception("supports \"key\" | \"seed\" only");
            _expectedAddress = addressToUse;

        go:
            //_instance.Go(_urlWalletSelect);
            _instance.HeClick(("a", "href", "chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/popup.8e8f209b.html\\#/wallet-select", "regexp", 0));
            Thread.Sleep(1000);

        waitWallets:
            _project.Deadline(60);
            if (_instance.ActiveTab.FindElementByAttribute("button", "class", "_wallet", "regexp", 0).IsVoid) 
                goto waitWallets;

            var wallets = _instance.ActiveTab.FindElementsByAttribute("button", "class", "_wallet", "regexp").ToList();

            foreach (HtmlElement wallet in wallets)
            {
                string masked = "";
                string balance = "";
                string ens = "";

                if (wallet.InnerHtml.Contains("M18 21a2.9 2.9 0 0 1-2.125-.875A2.9 2.9 0 0 1 15 18q0-1.25.875-2.125A2.9 2.9 0 0 1 18 15a3.1 3.1 0 0 1 .896.127 1.5 1.5 0 1 0 1.977 1.977Q21 17.525 21 18q0 1.25-.875 2.125A2.9 2.9 0 0 1 18 21")) continue;
                if (wallet.InnerText.Contains("·"))
                {
                    ens = wallet.InnerText.Split('\n')[0].Split('·')[0];
                    masked = wallet.InnerText.Split('\n')[0].Split('·')[1];
                    balance = wallet.InnerText.Split('\n')[1].Trim();

                }
                else
                {
                    masked = wallet.InnerText.Split('\n')[0];
                    balance = wallet.InnerText.Split('\n')[1];
                }
                masked = masked.Trim();

                _logger.Send($"[{masked}]{masked.ChkAddress(addressToUse)}[{addressToUse}]");

                if (masked.ChkAddress(addressToUse))
                {
                    _instance.HeClick(wallet);
                    return;
                }
            }
            _logger.Send("address not found");
            Add("seed");

            _instance.CloseExtraTabs(true);
            goto go;


        }

        private void TestnetMode(bool testMode = false)
        {
            bool current;

            string testmode = _instance.HeGet(("input:checkbox", "fulltagname", "input:checkbox", "text", 0), deadline: 1, atr: "value");

            if (testmode == "True")
                current = true;
            else
                current = false;

            if (testMode != current)
                _instance.HeClick(("input:checkbox", "fulltagname", "input:checkbox", "text", 0));

        }

        public bool WaitTx(int deadline = 60, bool log = false)
        {
            DateTime functionStart = DateTime.Now;
        check:
            bool result;
            if ((DateTime.Now - functionStart).TotalSeconds > deadline) throw new Exception($"!W Deadline [{deadline}]s exeeded");


            if (!_instance.ActiveTab.URL.Contains("chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/sidepanel.21ca0c41.html#/overview/history"))
            {
                Tab tab = _instance.NewTab("zw");
                if (tab.IsBusy) tab.WaitDownloading();
                _instance.ActiveTab.Navigate("chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/sidepanel.21ca0c41.html#/overview/history", "");

            }
            Thread.Sleep(2000);

            var status = _instance.HeGet(("div", "style", "padding: 0px 16px;", "regexp", 0));



            if (status.Contains("Pending")) goto check;
            else if (status.Contains("Failed")) result = false;
            else if (status.Contains("Execute")) result = true;
            else
            {
                _logger.Send($"unknown status {status}");
                goto check;
            }
            _instance.CloseExtraTabs();
            return result;

        }

        public List<string> Claimable(string address)
        {
            var res = new List<string>();
            var _h = new NetHttp(_project);
            address = address.ToLower();

            string url = $@"https://dna.zerion.io/api/v1/memberships/{address}/quests";

            var headers = new Dictionary<string, string>
            {
                { "Accept", "*/*" },
                { "Accept-Language", "en-US,en;q=0.9" },
                { "Origin", "https://app.zerion.io" },
                { "Referer", "https://app.zerion.io" },
                { "Priority", "u=1, i" }
            };

            string response = _h.GET(
                url: url,
                proxyString: "+",
                headers: headers,
                parse: false
            );


            int i = 0;
            try
            {
                JArray jArr = JArray.Parse(response);
                while (true)
                {
                    var id = "";
                    var kind = "";
                    var link = "";
                    var reward = "";
                    var kickoff = "";
                    var deadline = "";
                    var recurring = "";
                    var claimable = "";

                    try
                    {

                        id = jArr[i]["id"].ToString();
                        kind = jArr[i]["kind"].ToString();
                        recurring = jArr[i]["recurring"].ToString();
                        reward = jArr[i]["reward"].ToString();
                        kickoff = jArr[i]["kickoff"].ToString();
                        deadline = jArr[i]["deadline"].ToString();
                        claimable = jArr[i]["claimable"].ToString();
                        try { link = jArr[i]["meta"]["mint"]["link"]["url"].ToString(); } catch { }
                        try { link = jArr[i]["meta"]["call"]["link"]["url"].ToString(); } catch { }
                        var toLog = $"Unclaimed [{claimable}]Exp on Zerion  [{kind}]  [{id}]";
                        if (claimable != "0")
                        {
                            res.Add(id);
                            _project.log(toLog);
                        }
                        i++;
                    }
                    catch
                    {
                        break;
                    }
                }
            }
            catch
            {
                _project.log($"!W failed to parse : [{response}] ");
            }
            return res;

        }

        private string GetState()
        {
        check:
            string state = null;
            //Thread.Sleep(1000);
            if (!_instance.ActiveTab.URL.Contains(_extId))
                state = "noTab";
            else if (_instance.ActiveTab.URL.Contains("onboarding"))
                state = "onboarding";
            else if (_instance.ActiveTab.URL.Contains("login"))
                state = "unlock";
            else if (_instance.ActiveTab.URL.Contains("overview"))
                state = "overview";

            else
                goto check;
            return state;
        }

        private string GetActive()
        {
            string activeWallet = _instance.HeGet(("a", "href", "chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/popup.8e8f209b.html\\#/wallet-select", "regexp", 0));
            string total = _instance.HeGet(("div", "style", "display:\\ grid;\\ gap:\\ 0px;\\ grid-template-columns:\\ minmax\\(0px,\\ auto\\);\\ align-items:\\ start;", "regexp", 0)).Split('\n')[0];          
            _logger.Send($"wallet Now {activeWallet}  [{total}]");
            return activeWallet;
        }

        private void parseURL()
        {
            var urlNow = _instance.ActiveTab.URL;
            try
            {

                var type = "null";
                var data = "null";
                var origin = "null";

                var parts = urlNow.Split('?').ToList();

                foreach (string part in parts)
                {
                    //project.SendInfoToLog(part);
                    if (part.StartsWith("windowType"))
                    {
                        type = part.Split('=')[1];
                    }
                    if (part.StartsWith("origin"))
                    {
                        origin = part.Split('=')[1];
                        data = part.Split('=')[2];
                        data = data.Split('&')[0].Trim();
                    }

                }
                dynamic txData = JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(data);
                var gas = txData.gas.ToString();
                var value = txData.value.ToString();
                var sender = txData.from.ToString();
                var recipient = txData.to.ToString();
                var datastring = $"{txData.data}";


                BigInteger gasWei = BigInteger.Parse("0" + gas.TrimStart('0', 'x'), NumberStyles.AllowHexSpecifier);
                decimal gasGwei = (decimal)gasWei / 1000000000m;
                _logger.Send($"Sending {datastring} to {recipient}, gas: {gasGwei}");

            }
            catch { }
        }

        public string ActiveAddress()
        {
            var address = _instance.HeGet(("a", "href", "chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/popup.8e8f209b.html#/receive\\?address=", "regexp", 0), atr: "href")
                .Replace("chrome-extension://klghhnkeealcohjjanjjdaeeggmfmlpl/popup.8e8f209b.html#/receive?address=", "");
            _logger.Send($"active address: {address}");
            return address;
        }


        public static string TxFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL is null or empty");

            try
            {
                var uri = new Uri(url);
                var query = uri.Fragment.Contains("?") ? uri.Fragment.Split('?')[1] : uri.Query.TrimStart('?');

                string transactionJson = null;
                var pairs = query.Split('&');
                foreach (var pair in pairs)
                {
                    if (pair.StartsWith("transaction="))
                    {
                        transactionJson = Uri.UnescapeDataString(pair.Substring("transaction=".Length));
                        break;
                    }
                }

                if (string.IsNullOrEmpty(transactionJson))
                    throw new ArgumentException("Transaction data not found in URL");

                var temp = JsonConvert.DeserializeObject<Dictionary<string, string>>(transactionJson);
                if (temp == null || !temp.ContainsKey("to") || !temp.ContainsKey("from"))
                    throw new ArgumentException("Invalid transaction data");

                return transactionJson;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse transaction from URL: {ex.Message}, InnerException: {ex.InnerException?.Message}", ex);
            }
        }

        public static string Replace(string tx)
        {
            return "";
        }

    }

}
