using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.Enums.Http;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Newtonsoft.Json;

namespace z3nCore.Api
{
    public class Mexc
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;
        private string _apiKey;
        private string _secretKey;
        private string _proxy;

        public Mexc(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project;
            _logger = new Logger(project, log: log, classEmoji: "MEXC");
            LoadKeys();
        }

        private void LoadKeys()
        {
            var creds = _project.SqlGet("apikey, apisecret, proxy", "_api", where: "id = 'mexc'").Split('|');
            _apiKey = creds[0];
            _secretKey = creds[1];
            _proxy = creds.Length > 2 ? creds[2] : "";
        }

        private string MapNetwork(string chain)
        {
            _logger.Send("Mapping network: " + chain);
            chain = chain.ToLower();
            switch (chain)
            {
                case "arbitrum": return "ARBITRUM";
                case "ethereum": return "ERC20";
                case "base": return "BASE";
                case "bsc": return "BEP20(BSC)";
                case "avalanche": return "AVAX-C";
                case "polygon": return "POLYGON";
                case "optimism": return "OPTIMISM";
                case "trc20": return "TRC20";
                case "zksync": return "ZKSYNC";
                case "aptos": return "APTOS";
                default:
                    _logger.Send("Unsupported network: " + chain);
                    return chain.ToUpper();
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

        private string MexcPost(string url, string payload)
        {
            var result = ZennoPoster.HTTP.Request(
                HttpMethod.POST,
                "https://api.mexc.com" + url,
                payload,
                "application/json; charset=utf-8",
                _proxy,
                "UTF-8",
                ResponceType.BodyOnly,
                30000,
                "",
                _project.Profile.UserAgent,
                true,
                5,
                new string[]
                {
                    "X-MEXC-APIKEY: " + _apiKey,
                    "Content-Type: application/json; charset=utf-8"
                },
                "",
                false,
                false,
                _project.Profile.CookieContainer
            );

            _logger.Send($"Response: {result}");
            return result;
        }

        private string MexcGet(string url, string queryString = "")
        {
            string fullUrl = url;
            if (!string.IsNullOrEmpty(queryString))
            {
                fullUrl += "?" + queryString;
            }

            var result = ZennoPoster.HTTP.Request(
                HttpMethod.GET,
                "https://api.mexc.com" + fullUrl,
                "",
                "application/json",
                _proxy,
                "UTF-8",
                ResponceType.BodyOnly,
                30000,
                "",
                _project.Profile.UserAgent,
                true,
                5,
                new string[]
                {
                    "X-MEXC-APIKEY: " + _apiKey,
                    "Content-Type: application/json"
                },
                "",
                false,
                false,
                _project.Profile.CookieContainer
            );

            _logger.Send($"Response: {result}");
            return result;
        }

        public Dictionary<string, string> GetSpotBalance()
        {
            try
            {
                string timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                string message = $"timestamp={timestamp}";
                string signature = CalculateHmacSha256Signature(message);
                string queryString = $"timestamp={timestamp}&signature={signature}";

                string response = MexcGet("/api/v3/account", queryString);
                _project.Json.FromString(response);

                var balances = new Dictionary<string, string>();

                if (_project.Json.balances != null)
                {
                    foreach (var item in _project.Json.balances)
                    {
                        string asset = item.asset;
                        string free = item.free;

                        if (!string.IsNullOrEmpty(free) && decimal.Parse(free, CultureInfo.InvariantCulture) > 0)
                        {
                            balances.Add(asset, free);
                            _logger.Send($"Balance: {asset} = {free}");
                        }
                    }
                }

                return balances;
            }
            catch (Exception ex)
            {
                _logger.Send($"Exception in GetSpotBalance: {ex.Message}");
                return new Dictionary<string, string>();
            }
        }

        public string GetSpotBalance(string coin)
        {
            var balances = GetSpotBalance();
            return balances.ContainsKey(coin) ? balances[coin] : "0";
        }

        public string Withdraw(string coin, string network, string address, string amount, string memo = "", string remark = "")
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                string mappedNetwork = MapNetwork(network);
                string timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();

                string message = $"coin={coin}&address={address}&amount={amount}&netWork={mappedNetwork}&timestamp={timestamp}";
                if (!string.IsNullOrEmpty(memo))
                {
                    message += $"&memo={memo}";
                }
                if (!string.IsNullOrEmpty(remark))
                {
                    message += $"&remark={remark}";
                }

                string signature = CalculateHmacSha256Signature(message);
                string payload = message + $"&signature={signature}";

                string response = MexcPost("/api/v3/capital/withdraw", payload);
                _project.Json.FromString(response);

                if (_project.Json.id != null)
                {
                    string withdrawId = _project.Json.id;
                    _logger.Send($"Withdrawal successful: {address} [{amount} {coin} via {mappedNetwork}] - ID: {withdrawId}");
                    return withdrawId;
                }
                else
                {
                    string errorMsg = $"Withdrawal failed: {response}";
                    _logger.Send(errorMsg);
                    throw new Exception(errorMsg);
                }
            }
            catch (Exception ex)
            {
                _logger.Send($"Exception in Withdraw: {ex.Message}");
                throw;
            }
        }

        public List<string> GetWithdrawHistory(int limit = 1000)
        {
            try
            {
                string timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                string message = $"limit={limit}&timestamp={timestamp}";
                string signature = CalculateHmacSha256Signature(message);
                string queryString = $"limit={limit}&timestamp={timestamp}&signature={signature}";

                string response = MexcGet("/api/v3/capital/withdraw/history", queryString);
                _project.Json.FromString(response);

                var historyList = new List<string>();

                if (_project.Json != null && _project.Json.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                {
                    foreach (var item in _project.Json)
                    {
                        string id = item.id?.ToString() ?? "";
                        string coin = item.coin?.ToString() ?? "";
                        string amount = item.amount?.ToString() ?? "";
                        string status = item.status?.ToString() ?? "";
                        string address = item.address?.ToString() ?? "";
                        string network = item.network?.ToString() ?? "";

                        historyList.Add($"{id}:{coin}:{amount}:{status}:{address}:{network}");
                        _logger.Send($"Withdrawal: {id} - {amount} {coin} to {address} [{status}]");
                    }
                }

                return historyList;
            }
            catch (Exception ex)
            {
                _logger.Send($"Exception in GetWithdrawHistory: {ex.Message}");
                return new List<string>();
            }
        }

        public string GetWithdrawHistory(string searchId)
        {
            var historyList = GetWithdrawHistory();

            foreach (string withdrawal in historyList)
            {
                if (withdrawal.Contains(searchId))
                    return withdrawal;
            }
            return $"NoIdFound: {searchId}";
        }

        public List<string> GetDepositHistory(string coin = "", int limit = 1000)
        {
            try
            {
                string timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                string message = $"limit={limit}&timestamp={timestamp}";
                if (!string.IsNullOrEmpty(coin))
                {
                    message = $"coin={coin}&limit={limit}&timestamp={timestamp}";
                }

                string signature = CalculateHmacSha256Signature(message);
                string queryString = message + $"&signature={signature}";

                string response = MexcGet("/api/v3/capital/deposit/hisrec", queryString);
                _project.Json.FromString(response);

                var historyList = new List<string>();

                if (_project.Json != null && _project.Json.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                {
                    foreach (var item in _project.Json)
                    {
                        string amount = item.amount?.ToString() ?? "";
                        string coinName = item.coin?.ToString() ?? "";
                        string network = item.network?.ToString() ?? "";
                        string status = item.status?.ToString() ?? "";
                        string address = item.address?.ToString() ?? "";
                        string txId = item.txId?.ToString() ?? "";

                        historyList.Add($"{txId}:{coinName}:{amount}:{status}:{address}:{network}");
                        _logger.Send($"Deposit: {txId} - {amount} {coinName} from {address} [{status}]");
                    }
                }

                return historyList;
            }
            catch (Exception ex)
            {
                _logger.Send($"Exception in GetDepositHistory: {ex.Message}");
                return new List<string>();
            }
        }

        public string GetDepositAddress(string coin, string network = "")
        {
            try
            {
                string timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                string message = $"coin={coin}&timestamp={timestamp}";
                
                if (!string.IsNullOrEmpty(network))
                {
                    string mappedNetwork = MapNetwork(network);
                    message = $"coin={coin}&network={mappedNetwork}&timestamp={timestamp}";
                }

                string signature = CalculateHmacSha256Signature(message);
                string queryString = message + $"&signature={signature}";

                string response = MexcGet("/api/v3/capital/deposit/address", queryString);
                _project.Json.FromString(response);

                if (_project.Json != null && _project.Json.Type == Newtonsoft.Json.Linq.JTokenType.Array && _project.Json.Count > 0)
                {
                    var firstAddress = _project.Json[0];
                    string address = firstAddress.address?.ToString() ?? "";
                    string memo = firstAddress.memo?.ToString() ?? "";

                    _logger.Send($"Deposit address for {coin}: {address}" + 
                                (string.IsNullOrEmpty(memo) ? "" : $" (Memo: {memo})"));

                    return !string.IsNullOrEmpty(memo) ? $"{address}:{memo}" : address;
                }
                else
                {
                    string errorMsg = $"No deposit address found for {coin}";
                    _logger.Send(errorMsg);
                    throw new Exception(errorMsg);
                }
            }
            catch (Exception ex)
            {
                _logger.Send($"Exception in GetDepositAddress: {ex.Message}");
                throw;
            }
        }

        public void GetCoins()
        {
            try
            {
                string timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                string message = $"timestamp={timestamp}";
                string signature = CalculateHmacSha256Signature(message);
                string queryString = $"timestamp={timestamp}&signature={signature}";

                string response = MexcGet("/api/v3/capital/config/getall", queryString);
                _project.Json.FromString(response);
                
            }
            catch (Exception ex)
            {
                _logger.Send($"Exception in GetSupportedCoins: {ex.Message}");
            }
        }
        
        
        public List<string> GetSupportedCoins()
        {
            try
            {
                string timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                string message = $"timestamp={timestamp}";
                string signature = CalculateHmacSha256Signature(message);
                string queryString = $"timestamp={timestamp}&signature={signature}";

                string response = MexcGet("/api/v3/capital/config/getall", queryString);
                _project.Json.FromString(response);

                var coinsList = new List<string>();

                if (_project.Json != null && _project.Json.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                {
                    foreach (var item in _project.Json)
                    {
                        string coinName = item.coin?.ToString() ?? "";
                        string networks = "";

                        if (item.networkList != null)
                        {
                            foreach (var network in item.networkList)
                            {
                                string netWork = network.netWork?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(netWork))
                                {
                                    networks += netWork + ";";
                                }
                            }
                        }

                        coinsList.Add($"{coinName}:{networks.TrimEnd(';')}");
                        _logger.Send($"Supported coin: {coinName} - Networks: {networks}");
                    }
                }

                return coinsList;
            }
            catch (Exception ex)
            {
                _logger.Send($"Exception in GetSupportedCoins: {ex.Message}");
                return new List<string>();
            }
        }

        public T GetPrice<T>(string symbol)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                string response = MexcGet($"/api/v3/ticker/price?symbol={symbol}");
                _project.Json.FromString(response);

                if (_project.Json.price != null)
                {
                    string priceStr = _project.Json.price.ToString();
                    decimal price = decimal.Parse(priceStr, CultureInfo.InvariantCulture);

                    _logger.Send($"{symbol} price: {price}");

                    if (typeof(T) == typeof(string))
                        return (T)Convert.ChangeType(price.ToString("0.##################"), typeof(T));
                    return (T)Convert.ChangeType(price, typeof(T));
                }
                else
                {
                    throw new Exception($"Error getting price for {symbol}: {response}");
                }
            }
            catch (Exception ex)
            {
                _logger.Send($"Exception in GetPrice: {ex.Message}");
                throw;
            }
        }

        public string CancelWithdraw(string withdrawId)
        {
            try
            {
                string timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                string message = $"id={withdrawId}&timestamp={timestamp}";
                string signature = CalculateHmacSha256Signature(message);
                string payload = message + $"&signature={signature}";

                var result = ZennoPoster.HTTP.Request(
                    HttpMethod.DELETE,
                    "https://api.mexc.com/api/v3/capital/withdraw",
                    payload,
                    "application/x-www-form-urlencoded; charset=utf-8",
                    _proxy,
                    "UTF-8",
                    ResponceType.BodyOnly,
                    30000,
                    "",
                    _project.Profile.UserAgent,
                    true,
                    5,
                    new string[]
                    {
                        "X-MEXC-APIKEY: " + _apiKey,
                        "Content-Type: application/x-www-form-urlencoded; charset=utf-8"
                    },
                    "",
                    false,
                    false,
                    _project.Profile.CookieContainer
                );

                _project.Json.FromString(result);

                if (_project.Json.id != null)
                {
                    _logger.Send($"Withdrawal {withdrawId} cancelled successfully");
                    return _project.Json.id.ToString();
                }
                else
                {
                    string errorMsg = $"Failed to cancel withdrawal {withdrawId}: {result}";
                    _logger.Send(errorMsg);
                    throw new Exception(errorMsg);
                }
            }
            catch (Exception ex)
            {
                _logger.Send($"Exception in CancelWithdraw: {ex.Message}");
                throw;
            }
        }

        // Internal transfer between SPOT and FUTURES
        public string InternalTransfer(string asset, string amount, string fromAccountType = "SPOT", string toAccountType = "FUTURES")
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                string timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                string message = $"fromAccountType={fromAccountType}&toAccountType={toAccountType}&asset={asset}&amount={amount}&timestamp={timestamp}";
                string signature = CalculateHmacSha256Signature(message);
                string payload = message + $"&signature={signature}";

                string response = MexcPost("/api/v3/capital/transfer", payload);
                _project.Json.FromString(response);

                if (_project.Json != null && _project.Json.Type == Newtonsoft.Json.Linq.JTokenType.Array && _project.Json.Count > 0)
                {
                    string tranId = _project.Json[0].tranId?.ToString() ?? "";
                    _logger.Send($"Internal transfer successful: {amount} {asset} from {fromAccountType} to {toAccountType} - Transfer ID: {tranId}");
                    return tranId;
                }
                else
                {
                    string errorMsg = $"Internal transfer failed: {response}";
                    _logger.Send(errorMsg);
                    throw new Exception(errorMsg);
                }
            }
            catch (Exception ex)
            {
                _logger.Send($"Exception in InternalTransfer: {ex.Message}");
                throw;
            }
        }

        // Get transfer history
        public List<string> GetTransferHistory(string fromAccountType = "SPOT", string toAccountType = "FUTURES", int size = 10)
        {
            try
            {
                string timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                string message = $"fromAccountType={fromAccountType}&toAccountType={toAccountType}&size={size}&timestamp={timestamp}";
                string signature = CalculateHmacSha256Signature(message);
                string queryString = message + $"&signature={signature}";

                string response = MexcGet("/api/v3/capital/transfer", queryString);
                _project.Json.FromString(response);

                var transferList = new List<string>();

                if (_project.Json != null && _project.Json.Type == Newtonsoft.Json.Linq.JTokenType.Array && _project.Json.Count > 0)
                {
                    var rows = _project.Json[0].rows;
                    if (rows != null)
                    {
                        foreach (var transfer in rows)
                        {
                            string tranId = transfer.tranId?.ToString() ?? "";
                            string asset = transfer.asset?.ToString() ?? "";
                            string amount = transfer.amount?.ToString() ?? "";
                            string status = transfer.status?.ToString() ?? "";
                            string fromType = transfer.fromAccountType?.ToString() ?? "";
                            string toType = transfer.toAccountType?.ToString() ?? "";

                            transferList.Add($"{tranId}:{asset}:{amount}:{fromType}:{toType}:{status}");
                            _logger.Send($"Transfer: {tranId} - {amount} {asset} from {fromType} to {toType} [{status}]");
                        }
                    }
                }

                return transferList;
            }
            catch (Exception ex)
            {
                _logger.Send($"Exception in GetTransferHistory: {ex.Message}");
                return new List<string>();
            }
        }
    }
}