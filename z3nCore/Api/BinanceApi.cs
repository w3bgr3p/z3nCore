using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.Enums.Http;
using ZennoLab.InterfacesLibrary.ProjectModel;


namespace z3nCore
{
    public class BinanceApi
    {


        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;



        private string _apiKey;
        private string _secretKey;
        private string _proxy;

        public BinanceApi(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project;
            _logger = new Logger(project, log: log, classEmoji: "BINANCE");
            LoadKeys();
        }


        public string Withdraw(string coin, string network, string address, string amount)
        {

            network = MapNetwork(network);
            string timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
            string message = $"coin={coin}&network={network}&address={address}&amount={amount}&timestamp={timestamp}";
            string signature = CalculateHmacSha256Signature(message);
            string payload = $"coin={coin}&network={network}&address={address}&amount={amount}&timestamp={timestamp}&signature={signature}";
            string url = "https://api.binance.com/sapi/v1/capital/withdraw/apply";

            var result = Post(url, payload);
            _logger.Send($" => {address} [{amount} {coin} by {network}]: {result}");
            return result;

        }

        public Dictionary<string, string> GetUserAsset()
        {
            string url = "https://api.binance.com/sapi/v3/asset/getUserAsset";
            string message = $"timestamp={DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()}";
            string signature = CalculateHmacSha256Signature(message);
            string payload = $@"{message}&signature={signature}";

            var result = Post(url, payload);

            _project.Json.FromString(result);

            var balances = new Dictionary<string, string>();
            foreach (var item in _project.Json)
            {
                string asset = item.asset;
                string free = item.free;
                balances.Add(asset, free);
            }
            return balances;
        }
        public string GetUserAsset(string coin)
        {
            return GetUserAsset()[coin];
        }

        public List<string> GetWithdrawHistory()
        {

            string url = "https://api.binance.com/sapi/v1/capital/withdraw/history";
            string message = $"timestamp={DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString()}";
            string signature = CalculateHmacSha256Signature(message);
            string payload = $"{message}&signature={signature}";
            url = url + payload;


            string response = Get(url);


            _project.Json.FromString(response);

            var historyList = new List<string>();
            foreach (var item in _project.Json)
            {
                string id = item.id;
                string amount = item.amount;
                string coin = item.coin;
                string status = item.status.ToString();
                historyList.Add($"{id}:{amount}:{coin}:{status}");
            }
            return historyList;
        }
        public string GetWithdrawHistory(string searchId = "")
        {
            var historyList = GetWithdrawHistory();

            foreach (string withdrawal in historyList)
            {
                if (withdrawal.Contains(searchId))
                    return withdrawal;
            }
            return $"NoIdFound: {searchId}";
        }

        private string MapNetwork(string chain)
        {
            chain = chain.ToLower();
            switch (chain)
            {
                case "arbitrum": return "ARBITRUM";
                case "ethereum": return "ETH";
                case "base": return "BASE";
                case "bsc": return "BSC";
                case "avalanche": return "AVAXC";
                case "polygon": return "MATIC";
                case "optimism": return "OPTIMISM";
                case "trc20": return "TRC20";
                case "zksync": return "ZkSync";
                case "aptos": return "APT";
                default:
                    throw new ArgumentException("Unsupported network: " + chain);
            }
        }
        private string CalculateHmacSha256Signature(string message)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_secretKey);
            using (var hmacSha256 = new HMACSHA256(keyBytes))
            {
                var messageBytes = Encoding.UTF8.GetBytes(message);
                var hashBytes = hmacSha256.ComputeHash(messageBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
        private string Post(string url, string payload)
        {
            var result = ZennoPoster.HTTP.Request(
                ZennoLab.InterfacesLibrary.Enums.Http.HttpMethod.POST,
                url, // url
                payload,
                "application/x-www-form-urlencoded; charset=utf-8",
                _proxy,
                "UTF-8",
                ZennoLab.InterfacesLibrary.Enums.Http.ResponceType.BodyOnly,
                10000,
                "",
                _project.Profile.UserAgent,
                true,
                5,
                new string[] {
                    "X-MBX-APIKEY: "+ _apiKey,
                    "Content-Type: application/x-www-form-urlencoded; charset=utf-8"
                },
                "",
                false,
                false,
                _project.Profile.CookieContainer
                );
            return result;
        }
        private string Get(string url)
        {
            /*
            string result = ZennoPoster.HttpGet(
                            url,
                            _proxy,
                            "UTF-8",
                            ResponceType.BodyOnly,
                            30000,
                            "",
                            "Mozilla/4.0",
                            true,
                            5,
                            new string[] {
                            "X-MBX-APIKEY: "+_apiKey,
                            "Content-Type: application/x-www-form-urlencoded; charset=utf-8"
                            },
                            "",
                            true
                        );
            */
            string result = ZennoPoster.HTTP.Request(
                ZennoLab.InterfacesLibrary.Enums.Http.HttpMethod.GET,
                url,
                "",
                "application/json",
                _proxy,
                "UTF-8",
                ResponceType.BodyOnly,
                30000,
                "",
                "Mozilla/4.0",
                true,
                5,
                new string[] {
                    "X-MBX-APIKEY: "+_apiKey,
                    "Content-Type: application/x-www-form-urlencoded; charset=utf-8"
                },
                "",
                false,
                false,
                null);
            
            _logger.Send($"json received: [{result}]");
            _project.Json.FromString(result);

            return result;
        }
        private void LoadKeys()
        {
            var creds = _project.SqlGet("apikey, apisecret, proxy", "_api", where: "id = 'binance'").Split('|');


            _apiKey = creds[0];
            _secretKey = creds[1];
            _proxy = creds[2];


        }

    }
}
