using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class Relay
    {

        protected readonly IZennoPosterProjectModel _project;
        protected readonly Instance _instance;
        protected readonly bool _logShow;


        public Relay(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _logShow = log;
        }

        public void Go(string fromChainId, string to, string toCurrency = null, string fromCurrency = null)
        {
            var srcDefault = "0x0000000000000000000000000000000000000000";
            if (string.IsNullOrEmpty(fromCurrency)) fromCurrency = srcDefault;
            if (string.IsNullOrEmpty(toCurrency)) toCurrency = srcDefault;

            string url = $"https://relay.link/bridge/{to}?fromChainId={fromChainId}&toCurrency={toCurrency}&fromCurrency={fromCurrency}";
            _instance.Go(url);

        }
    }
}
