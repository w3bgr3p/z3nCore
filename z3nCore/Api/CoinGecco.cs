
using Newtonsoft.Json.Linq;
using System;

using System.Net.Http;
using System.Net.Http.Headers;

using System.Runtime.CompilerServices;
using System.Threading.Tasks;



namespace z3nCore.Api
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
            
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://pro-api.coingecko.com/api/v3/onchain/simple/networks/network/token_price/addresses")
            };
            AddHeaders(request.Headers, _apiKey); 

            using (var response = await _sharedClient.SendAsync(request))
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

    public static partial class W3bTools
    {
        public static async Task<decimal> CGPriceAsync(string CGid = "ethereum",
            [CallerMemberName] string callerName = "")
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
    }
}
