using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using z3nCore.Utilities;
using System.Threading.Tasks;
using System.Net.Http;
using Org.BouncyCastle.Crypto.Engines;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;








namespace z3nCore
{
    
    public class SuietWallet_
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;

        private readonly string _key;
        
        private readonly string _fileName;
        private string _expectedAddress;

        private readonly string _extId = "khpkpbbcccdmmclmpigdgddabeilkdpd";
        private readonly string _urlOnboardingTab = "chrome-extension://khpkpbbcccdmmclmpigdgddabeilkdpd/index.html#/onboard/welcome";
        private readonly string _urlPopup = "chrome-extension://khpkpbbcccdmmclmpigdgddabeilkdpd/index.html";
        private readonly string _urlNetworks = "chrome-extension://khpkpbbcccdmmclmpigdgddabeilkdpd/index.html#/settings/network";

        public SuietWallet_(IZennoPosterProjectModel project, Instance instance, bool log = false, string key = null, string fileName = "Suiet-Sui-Wallet-Chrome.crx")
        {
            _project = project;
            _instance = instance;
            _fileName = fileName;
            _logger = new Logger(project, log: log, classEmoji: "SUIet");
        }
        

        public string Launch(string fileName = null, string source = "key")
        {
            if (string.IsNullOrEmpty(fileName)) fileName = _fileName;

            var em = _instance.UseFullMouseEmulation;
            _instance.UseFullMouseEmulation = false;

            _logger.Send($"Launch: file={fileName}, extId={_extId}");
            new ChromeExt(_project, _instance).Switch(_extId);
            if (new ChromeExt(_project, _instance).Install(_extId, fileName))
                Import(source);
            else
                Unlock();

            var adr = ActiveAddress();
            _logger.Send($"Active address: {adr}");
            _instance.CloseExtraTabs();
            _instance.UseFullMouseEmulation = em;
            return adr;
        }
        
        public void Sign()
        {
            _instance.HeClick(("button", "class", "_button--primary_", "regexp", 0));
        }
        
        private void Import(string source = "seed")
        {
            _instance.Go(_urlPopup);
            _instance.HeClick(("button", "innertext", "Import\\ Wallet", "regexp", 0));
            var passw = SAFU.HWPass(_project);
            _instance.HeSet(("input:password", "fulltagname", "input:password", "regexp", 0),passw);
            _instance.HeSet(("input:password", "fulltagname", "input:password", "regexp", 1),passw);
            _instance.HeClick(("button", "innertext", "Next", "regexp", 0));

            var sourseIndex = source == "seed" ? 0 : 1; //0 - seed , 1 - key

            _instance.HeClick(("div", "class", "rounded-2xl\\ cursor-pointer\\ hover:bg-hover\\ border\\ border-border\\ hover:border-zinc-200\\ transition", "regexp", sourseIndex));
            
            if (source == "seed") {
                _instance.HeClick(("secrets.0","id"));
                var seed = _project.DbKey("seed");
                _instance.CtrlV(seed);
            }
            else
            {
                var key = _project.DbKey("evm");
                _instance.HeSet(("privateKey", "name"), key);
            }
            _instance.HeClick(("button", "innertext", "Confirm\\ and\\ Import", "regexp", 0));
            var currentAddress =  _instance.HeGet(("a", "href", "https://pay.suiet.app/\\?wallet_address=", "regexp", 0), atr:"href").Replace("https://pay.suiet.app/?wallet_address=","");
        }

        public void Unlock()
        {
            _instance.Go(_urlPopup);
            var passw = SAFU.HWPass(_project);
            _instance.HeSet(("input:password", "fulltagname", "input:password", "regexp", 0),passw);
            _instance.HeClick(("button", "innertext", "Unlock", "regexp", 0));
        }
        
        public void SwitchChain(string  mode = "Mainnet")
        {
            _instance.Go(_urlNetworks);
            int index = 0;
            switch (mode)
            {
                case "Testnet":
                    index = 1;
                    break;
                case "Devnet":
                    index = 2;
                    break;
                case "Mainnet":
                    break;
                    default:
                        break;
                    
            }

            _instance.HeClick(("div", "class", "_network-selection-container_", "regexp", index));
            _instance.HeClick(("button", "innertext", "Save", "regexp", 0));
            
        }
        
        public string ActiveAddress()
        {
            var currentAddress =  _instance.HeGet(("a", "href", "https://pay.suiet.app/\\?wallet_address=", "regexp", 0), atr:"href").Replace("https://pay.suiet.app/?wallet_address=","");
            return currentAddress;
        }
        
    }







    public class _BackpackWallet
    {
        #region  Members & Constructors
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;

        private readonly string _key;
        private readonly string _pass;
        private readonly string _fileName;

        private readonly string _extId = "aflkmfhebedbjioipglgcbcmnbpgliof";
        private readonly string _popout = $"chrome-extension://aflkmfhebedbjioipglgcbcmnbpgliof/popout.html";
        private readonly string _urlImport = $"chrome-extension://aflkmfhebedbjioipglgcbcmnbpgliof/options.html?onboarding=true";


        public _BackpackWallet(IZennoPosterProjectModel project, Instance instance, bool log = false, string key = null, string fileName = "Backpack0.10.94.crx")
        {
            _project = project;
            _instance = instance;
            _fileName = fileName;
            _key = KeyLoad(key);
            _pass = SAFU.HWPass(_project);
            _logger = new Logger(project, log: log, classEmoji: "🎒");
        }
        #endregion

        #region  Public Methods
        public string Launch(string fileName = null, bool log = false)
        {
            if (string.IsNullOrEmpty(fileName)) fileName = _fileName;

            var em = _instance.UseFullMouseEmulation;
            _instance.UseFullMouseEmulation = false;

            _logger.Send($"Launch: file={fileName}, extId={_extId}");
            new ChromeExt(_project, _instance, log: log).Switch(_extId);
            if (new ChromeExt(_project, _instance).Install(_extId, fileName, log))
                Import(log: log);
            else
                Unlock(log: log);

            var adr = ActiveAddress(log: log);
            _logger.Send($"Active address: {adr}");
            _instance.CloseExtraTabs();
            _instance.UseFullMouseEmulation = em;
            return adr;
        }

        public void Unlock(bool log = false)
        {
            var password = _pass;
            _project.Deadline();

            if (!_instance.ActiveTab.URL.Contains(_popout))
                _instance.ActiveTab.Navigate(_popout, "");

            check:
            Thread.Sleep(1000);
            string state = null;
            _project.Deadline(30);
            if (!_instance.ActiveTab.FindElementByAttribute("path", "d", "M12 5v14", "text", 0).IsVoid) state = "unlocked";
            else if (!_instance.ActiveTab.FindElementByAttribute("input:password", "fulltagname", "input:password", "regexp", 0).IsVoid) state = "unlock";

            _logger.Send($"Unlock state: {state ?? "waiting"}");

            switch (state)
            {
                case null:
                    Thread.Sleep(1000);
                    goto check;
                case "unlocked":
                    return;
                case "unlock":
                    _instance.HeSet(("input:password", "fulltagname", "input:password", "regexp", 0), password);
                    _instance.HeClick(("button", "innertext", "Unlock", "regexp", 0));
                    Thread.Sleep(2000);
                    goto check;
            }
        }

        public void Approve(bool log = false)
        {
            try
            {
                _instance.HeClick(("div", "innertext", "Approve", "regexp", 0), "last");
                _instance.CloseExtraTabs();
                _logger.Send("Approved: direct");
            }
            catch
            {
                _instance.HeSet(("input:password", "fulltagname", "input:password", "regexp", 0), _pass, deadline:3);
                _instance.HeClick(("button", "innertext", "Unlock", "regexp", 0));
                _instance.HeClick(("div", "innertext", "Approve", "regexp", 0), "last");
                _instance.CloseExtraTabs();
                _logger.Send("Approved: after unlock");
            }
        }

        public void Connect(bool log = false)
        {
            _project.Deadline();

            string action = null;
        getState:
            _project.Deadline(30);
            try
            {
                action = _instance.HeGet(("div", "innertext", "Approve", "regexp", 0), "last");
            }
            catch 
            {
                if (!_instance.ActiveTab.FindElementByAttribute("input:password", "fulltagname", "input:password", "regexp", 0).IsVoid)
                {
                    _instance.HeSet(("input:password", "fulltagname", "input:password", "regexp", 0), _pass);
                    _instance.HeClick(("button", "innertext", "Unlock", "regexp", 0));
                    Thread.Sleep(2000);
                    goto getState;
                }

                if (!_instance.ActiveTab.URL.Contains(_extId))
                {
                    _logger.Send($"Connect: no wallet tab, url={_instance.ActiveTab.URL}");
                    return;
                }
            }

            _logger.Send($"Connect action: {action ?? "none"}");

            switch (action)
            {
                case "Approve":
                    _instance.HeClick(("div", "innertext", "Approve", "regexp", 0), "last", emu: 1);
                    goto getState;

                default:
                    goto getState;
            }
        }
        
        #endregion
        
        
        private string KeyLoad(string key)
        {
            if (string.IsNullOrEmpty(key)) key = "key";

            switch (key)
            {
                case "key":
                    key = _project.DbKey("sol");
                    break;
                case "seed":
                    key = _project.DbKey("seed");
                    break;
                default:
                    return key;
            }
            return key;
        }

        private string KeyType(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            if (Regex.IsMatch(input, @"^[0-9a-fA-F]{64}$"))
                return "keyEvm";

            if (Regex.IsMatch(input, @"^[123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz]{87,88}$"))
                return "keySol";

            var words = input.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 12 || words.Length == 24)
                return "seed";

            return null;
        }

        private bool Import(bool log = false)
        {
            _project.Deadline();
            var key = _key;
            var password = _pass;
            var keyType = KeyType(_key);

            var type = "Solana";
            var source = "key";

            if (keyType == "keyEvm") type = "Ethereum";
            if (!keyType.Contains("key")) source = "phrase";

            _instance.CloseExtraTabs();
            _instance.Go(_urlImport);
            _logger.Send($"Import: keyType={keyType}, chain={type}, source={source}");

        check:
            _project.Deadline(60);
            Thread.Sleep(1000);
            string state = null;
            if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Select\\ one\\ or\\ more \\wallets", "regexp", 0).IsVoid) state = "NoFundedWallets";
            else if (!_instance.ActiveTab.FindElementByAttribute("button", "innertext", "Import\\ Wallet", "regexp", 0).IsVoid) state = "importButton";
            else if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Backpack\\ supports\\ multiple\\ blockchains.\\nWhich\\ do\\ you\\ want\\ to\\ use\\?\\ You\\ can\\ add\\ more\\ later.", "regexp", 0).IsVoid) state = "chooseChain";
            else if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Choose\\ a\\ method\\ to\\ import\\ your\\ wallet.", "regexp", 0).IsVoid) state = "chooseSource";
            else if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Enter private key", "text", 0).IsVoid) state = "enterKey";
            else if (!_instance.ActiveTab.FindElementByAttribute("button", "innertext", "Open\\ Backpack", "regexp", 0).IsVoid) state = "open";
            else if (!_instance.ActiveTab.FindElementByAttribute("p", "innertext", "Already\\ setup", "regexp", 0).IsVoid) state = "alreadySetup";
            else if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Enter\\ or\\ paste\\ your\\ 12\\ or\\ 24-word\\ phrase.", "regexp", 0).IsVoid) state = "enterSeed";
            else if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Create\\ a\\ Password", "regexp", 0).IsVoid) state = "inputPass";

            _logger.Send($"Import state: {state ?? "waiting"}");

            switch (state)
            {
                case null:
                    Thread.Sleep(2000);
                    goto check;
                case "importButton":
                    _instance.HeClick(("button", "innertext", "Import\\ Wallet", "regexp", 0));
                    goto check;

                case "chooseChain":
                    _instance.HeClick(("button", "innertext", type, "regexp", 0));
                    goto check;

                case "chooseSource":
                    _instance.HeClick(("button", "innertext", source, "text", 0));
                    goto check;

                case "enterKey":
                    _instance.HeSet(("textarea", "fulltagname", "textarea", "regexp", 0), key);
                    _instance.HeClick(("button", "innertext", "Import", "regexp", 0));
                    goto check;

                case "open":
                    _instance.HeClick(("button", "innertext", "Open\\ Backpack", "regexp", 0));
                    _instance.CloseExtraTabs();
                    _logger.Send("Import: completed successfully");
                    return true;

                case "alreadySetup":
                    _instance.CloseExtraTabs();
                    _logger.Send("Import: wallet already setup");
                    return false;

                case "enterSeed":
                    string[] seed = key.Split(' ');
                    _logger.Send($"Import seed: words={seed.Length}");
                    int i = 0;
                    foreach (string word in seed)
                    {
                        _instance.HeSet(("input:text", "fulltagname", "input:text", "regexp", i), word, delay: 0);
                        i++;
                    }
                    _instance.HeClick(("button", "innertext", "Import", "regexp", 0));
                    goto check;

                case "inputPass":
                    _instance.HeSet(("input:password", "placeholder", "Password", "regexp", 0), password);
                    _instance.HeSet(("input:password", "placeholder", "Confirm\\ Password", "regexp", 0), password);
                    _instance.HeClick(("input:checkbox", "class", "PrivateSwitchBase-input\\ ", "regexp", 0));
                    _instance.HeClick(("button", "innertext", "Next", "regexp", 0));
                    goto check;

                case "NoFundedWallets":
                    _instance.HeClick(("button", "class", "is_Button\\ ", "regexp", 0));
                    _instance.HeClick(("div", "class", "is_SelectItem\\ _bg-0active-744986709\\ _btc-0active-1163467620\\ _brc-0active-1163467620\\ _bbc-0active-1163467620\\ _blc-0active-1163467620\\ _bg-0hover-1067792163\\ _btc-0hover-1394778429\\ _brc-0hover-1394778429\\ _bbc-0hover-1394778429\\ _blc-0hover-1394778429\\ _bg-0focus-455866976\\ _btc-0focus-1452587353\\ _brc-0focus-1452587353\\ _bbc-0focus-1452587353\\ _blc-0focus-1452587353\\ _outlineWidth-0focus-visible-1px\\ _outlineStyle-0focus-visible-solid\\ _dsp-flex\\ _ai-center\\ _fd-row\\ _fb-auto\\ _bxs-border-box\\ _pos-relative\\ _mih-1611762906\\ _miw-0px\\ _fs-0\\ _pr-1316332129\\ _pl-1316332129\\ _pt-1316333028\\ _pb-1316333028\\ _jc-441309761\\ _fw-nowrap\\ _w-10037\\ _btc-2122800589\\ _brc-2122800589\\ _bbc-2122800589\\ _blc-2122800589\\ _maw-10037\\ _ox-hidden\\ _oy-hidden\\ _bg-1067792132\\ _cur-default\\ _outlineOffset--0d0t5px46", "regexp", 3));
                    _instance.HeClick(("div", "class", "is_Circle\\ ", "regexp", 0));
                    _instance.HeClick(("button", "innertext", "Import\\ Wallet", "regexp", 0));
                    goto check;

                default:
                    goto check;
            }
        }

        public string ActiveAddress(bool log = false)
        {
            if (_instance.ActiveTab.URL != _popout)
                _instance.ActiveTab.Navigate(_popout, "");
            _instance.CloseExtraTabs();

            try
            {
                while (_instance.ActiveTab.FindElementByAttribute("button", "class", "is_Button\\ ", "regexp", 0).IsVoid)
                    _instance.HeClick(("path", "d", "M12 5v14", "text", 0));

                var address = _instance.HeGet(("p", "class", "MuiTypography-root\\ MuiTypography-body1", "regexp", 0), "last");
                _instance.HeClick(("button", "aria-label", "TabsNavigator,\\ back", "regexp", 0));
                _logger.Send($"Address retrieved: {address}");
                return address;
            }
            catch (Exception ex)
            {
                _logger.Send($"Address check failed: error={ex.Message}");
                throw;
            }
        }

        public string CurrentChain(bool log = true)
        {
            
            string modeNow = null;
        ifNow:
            var mode = _instance.HeGet(("div", "aria-haspopup", "dialog", "regexp", 0), atr: "innerhtml");

            if (mode.Contains("solana.png")) modeNow = "mainnet";
            if (mode.Contains("devnet.png")) modeNow = "devnet";
            if (mode.Contains("testnet.png")) modeNow = "testnet";
            if (mode.Contains("ethereum.png")) modeNow = "ethereum";
            
            
            switch (modeNow)
            {
                case "devnet":
                case "mainnet":
                case "testnet":
                case "ethereum":
                    _project.log($"CurrentChain: {modeNow}");
                    break;

                default:
                    Thread.Sleep(1000);
                    goto ifNow;
            }
            return modeNow;
        }

        public void Devmode(bool enable = true)
        {
            _instance.Go(_popout);

        ifswitch:
            try
            {
            switchBox:
                bool DevModeNow = false;
                if (_instance.HeGet(("input:checkbox", "class", "css-1m9pwf3", "regexp", 0), deadline: 1, atr: "value") == "True") DevModeNow = true;

                if (enable != DevModeNow)
                {
                    _logger.Send($"DevMode toggle: current={DevModeNow}, target={enable}");
                    _instance.HeClick(("input:checkbox", "class", "css-1m9pwf3", "regexp", 0));
                    goto switchBox;
                }
                else
                {
                    _logger.Send($"DevMode: already {enable}");
                }
            }
            catch
            {
                _instance.HeClick(("button", "class", "css-xxmhpt\\ css-yt63r3", "regexp", 0));
                _instance.HeClick(("button", "innertext", "Settings", "regexp", 0));
                _instance.HeClick(("div", "innertext", "Preferences", "regexp", 0), "last");
                goto ifswitch;
            }
        }

        public void DevChain(string reqmode = "devnet")
        {
            Switch("Solana");
            var chain = CurrentChain();
        check:
            if (chain != reqmode)
            {
                _logger.Send($"DevChain switch: current={chain}, target={reqmode}");
                _instance.HeClick(("div", "aria-haspopup", "dialog", "regexp", 0));
                _instance.HeClick(("span", "innertext", "Add\\ Network", "regexp", 0), "last");

                try
                {
                    _instance.HeGet(("span", "innertext", "Test\\ Networks", "regexp", 0));
                }
                catch
                {
                    _logger.Send("DevChain: test networks not available, enabling devmode");
                    _instance.HeClick(("button", "aria-label", "TabsNavigator,\\ back", "regexp", 0));
                    Devmode();
                    goto check;
                }

                _instance.HeClick(("img", "src", $"{reqmode}.png", "regexp", 0));
                _instance.HeClick(("span", "innertext", "From\\ Solana", "regexp", 0), "last", deadline: 3, thr0w: false);
                _instance.HeClick(("button", "class", "is_Button\\ ", "regexp", 0), deadline: 3, thr0w: false);
            }
            else
            {
                _logger.Send($"DevChain: already on {reqmode}");
            }
        }

        public void Add(string type = "Ethereum", string source = "key")//"Solana" | "Ethereum" //"key" | "phrase"
        {
            string _urlAdd = "chrome-extension://aflkmfhebedbjioipglgcbcmnbpgliof/options.html?add-user-account=true";
            string key;
            if (type == "Ethereum") key = _project.DbKey("evm");
            else key = _project.DbKey("sol");
            
            _logger.Send($"Add wallet: type={type}, source={source}");
            _instance.Go(_urlAdd, true);

        check:
            string state = null;

            if (!_instance.ActiveTab.FindElementByAttribute("button", "innertext", "Import\\ Wallet", "regexp", 0).IsVoid) state = "importButton";
            else if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Backpack\\ supports\\ multiple\\ blockchains.\\nWhich\\ do\\ you\\ want\\ to\\ use\\?\\ You\\ can\\ add\\ more\\ later.", "regexp", 0).IsVoid) state = "chooseChain";
            else if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Choose\\ a\\ method\\ to\\ import\\ your\\ wallet.", "regexp", 0).IsVoid) state = "chooseSource";
            else if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "Enter private key", "text", 0).IsVoid) state = "enterKey";
            else if (!_instance.ActiveTab.FindElementByAttribute("button", "innertext", "Open\\ Backpack", "regexp", 0).IsVoid) state = "open";

            switch (state)
            {
                case "importButton":
                    _instance.HeClick(("button", "innertext", "Import\\ Wallet", "regexp", 0));
                    goto check;

                case "chooseChain":
                    _instance.HeClick(("button", "innertext", type, "regexp", 0));
                    goto check;

                case "chooseSource":
                    _instance.HeClick(("button", "innertext", source, "text", 0));
                    goto check;

                case "enterKey":
                    _instance.HeSet(("textarea", "fulltagname", "textarea", "regexp", 0), key);
                    _instance.HeClick(("button", "innertext", "Import", "regexp", 0));
                    goto check;

                case "open":
                    _instance.HeClick(("button", "innertext", "Open\\ Backpack", "regexp", 0));
                    _instance.CloseExtraTabs();
                    _logger.Send($"Add wallet completed: type={type}");
                    return;

                default:
                    goto check;
            }
        }

        public void Switch_(string type)//"Solana" | "Ethereum" //"key" | "phrase"
        {
        start:
            if (_instance.ActiveTab.URL != _popout) _instance.ActiveTab.Navigate(_popout, "");

            int toUse = 0;
            if (type == "Ethereum")
                toUse = 1;
            _instance.HeClick(("button", "class", "MuiButtonBase-root\\ MuiIconButton-root\\ MuiIconButton-sizeMedium\\ css-xxmhpt\\ css-yt63r3", "regexp", 0));
            int i = 0;
            while (!_instance.ActiveTab.FindElementByAttribute("button", "class", "MuiButtonBase-root\\ MuiButton-root\\ MuiButton-text\\ MuiButton-textPrimary\\ MuiButton-sizeMedium\\ MuiButton-textSizeMedium\\ MuiButton-root\\ MuiButton-text\\ MuiButton-textPrimary\\ MuiButton-sizeMedium\\ MuiButton-textSizeMedium\\ css-1y4j1ko", "regexp", i).InnerText.Contains("Add")) i++;

            if (i < 2)
            {
                _logger.Send($"Switch: wallet count={i}, adding missing {type}");
                Add();
                goto start;
            }
            
            _logger.Send($"Switch to: {type}, index={toUse}");
            _instance.HeClick(("button", "class", "MuiButtonBase-root\\ MuiButton-root\\ MuiButton-text\\ MuiButton-textPrimary\\ MuiButton-sizeMedium\\ MuiButton-textSizeMedium\\ MuiButton-root\\ MuiButton-text\\ MuiButton-textPrimary\\ MuiButton-sizeMedium\\ MuiButton-textSizeMedium\\ css-1y4j1ko", "regexp", toUse));
        }

        public List<string> GetAccounts()
        {
            _instance.HeClick(("button", "class", "MuiButtonBase-root\\ MuiIconButton-root\\ MuiIconButton-sizeMedium\\ css-xxmhpt\\ css-yt63r3", "regexp", 0));
            
            return null;


        }

        public void Switch(string type)//"Solana" | "Ethereum" //"key" | "phrase"
        {
            start:
            if (_instance.ActiveTab.URL != _popout) _instance.ActiveTab.Navigate(_popout, "");

            int toUse = 0;
            if (type == "Ethereum")
                toUse = 1;
            _instance.HeClick(("button", "class", "MuiButtonBase-root\\ MuiIconButton-root\\ MuiIconButton-sizeMedium\\ css-xxmhpt\\ css-yt63r3", "regexp", 0));
            int i = 0;
            while (!_instance.ActiveTab.FindElementByAttribute("button", "class", "MuiButtonBase-root\\ MuiButton-root\\ MuiButton-text\\ MuiButton-textPrimary\\ MuiButton-sizeMedium\\ MuiButton-textSizeMedium\\ MuiButton-root\\ MuiButton-text\\ MuiButton-textPrimary\\ MuiButton-sizeMedium\\ MuiButton-textSizeMedium\\ css-1y4j1ko", "regexp", i).InnerText.Contains("Add")) i++;

            if (i < 2)
            {
                _logger.Send($"Switch: wallet count={i}, adding missing {type}");
                Add();
                goto start;
            }
            
            _logger.Send($"Switch to: {type}, index={toUse}");
            _instance.HeClick(("button", "class", "MuiButtonBase-root\\ MuiButton-root\\ MuiButton-text\\ MuiButton-textPrimary\\ MuiButton-sizeMedium\\ MuiButton-textSizeMedium\\ MuiButton-root\\ MuiButton-text\\ MuiButton-textPrimary\\ MuiButton-sizeMedium\\ MuiButton-textSizeMedium\\ css-1y4j1ko", "regexp", toUse));
        }

       
    }

    



    
    
    public static class Test
    {
        public static string GetFileNameFromUrl(this string input, bool withExtension = false)
        {
            try
            {
                // Пробуем найти URL в строке
                var urlMatch = Regex.Match(input, @"(?:src|href)=[""']?([^""'\s>]+)", RegexOptions.IgnoreCase);
                var url = urlMatch.Success ? urlMatch.Groups[1].Value : input;

                // Извлекаем последний сегмент (имя файла)
                var fileMatch = Regex.Match(url, @"([^/\\?#]+)(?:\?[^/]*)?$");
                if (fileMatch.Success)
                {
                    var fileName = fileMatch.Groups[1].Value;
            
                    // Если нужно с расширением - возвращаем как есть
                    if (withExtension)
                    {
                        return fileName;
                    }
            
                    // Удаляем расширение
                    return Regex.Replace(fileName, @"\.[^.]+$", "");
                }

                return input;
            }
            catch
            {
                return input;
            }
        }
        
    }

    
}