using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class Stargate
    {

        protected readonly IZennoPosterProjectModel _project;
        protected readonly Instance _instance;
        protected readonly bool _logShow;


        public Stargate(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _logShow = log;
        }

        public void Go(string srcChain, string dstChain, string srcToken = null, string dstToken = null)
        {
            var srcDefault = "0xEeeeeEeeeEeEeeEeEeEeeEEEeeeeEeeeeeeeEEeE";
            if (string.IsNullOrEmpty(srcToken)) srcToken = srcDefault;
            if (string.IsNullOrEmpty(dstToken)) dstToken = srcDefault;
            string url = "https://stargate.finance/bridge?" + $"srcChain={srcChain}" + $"&srcToken={srcToken}" + $"&dstChain={dstChain}" + $"&dstToken={dstToken}";
            if (_instance.ActiveTab.URL != url) _instance.ActiveTab.Navigate(url, "");
            _instance.HeClick(("path", "d", "M6 9.75h12l-3.5-3.5M18 14.25H6l3.5 3.5", "regexp", 0));

        }
        public void Connect(string wallet)
        {

            var connected = new List<string>();
            _project.Deadline();
        check:

            _project.Deadline(60); Thread.Sleep(1000);

            var connectedButton = _instance.ActiveTab.FindElementByAttribute("button", "class", "css-x1wnqh", "regexp", 0);
            var unconnectedButton = _instance.ActiveTab.FindElementByAttribute("button", "sx", "\\[object\\ Object]", "regexp", 0).ParentElement;

            _project.L0g($"checking... {connectedButton.InnerText}  {unconnectedButton.InnerText}");
            if (unconnectedButton.IsVoid && connectedButton.IsVoid) goto check;

           // string state = null;

            if (!connectedButton.FindChildByAttribute("img", "alt", "Zerion", "regexp", 0).IsVoid) connected.Add("Zerion");//state += "Zerion";
            if (!connectedButton.FindChildByAttribute("img", "alt", "Backpack", "regexp", 0).IsVoid) connected.Add("Backpack");
            //else if (unconnectedButton.InnerText == "Connect Wallet") state = "Connect";


            if (connected.Contains(wallet))
            {
                _project.L0g($"{connectedButton.InnerText} connected with {wallet}");
                _instance.HeClick(("button", "class", "css-1k2e1h7", "regexp", 0), deadline: 1, thr0w: false);
            }

            else if (wallet == "Zerion")
            {
                _instance.HeClick(unconnectedButton, emu: 1);
                _instance.HeClick(("button", "innertext", "Zerion\\nConnect", "regexp", 0));
                new ZerionWallet(_project, _instance).Connect();
                goto check;

            }

            else if (wallet == "Backpack" && connected.Contains("Zerion"))
            {
                _instance.HeClick(connectedButton, emu: 1);
                _instance.HeClick(("path", "d", "M14 8H2M8 2v12", "text", 0));
                _instance.HeClick(("div", "innertext", "Connect\\ Another\\ Wallet", "regexp", 0), "last", thr0w: false);
                _instance.HeClick(("img", "alt", "Backpack", "regexp", 0));
                _instance.HeClick(("img", "alt", "Backpack", "regexp", 0));

                goto check;

            }


            else
            {
                _project.L0g($"unknown state {connectedButton.InnerText}  {unconnectedButton.InnerText}");
                _instance.HeClick(unconnectedButton, emu: 1);
                _instance.HeClick(("button", "innertext", "Zerion\\nConnect", "regexp", 0));
                new ZerionWallet(_project, _instance).Connect();

                goto check;
            }

        }
 
        public decimal LoadBalance()
        {
            _project.Deadline();
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        waitForBal:
            _project.Deadline(60);
            string est = _instance.HeGet(("div", "class", "css-n2rwim", "regexp", 0));

            try
            {
                decimal bal = decimal.Parse(est.Split('\n')[1].Replace("Balance: ", ""));
                return bal;
            }
            catch
            {
                goto waitForBal;
            }

        }
        public decimal WaitExpected()
        {
            _project.Deadline();
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        waitForBal:
            _project.Deadline(60);
            string est = _instance.HeGet(("input:text", "class", "css-109vo2x", "regexp", 1), atr: "value");

            try
            {
                decimal expected = decimal.Parse(est);
                return expected;
            }
            catch
            {
                goto waitForBal;
            }

        }

        public void SetManualAddress(string address)
        {
            _instance.HeClick(("button", "innertext", "Advanced\\ Transfer", "regexp", 0));
            _instance.HeClick(("button", "role", "switch", "regexp", 1));
            _instance.HeSet(("input:text", "fulltagname", "input:text", "regexp", 1), address);
        }
        public void GasOnDestination(string qnt, string sliperage = "0.5")
        {
            _instance.HeSet(("input:text", "class", "css-1qhcc16", "regexp", 0), qnt);
            _instance.HeSet(("input:text", "class", "css-1qhcc16", "regexp", 1), sliperage);
        }

        public void ParseStats()
        {
            _instance.Go("https://stargate.finance/");


            _instance.HeClick(("button", "class", "css-x1wnqh", "regexp", 0), deadline: 1, thr0w: false);//open
            _instance.HeClick(("button", "innertext", "Transactions", "regexp", 0), deadline: 1, thr0w: false);//open

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var txs = _instance.ActiveTab.FindElementByAttribute("a", "href", "https://layerzeroscan.com/tx/0x", "regexp", 1).ParentElement.GetChildren(false).ToList();
            int totalCnt = txs.Count;
            decimal totalVlmEth = 0;
            decimal totalVlmUSDe = 0;

            int totalArbitrum = 0;
            int totalOptimism = 0;
            int totalBase = 0;
            int totalSoneium = 0;
            int totalUnichain = 0;
            int totalSolana = 0;

            string strike = null;
            //string route = null;

            int multiplyer = 1;

            var route = new List<string>();

            string TmpD = null;

            foreach (HtmlElement tx in txs)
            {
                var src = tx.InnerText.Split('\n')[1].Trim();
                var dst = tx.InnerText.Split('\n')[2].Trim();
                if (src == "Arbitrum" || dst == "Arbitrum") totalArbitrum++;
                if (src == "Base" || dst == "Base") totalBase++;
                if (src == "OP Mainnet" || dst == "OP Mainnet") totalOptimism++;
                if (src == "Soneium" || dst == "Soneium") totalSoneium++;
                if (src == "Solana" || dst == "Solana") totalSolana++;
                if (src == "Unichain" || dst == "Unichain") totalUnichain++;


                route.Add($"[{src} > {dst}]");

                var val = tx.InnerText.Split('\n')[3].Trim();
                decimal valDec = decimal.Parse(val.Split(' ')[0].Trim());
                if (val.Contains("ETH"))
                {
                    totalVlmEth += valDec;
                }
                else if (val.Contains("USDe"))
                {
                    totalVlmUSDe += valDec;
                }

                string dat = tx.InnerText.Split('\n')[4].Split('·')[1].Replace("ago", "").Trim();

                if (dat != TmpD && TmpD != null)
                {
                    strike += $"[{TmpD} x {multiplyer}].";
                    multiplyer = 1;
                }
                else if (dat == TmpD)
                {
                    multiplyer++;
                }
                else
                {
                    multiplyer = 1;
                }

                TmpD = dat;

                if (tx == txs.Last())
                {
                    strike += $"[{dat} x {multiplyer}].";
                }
            }

            string routestring = string.Join(".", route.AsEnumerable().Reverse());
            strike = strike.Trim('.');

            new Sql(_project).Upd($@"
	            stats_volume = 'Total:[{totalCnt}] BridgedEth:[{totalVlmEth}] BridgedUSDe [{totalVlmUSDe}]',
	            stats_chains = 'Arbitrum: [{totalArbitrum}] Base:[{totalBase}] OP: [{totalOptimism}] Soneium:[{totalSoneium}] Solana:[{totalSolana}] Unichain:[{totalUnichain}]',
	            route = '{routestring}',
	            strike = '{strike}',
	            ");


            _project.SendInfoToLog($@"--------------------------
                Total:[{totalCnt}] BridgedEth:[{totalVlmEth}] BridgedUSDe [{totalVlmUSDe}]
                Arbitrum: [{totalArbitrum}] Base:[{totalBase}] OP: [{totalOptimism}] Soneium:[{totalSoneium}] Solana:[{totalSolana}] Unichain:[{totalUnichain}]
                {strike}
                {routestring}");

            _instance.HeClick(("button", "class", "css-1k2e1h7", "regexp", 0), deadline: 1, thr0w: false);//close
        }

    }
}
