using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using z3nCore.Utilities;

namespace z3nCore.Wallets
{
    public class AuroWallet
    {
        #region Essentials
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _log;
        private readonly Random _r = new Random();
        private readonly string _extId = "cnmamaachppnkjgnildpdmkaakejnhae";

        
        public AuroWallet(IZennoPosterProjectModel project, Instance instance, bool _showLog = false)
        {
            _project = project;
            _instance = instance;
            _log = new Logger(project, _showLog, "Auro", persistent:true);
        }
        #endregion

        public string Launch()
        {
            
            var e = new Extension(_project, _instance);
            
            var fresh = e.InstallFromCrx(_extId,"Auro-Wallet-Chrome-Web-Store.crx");
            Thread.Sleep(500);
            e.Switch(_extId);
            _project.Deadline();
            bool install = fresh;
            bool unlock = !fresh;

            var passw = SAFU.HWPass(_project);
            _instance.GetExtensionById(_extId).Activate();

            while(true)
            {
	            _project.log("loading");
                Thread.Sleep(1000);
	            if (_instance.ActiveTab.URL.Contains(_extId)) break;
	            _project.Deadline(10);
            }



            if(install)
            {
                _log.Send("installing");
	            _instance.UseFullMouseEmulation = false;
	            _instance.HeClick(("button", "innertext", "Restore", "regexp", 0), deadline:20);
	            _instance.HeClick(("div", "innertext", "Agree", "regexp", 0), "last", thrw:false);
	            _instance.HeSet(("input:password", "fulltagname", "input:password", "regexp", 0),passw);
	            _instance.HeSet(("input:password", "fulltagname", "input:password", "regexp", 1),passw);
	            _instance.HeClick(("button", "innertext", "Next", "regexp", 0));
	            _instance.HeClick(("input:password", "class", "sc-CZWsc\\ gOWAwb", "regexp", 0));
	            var seed = _project.DbKey("seed");
	            _instance.CtrlV(seed);
	            _instance.HeClick(("button", "innertext", "Next", "regexp", 1));
	            _instance.HeClick(("button", "innertext", "Done", "regexp", 0));
	            

            }


            if (unlock)
            {
	            Thread.Sleep(2000);
                _log.Send("unlocking");
                if (_instance.ActiveTab.FindElementByAttribute("p", "class", "sc-eZSpzM\\ sc-buTqWO\\ fkCDzC\\ gZChKS", "regexp", 0).IsVoid)  
	            {
		            _instance.HeSet(("input:password", "fulltagname", "input:password", "regexp", 0),passw, deadline:30);
	                _instance.HeClick(("button", "innertext", "Unlock", "regexp", 0));
	            }
	            
	            _instance.CloseExtraTabs();

            }

            _instance.ActiveTab.Navigate("chrome-extension://cnmamaachppnkjgnildpdmkaakejnhae/popup.html#/receive_page", "");
            if (_instance.ActiveTab.IsBusy) _instance.ActiveTab.WaitDownloading();

            while(true)
            {
                _log.Send("loading");
	            Thread.Sleep(1000);
	            if (_instance.ActiveTab.URL.Contains(_extId)) break;
	            _project.Deadline(20);
            }



            var minaAddress =  _instance.HeGet(("p", "class", "sc-lhsSio\\ kRdHCj", "regexp", 0));
            _project.DbUpd($"mina_address = '{minaAddress}'");
            _project.Var("minaAddress",minaAddress);
            _log.Send($"started with {minaAddress}");
            _instance.CloseExtraTabs();
            return minaAddress;
        }
        
        public void SwitchChain(string chain = "Testnet")
        {
            
            _instance.GetExtensionById(_extId).Activate();
            var chainNow = _instance.HeGet(("div", "class", "sc-etsjJW\\ bqjZXJ", "regexp", 0), deadline:20);
            if (chainNow.Contains (chain)) return ;
            
            _instance.HeClick(("div", "class", "sc-etsjJW\\ bqjZXJ", "regexp", 0));
            if (_instance.ActiveTab.FindElementByAttribute("img", "src", "chrome-extension://cnmamaachppnkjgnildpdmkaakejnhae/img/icon_zeko_testnet.svg", "regexp", 0).IsVoid)
                _instance.HeClick(("span", "class", "sc-ipUnzB\\ bEQScm", "regexp", 0));

            switch (chain)
            {
                case "Testnet":
                    _instance.HeClick(("img", "src", "chrome-extension://cnmamaachppnkjgnildpdmkaakejnhae/img/icon_zeko_testnet.svg", "regexp", 0));
                    break;
                case "Devnet":
                    _instance.HeClick(("img", "src", "chrome-extension://cnmamaachppnkjgnildpdmkaakejnhae/img/icon_mina_gray.svg", "regexp", 0));
                    break;		
                case "Mainnet":
                    _instance.HeClick(("img", "src", "chrome-extension://cnmamaachppnkjgnildpdmkaakejnhae/img/mina_color.svg", "regexp", 0));
                    break;
                default:
                    break;
            }
        }
        public void Unlock()
        {
            _instance.GetExtensionById(_extId).Activate();
            var passw = SAFU.HWPass(_project);
            
            Thread.Sleep(2000);
            _log.Send("unlocking");
            if (_instance.ActiveTab.FindElementByAttribute("p", "class", "sc-eZSpzM\\ sc-buTqWO\\ fkCDzC\\ gZChKS", "regexp", 0).IsVoid)  
            {
                _instance.HeSet(("input:password", "fulltagname", "input:password", "regexp", 0),passw, deadline:30);
                _instance.HeClick(("button", "innertext", "Unlock", "regexp", 0));
            }
        }


    }
}