using NBitcoin;
using System;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore.Wallets
{
    public class Keplr
    {
        #region Fields and Constants
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;
        protected readonly string _extId = "dmkamcknogkgcdfhhbddcghachkejeap";
        protected readonly string _fileName = "Keplr0.12.223.crx";
        
        // Константы для улучшения читаемости
        private const int CLICK_OFFSET = 450;
        private const int DEFAULT_DELAY = 1000;
        private const int SIGN_TIMEOUT_SECONDS = 20;
        #endregion

        #region Constructor
        public Keplr(IZennoPosterProjectModel project, Instance instance, bool log = false, string key = null, string seed = null)
        {
            _project = project;
            _instance = instance;
            _logger = new Logger(project, log: log, classEmoji: "K3PLR");
        }
        #endregion

        #region Public Methods
        public void Launch(string source = "seed", string fileName = null, bool log = false)
        {
            _project.Deadline();

            if (string.IsNullOrEmpty(fileName)) fileName = _fileName;
            var em = _instance.UseFullMouseEmulation;
            _instance.UseFullMouseEmulation = false;
            _project.Deadline(60);
            
            try
            {
                Unlock(source);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex.Message, thrw: true);
            }
            SetSource(source);
            _instance.CloseExtraTabs();
            _instance.UseFullMouseEmulation = em;
        }

        public void SetSource(string source)
        {
            _logger.Send($"Setting Keplr wallet source to {source}");

            while (true)
            {
                _instance.CloseExtraTabs();
                _instance.ActiveTab.Navigate($"chrome-extension://{_extId}/popup.html#/wallet/select", "");
                _instance.HeGet(("button", "innertext", "Add\\ Wallet", "regexp", 0));

                var imported = Prune();
                if (imported.Contains("seed") && imported.Contains("keyEvm"))
                {
                    KeplrClick(_instance.GetHe(("div", "innertext", source, "regexp", 0), "last"));
                    _logger.Send($"Source set to {source}");
                    return;
                }

                _logger.Send("Not all wallets imported, adding new wallet");
                KeplrClick(_instance.GetHe(("button", "innertext", "Add\\ Wallet", "regexp", 0)));
                Import("keyEvm");
            }
        }
        public void Unlock(string source = "seed")
        {
            _project.Deadline();
            check:
            if (!_instance.ActiveTab.URL.Contains(_extId)) _instance.Go($"chrome-extension://{_extId}/popup.html#/");
            _project.Deadline(60);

            var state = "unknown";
            
            if (!_instance.ActiveTab.FindElementByAttribute("div", "innertext", "Invalid\\ password", "regexp", 0).IsVoid)
                state = "invalidPassword";
            else if (!_instance.ActiveTab.FindElementByAttribute("span", "innertext", "dmkamcknogkgcdfhhbddcghachkejeap\\ is\\ blocked", "regexp", 0).IsVoid)
                state = "notInstalled";
            else if (!_instance.ActiveTab.FindElementByAttribute("div", "innertext", "Copy\\ Address", "regexp", 0).IsVoid)
                state = "unlocked";
            else if (!_instance.ActiveTab.FindElementByAttribute("input:password", "tagname", "input", "regexp", 0).IsVoid)
                state = "locked";

            _logger.Send(state);
            switch (state)
            {
                case "invalidPassword":
                    _instance.UninstallExtension(_extId);
                    _instance.CloseExtraTabs(true);
                    _logger.Warn("invalidPassword", thrw: true);
                    break;
                case "locked":
                    var password = SAFU.HWPass(_project);
                    _instance.HeSet(("input:password", "tagname", "input", "regexp", 0), password);
                    KeplrClick(_instance.GetHe(("button", "innertext", "Unlock", "regexp", 0)));
                    goto check;
                case "unlocked":
                    var bal = _instance.HeGet(("div", "innertext", "Total\\ Available\\n\\$", "regexp", 0), "last", deadline: 3).Replace("Total Available\n", "");
                    _logger.Send(bal);
                    return;
                case "notInstalled":
                    TurnOn(source);
                    goto check;
                default:
                    Thread.Sleep(1000);
                    goto check;
            }
        }
        private void TurnOn(string source = "seed")
        {
            var ext = new ChromeExt(_project, _instance);
            bool switched = ext.Switch(_extId);
            if (!switched) 
            {
                ext.Install(_extId, _fileName);
                Import(source);
            }
            
        }
        public void Sign(bool log = false)
        {
            _logger.Send("Approving Keplr transaction");
            var deadline = DateTime.Now.AddSeconds(SIGN_TIMEOUT_SECONDS);

            try
            {
                while (!(_instance.ActiveTab.URL.Contains(_extId)) && DateTime.Now < deadline)
                {
                    Thread.Sleep(100);
                }
                if (DateTime.Now >= deadline)
                {
                    _logger.Send("Timeout waiting for Keplr tab");
                    throw new Exception("No Keplr tab detected");
                }

                _instance.UseFullMouseEmulation = false;
            approve:
                _instance.HeClick(("button", "innertext", "Approve", "regexp", 0));
                _logger.Send("Approve button clicked");

                while (_instance.ActiveTab.URL.Contains(_extId) && DateTime.Now < deadline)
                {
                    Thread.Sleep(100);
                    goto approve;
                }
                if (DateTime.Now >= deadline)
                {
                    _logger.Send("Keplr tab stuck");
                    throw new Exception("Keplr tab stuck");
                }

                _logger.Send("Keplr transaction approved, tab closed");
                return;
            }
            catch (Exception ex)
            {
                _logger.Send($"Failed to approve Keplr transaction: {ex.Message}");
                throw;
            }
            finally
            {
                _instance.UseFullMouseEmulation = true;
            }
        }
        
        #endregion

        #region Private Helper Methods
        private void KeplrClick(HtmlElement he)
        {
            int x = int.Parse(he.GetAttribute("leftInTab")); 
            int y = int.Parse(he.GetAttribute("topInTab"));
            x = x - CLICK_OFFSET; 
            _instance.Click(x, x, y, y, "Left", "Normal"); 
            Thread.Sleep(DEFAULT_DELAY);
            return;
        }

        private string Prune(bool keepTemp = false)
        {
            _logger.Send("Pruning Keplr wallets");
            var imported = "";
            int i = 0;
            _instance.HeGet(("button", "innertext", "Add\\ Wallet", "regexp", 0));
            Thread.Sleep(DEFAULT_DELAY);

            try
            {
                while (true)
                {
                    var dotBtn = _instance.ActiveTab.FindElementByAttribute(
                        "path",
                        "d",
                        "M10.5 6C10.5 5.17157 11.1716 4.5 12 4.5C12.8284 4.5 13.5 5.17157 13.5 6C13.5 6.82843 12.8284 7.5 12 7.5C11.1716 7.5 10.5 6.82843 10.5 6ZM10.5 12C10.5 11.1716 11.1716 10.5 12 10.5C12.8284 10.5 13.5 11.1716 13.5 12C13.5 12.8284 12.8284 13.5 12 13.5C11.1716 13.5 10.5 12.8284 10.5 12ZM10.5 18C10.5 17.1716 11.1716 16.5 12 16.5C12.8284 16.5 13.5 17.1716 13.5 18C13.5 18.8284 12.8284 19.5 12 19.5C11.1716 19.5 10.5 18.8284 10.5 18Z",
                        "text",
                        i);

                    if (dotBtn.IsVoid) break;

                    var tileText = dotBtn.ParentElement.ParentElement.ParentElement.ParentElement.ParentElement.ParentElement.InnerText;

                    if (tileText.Contains("keyEvm")) { imported += "keyEvm"; i++; continue; }
                    if (tileText.Contains("seed")) { imported += "seed"; i++; continue; }
                    if (keepTemp && tileText.Contains("temp")) { imported += "temp"; i++; continue; }

                    KeplrClick(dotBtn);
                    KeplrClick(_instance.GetHe(("div", "innertext", "Delete\\ Wallet", "regexp", 0), "last"));
                    _instance.HeSet(("password", "name"), SAFU.HWPass(_project));
                    KeplrClick(_instance.GetHe(("button", "type", "submit", "regexp", 0)));
                    i++;
                }
                return imported;
            }
            catch (Exception ex)
            {
                _logger.Send("Failed to prune Keplr wallets: " + ex.Message);
                throw;
            }
        }

        private void Import(string importType, bool temp = false)
        {
            _logger.Send($"Importing Keplr wallet type: {importType}, temp: {temp}");

            var password = SAFU.HWPass(_project);
            string keyOrSeed;
            string walletName;

            #region Determine Import Credentials
            if (importType == "seed")
            {
                keyOrSeed = _project.DbKey("seed");
                walletName = "seed";
            }
            else if (importType == "keyEvm")
            {
                keyOrSeed = temp ? new Key().ToHex() : _project.DbKey("evm");
                walletName = temp ? "temp" : "keyEvm";
            }
            else
            {
                try
                {
                    string wType = importType.KeyType();
                    if (wType == "seed" || wType == "keyEvm")
                    {
                        keyOrSeed = importType;
                        importType = wType;
                    }
                    else
                        throw new ArgumentException("Unknown importType: " + importType);
                }
                catch (Exception ex)
                {
                    _project.SendWarningToLog(ex.Message);
                    throw new ArgumentException("Unknown importType: " + importType);
                }
            }
            #endregion

            #region Navigate to Import
            try { _instance.HeGet(("button", "innertext", "Import\\ an\\ existing\\ wallet", "regexp", 0), deadline: 3); }
            catch { _instance.ActiveTab.Navigate("chrome-extension://" + _extId + "/register.html#/", ""); }
            #endregion

            #region Perform Import
            try
            {
                _instance.HeClick(("button", "innertext", "Import\\ an\\ existing\\ wallet", "regexp", 0));
                _instance.HeClick(("button", "innertext", "Use\\ recovery\\ phrase\\ or\\ private\\ key", "regexp", 0));

                if (importType == "keyEvm")
                {
                    _instance.HeClick(("button", "innertext", "Private\\ key", "regexp", 1));
                    _instance.HeSet(("input:password", "tagname", "input", "regexp", 0), keyOrSeed);
                }
                else // seed
                {
                    var words = keyOrSeed.Split(' ');
                    for (int i = 0; i < words.Length; i++)
                        _instance.HeSet(("input", "fulltagname", "input:", "regexp", i), words[i], delay: 0);
                }

                _instance.HeClick(("button", "innertext", "Import", "regexp", 1));
                _instance.HeSet(("name", "name"), importType);

                try
                {
                    _instance.HeSet(("password", "name"), password, deadline: 3);
                    _instance.HeSet(("confirmPassword", "name"), password);
                }
                catch { }

                _instance.HeClick(("button", "innertext", "Next", "regexp", 0));

                if (importType == "seed")
                    _instance.HeClick(("input:checkbox", "fulltagname", "input:checkbox", "regexp", 0));

                _instance.HeClick(("button", "innertext", "Save", "regexp", 0));

                while (!_instance.ActiveTab.FindElementByAttribute("button", "innertext", "Import", "regexp", 0).IsVoid)
                {
                    _instance.HeClick(("button", "innertext", "Import", "regexp", 0));
                    Thread.Sleep(2000);
                }

                _instance.CloseExtraTabs();
            }
            catch (Exception ex)
            {
                _logger.Send($"Failed to import Keplr wallet ({importType}): {ex.Message}");
                throw;
            }
            #endregion
        }
        #endregion
    }
}