using System;
using System.Collections.Generic;

namespace z3nCore
{
    public static class Rpc
    {
        private static readonly Dictionary<string, string> _rpcs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"Ethereum", "https://ethereum-rpc.publicnode.com"},
            {"Arbitrum", "https://arbitrum-one.publicnode.com"},
            {"Base", "https://base-rpc.publicnode.com"},
            {"Blast", "https://rpc.blast.io"},
            {"Fantom", "https://rpc.fantom.network"},
            {"Linea", "https://rpc.linea.build"},
            {"Manta", "https://pacific-rpc.manta.network/http"},
            {"Optimism", "https://optimism-rpc.publicnode.com"},
            {"Scroll", "https://rpc.scroll.io"},
            {"Soneium", "https://rpc.soneium.org"},
            {"Taiko", "https://rpc.mainnet.taiko.xyz"},
            {"Unichain", "https://unichain.drpc.org"},
            {"Zero", "https://zero.drpc.org"},
            {"Zksync", "https://mainnet.era.zksync.io"},
            {"Zora", "https://rpc.zora.energy"},

            {"Avalanche", "https://avalanche-c-chain.publicnode.com"},
            {"Bsc", "https://bsc-rpc.publicnode.com"},
            {"Gravity", "https://rpc.gravity.xyz"},
            {"Opbnb", "https://opbnb-mainnet-rpc.bnbchain.org"},
            {"Polygon", "https://polygon-rpc.com"},

            {"Sepolia", "https://ethereum-sepolia-rpc.publicnode.com"},
            {"Monad", "https://testnet-rpc.monad.xyz"},
            {"Aptos", "https://fullnode.mainnet.aptoslabs.com/v1"},
            {"Movement", "https://mainnet.movementnetwork.xyz/v1"},

            {"Solana", "https://api.mainnet-beta.solana.com"},
            {"Solana_Devnet", "https://api.devnet.solana.com"},
            {"Solana_Testnet", "https://api.testnet.solana.com"}
        };

        public static string Get(string name)
        {
            if (_rpcs.TryGetValue(name, out var url))
                return url;
            foreach (var key in _rpcs.Keys)
            {
                if (string.Equals(key.Replace("_", ""), name.Replace("_", "").Trim(), StringComparison.OrdinalIgnoreCase))
                    return _rpcs[key];
            }
            throw new ArgumentException($"No RpcUrl provided for '{name}'");
        }
        public static string Ethereum => _rpcs["Ethereum"];
        public static string Arbitrum => _rpcs["Arbitrum"];
        public static string Base => _rpcs["Base"];
        public static string Blast => _rpcs["Blast"];
        public static string Fantom => _rpcs["Fantom"];
        public static string Linea => _rpcs["Linea"];
        public static string Manta => _rpcs["Manta"];
        public static string Optimism => _rpcs["Optimism"];
        public static string Scroll => _rpcs["Scroll"];
        public static string Soneium => _rpcs["Soneium"];
        public static string Taiko => _rpcs["Taiko"];
        public static string Unichain => _rpcs["Unichain"];
        public static string Zero => _rpcs["Zero"];
        public static string Zksync => _rpcs["Zksync"];
        public static string Zora => _rpcs["Zora"];
        public static string Avalanche => _rpcs["Avalanche"];
        public static string Bsc => _rpcs["Bsc"];
        public static string Gravity => _rpcs["Gravity"];
        public static string Opbnb => _rpcs["Opbnb"];
        public static string Polygon => _rpcs["Polygon"];
        public static string Sepolia => _rpcs["Sepolia"];
        public static string Reddio => _rpcs["Reddio"];
        public static string Xrp => _rpcs["Xrp"];
        public static string Monad => _rpcs["Monad"];
        public static string Aptos => _rpcs["Aptos"];
        public static string Movement => _rpcs["Movement"];
        public static string Solana => _rpcs["Solana"];
        public static string Solana_Devnet => _rpcs["Solana_Devnet"];
        public static string Solana_Testnet => _rpcs["Solana_Testnet"];
    }

}
