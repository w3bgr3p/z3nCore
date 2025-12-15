
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
    public class KuCoin
    {
        private static readonly HttpClient _sharedClient = new HttpClient();


        public async Task<string> OrderbookByTiker(string ticker = "ETH")
        {
            var request = new HttpRequestMessage
            {
                Method = System.Net.Http.HttpMethod.Get,
                RequestUri = new Uri($"https://api.kucoin.com/api/v1/market/orderbook/level1?symbol=" + ticker + "-USDT"),
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

}