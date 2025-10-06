using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace z3nCore.W3b
{
    public class AptosTools
    {
        public async Task<decimal> GetAptBalance(string rpc, string address, string proxy = "", bool log = false)
        {
            if (string.IsNullOrEmpty(rpc)) rpc = "https://fullnode.mainnet.aptoslabs.com/v1";
            string url = $"{rpc}/view";
            string coinType = "0x1::aptos_coin::AptosCoin";
            string requestBody = $@"{{
            ""function"": ""0x1::coin::balance"",
            ""type_arguments"": [""{coinType}""],
            ""arguments"": [""{address}""]
        }}";

            HttpClient client;
            if (!string.IsNullOrEmpty(proxy))
            {
                var proxyArray = proxy.Split(':');
                var webProxy = new System.Net.WebProxy($"http://{proxyArray[2]}:{proxyArray[3]}")
                {
                    Credentials = new System.Net.NetworkCredential(proxyArray[0], proxyArray[1])
                };
                var handler = new HttpClientHandler { Proxy = webProxy, UseProxy = true };
                client = new HttpClient(handler);
            }
            else
            {
                client = new HttpClient();
            }

            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            client.Timeout = TimeSpan.FromSeconds(5);

            using (client)
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(url),
                    Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
                };

                try
                {
                    using (var response = await client.SendAsync(request))
                    {
                        response.EnsureSuccessStatusCode();
                        var body = await response.Content.ReadAsStringAsync();
                        var json = JArray.Parse(body);
                        string octas = json[0]?.ToString() ?? "0";
                        decimal balance = decimal.Parse(octas, CultureInfo.InvariantCulture) / 100000000m; // 8 decimals for Aptos
                        if (log) Console.WriteLine($"NativeBal: [{balance}] by {rpc} ({address})");
                        return balance;
                    }
                }
                catch (HttpRequestException ex)
                {
                    if (log) Console.WriteLine($"Request error: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    if (log) Console.WriteLine($"Failed to parse response: {ex.Message}");
                    return 0;
                }
            }
        }

        public async Task<decimal> GetAptTokenBalance(string coinType, string rpc, string address, string proxy = "", bool log = false)
        {
            if (string.IsNullOrEmpty(rpc)) rpc = "https://fullnode.mainnet.aptoslabs.com/v1";
            string url = $"{rpc}/accounts/{address}/resource/0x1::coin::CoinStore<{coinType}>";

            HttpClient client;
            if (!string.IsNullOrEmpty(proxy))
            {
                var proxyArray = proxy.Split(':');
                var webProxy = new System.Net.WebProxy($"http://{proxyArray[2]}:{proxyArray[3]}")
                {
                    Credentials = new System.Net.NetworkCredential(proxyArray[0], proxyArray[1])
                };
                var handler = new HttpClientHandler { Proxy = webProxy, UseProxy = true };
                client = new HttpClient(handler);
            }
            else
            {
                client = new HttpClient();
            }

            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            client.Timeout = TimeSpan.FromSeconds(5);

            using (client)
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(url)
                };

                try
                {
                    using (var response = await client.SendAsync(request))
                    {
                        response.EnsureSuccessStatusCode();
                        var body = await response.Content.ReadAsStringAsync();
                        var json = JObject.Parse(body);
                        string octas = json["data"]?["coin"]?["value"]?.ToString() ?? "0";
                        decimal balance = decimal.Parse(octas, CultureInfo.InvariantCulture) / 1000000m; // Assuming 6 decimals for tokens
                        if (log) Console.WriteLine($"{address}: {balance} TOKEN ({coinType})");
                        return balance;
                    }
                }
                catch (HttpRequestException ex)
                {
                    if (log) Console.WriteLine($"Request error: {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    if (log) Console.WriteLine($"Failed to parse response: {ex.Message}");
                    return 0;
                }
            }
        }
    }
}