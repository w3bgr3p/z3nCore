
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


namespace z3nCore.Api
{
    public class DexScreener
    {
        private static readonly HttpClient _sharedClient = new HttpClient();
        
        public async Task<string> CoinInfo(string contract, string chain)
        {
            
            var request = new HttpRequestMessage
            {
                Method = System.Net.Http.HttpMethod.Get,
                RequestUri = new Uri($"https://api.dexscreener.com/tokens/v1/{chain}/{contract}"),
                Headers =
                {
                    { "accept", "application/json" },
                },
            };

            using (var response = await _sharedClient.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                return body;
            }
        }
            
    }

    public static partial class W3bTools
    {

        public static async Task<decimal> DSPriceAsync(string contract = "So11111111111111111111111111111111111111112",
            string chain = "solana", [CallerMemberName] string callerName = "")
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

        public static decimal DSPrice(string contract = "So11111111111111111111111111111111111111112",
            string chain = "solana", [CallerMemberName] string callerName = "")
        {
            return Task.Run(async () =>
                await DSPriceAsync(contract, chain, callerName).ConfigureAwait(false)
            ).GetAwaiter().GetResult();
        }
    }
}