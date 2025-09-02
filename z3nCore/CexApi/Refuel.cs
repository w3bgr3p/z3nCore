using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZennoLab.InterfacesLibrary.ProjectModel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace z3nCore.CexApi
{
    public class Refuel
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly bool _logShow;
        private readonly Sql _sql;

        
        

        private string _acc0;

        public Refuel(IZennoPosterProjectModel project, bool logShow = false)
        {
            _project = project;
            _sql = new Sql(_project);
            _logShow = logShow;
            _acc0 = Acc0();
        }

        private string Acc0()
        {
            try
            {
                return int.TryParse(_project.Variables["acc0"].Value, out _)
                    ? _project.Variables["acc0"].Value
                    : "";
            }
            catch
            {
                Log("acc0 is empty `y");
                return "";
            }
        }

        private string ChekAdr(string address)
        {
            if (!string.IsNullOrEmpty(address)) return address;
            else if (string.IsNullOrEmpty(address)) address = _sql.Address("EVM");
            if (!string.IsNullOrEmpty(address)) return address;
            throw new ArgumentException("!W address is nullOrEmpty");
        }

        public void Log(string toSend = "", [CallerMemberName] string callerName = "", bool log = false)
        {
            if (!_logShow && !log) return;
            var stackFrame = new System.Diagnostics.StackFrame(1);
            var callingMethod = stackFrame.GetMethod();
            if (callingMethod == null || callingMethod.DeclaringType == null || callingMethod.DeclaringType.FullName.Contains("Zenno")) callerName = "null";
            _project.L0g($"[ ⛽️  {callerName}] {toSend} ");
        }

        public void OkxRefuel(decimal inUsd, string chain, string address = null, string tiker = "ETH")
        {
            var _okx = new OKXApi(_project);
            address = ChekAdr(address);
            decimal ethPrice = _okx.OKXPrice<decimal>("ETH-USDT");
            decimal usdAmount = (inUsd) / ethPrice;

            var ballist = _okx.OKXGetSubsBal();
            if (ballist.Count > 0) _okx.OKXDrainSubs();
            Thread.Sleep(3000);

            decimal native = W3bTools.EvmNative(Rpc.Get(chain), address);
                //_w3b.NativeEVM<decimal>(_w3b.Rpc(chain), address, log: true);
            decimal needed = usdAmount - native;
            decimal randomed = needed;
            Log($"widrawing to {chain} {randomed}Eth");
            _okx.OKXWithdraw(address, "ETH", chain, randomed, 0.00004m);


            while (true)
            {
                native = W3bTools.EvmNative(Rpc.Get(chain), address);
                if (native > (randomed - 0.00005m)) break;
                Thread.Sleep(10000);
            }
        }
    }
}
