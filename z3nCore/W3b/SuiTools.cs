using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using NBitcoin; // BIP39
using Chaos.NaCl; // Ed25519
using System.Linq;
using System.Security.Cryptography;
using ZennoLab.InterfacesLibrary.ProjectModel;
using ZennoLab.CommandCenter;

namespace z3nCore.W3b
{
    internal class SuiTools
    {
        public async Task<decimal> GetSuiBalance(string rpc, string address, string proxy = "", bool log = false)
        {
            if (string.IsNullOrEmpty(rpc)) rpc = "https://fullnode.mainnet.sui.io";
            string jsonBody =
                $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""suix_getBalance"", ""params"": [""{address}"", ""0x2::sui::SUI""], ""id"": 1 }}";

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
                        decimal balance =
                            decimal.Parse(mist, CultureInfo.InvariantCulture) / 1000000000m; // 9 decimals for SUI
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

        public async Task<decimal> GetSuiTokenBalance( string coinType, string rpc, string address, string proxy = "",
            bool log = false)
        {
           
            if (string.IsNullOrEmpty(rpc)) rpc = "https://fullnode.mainnet.sui.io";
            string jsonBody =
                $@"{{ ""jsonrpc"": ""2.0"", ""method"": ""suix_getBalance"", ""params"": [""{address}"", ""{coinType}""], ""id"": 1 }}";

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
                        decimal balance =
                            decimal.Parse(mist, CultureInfo.InvariantCulture) /
                            1000000m; // Assuming 6 decimals for tokens
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
        
                #region Send Native

        /// <summary>
        /// Отправить нативную монету SUI с адреса на адрес
        /// </summary>
        /// <param name="to">Адрес получателя</param>
        /// <param name="amount">Сумма в SUI (будет конвертирована в MIST)</param>
        /// <param name="rpc">URL RPC ноды Sui</param>
        /// <param name="privateKeyHex">Приватный ключ в HEX формате (32 bytes)</param>
        /// <param name="debug">Режим отладки</param>
        /// <returns>Transaction digest (hash)</returns>
        public async Task<string> SendNative(string to, decimal amount, string rpc, string privateKeyHex, IZennoPosterProjectModel project, bool debug = false)
        {
            var _logger = new Logger(project, true);
            var _log = true;
            try
            {
                if (string.IsNullOrEmpty(rpc))
                    throw new ArgumentException("RPC URL is null or empty");

                if (string.IsNullOrEmpty(privateKeyHex))
                    throw new ArgumentException("Private key is null or empty");

                if (string.IsNullOrEmpty(to))
                    throw new ArgumentException("Recipient address is null or empty");

                // Конвертируем decimal в MIST (1 SUI = 10^9 MIST)
                long amountInMist = (long)(amount * 1000000000m);

                if (_log) _logger?.Send($"Sending {amount} SUI ({amountInMist} MIST) to {to}");

                // Получаем адрес отправителя из приватного ключа
                byte[] privateKey = HexToBytes(privateKeyHex);
                string fromAddress = PrivateKeyToAddress(privateKey);

                if (_log) _logger?.Send($"From address: {fromAddress}");

                // Получаем gas coins для отправки
                var gasCoins = await GetGasCoins(rpc, fromAddress);
                if (gasCoins.Count == 0)
                {
                    throw new Exception("No gas coins available for transaction");
                }

                // Берем первый coin как газ
                string gasCoinId = gasCoins[0]["coinObjectId"].ToString();
                if (_log) _logger?.Send($"Using gas coin: {gasCoinId}");

                // Пробуем использовать unsafe_paySui (упрощенный метод)
                string txDigest = await SendViaPaySui(rpc, fromAddress, to, amountInMist, gasCoinId, privateKey, project);

                if (_log) _logger?.Send($"Transaction sent: {txDigest}");

                return txDigest;
            }
            catch (Exception ex)
            {
                if (_log) _logger?.Send($"!W Error sending transaction: {ex.Message}", show: true);
                throw;
            }
        }

        /// <summary>
        /// Получить список gas coins для адреса
        /// </summary>
        private async Task<JArray> GetGasCoins(string rpc, string address)
        {
            string jsonBody = $@"{{
                ""jsonrpc"": ""2.0"",
                ""id"": 1,
                ""method"": ""suix_getCoins"",
                ""params"": [
                    ""{address}"",
                    ""0x2::sui::SUI"",
                    null,
                    10
                ]
            }}";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
                client.Timeout = TimeSpan.FromSeconds(10);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(rpc),
                    Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                };

                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(body);

                    if (json["error"] != null)
                    {
                        throw new Exception($"RPC Error: {json["error"]["message"]}");
                    }

                    return (JArray)json["result"]["data"];
                }
            }
        }

        /// <summary>
        /// Отправка через unsafe_paySui (упрощенный метод)
        /// </summary>
        private async Task<string> SendViaPaySui(string rpc, string from, string to, long amountInMist, 
            string gasCoinId, byte[] privateKey, IZennoPosterProjectModel project)
        {
            var _logger = new Logger(project, true);
            var _log = true;
            // Строим транзакцию через unsafe_paySui
            string jsonBody = $@"{{
                ""jsonrpc"": ""2.0"",
                ""id"": 1,
                ""method"": ""unsafe_paySui"",
                ""params"": [
                    ""{from}"",
                    [""{gasCoinId}""],
                    [""{to}""],
                    [{amountInMist}],
                    1000000
                ]
            }}";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
                client.Timeout = TimeSpan.FromSeconds(30);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(rpc),
                    Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                };

                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(body);

                    if (json["error"] != null)
                    {
                        string errorMsg = json["error"]["message"]?.ToString() ?? "Unknown RPC error";
                        
                        // Если метод не поддерживается, пробуем альтернативный способ
                        if (errorMsg.Contains("Method not found") || errorMsg.Contains("unsafe"))
                        {
                            if (_log) _logger?.Send("unsafe_paySui not supported, trying alternative method...");
                            return await SendViaTransactionBlock(rpc, from, to, amountInMist, gasCoinId, privateKey);
                        }
                        
                        throw new Exception($"RPC Error: {errorMsg}");
                    }

                    // Получаем tx_bytes для подписи
                    string txBytes = json["result"]["txBytes"].ToString();
                    
                    // Подписываем транзакцию
                    byte[] txBytesArray = Convert.FromBase64String(txBytes);
                    byte[] signature = SignTransactionBytes(txBytesArray, privateKey);
                    string signatureBase64 = Convert.ToBase64String(signature);

                    // Получаем публичный ключ
                    byte[] publicKey = GetPublicKeyFromPrivate(privateKey);
                    string publicKeyBase64 = Convert.ToBase64String(publicKey);

                    // Отправляем подписанную транзакцию
                    return await ExecuteSignedTransaction(rpc, txBytes, signatureBase64, publicKeyBase64);
                }
            }
        }

        /// <summary>
        /// Альтернативный метод через Transaction Block (если unsafe_paySui не работает)
        /// </summary>
        private Task<string> SendViaTransactionBlock(string rpc, string from, string to, 
            long amountInMist, string gasCoinId, byte[] privateKey)
        {
            // TODO: Реализовать через sui_moveCall и Transaction Block
            // Это более сложный метод, требует BCS serialization
            throw new NotImplementedException(
                "Transaction Block method not implemented yet. " +
                "Please use newer Sui RPC node that supports unsafe_paySui or wait for full implementation.");
        }

        /// <summary>
        /// Подписать байты транзакции с помощью Ed25519
        /// </summary>
        private byte[] SignTransactionBytes(byte[] txBytes, byte[] privateKey32)
        {
            // Добавляем intent prefix для Sui (3 байта)
            byte[] intentMessage = new byte[3 + txBytes.Length];
            intentMessage[0] = 0; // Intent scope: TransactionData
            intentMessage[1] = 0; // Intent version
            intentMessage[2] = 0; // Intent app id
            Buffer.BlockCopy(txBytes, 0, intentMessage, 3, txBytes.Length);

            // Хешируем Blake2b
            byte[] messageHash = Blake2b.ComputeHash(intentMessage, 32);

            // Получаем expanded key для подписи
            byte[] publicKey = new byte[32];
            byte[] expandedPrivateKey = new byte[64];
            Ed25519.KeyPairFromSeed(out publicKey, out expandedPrivateKey, privateKey32);

            // Подписываем
            byte[] signature = Ed25519.Sign(messageHash, expandedPrivateKey);

            // Формат подписи для Sui: flag (1 byte) + signature (64 bytes) + public key (32 bytes)
            byte[] suiSignature = new byte[1 + 64 + 32];
            suiSignature[0] = 0x00; // Ed25519 flag
            Buffer.BlockCopy(signature, 0, suiSignature, 1, 64);
            Buffer.BlockCopy(publicKey, 0, suiSignature, 65, 32);

            return suiSignature;
        }

        /// <summary>
        /// Получить публичный ключ из приватного
        /// </summary>
        private byte[] GetPublicKeyFromPrivate(byte[] privateKey32)
        {
            byte[] publicKey = new byte[32];
            byte[] expanded = new byte[64];
            Ed25519.KeyPairFromSeed(out publicKey, out expanded, privateKey32);
            return publicKey;
        }

        /// <summary>
        /// Выполнить подписанную транзакцию
        /// </summary>
        private async Task<string> ExecuteSignedTransaction(string rpc, string txBytes, 
            string signature, string publicKey)
        {
            string jsonBody = $@"{{
                ""jsonrpc"": ""2.0"",
                ""id"": 1,
                ""method"": ""sui_executeTransactionBlock"",
                ""params"": [
                    ""{txBytes}"",
                    [""{signature}""],
                    {{
                        ""showInput"": true,
                        ""showEffects"": true,
                        ""showEvents"": true
                    }},
                    ""WaitForLocalExecution""
                ]
            }}";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
                client.Timeout = TimeSpan.FromSeconds(30);

                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(rpc),
                    Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                };

                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(body);

                    if (json["error"] != null)
                    {
                        throw new Exception($"Execution Error: {json["error"]["message"]}");
                    }

                    string digest = json["result"]["digest"].ToString();
                    return digest;
                }
            }
        }

        /// <summary>
        /// Конвертировать приватный ключ в адрес
        /// </summary>
        private string PrivateKeyToAddress(byte[] privateKey32)
        {
            byte[] pub = new byte[32];
            byte[] expanded = new byte[64];
            Ed25519.KeyPairFromSeed(out pub, out expanded, privateKey32);

            pub = expanded.Skip(32).Take(32).ToArray();

            byte[] dataToHash = new byte[1 + 32];
            dataToHash[0] = 0x00;
            Buffer.BlockCopy(pub, 0, dataToHash, 1, 32);
            byte[] addr = Blake2b.ComputeHash(dataToHash, 32);

            return "0x" + SuiKeyGen.ToHex(addr);
        }

        /// <summary>
        /// Конвертировать HEX строку в байты
        /// </summary>
        private byte[] HexToBytes(string hex)
        {
            if (hex.StartsWith("0x") || hex.StartsWith("0X"))
                hex = hex.Substring(2);

            if (hex.Length % 2 != 0)
                throw new Exception("Invalid hex string length");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        #endregion
        
        
        
    }

    public static class SuiKeyGen
    {
        public static SuiKeys Generate(string mnemonic, string passphrase = "", string path = "m/44'/784'/0'/0'/0'")
        {
            var mn = new Mnemonic(mnemonic.Trim());
            byte[] bip39 = mn.DeriveSeed(passphrase);
            uint[] p = ParsePath(path);
            var (privSeed, chain) = Slip10Ed25519(bip39, p);

            byte[] pub = new byte[32];
            byte[] expanded = new byte[64];
            Ed25519.KeyPairFromSeed(out pub, out expanded, privSeed);

            // Настоящий публичный ключ Ed25519 = expanded[32..63]
            pub = expanded.Skip(32).Take(32).ToArray();

            // В Sui адрес = Blake2b(flag + pubkey)
            // flag = 0x00 для Ed25519
            byte[] dataToHash = new byte[1 + 32];
            dataToHash[0] = 0x00; // Ed25519 flag
            Buffer.BlockCopy(pub, 0, dataToHash, 1, 32);
            byte[] addr = Blake2b.ComputeHash(dataToHash, 32);

            return new SuiKeys
            {
                Mnemonic = mnemonic,
                DerivationPath = path,
                Priv32 = privSeed,
                PrivExpanded64 = expanded,
                Pub32 = pub,
                Address = "0x" + ToHex(addr)
            };
        }

        private static (byte[], byte[]) Slip10Ed25519(byte[] seed, uint[] path)
        {
            byte[] key = Encoding.ASCII.GetBytes("ed25519 seed");
            byte[] I = HmacSha512(key, seed);
            byte[] k = I.Take(32).ToArray();
            byte[] c = I.Skip(32).Take(32).ToArray();

            foreach (uint i in path)
            {
                byte[] data = new byte[1 + 32 + 4];
                data[0] = 0x00;
                Buffer.BlockCopy(k, 0, data, 1, 32);
                data[33] = (byte)(i >> 24);
                data[34] = (byte)(i >> 16);
                data[35] = (byte)(i >> 8);
                data[36] = (byte)i;
                byte[] I2 = HmacSha512(c, data);
                k = I2.Take(32).ToArray();
                c = I2.Skip(32).Take(32).ToArray();
            }

            return (k, c);
        }

        private static uint[] ParsePath(string path)
        {
            return path.Substring(2)
                .Split('/')
                .Select(x => (uint.Parse(x.TrimEnd('\'')) | 0x80000000u))
                .ToArray();
        }

        private static byte[] HmacSha512(byte[] key, byte[] data)
        {
            using (var h = new HMACSHA512(key))
                return h.ComputeHash(data);
        }

        public static string ToHex(byte[] b) => BitConverter.ToString(b).Replace("-", "").ToLower();

        public static string ToSuiPrivateKey(byte[] privateKey32)
        {
            // Формат: flag (0x00 для Ed25519) + 32 байта приватного ключа
            byte[] data = new byte[33];
            data[0] = 0x00; // Ed25519 flag
            Buffer.BlockCopy(privateKey32, 0, data, 1, 32);

            return Bech32.Encode("suiprivkey", data);
        }
    }

    public class SuiKeys
    {
        public string Mnemonic;
        public string DerivationPath;
        public byte[] Priv32;
        public byte[] PrivExpanded64;
        public byte[] Pub32;
        public string Address;
        public string PrivateKeyBech32 => SuiKeyGen.ToSuiPrivateKey(Priv32);
    }

}

namespace z3nCore
{
    using W3b;
    public static partial class W3bTools
    {
        public static decimal SuiNative(string rpc, string address)
        {
            return new W3b.SuiTools().GetSuiBalance(rpc, address).GetAwaiter().GetResult();
        }
        public static decimal SuiNative(this IZennoPosterProjectModel project, string rpc = null, string address = null, bool log = false)
        {
            if (string.IsNullOrEmpty(address)) address = (project.Var("addressSui"));
            return SuiNative(rpc, address);
        }
        public static decimal SuiTokenBalance(string coinType,string rpc, string address)
        {
            return new W3b.SuiTools().GetSuiTokenBalance( coinType,rpc, address).GetAwaiter().GetResult();
        }
        public static decimal SuiTokenBalance(this IZennoPosterProjectModel project, string coinType, string rpc = null, string address = null, bool log = false)
        {
            if (string.IsNullOrEmpty(address)) address = (project.Var("addressSui"));
            return SuiTokenBalance(coinType,rpc, address);
        }

        public static void SuiFaucet(this Instance instance, IZennoPosterProjectModel project, string address, int successRequired = 3, string tableToUpdate = null)
        {
            var attemptsCounter = 0;
            var succsessCounter = 0;

            while (attemptsCounter <= 3)
            {
                if (succsessCounter >= successRequired)
                {
                    project.log($"Faucet complete: address={address}, success={succsessCounter}/{successRequired}, attempts={attemptsCounter}");
                    return;
                }
                
                attemptsCounter++;
                
                try
                {
                    instance.ActiveTab.Navigate($"https://faucet.sui.io/?address={address}", "");
                    if (instance.ActiveTab.IsBusy) instance.ActiveTab.WaitDownloading();
                    //instance.HeSet(("sui-address", "name"), address);
                    instance.CFSolve();
                    instance.HeClick(("button", "type", "submit", "regexp", 0));

                    project.Deadline();
                    string faucetResponce = "";

                    while (string.IsNullOrEmpty(faucetResponce))
                    {
                        Thread.Sleep(3000);
                        project.Deadline(60);
                        var resp = new Traffic(project, instance, true).FindAllTrafficElements("https://faucet.testnet.sui.io/v2", false);
                        
                        foreach (var t in resp)
                        {
                            var url = t.Url;
                            var body = t.ResponseBody;
                            
                            if (!string.IsNullOrEmpty(body) && url.Contains("request_challenge"))
                            {
                                if (body.Contains("Failure"))
                                {
                                    project.warn($"Challenge failed: address={address}, attempt={attemptsCounter}/3, response={body}");
                                    throw new Exception(body);
                                }
                            }
                            
                            if (!string.IsNullOrEmpty(body) && url.Contains("faucet_web_gas"))
                            {
                                faucetResponce = body;
                                break;
                            }
                        }
                    }

                    bool succsess = !faucetResponce.Contains("Failure");
                    
                    if (!succsess)
                    {
                        project.warn($"Faucet failed: address={address}, attempt={attemptsCounter}/3, success={succsessCounter}/{successRequired}, response={faucetResponce}");
                    }
                    else
                    {
                        succsessCounter++;
                        Thread.Sleep(7000);
                        var native = project.SuiNative("https://fullnode.testnet.sui.io:443", address);
                        
                        if (!string.IsNullOrEmpty(tableToUpdate))
                            project.DbUpd($"sui_balance = '{native}'", tableToUpdate);
                        
                        project.log($"Faucet success: address={address}, balance={native}, progress={succsessCounter}/{successRequired}, attempt={attemptsCounter}");
                    }
                }
                catch (Exception ex)
                {
                    bool finishNow = ex.Message.Contains("You can request a new token in");
                    project.warn($"Faucet error: address={address}, attempt={attemptsCounter}/3, success={succsessCounter}/{successRequired}, ratelimit={finishNow}, error={ex.Message}");
                    
                    if (finishNow) return;
                }
            }
            
            project.warn($"Faucet exhausted: address={address}, success={succsessCounter}/{successRequired}, totalAttempts={attemptsCounter}");
        }
        
        /// <summary>
        /// Отправить нативную монету SUI (аналог Tx.SendNative для EVM)
        /// </summary>
        /// <param name="project">Project model</param>
        /// <param name="to">Адрес получателя</param>
        /// <param name="amount">Сумма в SUI</param>
        /// <param name="rpc">RPC URL (если null, используется mainnet)</param>
        /// <param name="debug">Режим отладки</param>
        /// <returns>Transaction digest (hash)</returns>
        public static string SendNativeSui(this IZennoPosterProjectModel project, string to, decimal amount, 
            string rpc = null, bool debug = false)
        {
            if (string.IsNullOrEmpty(rpc)) 
                rpc = "https://fullnode.mainnet.sui.io";

            // Получаем приватный ключ (тот же что и для EVM)
            string privateKeyHex = project.DbKey("evm");
            
            if (string.IsNullOrEmpty(privateKeyHex))
                throw new Exception("Private key not found in database");

            var suiTools = new SuiTools();
            
            try
            {
                string txHash = suiTools.SendNative(to, amount, rpc, privateKeyHex, project).GetAwaiter().GetResult();
                
                // Сохраняем хеш в переменную (как в Tx.SendNative)
                try
                {
                    project.Variables["blockchainHash"].Value = txHash;
                }
                catch { }

                return txHash;
            }
            catch (Exception ex)
            {
                project.warn($"SendNativeSui error: {ex.Message}", thrw: true);
                throw;
            }
        }



        public static string SuiKey(this string mnemonic, string keyType = "HEX")
        {
            var keys = SuiKeyGen.Generate(mnemonic);
            
            switch (keyType)
            {
                case "HEX":
                    return SuiKeyGen.ToHex(keys.Priv32);
                case "Bech32":
                    return keys.PrivateKeyBech32;
                case "PubHEX":
                    return SuiKeyGen.ToHex(keys.Pub32);
                case "Address":
                    return keys.Address;
                default:
                    return SuiKeyGen.ToHex(keys.Priv32);
            }
        }
        public static string SuiAddress(this string input)
        {
            var inputType = input.KeyType();
    
            switch (inputType)
            {
                case "seed":
                    // Из мнемоники
                    var keysFromSeed = SuiKeyGen.Generate(input);
                    return keysFromSeed.Address;
            
                case "keySui":
                    // Из Bech32 приватного ключа (suiprivkey1...)
                    byte[] privKeyFromBech32 = DecodeSuiPrivateKey(input);
                    return PrivateKeyToAddress(privKeyFromBech32);
            
                case "keyEvm":
                    // Из HEX приватного ключа (64 символа)
                    string cleanHex = input.StartsWith("0x") ? input.Substring(2) : input;
                    byte[] privKeyFromHex = HexToBytes(cleanHex);
                    return PrivateKeyToAddress(privKeyFromHex);
            
                case "addressSui":
                    // Уже адрес
                    return input;
            
                default:
                    throw new Exception($"Cannot convert {inputType} to SUI address");
            }
        }
        private static byte[] DecodeSuiPrivateKey(string bech32Key)
        {
            // Декодируем suiprivkey1... обратно в байты
            byte[] decoded = Bech32.Bech32ToBytes(bech32Key, "suiprivkey");
    
            // Первый байт - флаг (0x00), остальные 32 - приватный ключ
            if (decoded.Length != 33 || decoded[0] != 0x00)
                throw new Exception("Invalid SUI private key format");
    
            byte[] privateKey = new byte[32];
            Buffer.BlockCopy(decoded, 1, privateKey, 0, 32);
            return privateKey;
        }
        private static string PrivateKeyToAddress(byte[] privateKey32)
        {
            // Генерируем публичный ключ из приватного
            byte[] pub = new byte[32];
            byte[] expanded = new byte[64];
            Ed25519.KeyPairFromSeed(out pub, out expanded, privateKey32);
    
            // Настоящий публичный ключ Ed25519 = expanded[32..63]
            pub = expanded.Skip(32).Take(32).ToArray();
    
            // Адрес = Blake2b(flag + pubkey)
            byte[] dataToHash = new byte[1 + 32];
            dataToHash[0] = 0x00; // Ed25519 flag
            Buffer.BlockCopy(pub, 0, dataToHash, 1, 32);
            byte[] addr = Blake2b.ComputeHash(dataToHash, 32);
    
            return "0x" + SuiKeyGen.ToHex(addr);
        }
        private static byte[] HexToBytes(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new Exception("Invalid hex string length");
    
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
        
        
        
        
        
    }
    
}