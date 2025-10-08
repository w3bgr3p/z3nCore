
using System;
using System.Globalization;
using System.Threading;
using ZennoLab.InterfacesLibrary.ProjectModel;


namespace z3nCore
{

    public class GazZip 
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;
        public GazZip(IZennoPosterProjectModel project, string key = null, bool log = false)

        {
            _project = project;
            _logger = new Logger(project, log: log, classEmoji: " GZ ");
        }
        private string Target(string destination, bool log = false)
        {
            // 0x010066 Sepolia | 0x01019e Soneum | 0x01000e BNB | 0x0100f0 Gravity | 0x010169 Zero
            
            if (destination.StartsWith("0x")) return destination;
            
            destination = destination.ToLower();
            switch (destination)
            {
                case "sepolia":
                    return "0x010066";
                case "soneum":
                    return "0x01019e";
                case "bsc":
                    return "0x01000e";
                case "gravity":
                    return "0x0100f0";
                case "zero":
                    return "0x010169";
                case "opbnb":
                    return "0x01003a";
                default:
                    return "null";
            }

        }

        private void PreCheck(string rpc, decimal value)
        {
            decimal fee = 0.0002m;
            string key = _project.DbKey("evm");
            var accountAddress = key.ToPubEvm(); 
            var native = W3bTools.EvmNative(rpc, accountAddress);
            
            if (native < value + fee)
            {
                _project.warn( $"!no balance over [{value} + {fee}]ETH on {rpc}");
            }
            
        }
        
        public string Refuel(string chainTo, decimal value, string rpc, bool log = false)
        {
            chainTo = Target(chainTo);
            string txHash = null;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Random rnd = new Random();
            
            string key = _project.DbKey("evm");
            var accountAddress = key.ToPubEvm(); 
            PreCheck (rpc, value);
            
            string[] types = { };
            object[] values = { };
            try
            {
                string dataEncoded = chainTo;
                txHash = new Tx(_project).SendTx(rpc, "0x391E7C679d29bD940d63be94AD22A25d25b5A604", dataEncoded, value, key, 2, 3);
                Thread.Sleep(1000);
                _project.Variables["blockchainHash"].Value = txHash;
            }
            catch (Exception ex) { _project.SendWarningToLog($"{ex.Message}", true); throw; }

            if (log) _logger.Send(txHash);
            W3bTools.WaitTx(rpc, txHash);
            return txHash;
        }

        public string Refuel(string chainTo, decimal value, string[] ChainsFrom = null, bool log = false)
        {
            string rpc = "";
            string key = _project.DbKey("evm");
            var accountAddress = key.ToPubEvm(); 
            bool found = false;
            foreach (string RPC in ChainsFrom)
            {
                rpc = Rpc.Get(RPC);
                
                var native = W3bTools.EvmNative(rpc, accountAddress);
                var required = value;// + 0.00015m;
                if (native > required)
                {
                    _logger.Send($"CHOSEN: rpc:[{rpc}] native:[{native}]");
                    found = true;
                    break;
                }
                if (log) _logger.Send($"rpc:[{rpc}] native:[{native}] lower than [{required}]");
                Thread.Sleep(1000);
            }

            if (!found)
            {
                throw new Exception($"fail: no balance over {value}ETH found by all Chains") ;
            }

            return Refuel(chainTo, value, rpc, log: log);
        }

    }

    public static partial class ProjectExtensions
    {
        public static decimal RandomDecimal(decimal min, decimal max, int decimals = 0)
        {
            Random random = new Random();
            double range = (double)(max - min);
            double sample = random.NextDouble();
            decimal result = (decimal)(sample * range) + min;
            if (decimals != 0) result =  Math.Round(result, decimals);
            return result;
        }
    }


}
