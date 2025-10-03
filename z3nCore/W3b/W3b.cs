
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

    public class EvmTools
    {
        
        public async Task<bool> WaitTxExtended(string rpc, string hash, int deadline = 60, string proxy = "", bool log = false)
        {
            string jsonReceipt = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_getTransactionReceipt"", ""params"": [""{hash}""], ""id"": 1 }}";
            string jsonRaw = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_getTransactionByHash"", ""params"": [""{hash}""], ""id"": 1 }}";

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
        }
        public async Task<bool> WaitTx(string rpc, string hash, int deadline = 60, string proxy = "", bool log = false)
        {
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_getTransactionReceipt"", ""params"": [""{hash}""], ""id"": 1 }}";

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
                }
            }
        }
        //new
        public async Task<string> Native(string rpc, string address)
        {
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_getBalance"", ""params"": [""{address}"", ""latest""], ""id"": 1 }}";

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
                    string hexBalance = json["result"]?.ToString().Replace("0x", "") ?? "0";
                    return hexBalance;

                }
            }
        }
        public async Task<string> Erc20(string tokenContract, string rpc, string address)
        {
            string data = "0x70a08231000000000000000000000000" + address.Replace("0x", "");
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_call"", ""params"": [{{ ""to"": ""{tokenContract}"", ""data"": ""{data}"" }}, ""latest""], ""id"": 1 }}";

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
                    string hexBalance = json["result"]?.ToString().Replace("0x", "") ?? "0";
                    return hexBalance;

                }
            }
        }
        public async Task<string> Erc721(string tokenContract, string rpc, string address)
        {
            string data = "0x70a08231000000000000000000000000" + address.Replace("0x", "").ToLower();
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_call"", ""params"": [{{ ""to"": ""{tokenContract}"", ""data"": ""{data}"" }}, ""latest""], ""id"": 1 }}";

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
                    string hexBalance = json["result"]?.ToString().Replace("0x", "") ?? "0";
                    return hexBalance;
                }
            }
        }
        public async Task<string> Erc1155(string tokenContract, string tokenId, string rpc, string address)
        {
            string data = "0x00fdd58e" + address.Replace("0x", "").ToLower().PadLeft(64, '0') + BigInteger.Parse(tokenId).ToString("x").PadLeft(64, '0');
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_call"", ""params"": [{{ ""to"": ""{tokenContract}"", ""data"": ""{data}"" }}, ""latest""], ""id"": 1 }}";

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
                    string hexBalance = json["result"]?.ToString().Replace("0x", "") ?? "0";
                    return hexBalance;

                }
            }
        }

        public async Task<string> Nonce(string rpc, string address, string proxy = "", bool log = false)
        {
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_getTransactionCount"", ""params"": [""{address}"", ""latest""], ""id"": 1 }}";

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
        }
        public async Task<string> ChainId(string rpc, string proxy = "", bool log = false)
        {
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_chainId"", ""params"": [], ""id"": 1 }}";

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
        }
        public async Task<string> GasPrice(string rpc, string proxy = "", bool log = false)
        {
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""eth_gasPrice"", ""params"": [], ""id"": 1 }}";

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
        }


    }


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

    public class AptTools
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

    public class SuiTools
    {
        public async Task<decimal> GetSuiBalance(string rpc, string address, string proxy = "", bool log = false)
        {
            if (string.IsNullOrEmpty(rpc)) rpc = "https://fullnode.mainnet.sui.io";
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""suix_getBalance"", ""params"": [""{address}"", ""0x2::sui::SUI""], ""id"": 1 }}";

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
                        string mist = json["result"]?["totalBalance"]?.ToString() ?? "0";
                        decimal balance = decimal.Parse(mist, CultureInfo.InvariantCulture) / 1000000000m; // 9 decimals for SUI
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

        public async Task<decimal> GetSuiTokenBalance(string coinType, string rpc, string address, string proxy = "", bool log = false)
        {
            if (string.IsNullOrEmpty(rpc)) rpc = "https://fullnode.mainnet.sui.io";
            string jsonBody = $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""suix_getBalance"", ""params"": [""{address}"", ""{coinType}""], ""id"": 1 }}";

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
                        string mist = json["result"]?["totalBalance"]?.ToString() ?? "0";
                        decimal balance = decimal.Parse(mist, CultureInfo.InvariantCulture) / 1000000m; // Assuming 6 decimals for tokens
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

    public class CoinGecco
    {

        private readonly string _apiKey = "CG-TJ3DRjP93bTSCto6LiPbMgaV";
        private void AddHeaders(HttpRequestHeaders headers, string apiKey)
        {
            headers.Add("accept", "application/json");
            headers.Add("x-cg-pro-api-key", apiKey);
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
        public async Task<string> CoinInfo(string CGid = "ethereum")
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = System.Net.Http.HttpMethod.Get,
                RequestUri = new Uri($"https://api.coingecko.com/api/v3/coins/{CGid}")
            };
            AddHeaders(request.Headers, _apiKey); 

            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                return body;
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
        public static decimal PriceById(string CGid = "ethereum", [CallerMemberName] string callerName = "")
        {
            try
            {
                string result = new CoinGecco().CoinInfo(CGid).GetAwaiter().GetResult();

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


    public static class W3bTools 
    
    {
        public static decimal EvmNative(string rpc, string address)
        {
            string nativeHex = new EvmTools().Native(rpc, address).GetAwaiter().GetResult();
            return nativeHex.ToDecimal();
        }
        public static decimal EvmNative(this IZennoPosterProjectModel project, string rpc, string address = null, bool log = false)
        {
            if (string.IsNullOrEmpty(address)) address = (project.Var("addressEvm"));
            return new W3bLegacy(project).NativeEvm(rpc, address, log:log);
        }
        
        public static decimal ERC20(string tokenContract, string rpc, string address, string tokenDecimal = "18")
        {
            string balanceHex = new EvmTools().Erc20(tokenContract, rpc, address).GetAwaiter().GetResult();
            return balanceHex.ToDecimal();
        }
        public static decimal ERC721(string tokenContract, string rpc, string address)
        {
            string balanceHex = new EvmTools().Erc721(tokenContract, rpc, address).GetAwaiter().GetResult();
            return balanceHex.ToDecimal();
        }
        public static decimal ERC1155(string tokenContract, string tokenId, string rpc, string address)
        {
            string balanceHex = new EvmTools().Erc1155(tokenContract,  tokenId, rpc, address).GetAwaiter().GetResult();
            return balanceHex.ToDecimal();
        }
        public static decimal GasPrice(string rpc)
        {
            string balanceHex = new EvmTools().GasPrice(rpc).GetAwaiter().GetResult();
            return balanceHex.ToDecimal(10);
        }

        public static int Nonce(string rpc, string address)
        {
            string nonceHex = new EvmTools().Nonce(rpc, address).GetAwaiter().GetResult();
            int transactionCount = nonceHex == "0" ? 0 : Convert.ToInt32(nonceHex, 16);
            return transactionCount;
        }
        public static int ChainId(string rpc)
        {
            string idHex = new EvmTools().ChainId(rpc).GetAwaiter().GetResult();
            int id = idHex == "0" ? 0 : Convert.ToInt32(idHex, 16);
            return id;
        }

        public static bool WaitTx(string rpc, string hash, int deadline = 60, string proxy = "", bool log = false, bool extended = false)
        {
            if (extended) return  new EvmTools().WaitTxExtended(rpc,  hash, deadline, proxy, log).GetAwaiter().GetResult();
            else return new EvmTools().WaitTx(rpc,  hash, deadline, proxy, log).GetAwaiter().GetResult();
        }

        public static decimal SolNative(string address, string rpc = "https://api.mainnet-beta.solana.com")
        {
            return new SolTools().GetSolanaBalance(rpc, address).GetAwaiter().GetResult();
        }
        public static decimal SPL(string tokenMint, string walletAddress, string rpc = "https://api.mainnet-beta.solana.com")
        {
            return new SolTools().GetSplTokenBalance(rpc, walletAddress, tokenMint).GetAwaiter().GetResult();
        }
        public static decimal SolTxFee(string transactionHash, string rpc = null, string tokenDecimal = "9")
        {
            return new SolTools().SolFeeByTx(transactionHash, rpc, tokenDecimal).GetAwaiter().GetResult();
        }


        public static decimal CGPrice(string CGid = "ethereum",[CallerMemberName] string callerName = "")
        {
            try
            {
                string result = new CoinGecco().CoinInfo(CGid).GetAwaiter().GetResult();

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
        public static decimal DSPrice(string contract = "So11111111111111111111111111111111111111112", string chain = "solana",[CallerMemberName] string callerName = "")
        {
            try
            {
                string result = new DexScreener().CoinInfo(contract, chain).GetAwaiter().GetResult();

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
