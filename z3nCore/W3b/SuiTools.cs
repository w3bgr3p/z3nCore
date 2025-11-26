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
    public class SuiTools
    {

        private static string Rpc(string rpc)
        {
            switch (rpc)
            {
                case null:
                case "":
                    return "https://fullnode.mainnet.sui.io";
                case "testnet":
                    return "https://fullnode.testnet.sui.io:443";
                default:
                    return rpc;
            }
        }

        #region Balance
        public async Task<decimal> GetSuiBalance(string rpc, string address, string proxy = "", bool log = false)
        {
            rpc = Rpc(rpc);
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
        public async Task<decimal> GetSuiTokenBalance( string coinType, string rpc, string address, string proxy = "", bool log = false)
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
        
        #endregion
        
        #region Tx
        /// <summary>
        /// Отправить нативную монету SUI с адреса на адрес
        /// </summary>
        /// <param name="to">Адрес получателя</param>
        /// <param name="amount">Сумма в SUI (будет конвертирована в MIST)</param>
        /// <param name="rpc">URL RPC ноды Sui</param>
        /// <param name="privateKeyHex">Приватный ключ в HEX формате (32 bytes)</param>
        /// <param name="debug">Режим отладки</param>
        /// <returns>Transaction digest (hash)</returns>
        public async Task<string> SendNative(string to, decimal amount, string rpc, string privateKeyHex)
    {
        var debug = new StringBuilder();
        rpc = Rpc(rpc);
        try
        {
            long amountInMist = (long)(amount * 1000000000m);
            
            byte[] privateKey = HexToBytes(privateKeyHex);
            string fromAddress = PrivateKeyToAddress(privateKey);

            debug.AppendLine($"Sending {amount} SUI ({amountInMist} MIST)");
            debug.AppendLine($"From: {fromAddress}");
            debug.AppendLine($"To: {to}");

            // Получаем ВСЕ coins
            var coins = await GetGasCoins(rpc, fromAddress);
            if (coins.Count == 0)
                throw new Exception("No coins available");

            debug.AppendLine($"Found {coins.Count} coins:");
            foreach (var coin in coins)
            {
                string coinId = coin["coinObjectId"].ToString();
                string balance = coin["balance"].ToString();
                debug.AppendLine($"  - Coin: {coinId}, Balance: {balance} MIST");
            }

            // Берем все coinObjectId в список
            var coinIds = coins.Select(c => c["coinObjectId"].ToString()).ToList();
            
            debug.AppendLine($"Using {coinIds.Count} coins for transaction");

            // Строим транзакцию через unsafe_paySui
            string jsonBody = $@"{{
                ""jsonrpc"": ""2.0"",
                ""id"": 1,
                ""method"": ""unsafe_paySui"",
                ""params"": [
                    ""{fromAddress}"",
                    [{string.Join(",", coinIds.Select(id => $"\"{id}\""))}],
                    [""{to}""],
                    [""{amountInMist}""],
                    ""1000000""
                ]
            }}";

            debug.AppendLine($"Request JSON:\n{jsonBody}");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
                client.Timeout = TimeSpan.FromSeconds(30);

                var request = new HttpRequestMessage
                {
                    Method = System.Net.Http.HttpMethod.Post,
                    RequestUri = new Uri(rpc),
                    Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                };

                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                    
                    debug.AppendLine($"Response:\n{body}");
                    
                    var json = JObject.Parse(body);

                    if (json["error"] != null)
                    {
                        string errorMsg = json["error"]["message"]?.ToString() ?? "Unknown RPC error";
                        string errorCode = json["error"]["code"]?.ToString() ?? "N/A";
                        string errorData = json["error"]["data"]?.ToString() ?? "N/A";
                        
                        debug.AppendLine($"RPC Error Code: {errorCode}");
                        debug.AppendLine($"RPC Error Message: {errorMsg}");
                        debug.AppendLine($"RPC Error Data: {errorData}");
                        
                        throw new Exception($"RPC Error [{errorCode}]: {errorMsg}\n\nDebug:\n{debug}");
                    }

                    string txBytes = json["result"]["txBytes"].ToString();
                    
                    debug.AppendLine("Got txBytes, signing...");
                    
                    byte[] txBytesArray = Convert.FromBase64String(txBytes);
                    byte[] signature = SignTransactionBytes(txBytesArray, privateKey);
                    string signatureBase64 = Convert.ToBase64String(signature);

                    string txHash = await ExecuteSignedTransaction(rpc, txBytes, signatureBase64, debug);
                    
                    debug.AppendLine($"Transaction sent: {txHash}");
                    return txHash;
                }
            }
        }
        catch (Exception ex)
        {
            debug.AppendLine($"Error: {ex.Message}");
            debug.AppendLine($"StackTrace:\n{ex.StackTrace}");
            throw new Exception($"{ex.Message}\n\nDebug:\n{debug}");
        }
    }
        
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
        private byte[] GetPublicKeyFromPrivate(byte[] privateKey32)
        {
            byte[] publicKey = new byte[32];
            byte[] expanded = new byte[64];
            Ed25519.KeyPairFromSeed(out publicKey, out expanded, privateKey32);
            return publicKey;
        }
        private async Task<string> ExecuteSignedTransaction(string rpc, string txBytes, string signature, StringBuilder debug)
        {
            string jsonBody = $@"{{
            ""jsonrpc"": ""2.0"",
            ""id"": 1,
            ""method"": ""sui_executeTransactionBlock"",
            ""params"": [
                ""{txBytes}"",
                [""{signature}""],
                {{""showEffects"": true}},
                ""WaitForLocalExecution""
            ]
        }}";

            debug.AppendLine($"Execute request:\n{jsonBody}");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
                client.Timeout = TimeSpan.FromSeconds(30);

                var request = new HttpRequestMessage
                {
                    Method = System.Net.Http.HttpMethod.Post,
                    RequestUri = new Uri(rpc),
                    Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
                };

                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var body = await response.Content.ReadAsStringAsync();
                
                    debug.AppendLine($"Execute response:\n{body}");
                
                    var json = JObject.Parse(body);

                    if (json["error"] != null)
                    {
                        string errorMsg = json["error"]["message"]?.ToString() ?? "Unknown error";
                        throw new Exception($"Execution Error: {errorMsg}");
                    }

                    return json["result"]["digest"].ToString();
                }
            }
        }
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
        #region Keys
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
        #endregion

        public static class Sui
        {
            public static decimal Native(string rpc, string address = null, IZennoPosterProjectModel project = null)
            {
                address = address ?? project?.Var("addressSui");
    
                if (string.IsNullOrEmpty(address))
                    throw new ArgumentException("Address is required");
    
                return new SuiTools().GetSuiBalance(rpc, address).GetAwaiter().GetResult();
            }
            public static decimal BalanceToken(string coinType, string rpc, string address = null, IZennoPosterProjectModel project = null)
            {
                address = address ?? project?.Var("addressSui");
    
                if (string.IsNullOrEmpty(address))
                    throw new ArgumentException("Address is required");
    
                return new W3b.SuiTools().GetSuiTokenBalance( coinType,rpc, address).GetAwaiter().GetResult();
            }
            public static string SendNative(string to, decimal amount, string rpc = null, string key = null, IZennoPosterProjectModel project = null)
            {
                key = key ?? project?.DbKey("evm");
                
        
                if (string.IsNullOrEmpty(key))
                    throw new Exception("Private key not found in database");

                var suiTools = new SuiTools();
        
                try
                {
                    string txHash = suiTools.SendNative(to, amount, rpc, key).GetAwaiter().GetResult();
                    
                    return txHash;
                }
                catch (Exception ex)
                {
                    project.warn($"SendNativeSui error: {ex.Message}", thrw: true);
                    throw;
                }
            }
            public static void SuiFaucet(Instance instance, IZennoPosterProjectModel project, string address, int successRequired = 3, string tableToUpdate = null)
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

        }

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
        
        public static string SuiKey(this string mnemonic, string keyType = "HEX")
        {
            var keys = SuiTools.SuiKeyGen.Generate(mnemonic);
            
            switch (keyType)
            {
                case "HEX":
                    return SuiTools.SuiKeyGen.ToHex(keys.Priv32);
                case "Bech32":
                    return keys.PrivateKeyBech32;
                case "PubHEX":
                    return SuiTools.SuiKeyGen.ToHex(keys.Pub32);
                case "Address":
                    return keys.Address;
                default:
                    return SuiTools.SuiKeyGen.ToHex(keys.Priv32);
            }
        }
        public static string SuiAddress(this string input)
        {
            var inputType = input.KeyType();
    
            switch (inputType)
            {
                case "seed":
                    // Из мнемоники
                    var keysFromSeed = SuiTools.SuiKeyGen.Generate(input);
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
    
            return "0x" + SuiTools.SuiKeyGen.ToHex(addr);
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