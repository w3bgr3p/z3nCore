using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NBitcoin; // BIP39
using Chaos.NaCl; // Ed25519

using System.Linq;

using System.Security.Cryptography;
using ZennoLab.InterfacesLibrary.ProjectModel;


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

        public async Task<decimal> GetSuiTokenBalance(string coinType, string rpc, string address, string proxy = "",
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