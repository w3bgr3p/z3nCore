using NBitcoin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class RabbyWallet
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;
        private readonly string _extId;
        private readonly string _fileName;
        private readonly string _key;
        private readonly string _pass;
        private string _expectedAddress;

        public RabbyWallet(IZennoPosterProjectModel project, Instance instance, bool log = false, string key = null, string fileName = "Zerion1.21.3.crx")
        {
            _project = project;
            _instance = instance;
            _fileName = fileName;

            _key = KeyLoad(key);
            _pass = SAFU.HWPass(_project);
            _logger = new Logger(project, log: log, classEmoji: "Rabby");

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

        public void RabbyLnch(string fileName = null, bool log = false)
        {
            if (string.IsNullOrEmpty(fileName)) fileName = _fileName;

            var em = _instance.UseFullMouseEmulation;
            _instance.UseFullMouseEmulation = true;

            _logger.Send($"Launching Rabby wallet with file {fileName}");
            var ext = new ChromeExt(_project, _instance, log: log);
            
            ext.Switch(_extId);
            if (ext.Install(_extId, fileName, log))
                RabbyImport(log: log);
            else
                RabbyUnlock(log: log);

            _instance.CloseExtraTabs();
            _instance.UseFullMouseEmulation = em;
        }

        public void RabbyImport(bool log = false)
        {
            _logger.Send("Importing Rabby wallet with private key");
            var key = _key;
            var password = _pass;

            try
            {
                _instance.HeClick(("button", "innertext", "I\\ already\\ have\\ an\\ address", "regexp", 0));
                _instance.HeClick(("img", "src", $"chrome-extension://{_extId}/generated/svgs/d5409491e847b490e71191a99ddade8b.svg", "regexp", 0));
                _instance.HeSet(("privateKey", "id"), key);
                _instance.HeClick(("button", "innertext", "Confirm", "regexp", 0));
                _instance.HeSet(("password", "id"), password);
                _instance.HeSet(("confirmPassword", "id"), password);
                _instance.HeClick(("button", "innertext", "Confirm", "regexp", 0));
                _instance.HeClick(("button", "innertext", "Get\\ Started", "regexp", 0));
                _logger.Send("Successfully imported Rabby wallet");
            }
            catch (Exception ex)
            {
                _logger.Send($"Failed to import Rabby wallet: {ex.Message}");
                throw;
            }
        }

        public void RabbyUnlock(bool log = false)
        {
            _logger.Send("Unlocking Rabby wallet");
            var password = _pass;

            _instance.UseFullMouseEmulation = true;

            while (_instance.ActiveTab.URL == $"chrome-extension://{_extId}/offscreen.html")
            {
                _logger.Send("Closing offscreen tab and retrying unlock");
                _instance.ActiveTab.Close();
                _instance.ActiveTab.Navigate($"chrome-extension://{_extId}/index.html#/unlock", "");
            }

            try
            {
                _instance.HeSet(("password", "id"), password);
                _instance.HeClick(("button", "innertext", "Unlock", "regexp", 0));
                _logger.Send("Wallet unlocked successfully");
            }
            catch (Exception ex)
            {
                _logger.Send($"Failed to unlock Rabby wallet: {ex.Message}");
                throw;
            }
        }
    }

}
