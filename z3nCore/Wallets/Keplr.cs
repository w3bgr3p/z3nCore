using NBitcoin;
using System;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class Keplr
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;

        private const string EXTENSION_ID = "dmkamcknogkgcdfhhbddcghachkejeap";
        private const string DEFAULT_FILE_NAME = "Keplr0.12.223.crx";
        private const int CLICK_DELAY = 1000;
        private const int DEFAULT_TIMEOUT = 20;
        private const int COORDINATE_OFFSET = 450;

        public Keplr(IZennoPosterProjectModel project, Instance instance, bool log = false, string key = null, string seed = null)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            _logger = new Logger(project, log: log, classEmoji: "K3PLR");
        }

        public void Launch(string source = "seed", string fileName = null, bool log = false)
        {
            fileName = fileName ?? DEFAULT_FILE_NAME;
            _project.Deadline();

            var originalMouseEmulation = _instance.UseFullMouseEmulation;
            _instance.UseFullMouseEmulation = false;

            try
            {
                LaunchWallet(source, fileName, log);
            }
            finally
            {
                _instance.UseFullMouseEmulation = originalMouseEmulation;
            }
        }

        public void SetSource(string source, bool log = false)
        {
            _logger.Send($"Setting Keplr wallet source to {source}");

            while (true)
            {
                NavigateToWalletSelect();
                var imported = PruneWallets(log: log);
                
                if (HasRequiredWallets(imported))
                {
                    SelectWalletSource(source);
                    return;
                }

                AddMissingWallet(log);
            }
        }

        public void Unlock(bool log = false)
        {
            _logger.Send("Unlocking Keplr wallet");
            
            while (true)
            {
                if (IsWalletUnlocked()) return;
                
                try
                {
                    PerformUnlock();
                    return;
                }
                catch (Exception ex)
                {
                    _logger.Send($"Unlock attempt failed: {ex.Message}");
                    _instance.CloseAllTabs();
                }
            }
        }

        public void Sign(bool log = false)
        {
            _logger.Send("Approving Keplr transaction");
            
            var originalMouseEmulation = _instance.UseFullMouseEmulation;
            _instance.UseFullMouseEmulation = false;

            try
            {
                WaitForKeplrTab();
                ApproveTransaction();
            }
            finally
            {
                _instance.UseFullMouseEmulation = originalMouseEmulation;
            }
        }

        [Obsolete("Use Sign method instead")]
        public string KeplrApprove(bool log = false)
        {
            _project.ObsoleteCode("Sign");
            Sign(log);
            return "done";
        }

        private void LaunchWallet(string source, string fileName, bool log)
        {
            while (true)
            {
                try
                {
                    new ChromeExt(_project, _instance, log: log).Switch(EXTENSION_ID);
                    _logger.Send("Launching " + fileName);
                    _project.Deadline(60);

                    var chromeExt = new ChromeExt(_project, _instance);
                    if (chromeExt.Install(EXTENSION_ID, fileName, log))
                    {
                        ImportWallet(source, log: log);
                    }
                    else
                    {
                        Unlock(log);
                    }

                    SetSource(source, log);
                    _instance.CloseExtraTabs();
                    return;
                }
                catch (Exception ex)
                {
                    _project.SendWarningToLog(ex.Message);
                }
            }
        }

        private void NavigateToWalletSelect()
        {
            _instance.CloseExtraTabs();
            _instance.ActiveTab.Navigate($"chrome-extension://{EXTENSION_ID}/popup.html#/wallet/select", "");
            WaitForElement("button", "innertext", "Add\\ Wallet", "regexp");
        }

        private bool HasRequiredWallets(string imported)
        {
            return imported.Contains("seed") && imported.Contains("keyEvm");
        }

        private void SelectWalletSource(string source)
        {
            var sourceElement = GetElement("div", "innertext", source, "regexp", "last");
            ClickElement(sourceElement);
            _logger.Send($"Source set to {source}");
        }

        private void AddMissingWallet(bool log)
        {
            _logger.Send("Not all wallets imported, adding new wallet");
            var addWalletBtn = GetElement("button", "innertext", "Add\\ Wallet", "regexp");
            ClickElement(addWalletBtn);
            ImportWallet("keyEvm", log: log);
        }

        private string PruneWallets(bool keepTemp = false, bool log = false)
        {
            _logger.Send("Pruning Keplr wallets");
            var imported = "";
            int walletIndex = 0;

            try
            {
                while (true)
                {
                    var dotBtn = _instance.ActiveTab.FindElementByAttribute(
                        "path", "d", GetDotButtonPath(), "text", walletIndex);

                    if (dotBtn.IsVoid) break;

                    var walletInfo = GetWalletInfo(dotBtn);
                    if (ShouldKeepWallet(walletInfo, keepTemp))
                    {
                        imported += GetWalletType(walletInfo);
                        walletIndex++;
                        continue;
                    }

                    DeleteWallet(dotBtn);
                    walletIndex++;
                }
                return imported;
            }
            catch (Exception ex)
            {
                _logger.Send("Failed to prune Keplr wallets: " + ex.Message);
                throw;
            }
        }

        private void ImportWallet(string importType, bool temp = false, bool log = false)
        {
            _logger.Send($"Importing Keplr wallet type: {importType}, temp: {temp}");

            var (keyOrSeed, walletName) = GetWalletCredentials(importType, temp);
            
            try
            {
                NavigateToImport();
                PerformImport(importType, keyOrSeed);
                CompleteImport();
                _instance.CloseExtraTabs();
            }
            catch (Exception ex)
            {
                _logger.Send($"Failed to import Keplr wallet ({importType}): {ex.Message}");
                throw;
            }
        }

        private (string keyOrSeed, string walletName) GetWalletCredentials(string importType, bool temp)
        {
            switch (importType)
            {
                case "seed":
                    return (_project.DbKey("seed"), "seed");
                case "keyEvm":
                    var key = temp ? new Key().ToHex() : _project.DbKey("evm");
                    var name = temp ? "temp" : "keyEvm";
                    return (key, name);
                default:
                    return ProcessCustomImportType(importType);
            }
        }

        private (string, string) ProcessCustomImportType(string importType)
        {
            try
            {
                string wType = importType.KeyType();
                if (wType == "seed" || wType == "keyEvm")
                {
                    return (importType, wType);
                }
                throw new ArgumentException("Unknown importType: " + importType);
            }
            catch (Exception ex)
            {
                _project.SendWarningToLog(ex.Message);
                throw new ArgumentException("Unknown importType: " + importType);
            }
        }

        private void NavigateToImport()
        {
            try 
            { 
                WaitForElement("button", "innertext", "Import\\ an\\ existing\\ wallet", "regexp", deadline: 3); 
            }
            catch 
            { 
                _instance.ActiveTab.Navigate("chrome-extension://" + EXTENSION_ID + "/register.html#/", ""); 
            }

            ClickElement("button", "innertext", "Import\\ an\\ existing\\ wallet", "regexp");
            ClickElement("button", "innertext", "Use\\ recovery\\ phrase\\ or\\ private\\ key", "regexp");
        }

        private void PerformImport(string importType, string keyOrSeed)
        {
            if (importType == "keyEvm")
            {
                ImportPrivateKey(keyOrSeed);
            }
            else
            {
                ImportSeedPhrase(keyOrSeed);
            }

            ClickElement("button", "innertext", "Import", "regexp", index: 1);
            SetElement("name", "name", importType);
            SetPassword();
            ClickElement("button", "innertext", "Next", "regexp");
        }

        private void ImportPrivateKey(string privateKey)
        {
            ClickElement("button", "innertext", "Private\\ key", "regexp", index: 1);
            SetElement("input:password", "tagname", privateKey);
        }

        private void ImportSeedPhrase(string seedPhrase)
        {
            var words = seedPhrase.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                SetElement("input", "fulltagname", words[i], "input:", index: i, delay: 0);
            }
        }

        private void CompleteImport()
        {
            ClickElement("input:checkbox", "fulltagname", "input:checkbox", "regexp");
            ClickElement("button", "innertext", "Save", "regexp");
            
            WaitForImportCompletion();
        }

        private void WaitForImportCompletion()
        {
            while (!_instance.ActiveTab.FindElementByAttribute("button", "innertext", "Import", "regexp", 0).IsVoid)
            {
                ClickElement("button", "innertext", "Import", "regexp");
                Thread.Sleep(2000);
            }
        }

        private bool IsWalletUnlocked()
        {
            return !_instance.ActiveTab.FindElementByAttribute("div", "innertext", "Copy\\ Address", "regexp", 0).IsVoid;
        }

        private void PerformUnlock()
        {
            _instance.Go($"chrome-extension://{EXTENSION_ID}/popup.html#/");
            
            if (TryGetBalance()) return;
            
            var password = SAFU.HWPass(_project);
            WaitForElement("input:password", "tagname", "input", "regexp");
            SetElement("input:password", "tagname", password, "input", "regexp");
            ClickElement("button", "innertext", "Unlock", "regexp");

            ValidateUnlock();
        }

        private bool TryGetBalance()
        {
            try
            {
                var balance = GetElement("div", "innertext", "Total\\ Available\\n\\$", "regexp", "last", deadline: 3).InnerText
                    .Replace("Total Available\n", "");
                _logger.Send(balance);
                return true;
            }
            catch (Exception ex) 
            { 
                _project.SendWarningToLog(ex.Message);
                return false;
            }
        }

        private void ValidateUnlock()
        {
            if (!_instance.ActiveTab.FindElementByAttribute("div", "innertext", "Invalid\\ password", "regexp", 0).IsVoid)
            {
                _instance.CloseAllTabs();
                _instance.UninstallExtension(EXTENSION_ID);
                throw new Exception("Wrong password for Keplr");
            }
        }

        private void WaitForKeplrTab()
        {
            var deadline = DateTime.Now.AddSeconds(DEFAULT_TIMEOUT);
            
            while (!_instance.ActiveTab.URL.Contains(EXTENSION_ID) && DateTime.Now < deadline)
            {
                Thread.Sleep(100);
            }
            
            if (DateTime.Now >= deadline)
            {
                _logger.Send("Timeout waiting for Keplr tab");
                throw new Exception("No Keplr tab detected");
            }
        }

        private void ApproveTransaction()
        {
            var deadline = DateTime.Now.AddSeconds(DEFAULT_TIMEOUT);
            
            while (_instance.ActiveTab.URL.Contains(EXTENSION_ID) && DateTime.Now < deadline)
            {
                ClickElement("button", "innertext", "Approve", "regexp");
                _logger.Send("Approve button clicked");
                Thread.Sleep(100);
            }
            
            if (DateTime.Now >= deadline)
            {
                _logger.Send("Keplr tab stuck");
                throw new Exception("Keplr tab stuck");
            }
            
            _logger.Send("Keplr transaction approved, tab closed");
        }

        private void SetPassword()
        {
            var password = SAFU.HWPass(_project);
            try
            {
                SetElement("password", "name", password, deadline: 3);
                SetElement("confirmPassword", "name", password);
            }
            catch { }
        }

        private void DeleteWallet(HtmlElement dotBtn)
        {
            ClickElement(dotBtn);
            ClickElement(GetElement("div", "innertext", "Delete\\ Wallet", "regexp", "last"));
            SetElement("password", "name", SAFU.HWPass(_project));
            ClickElement("button", "type", "submit", "regexp");
        }

        private string GetWalletInfo(HtmlElement dotBtn)
        {
            return dotBtn.ParentElement.ParentElement.ParentElement
                .ParentElement.ParentElement.ParentElement.InnerText;
        }

        private bool ShouldKeepWallet(string walletInfo, bool keepTemp)
        {
            return walletInfo.Contains("keyEvm") || 
                   walletInfo.Contains("seed") || 
                   (keepTemp && walletInfo.Contains("temp"));
        }

        private string GetWalletType(string walletInfo)
        {
            if (walletInfo.Contains("keyEvm")) return "keyEvm";
            if (walletInfo.Contains("seed")) return "seed";
            if (walletInfo.Contains("temp")) return "temp";
            return "";
        }

        private string GetDotButtonPath()
        {
            return "M10.5 6C10.5 5.17157 11.1716 4.5 12 4.5C12.8284 4.5 13.5 5.17157 13.5 6C13.5 6.82843 12.8284 7.5 12 7.5C11.1716 7.5 10.5 6.82843 10.5 6ZM10.5 12C10.5 11.1716 11.1716 10.5 12 10.5C12.8284 10.5 13.5 11.1716 13.5 12C13.5 12.8284 12.8284 13.5 12 13.5C11.1716 13.5 10.5 12.8284 10.5 12ZM10.5 18C10.5 17.1716 11.1716 16.5 12 16.5C12.8284 16.5 13.5 17.1716 13.5 18C13.5 18.8284 12.8284 19.5 12 19.5C11.1716 19.5 10.5 18.8284 10.5 18Z";
        }

        // Вспомогательные методы для работы с элементами
        private void ClickElement(HtmlElement element)
        {
            int x = int.Parse(element.GetAttribute("leftInTab"));
            int y = int.Parse(element.GetAttribute("topInTab"));
            x = x - COORDINATE_OFFSET;
            _instance.Click(x, x, y, y, "Left", "Normal");
            Thread.Sleep(CLICK_DELAY);
        }

        private void ClickElement(string tag, string attr, string value, string searchType, int index = 0)
        {
            var element = GetElement(tag, attr, value, searchType, index: index);
            ClickElement(element);
        }

        private HtmlElement GetElement(string tag, string attr, string value, string searchType, string position = "", int index = 0, int deadline = 30)
        {
            return string.IsNullOrEmpty(position) 
                ? _instance.GetHe((tag, attr, value, searchType, index))
                : _instance.GetHe((tag, attr, value, searchType, index), position);
        }

        private void WaitForElement(string tag, string attr, string value, string searchType, int index = 0, int deadline = 30)
        {
            _instance.HeGet((tag, attr, value, searchType, index), deadline: deadline);
        }

        private void SetElement(string attr, string name, string value, string tag = "", string searchType = "", int index = 0, int delay = 1000, int deadline = 30)
        {
            if (string.IsNullOrEmpty(tag))
            {
                _instance.HeSet((attr, name), value, delay: delay, deadline: deadline);
            }
            else
            {
                _instance.HeSet((tag, attr, searchType, index), value, delay: delay);
            }
        }
    }
}