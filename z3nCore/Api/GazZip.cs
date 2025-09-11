
using Nethereum.Signer;
using System;

using System.Globalization;

using System.Numerics;

using System.Threading;

using ZennoLab.InterfacesLibrary.ProjectModel;


namespace z3nCore
{
    public enum GZto
    {
        Sepolia,
        Soneum,
        BNB,
        OpBNB,
        Gravity,
        Zero,
    }

    public class GazZip 
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;
        public GazZip(IZennoPosterProjectModel project, string key = null, bool log = false)

        {
            _project = project;
            _logger = new Logger(project, log: log, classEmoji: " GZ ");
        }
        public string GzTarget(GZto destination, bool log = false)
        {
            // 0x010066 Sepolia | 0x01019e Soneum | 0x01000e BNB | 0x0100f0 Gravity | 0x010169 Zero

            switch (destination)
            {
                case GZto.Sepolia:
                    return "0x010066";
                case GZto.Soneum:
                    return "0x01019e";
                case GZto.BNB:
                    return "0x01000e";
                case GZto.Gravity:
                    return "0x0100f0";
                case GZto.Zero:
                    return "0x010169";
                case GZto.OpBNB:
                    return "0x01003a";
                default:
                    return "null";
            }

        }
        public string GZ(string chainTo, decimal value, string rpc = null, bool log = false)

        {

            // 0x010066 Sepolia | 0x01019e Soneum | 0x01000e BNB | 0x0100f0 Gravity | 0x010169 Zero
            string txHash = null;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Random rnd = new Random();
            
            string key = _project.DbKey("evm");
            var accountAddress = key.ToPubEvm(); 

            if (string.IsNullOrEmpty(rpc))
            {
                string chainList = @"https://mainnet.era.zksync.io,
				https://linea-rpc.publicnode.com,
				https://arb1.arbitrum.io/rpc,
				https://optimism-rpc.publicnode.com,
				https://scroll.blockpi.network/v1/rpc/public,
				https://rpc.taiko.xyz,
				https://base.blockpi.network/v1/rpc/public,
				https://rpc.zora.energy";


                bool found = false;
                foreach (string RPC in chainList.Split(','))
                {
                    rpc = RPC.Trim();
                    var native = W3bTools.EvmNative(rpc, accountAddress);
                    var required = value + 0.00015m;
                    if (native > required)
                    {
                        _project.L0g($"CHOSEN: rpc:[{rpc}] native:[{native}]");
                        found = true; break;
                    }
                    if (log) _logger.Send($"rpc:[{rpc}] native:[{native}] lower than [{required}]");
                    Thread.Sleep(1000);
                }


                if (!found)
                {
                    return $"fail: no balance over {value}ETH found by all Chains";
                }
            }

            else
            {
                var native = W3bTools.EvmNative(rpc, accountAddress);
                if (log) _logger.Send($"rpc:[{rpc}] native:[{native}]");
                if (native < value + 0.0002m)
                {
                    return $"fail: no balance over {value}ETH found on {rpc}";
                }
            }
            string[] types = { };
            object[] values = { };


            try
            {
                string dataEncoded = chainTo;//0x010066 for Sepolia | 0x01019e Soneum | 0x01000e BNB
                txHash = new W3b(_project).SendTx(rpc, "0x391E7C679d29bD940d63be94AD22A25d25b5A604", dataEncoded, value, key, 2, 3);
                Thread.Sleep(1000);
                _project.Variables["blockchainHash"].Value = txHash;
            }
            catch (Exception ex) { _project.SendWarningToLog($"{ex.Message}", true); throw; }

            if (log) _logger.Send(txHash);
            _project.WaitTx(rpc, txHash);
            return txHash;
        }
    }
}
