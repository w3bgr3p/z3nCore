using System;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore.Wallets
{
     public class SuietWallet
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;
        private readonly string _fileName;

        private readonly string _extId = "khpkpbbcccdmmclmpigdgddabeilkdpd";
        private readonly string _urlPopup = "chrome-extension://khpkpbbcccdmmclmpigdgddabeilkdpd/index.html";
        private readonly string _urlNetworks = "chrome-extension://khpkpbbcccdmmclmpigdgddabeilkdpd/index.html#/settings/network";

        public SuietWallet(IZennoPosterProjectModel project, Instance instance, bool log = false, string key = null, string fileName = "Suiet-Sui-Wallet-Chrome.crx")
        {
            _project = project;
            _instance = instance;
            _fileName = fileName;
            _logger = new Logger(project, log: log, classEmoji: "SUIet");
        }

        private string DefineKey(string key)
        {
            if (key == "key") 
                return _project.DbKey();
            if (key == "seed") 
                return _project.DbKey("seed").SuiKey();
            
            var keyType = key.KeyType();
            _logger.Send($"Key type detected: type={keyType}, key_prefix={key?.Substring(0, Math.Min(10, key?.Length ?? 0))}...");
            
            if (keyType == "seed") 
                return key.SuiKey();
            if (keyType == "keyEvm") 
                return key;
            
            _project.warn($"Key type undefined: type={keyType}, key_prefix={key?.Substring(0, Math.Min(10, key?.Length ?? 0))}...", thrw:true);
            return key;
        }

        public string Launch(string source = null)
        {
            if (string.IsNullOrEmpty(source)) source = "key";
            string key = DefineKey(source);
            
            var em = _instance.UseFullMouseEmulation;
            _instance.UseFullMouseEmulation = false;

            _logger.Send($"Launch: file={_fileName}, extId={_extId}, source={source}");
            
            new ChromeExt(_project, _instance).Switch(_extId);
            bool isNewInstall = new ChromeExt(_project, _instance).Install(_extId, _fileName);
            _logger.Send($"Extension state: isNewInstall={isNewInstall}, extId={_extId}");
            
            if (isNewInstall)
                Import(key);
            else
                Unlock();

            var adr = ActiveAddress();
            _logger.Send($"Wallet ready: address={adr}");
            
            _instance.CloseExtraTabs();
            _instance.UseFullMouseEmulation = em;
            return adr;
        }
        
        public void Sign(int deadline = 10,  int delay = 3)
        {
            _instance.HeClick(("button", "class", "_button--primary_", "regexp", 0),deadline: deadline, delay:delay);
        }
        
        
        private void Import(string key)
        {
            _instance.Go(_urlPopup);
            _instance.HeClick(("button", "innertext", "Import\\ Wallet", "regexp", 0));
            
            var passw = SAFU.HWPass(_project);
            _instance.HeSet(("input:password", "fulltagname", "input:password", "regexp", 0), passw);
            _instance.HeSet(("input:password", "fulltagname", "input:password", "regexp", 1), passw);
            _instance.HeClick(("button", "innertext", "Next", "regexp", 0));
            _instance.HeClick(("div", "class", "rounded-2xl\\ cursor-pointer\\ hover:bg-hover\\ border\\ border-border\\ hover:border-zinc-200\\ transition", "regexp", 1));
            _instance.HeSet(("privateKey", "name"), key);
            _instance.HeClick(("button", "innertext", "Confirm\\ and\\ Import", "regexp", 0));
            
            var currentAddress = _instance.HeGet(("a", "href", "https://pay.suiet.app/\\?wallet_address=", "regexp", 0), atr:"href").Replace("https://pay.suiet.app/?wallet_address=","");
            _logger.Send($"Import complete: address={currentAddress}");
        }

        public void Unlock()
        {
            _instance.Go(_urlPopup);
            var passw = SAFU.HWPass(_project);
            _instance.HeSet(("input:password", "fulltagname", "input:password", "regexp", 0), passw);
            _instance.HeClick(("button", "innertext", "Unlock", "regexp", 0));
        }
        
        public void SwitchChain(string mode = "Mainnet")
        {
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

            _logger.Send($"Chain switch: mode={mode}, index={index}");
            
            _instance.Go(_urlNetworks);
            _instance.HeClick(("div", "class", "_network-selection-container_", "regexp", index));
            _instance.HeClick(("button", "innertext", "Save", "regexp", 0));
        }
        
        public string ActiveAddress()
        {
            var currentAddress = _instance.HeGet(("a", "href", "https://pay.suiet.app/\\?wallet_address=", "regexp", 0), atr:"href").Replace("https://pay.suiet.app/?wallet_address=","");
            return currentAddress;
        }
    }
}