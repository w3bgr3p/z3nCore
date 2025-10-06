
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ZennoLab.InterfacesLibrary.ProjectModel;


namespace z3nCore.W3b
{
    public class SolTools
    {
        public async Task<decimal> GetSolanaBalance(string rpc, string address)
        {
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""getBalance"", ""params"": [""{address}""], ""id"": 1 }}";

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage
                {
                    Method = System.Net.Http.HttpMethod.Post,
                    RequestUri = new Uri(rpc),
                    Content = new System.Net.Http.StringContent(jsonBody, Encoding.UTF8, "application/json")
                };

                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();

                    var json = JObject.Parse(body);
                    BigInteger balanceLamports = json["result"]?["value"]?.ToObject<BigInteger>() ?? 0;
                    decimal balance = (decimal)balanceLamports / 1_000_000_000m; // Convert lamports to SOL
                    return balance;
                }
            }
        }
        
        public async Task<decimal> GetSplTokenBalance(string rpc, string walletAddress, string tokenMint)
        {
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""getTokenAccountsByOwner"", ""params"": [""{walletAddress}"", {{""mint"": ""{tokenMint}""}}, {{""encoding"": ""jsonParsed""}}], ""id"": 1 }}";

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage
                {
                    Method = System.Net.Http.HttpMethod.Post,
                    RequestUri = new Uri(rpc),
                    Content = new System.Net.Http.StringContent(jsonBody, Encoding.UTF8, "application/json")
                };

                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(body);

                    var accounts = json["result"]?["value"] as JArray;
                    if (accounts == null || accounts.Count == 0)
                        return 0m;

                    var tokenData = accounts[0]?["account"]?["data"]?["parsed"]?["info"];
                    if (tokenData == null)
                        return 0m;

                    string amount = tokenData["tokenAmount"]?["uiAmountString"]?.ToString();
                    if (string.IsNullOrEmpty(amount))
                        return 0m;

                    return decimal.Parse(amount, CultureInfo.InvariantCulture);
                }
            }
        }
        
        public async Task<decimal> SolFeeByTx(string transactionHash, string rpc = null, string tokenDecimal = "9")
        {
            if (string.IsNullOrEmpty(rpc)) rpc = "https://api.mainnet-beta.solana.com";

            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""getTransaction"", ""params"": [""{transactionHash}"", {{""encoding"": ""jsonParsed"", ""maxSupportedTransactionVersion"": 0}}], ""id"": 1 }}";

            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage
                {
                    Method = System.Net.Http.HttpMethod.Post,
                    RequestUri = new Uri(rpc),
                    Content = new System.Net.Http.StringContent(jsonBody, Encoding.UTF8, "application/json")
                };

                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();

                    var json = JObject.Parse(body);
                    string feeLamports = json["result"]?["meta"]?["fee"]?.ToString() ?? "0";
                    BigInteger balanceRaw = BigInteger.Parse(feeLamports);
                    decimal decimals = (decimal)Math.Pow(10, double.Parse(tokenDecimal, CultureInfo.InvariantCulture));
                    decimal balance = (decimal)balanceRaw / decimals;
                    return balance;
                }
            }
        }

    }
}

namespace z3nCore
{
    public static partial class W3bTools 
    
    {
        
        public static decimal SolNative(string address, string rpc = "https://api.mainnet-beta.solana.com")
        {
            return new W3b.SolTools().GetSolanaBalance(rpc, address).GetAwaiter().GetResult();
        }
        public static decimal SPL(string tokenMint, string walletAddress, string rpc = "https://api.mainnet-beta.solana.com")
        {
            return new W3b.SolTools().GetSplTokenBalance(rpc, walletAddress, tokenMint).GetAwaiter().GetResult();
        }
        public static decimal SolTxFee(string transactionHash, string rpc = null, string tokenDecimal = "9")
        {
            return new W3b.SolTools().SolFeeByTx(transactionHash, rpc, tokenDecimal).GetAwaiter().GetResult();
        }
        
    }
    
}