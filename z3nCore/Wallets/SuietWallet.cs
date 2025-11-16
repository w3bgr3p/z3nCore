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
            
            _instance.HeClick(("div", "class", "rounded-2xl\\ cursor-pointer\\ hover:bg-hover\\ border\\ border-border\\ hover:border-zinc-200\\ transition", "regexp", 1));
            
            string key = source == "seed" ? _project.DbKey("seed").SuiKey() : _project.DbKey("evm");
            _instance.HeSet(("privateKey", "name"), key);
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
}