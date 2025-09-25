using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Newtonsoft.Json;
using System.Security.Cryptography;
using ZennoLab.InterfacesLibrary.Enums.Http;
using System.Globalization;


namespace z3nCore.Api
{
    public class Bitget
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;
        private string _apiKey;
        private string _secretKey;
        private string _passphrase;
        private string _proxy;

        public Bitget(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project;
            _logger = new Logger(project, log: log, classEmoji: "BITGET");
            LoadKeys();
        }

        private void LoadKeys()
        {
            var creds = _project.SqlGet("apikey, apisecret, passphrase, proxy", "_api", where: "id = 'bitget'").Split('|');
            _apiKey = creds[0];
            _secretKey = creds[1];
            _passphrase = creds[2];
            _proxy = creds.Length > 3 ? creds[3] : "";
        }

        private string MapNetwork(string chain)
        {
            _logger.Send("Mapping network: " + chain);
            chain = chain.ToLower();
            switch (chain)
            {
                case "arbitrum": return "Arbitrum One";
                case "ethereum": return "ERC20";
                case "base": return "Base";
                case "bsc": return "BEP20";
                case "avalanche": return "AVAX-C";
                case "polygon": return "Polygon";
                case "optimism": return "Optimism";
                case "trc20": return "TRC20";
                case "zksync": return "zkSync Era";
                case "aptos": return "Aptos";
                default:
                    _logger.Send("Unsupported network: " + chain);
                    return chain.ToUpper();
            }
        }

        private string CalculateHmacSha256ToBaseSignature(string message, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var messageBytes = Encoding.UTF8.GetBytes(message);
            using (var hmacSha256 = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmacSha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        private string BitgetPost(string url, object body, bool log = false)
        {
            var jsonBody = JsonConvert.SerializeObject(body);
            string timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();

            // Build signature string: timestamp + method + requestPath + body
            string message = timestamp + "POST" + url + jsonBody;
            string signature = CalculateHmacSha256ToBaseSignature(message, _secretKey);

            _logger.Send($"Request: {jsonBody}");

            string result = ZennoPoster.HTTP.Request(
                HttpMethod.POST,
                "https://api.bitget.com" + url,
                jsonBody,
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
                    "Content-Type: application/json",
                    "ACCESS-KEY: " + _apiKey,
                    "ACCESS-SIGN: " + signature,
                    "ACCESS-TIMESTAMP: " + timestamp,
                    "ACCESS-PASSPHRASE: " + _passphrase,
                    "locale: en-US"
                },
                "",
                false,
                false,
                _project.Profile.CookieContainer);

            _logger.Send($"Response: {result}");
            return result;
        }

        private string BitgetGet(string url, string queryString = "", bool log = false)
        {
            string timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
            string fullUrl = url;

            if (!string.IsNullOrEmpty(queryString))
            {
                fullUrl += "?" + queryString;
            }

            // Build signature string: timestamp + method + requestPath + queryString
            string message = timestamp + "GET" + fullUrl;
            string signature = CalculateHmacSha256ToBaseSignature(message, _secretKey);

            string result = ZennoPoster.HTTP.Request(
                HttpMethod.GET,
                "https://api.bitget.com" + fullUrl,
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
                    "Content-Type: application/json",
                    "ACCESS-KEY: " + _apiKey,
                    "ACCESS-SIGN: " + signature,
                    "ACCESS-TIMESTAMP: " + timestamp,
                    "ACCESS-PASSPHRASE: " + _passphrase,
                    "locale: en-US"
                },
                "",
                false,
                false,
                _project.Profile.CookieContainer);

            _logger.Send($"Response: {result}");
            return result;
        }

        public Dictionary<string, string> GetSpotBalance()
        {
            try
            {
                string response = BitgetGet("/api/spot/v1/account/assets");
                _project.Json.FromString(response);

                var balances = new Dictionary<string, string>();

                if (_project.Json.code == "00000")
                {
                    foreach (var item in _project.Json.data)
                    {
                        string coin = item.coinName;
                        string available = item.available;

                        if (!string.IsNullOrEmpty(available) && decimal.Parse(available, CultureInfo.InvariantCulture) > 0)
                        {
                            balances.Add(coin, available);
                            _logger.Send($"Balance: {coin} = {available}");
                        }
                    }
                }
                else
                {
                    _logger.Send($"Error getting balance: {_project.Json.msg}");
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

        public string Withdraw(string coin, string chain, string address, string amount, string tag = "", string remark = "", string clientOid = "")
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                string network = MapNetwork(chain);

                var body = new
                {
                    coin = coin,
                    address = address,
                    chain = network,
                    amount = amount,
                    tag = tag,
                    remark = remark
                };

                string response = BitgetPost("/api/spot/v1/wallet/withdrawal-v2", body);
                _project.Json.FromString(response);

                if (_project.Json.code == "00000")
                {
                    string orderId = _project.Json.data.orderId;
                    _logger.Send($"Withdrawal successful: {address} [{amount} {coin} via {network}] - Order ID: {orderId}");
                    return orderId;
                }
                else
                {
                    string errorMsg = $"Withdrawal failed: {_project.Json.msg}";
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

        public List<string> GetWithdrawHistory(int limit = 100)
        {
            try
            {
                string queryString = $"limit={limit}";
                string response = BitgetGet("/api/spot/v1/wallet/withdrawal-list", queryString);
                _project.Json.FromString(response);

                var historyList = new List<string>();

                if (_project.Json.code == "00000")
                {
                    foreach (var item in _project.Json.data)
                    {
                        string orderId = item.orderId;
                        string coin = item.coin;
                        string amount = item.amount;
                        string status = item.status;
                        string address = item.toAddress;
                        string chain = item.chain;

                        historyList.Add($"{orderId}:{coin}:{amount}:{status}:{address}:{chain}");
                        _logger.Send($"Withdrawal: {orderId} - {amount} {coin} to {address} [{status}]");
                    }
                }
                else
                {
                    _logger.Send($"Error getting withdrawal history: {_project.Json.msg}");
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

        public List<string> GetSupportedCoins()
        {
            try
            {
                string response = BitgetGet("/api/spot/v1/public/currencies");
                _project.Json.FromString(response);

                var coinsList = new List<string>();

                if (_project.Json.code == "00000")
                {
                    foreach (var item in _project.Json.data)
                    {
                        string coinName = item.coinName;
                        string chains = "";

                        if (item.chains != null)
                        {
                            foreach (var chain in item.chains)
                            {
                                chains += chain.chain + ";";
                            }
                        }

                        coinsList.Add($"{coinName}:{chains.TrimEnd(';')}");
                        _logger.Send($"Supported coin: {coinName} - Chains: {chains}");
                    }
                }
                else
                {
                    _logger.Send($"Error getting supported coins: {_project.Json.msg}");
                }

                return coinsList;
            }
            catch (Exception ex)
            {
                _logger.Send($"Exception in GetSupportedCoins: {ex.Message}");
                return new List<string>();
            }
        }

        // Get current market price for a trading pair
        public T GetPrice<T>(string symbol)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                string queryString = $"symbol={symbol}";
                string response = BitgetGet("/api/spot/v1/market/ticker", queryString);
                _project.Json.FromString(response);

                if (_project.Json.code == "00000")
                {
                    string lastPrice = _project.Json.data.close;
                    decimal price = decimal.Parse(lastPrice, CultureInfo.InvariantCulture);

                    _logger.Send($"{symbol} price: {price}");

                    if (typeof(T) == typeof(string))
                        return (T)Convert.ChangeType(price.ToString("0.##################"), typeof(T));
                    return (T)Convert.ChangeType(price, typeof(T));
                }
                else
                {
                    throw new Exception($"Error getting price: {_project.Json.msg}");
                }
            }
            catch (Exception ex)
            {
                _logger.Send($"Exception in GetPrice: {ex.Message}");
                throw;
            }
        }

        // Get all sub-account spot assets
        public List<string> GetSubAccountsAssets()
        {
            try
            {
                string response = BitgetPost("/api/spot/v1/account/sub-account-spot-assets", new { });
                _project.Json.FromString(response);

                var subAccountsList = new List<string>();

                if (_project.Json.code == "00000")
                {
                    foreach (var account in _project.Json.data)
                    {
                        string userId = account.userId.ToString();

                        if (account.spotAssetsList != null)
                        {
                            foreach (var asset in account.spotAssetsList)
                            {
                                string coinName = asset.coinName;
                                string available = asset.available;
                                string frozen = asset.frozen;
                                string locked = asset.@lock;

                                // Only add if there are any assets
                                decimal totalBalance = decimal.Parse(available, CultureInfo.InvariantCulture) +
                                                     decimal.Parse(frozen, CultureInfo.InvariantCulture) +
                                                     decimal.Parse(locked, CultureInfo.InvariantCulture);

                                if (totalBalance > 0)
                                {
                                    subAccountsList.Add($"{userId}:{coinName}:{available}:{frozen}:{locked}");
                                    _logger.Send($"Sub-account {userId}: {coinName} - Available: {available}, Frozen: {frozen}, Locked: {locked}");
                                }
                            }
                        }
                    }
                }
                else
                {
                    _logger.Send($"Error getting sub-accounts assets: {_project.Json.msg}");
                }

                return subAccountsList;
            }
            catch (Exception ex)
            {
                _logger.Send($"Exception in GetSubAccountsAssets: {ex.Message}");
                return new List<string>();
            }
        }

        // Get account info (including sub-account verification)
        public Dictionary<string, object> GetAccountInfo()
        {
            try
            {
                string response = BitgetGet("/api/spot/v1/account/getInfo");
                _project.Json.FromString(response);

                var accountInfo = new Dictionary<string, object>();

                if (_project.Json.code == "00000")
                {
                    accountInfo["userId"] = _project.Json.data.user_id.ToString();
                    accountInfo["inviterId"] = _project.Json.data.inviter_id?.ToString() ?? "";
                    accountInfo["parentId"] = _project.Json.data.parentId?.ToString() ?? "";
                    accountInfo["isTrader"] = _project.Json.data.trader?.ToString() ?? "false";
                    accountInfo["isSpotTrader"] = _project.Json.data.isSpotTrader?.ToString() ?? "false";

                    // Parse authorities array
                    string authorities = "";
                    if (_project.Json.data.authorities != null)
                    {
                        foreach (var auth in _project.Json.data.authorities)
                        {
                            authorities += auth.ToString() + ";";
                        }
                        authorities = authorities.TrimEnd(';');
                    }
                    accountInfo["authorities"] = authorities;

                    _logger.Send($"Account Info - User ID: {accountInfo["userId"]}, Authorities: {authorities}");
                }
                else
                {
                    _logger.Send($"Error getting account info: {_project.Json.msg}");
                }

                return accountInfo;
            }
            catch (Exception ex)
            {
                _logger.Send($"Exception in GetAccountInfo: {ex.Message}");
                return new Dictionary<string, object>();
            }
        }

        // Transfer funds between accounts (main/sub transfers)
        public string SubTransfer(string fromUserId, string toUserId, string coin, string amount,
                                string fromType = "spot", string toType = "spot", string clientOid = null)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                if (string.IsNullOrEmpty(clientOid))
                {
                    clientOid = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                }

                var body = new
                {
                    fromType = fromType,
                    toType = toType,
                    amount = amount,
                    coin = coin,
                    clientOid = clientOid,
                    fromUserId = fromUserId,
                    toUserId = toUserId
                };

                string response = BitgetPost("/api/spot/v1/wallet/subTransfer", body);
                _project.Json.FromString(response);

                if (_project.Json.code == "00000")
                {
                    _logger.Send($"Transfer successful: {amount} {coin} from user {fromUserId} to user {toUserId}");
                    return "Success";
                }
                else
                {
                    string errorMsg = $"Transfer failed: {_project.Json.msg}";
                    _logger.Send(errorMsg);
                    throw new Exception(errorMsg);
                }
            }
            catch (Exception ex)
            {
                _logger.Send($"Exception in SubTransfer: {ex.Message}");
                throw;
            }
        }

        // Internal account transfer (within same account between different types)
        public string InternalTransfer(string coin, string amount, string fromType = "spot", string toType = "spot", string clientOid = null)
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                if (string.IsNullOrEmpty(clientOid))
                {
                    clientOid = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                }

                var body = new
                {
                    fromType = fromType,
                    toType = toType,
                    amount = amount,
                    coin = coin,
                    clientOid = clientOid
                };

                string response = BitgetPost("/api/spot/v1/wallet/transfer-v2", body);
                _project.Json.FromString(response);

                if (_project.Json.code == "00000")
                {
                    string transferId = _project.Json.data?.transferId?.ToString() ?? "N/A";
                    _logger.Send($"Internal transfer successful: {amount} {coin} from {fromType} to {toType} - Transfer ID: {transferId}");
                    return transferId;
                }
                else
                {
                    string errorMsg = $"Internal transfer failed: {_project.Json.msg}";
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

        // Drain all sub-accounts to main account
        public void DrainSubAccounts()
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                _logger.Send("Starting sub-accounts drain process...");

                // Get main account info
                var accountInfo = GetAccountInfo();
                string mainUserId = accountInfo.ContainsKey("userId") ? accountInfo["userId"].ToString() : "";

                if (string.IsNullOrEmpty(mainUserId))
                {
                    throw new Exception("Could not determine main account user ID");
                }

                var subAssets = GetSubAccountsAssets();

                if (subAssets.Count == 0)
                {
                    _logger.Send("No sub-account assets found to transfer");
                    return;
                }

                int transferCount = 0;
                foreach (string assetInfo in subAssets)
                {
                    var parts = assetInfo.Split(':');
                    if (parts.Length >= 5)
                    {
                        string subUserId = parts[0];
                        string coinName = parts[1];
                        string available = parts[2];

                        // Skip if this is the main account
                        if (subUserId == mainUserId) continue;

                        decimal availableAmount = decimal.Parse(available, CultureInfo.InvariantCulture);

                        if (availableAmount > 0)
                        {
                            try
                            {
                                _logger.Send($"Transferring {availableAmount} {coinName} from sub-account {subUserId} to main account {mainUserId}");

                                SubTransfer(subUserId, mainUserId, coinName, available, "spot", "spot");
                                transferCount++;

                                // Add delay to avoid rate limits
                                Thread.Sleep(1000);
                            }
                            catch (Exception transferEx)
                            {
                                _logger.Send($"Failed to transfer {availableAmount} {coinName} from sub-account {subUserId}: {transferEx.Message}");
                            }
                        }
                    }
                }

                _logger.Send($"Sub-accounts drain completed. {transferCount} transfers executed.");
            }
            catch (Exception ex)
            {
                _logger.Send($"Exception in DrainSubAccounts: {ex.Message}");
                throw;
            }
        }

        // Get deposit address for a specific coin and chain
        public string GetDepositAddress(string coin, string chain)
        {
            try
            {
                string network = MapNetwork(chain);
                string queryString = $"coin={coin}&chain={network}";
                string response = BitgetGet("/api/spot/v1/wallet/deposit-address", queryString);
                _project.Json.FromString(response);

                if (_project.Json.code == "00000")
                {
                    string address = _project.Json.data.address;
                    string tag = _project.Json.data.tag?.ToString() ?? "";

                    _logger.Send($"Deposit address for {coin} on {network}: {address}" + (string.IsNullOrEmpty(tag) ? "" : $" (Tag: {tag})"));

                    return !string.IsNullOrEmpty(tag) ? $"{address}:{tag}" : address;
                }
                else
                {
                    string errorMsg = $"Error getting deposit address: {_project.Json.msg}";
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

        // Get transfer records for analysis
        public List<string> GetTransferHistory(string coinId, string fromType, string after, string before, int limit = 100)
        {
            try
            {
                string queryString = $"coinId={coinId}&fromType={fromType}&after={after}&before={before}&limit={limit}";
                string response = BitgetGet("/api/spot/v1/account/transferRecords", queryString);
                _project.Json.FromString(response);

                var transferList = new List<string>();

                if (_project.Json.code == "00000")
                {
                    foreach (var transfer in _project.Json.data)
                    {
                        string coinName = transfer.coinName;
                        string status = transfer.status;
                        string toType = transfer.toType;
                        string fromTypeResp = transfer.fromType;
                        string amount = transfer.amount;
                        string tradeTime = transfer.tradeTime;
                        string transferId = transfer.transferId;

                        transferList.Add($"{transferId}:{coinName}:{amount}:{fromTypeResp}:{toType}:{status}:{tradeTime}");
                        _logger.Send($"Transfer: {transferId} - {amount} {coinName} from {fromTypeResp} to {toType} [{status}]");
                    }
                }
                else
                {
                    _logger.Send($"Error getting transfer history: {_project.Json.msg}");
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