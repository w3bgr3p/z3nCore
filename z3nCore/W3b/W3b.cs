
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

namespace z3nCore
{
    

    public class CoinGecco
    {

        private readonly string _apiKey = "CG-TJ3DRjP93bTSCto6LiPbMgaV";
    
        // ПРАВИЛЬНО: один клиент на весь класс
        private static readonly HttpClient _sharedClient = new HttpClient();
    
        private void AddHeaders(HttpRequestHeaders headers, string apiKey)
        {
            headers.Add("accept", "application/json");
            headers.Add("x-cg-pro-api-key", apiKey);
        }
        
        public async Task<string> CoinInfo(string CGid = "ethereum")
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://api.coingecko.com/api/v3/coins/{CGid}")
            };
            AddHeaders(request.Headers, _apiKey); 

            using (var response = await _sharedClient.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                return body;
            }
        }
        
        
        private static string IdByTiker(string tiker)
        {
            switch (tiker)
            {
                case "ETH":
                    return "ethereum";
                case "BNB":
                    return "binancecoin";
                case "SOL":
                    return "solana";
                default:
                    throw new Exception($"unknown tiker {tiker}");
            }
        }
        public async Task<string> TokenByAddress(string CGid = "ethereum")
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://pro-api.coingecko.com/api/v3/onchain/simple/networks/network/token_price/addresses")
            };
            AddHeaders(request.Headers, _apiKey); 

            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine(body);
                return body;
            }
        }
        public static decimal PriceByTiker(string tiker, [CallerMemberName] string callerName = "")
        {
            try
            {
                string CGid = IdByTiker(tiker);
                return PriceById(CGid, callerName);
            }
            catch (Exception ex)
            {
                var stackFrame = new System.Diagnostics.StackFrame(1);
                var callingMethod = stackFrame.GetMethod();
                string method = string.Empty;
                if (callingMethod != null)
                    method = $"{callingMethod.DeclaringType.Name}.{callerName}";
                throw new Exception(ex.Message + $"\n{method}");
            }
        }
        public static async Task<decimal> PriceByIdAsync(string CGid = "ethereum", [CallerMemberName] string callerName = "")
        {
            try
            {
                string result = await new CoinGecco().CoinInfo(CGid);

                var json = JObject.Parse(result);
                JToken usdPriceToken = json["market_data"]?["current_price"]?["usd"];

                if (usdPriceToken == null)
                {
                    return 0m;
                }

                decimal usdPrice = usdPriceToken.Value<decimal>();
                return usdPrice;
            }
            catch (Exception ex)
            {
                var stackFrame = new System.Diagnostics.StackFrame(1);
                var callingMethod = stackFrame.GetMethod();
                string method = string.Empty;
                if (callingMethod != null)
                    method = $"{callingMethod.DeclaringType.Name}.{callerName}";
                throw new Exception(ex.Message + $"\n{method}");
            }
        }
        public static decimal PriceById(string CGid = "ethereum", [CallerMemberName] string callerName = "")
        {
            return Task.Run(async () => 
                await PriceByIdAsync(CGid, callerName).ConfigureAwait(false)
            ).GetAwaiter().GetResult();
        }

    }

    public class DexScreener
    {

        public async Task<string> CoinInfo(string contract, string chain)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = System.Net.Http.HttpMethod.Get,
                RequestUri = new Uri($"https://api.dexscreener.com/tokens/v1/{chain}/{contract}"),
                Headers =
                {
                    { "accept", "application/json" },
                },
            };

            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                return body;
            }
        }

    }


    public class KuCoin
    {
        public async Task<string> OrderbookByTiker(string ticker = "ETH")
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = System.Net.Http.HttpMethod.Get,
                RequestUri = new Uri($"https://api.kucoin.com/api/v1/market/orderbook/level1?symbol=" + ticker + "-USDT"),
                Headers =
                {
                    { "accept", "application/json" },
                },
            };

            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                return body;
            }
        }

        public static decimal KuPrice(string tiker = "ETH", [CallerMemberName] string callerName = "")
        {
            try
            {
                string result = new KuCoin().OrderbookByTiker(tiker).GetAwaiter().GetResult();

                var json = JObject.Parse(result); // Парсим как объект
                JToken priceToken = json["data"]?["price"]; // Обращаемся к data.price

                if (priceToken == null)
                {
                    return 0m;
                }

                return priceToken.Value<decimal>();
            }
            catch (Exception ex)
            {
                var stackFrame = new System.Diagnostics.StackFrame(1);
                var callingMethod = stackFrame.GetMethod();
                string method = string.Empty;
                if (callingMethod != null)
                    method = $"{callingMethod.DeclaringType.Name}.{callerName}";
                throw new Exception(ex.Message + $"\n{method}");
            }
        }

    }


    public static partial class W3bTools 
    {
        public static async Task<decimal> CGPriceAsync(string CGid = "ethereum", [CallerMemberName] string callerName = "")
        {
            try
            {
                string result = await new CoinGecco().CoinInfo(CGid);

                var json = JObject.Parse(result);
                JToken usdPriceToken = json["market_data"]?["current_price"]?["usd"];

                if (usdPriceToken == null)
                {
                    return 0m;
                }

                decimal usdPrice = usdPriceToken.Value<decimal>();
                return usdPrice;
            }
            catch (Exception ex)
            {
                var stackFrame = new System.Diagnostics.StackFrame(1);
                var callingMethod = stackFrame.GetMethod();
                string method = string.Empty;
                if (callingMethod != null)
                    method = $"{callingMethod.DeclaringType.Name}.{callerName}";
                throw new Exception(ex.Message + $"\n{method}");
            }
        }

        public static decimal CGPrice(string CGid = "ethereum", [CallerMemberName] string callerName = "")
        {
            return Task.Run(async () => 
                await CGPriceAsync(CGid, callerName).ConfigureAwait(false)
            ).GetAwaiter().GetResult();
        }
        
        
        public static async Task<decimal> DSPriceAsync(string contract = "So11111111111111111111111111111111111111112", string chain = "solana", [CallerMemberName] string callerName = "")
        {
            try
            {
                string result = await new DexScreener().CoinInfo(contract, chain);

                var json = JArray.Parse(result);
                JToken priceToken = json.FirstOrDefault()?["priceNative"];

                if (priceToken == null)
                {
                    return 0m;
                }

                return priceToken.Value<decimal>();
            }
            catch (Exception ex)
            {
                var stackFrame = new System.Diagnostics.StackFrame(1);
                var callingMethod = stackFrame.GetMethod();
                string method = string.Empty;
                if (callingMethod != null)
                    method = $"{callingMethod.DeclaringType.Name}.{callerName}";
                throw new Exception(ex.Message + $"\n{method}");
            }
        }

        public static decimal DSPrice(string contract = "So11111111111111111111111111111111111111112", string chain = "solana", [CallerMemberName] string callerName = "")
        {
            return Task.Run(async () => 
                await DSPriceAsync(contract, chain, callerName).ConfigureAwait(false)
            ).GetAwaiter().GetResult();
        }
        public static decimal OKXPrice(this IZennoPosterProjectModel project, string tiker)
        {
            tiker = tiker.ToUpper();
            return new OkxApi(project).OKXPrice<decimal>($"{tiker}-USDT");

        }
        public static decimal UsdToToken(this IZennoPosterProjectModel project, decimal usdAmount, string tiker, string apiProvider = "KuCoin")
        {
            decimal price;
            switch (apiProvider)
            {
                case "KuCoin":
                    price = KuCoin.KuPrice(tiker);
                    break;
                case "OKX":
                    price = project.OKXPrice(tiker);
                    break;
                case "CoinGecco":
                    price = CoinGecco.PriceByTiker(tiker);
                    break;
                default:
                    throw new ArgumentException($"unknown method {apiProvider}");
            }
            return usdAmount / price;
        }

        private static decimal ToDecimal(this BigInteger balanceWei, int decimals = 18)
        {
            BigInteger divisor = BigInteger.Pow(10, decimals);
            BigInteger integerPart = balanceWei / divisor;
            BigInteger fractionalPart = balanceWei % divisor;

            decimal result = (decimal)integerPart + ((decimal)fractionalPart / (decimal)divisor);
            return result;
        }
        private static decimal ToDecimal(this string balanceHex, int decimals = 18)
        {
            BigInteger number = BigInteger.Parse("0" + balanceHex, NumberStyles.AllowHexSpecifier);
            return ToDecimal(number, decimals);
        }


    }

        public class W3bLegacy
    {
        protected readonly IZennoPosterProjectModel _project;
        protected readonly Logger _logger;

        public W3bLegacy(IZennoPosterProjectModel project, bool log = false, string key = null)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            _project = project;
            _logger = new Logger(project, log: log, classEmoji: "💠");
        }

        private string ChekAdr(string address)
        {
            if (string.IsNullOrEmpty(address)) address = _project.Var("addressEvm");
            if (string.IsNullOrEmpty(address)) throw new ArgumentException("!W address is nullOrEmpty");
            return address;
        }
        private string CheckRpc(string rpc)
        {
            if (string.IsNullOrEmpty(rpc)) rpc = _project.Var("blockchainRPC");
            if (string.IsNullOrEmpty(rpc)) throw new ArgumentException("!W rpc is nullOrEmpty");
            return rpc;
        }

        private static decimal ToDecimal(BigInteger balanceWei, int decimals = 18)
        {
            BigInteger divisor = BigInteger.Pow(10, decimals);
            BigInteger integerPart = balanceWei / divisor;
            BigInteger fractionalPart = balanceWei % divisor;

            decimal result = (decimal)integerPart + ((decimal)fractionalPart / (decimal)divisor);
            return result;
        }
        private static decimal ToDecimal(string balanceHex)
        {
            BigInteger number = BigInteger.Parse("0" + balanceHex, NumberStyles.AllowHexSpecifier);
            return ToDecimal(number);
        }

        public decimal NativeEvm(string rpc = null, string address = null, string proxy = null, bool log = false)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            address = ChekAdr(address);
            rpc = CheckRpc(rpc);

            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_getBalance"", ""params"": [""{address}"", ""latest""], ""id"": 1 }}";
            string response;
            try
            {
                response = _project.POST(rpc, jsonBody, proxy:proxy, log:log);
            }
            catch (Exception ex)
            {
                _logger.Send(ex.Message, show:true);
                throw ex;
            }

            var json = JObject.Parse(response);
            string hexBalance = json["result"]?.ToString()?.TrimStart('0', 'x') ?? "0";
            BigInteger balanceWei = BigInteger.Parse("0" + hexBalance, NumberStyles.AllowHexSpecifier);
            decimal balance = ToDecimal(hexBalance);
            _logger.Send($"NativeBal: [{balance}] by {rpc} ({address})");
            return balance;

        }

    }

    public static partial class StringExtensions
    {
        public static BigInteger WeiToEth(this string wei, int decimals = 18)
        {
            return BigInteger.Parse(wei, NumberStyles.AllowHexSpecifier);
        }
    }



}
