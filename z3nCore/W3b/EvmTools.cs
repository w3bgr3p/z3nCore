using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore.W3b
{
    /// <summary>
    /// ✅ ИСПРАВЛЕНО: Класс для работы с EVM RPC
    /// Использует singleton HttpClient для предотвращения socket exhaustion
    /// </summary>
    public class EvmTools
    {
        // ✅ ИСПРАВЛЕНИЕ #1: Static HttpClient для запросов без proxy
        private static readonly HttpClient _defaultClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        // ✅ ИСПРАВЛЕНИЕ #2: Кеш клиентов с proxy
        private static readonly ConcurrentDictionary<string, HttpClient> _proxyClients
            = new ConcurrentDictionary<string, HttpClient>();

        private const int MAX_PROXY_CACHE = 50;

        static EvmTools()
        {
            _defaultClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
        }

        /// <summary>
        /// ✅ ИСПРАВЛЕНИЕ: Получает или создает HttpClient
        /// </summary>
        private HttpClient GetHttpClient(string proxy)
        {
            if (string.IsNullOrEmpty(proxy))
            {
                return _defaultClient;
            }

            return _proxyClients.GetOrAdd(proxy, proxyStr =>
            {
                if (_proxyClients.Count >= MAX_PROXY_CACHE)
                {
                    // Кеш переполнен, создаем временный клиент (не кешируем)
                    return CreateProxyClient(proxyStr);
                }

                return CreateProxyClient(proxyStr);
            });
        }

        /// <summary>
        /// Создает HttpClient с proxy
        /// </summary>
        private HttpClient CreateProxyClient(string proxy)
        {
            var proxyArray = proxy.Split(':');
            var webProxy = new WebProxy($"http://{proxyArray[2]}:{proxyArray[3]}")
            {
                Credentials = new NetworkCredential(proxyArray[0], proxyArray[1])
            };

            var handler = new HttpClientHandler
            {
                Proxy = webProxy,
                UseProxy = true
            };

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            return client;
        }

        /// <summary>
        /// ✅ ИСПРАВЛЕНО: Использует переиспользуемый HttpClient
        /// </summary>
        public async Task<bool> WaitTxExtended(string rpc, string hash, int deadline = 60, string proxy = "", bool log = false)
        {
            string jsonReceipt = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_getTransactionReceipt"", ""params"": [""{hash}""], ""id"": 1 }}";
            string jsonRaw = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_getTransactionByHash"", ""params"": [""{hash}""], ""id"": 1 }}";

            // ✅ Используем переиспользуемый клиент
            HttpClient client = GetHttpClient(proxy);

            var startTime = DateTime.Now;
            var timeout = TimeSpan.FromSeconds(deadline);

            while (true)
            {
                if (DateTime.Now - startTime > timeout)
                    throw new Exception($"Timeout {deadline}s");

                try
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(rpc),
                        Content = new StringContent(jsonReceipt, Encoding.UTF8, "application/json")
                    };

                    using (var response = await client.SendAsync(request))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            if (log) Console.WriteLine($"Server error (receipt): {response.StatusCode}");
                            await Task.Delay(2000);
                            continue;
                        }

                        var body = await response.Content.ReadAsStringAsync();
                        var json = JObject.Parse(body);

                        if (string.IsNullOrWhiteSpace(body) || json["result"] == null)
                        {
                            request = new HttpRequestMessage
                            {
                                Method = HttpMethod.Post,
                                RequestUri = new Uri(rpc),
                                Content = new StringContent(jsonRaw, Encoding.UTF8, "application/json")
                            };

                            using (var rawResponse = await client.SendAsync(request))
                            {
                                if (!rawResponse.IsSuccessStatusCode)
                                {
                                    if (log) Console.WriteLine($"Server error (raw): {rawResponse.StatusCode}");
                                    await Task.Delay(2000);
                                    continue;
                                }

                                var rawBody = await rawResponse.Content.ReadAsStringAsync();
                                var rawJson = JObject.Parse(rawBody);

                                if (string.IsNullOrWhiteSpace(rawBody) || rawJson["result"] == null)
                                {
                                    if (log) Console.WriteLine($"[{rpc} {hash}] not found");
                                }
                                else
                                {
                                    if (log)
                                    {
                                        string gas = (rawJson["result"]?["maxFeePerGas"]?.ToString() ?? "0").Replace("0x", "");
                                        string gasPrice = (rawJson["result"]?["gasPrice"]?.ToString() ?? "0").Replace("0x", "");
                                        string nonce = (rawJson["result"]?["nonce"]?.ToString() ?? "0").Replace("0x", "");
                                        string value = (rawJson["result"]?["value"]?.ToString() ?? "0").Replace("0x", "");
                                        Console.WriteLine($"[{rpc} {hash}] pending  gasLimit:[{BigInteger.Parse(gas, NumberStyles.AllowHexSpecifier)}] gasNow:[{BigInteger.Parse(gasPrice, NumberStyles.AllowHexSpecifier)}] nonce:[{BigInteger.Parse(nonce, NumberStyles.AllowHexSpecifier)}] value:[{BigInteger.Parse(value, NumberStyles.AllowHexSpecifier)}]");
                                    }
                                }
                            }
                        }
                        else
                        {
                            string status = json["result"]?["status"]?.ToString().Replace("0x", "") ?? "0";
                            string gasUsed = json["result"]?["gasUsed"]?.ToString().Replace("0x", "") ?? "0";
                            string gasPrice = json["result"]?["effectiveGasPrice"]?.ToString().Replace("0x", "") ?? "0";

                            bool success = status == "1";
                            if (log)
                            {
                                Console.WriteLine($"[{rpc} {hash}] {(success ? "SUCCESS" : "FAIL")} gasUsed: {BigInteger.Parse(gasUsed, NumberStyles.AllowHexSpecifier)}");
                            }
                            return success;
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    if (log) Console.WriteLine($"Request error: {ex.Message}");
                    await Task.Delay(2000);
                    continue;
                }

                await Task.Delay(3000);
            }
        }

        /// <summary>
        /// ✅ ИСПРАВЛЕНО: Использует переиспользуемый HttpClient
        /// </summary>
        public async Task<bool> WaitTx(string rpc, string hash, int deadline = 60, string proxy = "", bool log = false)
        {
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_getTransactionReceipt"", ""params"": [""{hash}""], ""id"": 1 }}";

            // ✅ Используем переиспользуемый клиент
            HttpClient client = GetHttpClient(proxy);

            var startTime = DateTime.Now;
            var timeout = TimeSpan.FromSeconds(deadline);

            while (true)
            {
                if (DateTime.Now - startTime > timeout)
                    throw new Exception($"Timeout {deadline}s");

                try
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(rpc),
                        Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                    };

                    using (var response = await client.SendAsync(request))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            if (log) Console.WriteLine($"Server error: {response.StatusCode}");
                            await Task.Delay(2000);
                            continue;
                        }

                        var body = await response.Content.ReadAsStringAsync();
                        var json = JObject.Parse(body);

                        if (string.IsNullOrWhiteSpace(body) || json["result"] == null)
                        {
                            if (log) Console.WriteLine($"[{rpc} {hash}] not found");
                            await Task.Delay(2000);
                            continue;
                        }

                        string status = json["result"]?["status"]?.ToString().Replace("0x", "") ?? "0";
                        bool success = status == "1";
                        if (log) Console.WriteLine($"[{rpc} {hash}] {(success ? "SUCCESS" : "FAIL")}");
                        return success;
                    }
                }
                catch (HttpRequestException ex)
                {
                    if (log) Console.WriteLine($"Request error: {ex.Message}");
                    await Task.Delay(2000);
                    continue;
                }
                catch (Exception ex)
                {
                    if (log) Console.WriteLine($"Request error: {ex.Message}");
                    await Task.Delay(2000);
                    continue;
                }
            }
        }

        /// <summary>
        /// ✅ ИСПРАВЛЕНО: Использует static _defaultClient
        /// </summary>
        public async Task<string> Native(string rpc, string address)
        {
            address = address.NormalizeAddress();
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_getBalance"", ""params"": [""{address}"", ""latest""], ""id"": 1 }}";

            // ❌ БЫЛО: using (var client = new HttpClient())
            // ✅ СТАЛО: используем _defaultClient
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(rpc),
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            using (var response = await _defaultClient.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();

                var json = JObject.Parse(body);
                string hexBalance = json["result"]?.ToString().Replace("0x", "") ?? "0";
                return hexBalance;
            }
        }

        /// <summary>
        /// ✅ ИСПРАВЛЕНО: Использует static _defaultClient
        /// </summary>
        public async Task<string> Erc20(string tokenContract, string rpc, string address)
        {
            tokenContract = tokenContract.NormalizeAddress();
            address = address.NormalizeAddress();
            string data = "0x70a08231000000000000000000000000" + address.Replace("0x", "");
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_call"", ""params"": [{{ ""to"": ""{tokenContract}"", ""data"": ""{data}"" }}, ""latest""], ""id"": 1 }}";

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(rpc),
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            using (var response = await _defaultClient.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();

                var json = JObject.Parse(body);
                string hexBalance = json["result"]?.ToString().Replace("0x", "") ?? "0";
                return hexBalance;
            }
        }

        /// <summary>
        /// ✅ ИСПРАВЛЕНО: Использует static _defaultClient
        /// </summary>
        public async Task<string> Erc721(string tokenContract, string rpc, string address)
        {
            tokenContract = tokenContract.NormalizeAddress();
            address = address.NormalizeAddress();
            string data = "0x70a08231000000000000000000000000" + address.Replace("0x", "").ToLower();
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_call"", ""params"": [{{ ""to"": ""{tokenContract}"", ""data"": ""{data}"" }}, ""latest""], ""id"": 1 }}";

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(rpc),
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            using (var response = await _defaultClient.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();

                var json = JObject.Parse(body);
                string hexBalance = json["result"]?.ToString().Replace("0x", "") ?? "0";
                return hexBalance;
            }
        }

        /// <summary>
        /// ✅ ИСПРАВЛЕНО: Использует static _defaultClient
        /// </summary>
        public async Task<string> Erc1155(string tokenContract, string tokenId, string rpc, string address)
        {
            tokenContract = tokenContract.NormalizeAddress();
            address = address.NormalizeAddress();
            string data = "0x00fdd58e" + address.Replace("0x", "").ToLower().PadLeft(64, '0') + BigInteger.Parse(tokenId).ToString("x").PadLeft(64, '0');
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_call"", ""params"": [{{ ""to"": ""{tokenContract}"", ""data"": ""{data}"" }}, ""latest""], ""id"": 1 }}";

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(rpc),
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            using (var response = await _defaultClient.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();

                var json = JObject.Parse(body);
                string hexBalance = json["result"]?.ToString().Replace("0x", "") ?? "0";
                return hexBalance;
            }
        }

        /// <summary>
        /// ✅ ИСПРАВЛЕНО: Использует GetHttpClient
        /// </summary>
        public async Task<string> Nonce(string rpc, string address, string proxy = "", bool log = false)
        {
            address = address.NormalizeAddress();
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_getTransactionCount"", ""params"": [""{address}"", ""latest""], ""id"": 1 }}";

            HttpClient client = GetHttpClient(proxy);

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(rpc),
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            try
            {
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(body);
                    string hexResult = json["result"]?.ToString()?.Replace("0x", "") ?? "0";
                    return hexResult;
                }
            }
            catch (HttpRequestException ex)
            {
                if (log) Console.WriteLine($"Request error: {ex.Message}");
                throw ex;
            }
            catch (Exception ex)
            {
                if (log) Console.WriteLine($"Failed to parse response: {ex.Message}");
                throw ex;
            }
        }

        /// <summary>
        /// ✅ ИСПРАВЛЕНО: Использует GetHttpClient
        /// </summary>
        public async Task<string> ChainId(string rpc, string proxy = "", bool log = false)
        {
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_chainId"", ""params"": [], ""id"": 1 }}";

            HttpClient client = GetHttpClient(proxy);

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(rpc),
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            try
            {
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(body);
                    return json["result"]?.ToString() ?? "0x0";
                }
            }
            catch (HttpRequestException ex)
            {
                if (log) Console.WriteLine($"Request error: {ex.Message}");
                throw ex;
            }
            catch (Exception ex)
            {
                if (log) Console.WriteLine($"Failed to parse response: {ex.Message}");
                throw ex;
            }
        }

        /// <summary>
        /// ✅ ИСПРАВЛЕНО: Использует GetHttpClient
        /// </summary>
        public async Task<string> GasPrice(string rpc, string proxy = "", bool log = false)
        {
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_gasPrice"", ""params"": [], ""id"": 1 }}";

            HttpClient client = GetHttpClient(proxy);

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(rpc),
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            try
            {
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(body);
                    return json["result"]?.ToString()?.Replace("0x", "") ?? "0";
                }
            }
            catch (HttpRequestException ex)
            {
                if (log) Console.WriteLine($"Request error: {ex.Message}");
                throw ex;
            }
            catch (Exception ex)
            {
                if (log) Console.WriteLine($"Failed to parse response: {ex.Message}");
                throw ex;
            }
        }

        /// <summary>
        /// ✅ ДОПОЛНИТЕЛЬНО: Метод для очистки кеша proxy клиентов
        /// </summary>
        public static void ClearProxyCache()
        {
            var oldClients = _proxyClients.ToArray();
            _proxyClients.Clear();

            foreach (var kvp in oldClients)
            {
                try
                {
                    kvp.Value?.Dispose();
                }
                catch { }
            }
        }
    }
}

namespace z3nCore
{
    public static partial class W3bTools
    {
        public static decimal EvmNative(string rpc, string address)
        {
            string nativeHex = new W3b.EvmTools().Native(rpc, address).GetAwaiter().GetResult();
            return nativeHex.ToDecimal();
        }

        public static decimal EvmNative(this IZennoPosterProjectModel project, string rpc, string address = null, bool log = false)
        {
            if (string.IsNullOrEmpty(address)) address = (project.Var("addressEvm"));
            return EvmNative(rpc, address);
        }

        public static decimal ERC20(string tokenContract, string rpc, string address, string tokenDecimal = "18")
        {
            string balanceHex = new W3b.EvmTools().Erc20(tokenContract, rpc, address).GetAwaiter().GetResult();
            return balanceHex.ToDecimal();
        }

        public static decimal ERC721(string tokenContract, string rpc, string address)
        {
            string balanceHex = new W3b.EvmTools().Erc721(tokenContract, rpc, address).GetAwaiter().GetResult();
            return balanceHex.ToDecimal();
        }

        public static decimal ERC1155(string tokenContract, string tokenId, string rpc, string address)
        {
            string balanceHex = new W3b.EvmTools().Erc1155(tokenContract, tokenId, rpc, address).GetAwaiter().GetResult();
            return balanceHex.ToDecimal();
        }

        public static decimal GasPrice(string rpc)
        {
            string balanceHex = new W3b.EvmTools().GasPrice(rpc).GetAwaiter().GetResult();
            return balanceHex.ToDecimal(10);
        }

        public static int Nonce(string rpc, string address)
        {
            string nonceHex = new W3b.EvmTools().Nonce(rpc, address).GetAwaiter().GetResult();
            int transactionCount = nonceHex == "0" ? 0 : Convert.ToInt32(nonceHex, 16);
            return transactionCount;
        }

        public static int ChainId(string rpc)
        {
            string idHex = new W3b.EvmTools().ChainId(rpc).GetAwaiter().GetResult();
            int id = idHex == "0" ? 0 : Convert.ToInt32(idHex, 16);
            return id;
        }

        public static bool WaitTx(string rpc, string hash, int deadline = 60, string proxy = "", bool log = false, bool extended = false)
        {
            if (extended) return new W3b.EvmTools().WaitTxExtended(rpc, hash, deadline, proxy, log).GetAwaiter().GetResult();
            else return new W3b.EvmTools().WaitTx(rpc, hash, deadline, proxy, log).GetAwaiter().GetResult();
        }
    }
}