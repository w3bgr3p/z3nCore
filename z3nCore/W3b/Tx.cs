using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Globalization;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace z3nCore
{
    /// <summary>
    /// Универсальный RPC клиент с двумя режимами работы:
    /// useNetHttp=true → прокси работают, async нативно
    /// useNetHttp=false → видно трафик в ZP UI
    /// </summary>
    public class RpcClient
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;
        private readonly bool _useNetHttp;
        private readonly string _rpcUrl;
        private readonly string _proxy;
        private int _requestId = 1;

        public RpcClient(
            IZennoPosterProjectModel project, 
            string rpcUrl,
            string proxy = "",
            bool useNetHttp = false,
            bool log = false)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _rpcUrl = rpcUrl ?? throw new ArgumentNullException(nameof(rpcUrl));
            _proxy = proxy;
            _useNetHttp = useNetHttp;
            _logger = new Logger(project, log: log, classEmoji: "🔗");
        }

        public async Task<T> CallAsync<T>(string method, params object[] parameters)
        {
            string json = BuildJsonRpc(method, parameters);
            _logger.Send($"[RPC] {method} → {_rpcUrl}");

            string response;

            if (_useNetHttp)
            {
                var http = new NetHttpAsync(_project, log: false);
                var headers = new Dictionary<string, string>();
                
                response = await http.PostAsync(
                    _rpcUrl, 
                    json, 
                    _proxy, 
                    headers, 
                    parse: false,
                    deadline: 30,
                    throwOnFail: false
                );
            }
            else
            {
                response = await Task.Run(() =>
                {
                    string[] headers = new[] { "Content-Type: application/json" };
                    return _project.POST(
                        _rpcUrl,
                        json,
                        _proxy,
                        headers,
                        cookies: null,
                        log: false,
                        parse: false,
                        deadline: 30,
                        thrw: false,
                        useNetHttp: false
                    );
                }).ConfigureAwait(false);
            }

            return ParseJsonRpcResponse<T>(response, method);
        }

        /// <summary>
        /// SYNC обёртка для CallAsync - используй в ZennoPoster контексте
        /// </summary>
        public T Call<T>(string method, params object[] parameters)
        {
            return Task.Run(async () =>
                await CallAsync<T>(method, parameters).ConfigureAwait(false)
            ).GetAwaiter().GetResult();
        }

        private string BuildJsonRpc(string method, object[] parameters)
        {
            var request = new
            {
                jsonrpc = "2.0",
                method = method,
                @params = parameters ?? new object[0],
                id = _requestId++
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(request);
        }

        /// <summary>
        /// Парсит JSON-RPC ответ с автоматической конвертацией hex → BigInteger/int
        /// Добавляет ведущий 0 к hex числам начинающимся с 8-F для корректного парсинга
        /// </summary>
        private T ParseJsonRpcResponse<T>(string response, string method)
        {
            if (string.IsNullOrEmpty(response))
                throw new Exception($"Empty response from RPC call: {method}");

            if (response.Contains("!!!"))
            {
                throw new Exception($"HTTP error in RPC call {method}: {response}");
            }

            try
            {
                dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(response);

                if (result.error != null)
                {
                    string errorMsg = result.error.message?.ToString() ?? "Unknown RPC error";
                    string errorCode = result.error.code?.ToString() ?? "";
                    throw new Exception($"RPC error [{errorCode}]: {errorMsg}");
                }

                if (result.result == null)
                {
                    throw new Exception($"No result in RPC response for {method}");
                }

                string resultStr = result.result.ToString();

                if (typeof(T) == typeof(string))
                {
                    return (T)(object)resultStr;
                }
                else if (typeof(T) == typeof(int))
                {
                    string hex = resultStr.Replace("0x", "").Replace("0X", "");
                    if (string.IsNullOrEmpty(hex)) hex = "0";
                    
                    if (hex.Length > 0 && "89ABCDEFabcdef".Contains(hex[0]))
                    {
                        hex = "0" + hex;
                    }
                    
                    return (T)(object)Convert.ToInt32(hex, 16);
                }
                else if (typeof(T) == typeof(BigInteger))
                {
                    string hex = resultStr.Replace("0x", "").Replace("0X", "");
                    if (string.IsNullOrEmpty(hex)) hex = "0";
                    
                    // Ведущий 0 если первый символ >= 8, чтобы не интерпретировалось как отрицательное
                    if (hex.Length > 0 && "89ABCDEFabcdef".Contains(hex[0]))
                    {
                        hex = "0" + hex;
                    }
                    
                    BigInteger value = BigInteger.Parse(hex, NumberStyles.AllowHexSpecifier);
                    return (T)(object)value;
                }
                else if (typeof(T) == typeof(HexBigInteger))
                {
                    return (T)(object)new HexBigInteger(resultStr);
                }
                else
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(result.result.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.Send($"!W Failed to parse RPC response for {method}: {ex.Message}");
                throw new Exception($"Failed to parse RPC response for {method}: {ex.Message}", ex);
            }
        }
    }
    
    /// <summary>
    /// Новый класс для blockchain транзакций через RpcClient
    /// useNetHttp=true → прокси работают эффективно
    /// useNetHttp=false → видно весь трафик в ZP UI (для дебага)
    /// </summary>
    public class Tx
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;
        private readonly bool _useNetHttp;

        public Tx(IZennoPosterProjectModel project, bool useNetHttp = false, bool log = false)
        {
            _project = project;
            _useNetHttp = useNetHttp;
            _logger = new Logger(project, log: log, classEmoji: "💠");
        }

        #region READ

        public string Read(string contract, string functionName, string abi, string rpc, params object[] parameters)
        {
            try
            {
                var blockchain = new Blockchain(rpc);
                var result = blockchain.ReadContract(contract, functionName, abi, parameters).Result;
                _logger.Send($"[READ] {functionName} from {contract}: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Send($"!W: Failed to read {functionName} from {contract}: {ex.Message}", show: true);
                throw;
            }
        }

        public BigInteger ReadErc20Balance(string tokenContract, string ownerAddress, string rpc)
        {
            string abi = @"[{""inputs"":[{""name"":""account"",""type"":""address""}],""name"":""balanceOf"",""outputs"":[{""name"":"""",""type"":""uint256""}],""stateMutability"":""view"",""type"":""function""}]";
            var result = Read(tokenContract, "balanceOf", abi, rpc, ownerAddress);
            return BigInteger.Parse(result.Replace("0x", ""), NumberStyles.HexNumber);
        }

        public BigInteger ReadErc20Allowance(string tokenContract, string ownerAddress, string spenderAddress, string rpc)
        {
            string abi = @"[{""inputs"":[{""name"":""owner"",""type"":""address""},{""name"":""spender"",""type"":""address""}],""name"":""allowance"",""outputs"":[{""name"":"""",""type"":""uint256""}],""stateMutability"":""view"",""type"":""function""}]";
            var result = Read(tokenContract, "allowance", abi, rpc, ownerAddress, spenderAddress);
            return BigInteger.Parse(result.Replace("0x", ""), NumberStyles.HexNumber);
        }

        #endregion

        #region SEND

        /// <summary>
        /// proxy: "+" → берёт из SQL, "login:pass@ip:port" → явно указанный
        /// txType: 0=Legacy, 2=EIP-1559
        /// speedup: множитель gas price (1-10, default 1)
        /// value: принимает decimal/int/BigInteger/HexBigInteger/string(hex)
        /// </summary>
        public string SendTx(
            string chainRpc,
            string contractAddress,
            string encodedData,
            object value,
            string walletKey = null,
            int txType = 2,
            int speedup = 1,
            bool debug = false,
            string proxy = "")
        {
            return Task.Run(async () =>
                await SendTxAsync(chainRpc, contractAddress, encodedData, value, walletKey, proxy, txType, speedup, debug)
                    .ConfigureAwait(false)
            ).GetAwaiter().GetResult();
        }

        private async Task<string> SendTxAsync(string chainRpc, string contractAddress, string encodedData, object value, string walletKey, string proxy, int txType, int speedup, bool debug)
        {
            var report = new StringBuilder();
            contractAddress = contractAddress.NormalizeAddress();

            try
            {
                if (string.IsNullOrEmpty(chainRpc))
                    throw new ArgumentException("Chain RPC is null or empty");

                if (string.IsNullOrEmpty(walletKey))
                    walletKey = _project.DbKey("evm");

                if (string.IsNullOrEmpty(walletKey))
                    throw new ArgumentException("Wallet key is null or empty");

                if (proxy == "+")
                    proxy = _project.SqlGet("proxy", "_instance");

                report.AppendLine($"rpc: {chainRpc}");
                report.AppendLine($"useNetHttp: {_useNetHttp}");
                report.AppendLine($"proxy: {proxy}");

                var rpc = new RpcClient(_project, chainRpc, proxy, _useNetHttp, log: false);

                int chainId = await rpc.CallAsync<int>("eth_chainId");
                report.AppendLine($"chainId: {chainId}");

                var ethECKey = new Nethereum.Signer.EthECKey(walletKey);
                string fromAddress = ethECKey.GetPublicAddress();
                report.AppendLine($"from: {fromAddress}");

                BigInteger _value = ConvertValueToWei(value);
                report.AppendLine($"_value: {_value}");

                BigInteger gasPrice = 0;
                BigInteger maxFeePerGas = 0;
                BigInteger priorityFee = 0;

                BigInteger baseGasPrice = await rpc.CallAsync<BigInteger>("eth_gasPrice");
                report.AppendLine($"baseGasPrice: {baseGasPrice}");

                BigInteger adjustedPrice = baseGasPrice + (baseGasPrice / 100);

                if (txType == 0)
                {
                    gasPrice = adjustedPrice + (adjustedPrice / 100 * speedup);
                    report.AppendLine($"gasPrice: {gasPrice}");
                }
                else
                {
                    priorityFee = adjustedPrice + (adjustedPrice / 100 * speedup);
                    maxFeePerGas = adjustedPrice + (adjustedPrice / 100 * speedup);
                    report.AppendLine($"priorityFee: {priorityFee}");
                    report.AppendLine($"maxFeePerGas: {maxFeePerGas}");
                }

                var estimateParams = new
                {
                    from = fromAddress,
                    to = contractAddress,
                    data = encodedData,
                    value = "0x" + _value.ToString("X")
                };

                BigInteger gasEstimate;
                try
                {
                    gasEstimate = await rpc.CallAsync<BigInteger>("eth_estimateGas", estimateParams);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Gas estimation failed: {ex.Message}", ex);
                }

                BigInteger gasLimit = gasEstimate + (gasEstimate / 2);
                report.AppendLine($"gasLimit: {gasLimit}");

                BigInteger nonce = await rpc.CallAsync<BigInteger>("eth_getTransactionCount", fromAddress, "pending");
                report.AppendLine($"nonce: {nonce}");

                string hash;
                if (txType == 0)
                {
                    var account = new Nethereum.Web3.Accounts.Account(walletKey, chainId);
                    var web3 = new Web3(account);
                    web3.TransactionManager.UseLegacyAsDefault = true;
                    
                    var transaction = new TransactionInput
                    {
                        From = account.Address,
                        To = contractAddress,
                        Value = new HexBigInteger(_value),
                        Data = encodedData,
                        Gas = new HexBigInteger(gasLimit),
                        GasPrice = new HexBigInteger(gasPrice),
                        Nonce = new HexBigInteger(nonce)
                    };
                    
                    var signedTx = await web3.TransactionManager.SignTransactionAsync(transaction);
                    
                    if (!signedTx.StartsWith("0x"))
                    {
                        signedTx = "0x" + signedTx;
                    }
                    
                    report.AppendLine($"signedTx: {signedTx.Substring(0, Math.Min(66, signedTx.Length))}...");
                    
                    hash = await rpc.CallAsync<string>("eth_sendRawTransaction", signedTx);
                }
                else
                {
                    var account = new Nethereum.Web3.Accounts.Account(walletKey, chainId);
                    var web3 = new Web3(account);
                    
                    var transaction = new TransactionInput
                    {
                        From = account.Address,
                        To = contractAddress,
                        Value = new HexBigInteger(_value),
                        Data = encodedData,
                        Gas = new HexBigInteger(gasLimit),
                        MaxFeePerGas = new HexBigInteger(maxFeePerGas),
                        MaxPriorityFeePerGas = new HexBigInteger(priorityFee),
                        Type = new HexBigInteger(2),
                        Nonce = new HexBigInteger(nonce)
                    };
                    
                    var signedTx = await web3.TransactionManager.SignTransactionAsync(transaction);
                    
                    if (!signedTx.StartsWith("0x"))
                    {
                        signedTx = "0x" + signedTx;
                    }
                    
                    report.AppendLine($"signedTx: {signedTx.Substring(0, Math.Min(66, signedTx.Length))}...");
                    
                    hash = await rpc.CallAsync<string>("eth_sendRawTransaction", signedTx);
                }

                report.AppendLine($"hash: {hash}");
                _logger.Send($"[TX] Sent: {hash}");

                try
                {
                    _project.Variables["blockchainHash"].Value = hash;
                }
                catch { }

                return hash;
            }
            catch (Exception ex)
            {
                if (debug)
                    _project.warn(report.ToString());

                _logger.Send($"!W: SendTx failed: {ex.Message}", show: true);
                throw;
            }
        }

        /// <summary>
        /// Конвертирует любой тип value в Wei (BigInteger)
        /// Поддерживает: decimal/int/long/double/float → умножает на 10^18
        /// BigInteger/HexBigInteger/string(hex) → используется как есть (уже в Wei)
        /// </summary>
        private BigInteger ConvertValueToWei(object value)
        {
            if (value is BigInteger bigInt)
                return bigInt;

            if (value is HexBigInteger hexBigInt)
                return hexBigInt.Value;

            if (value is string strValue)
            {
                if (string.IsNullOrWhiteSpace(strValue))
                    return BigInteger.Zero;

                string hexValue = strValue.StartsWith("0x") || strValue.StartsWith("0X")
                    ? strValue.Substring(2)
                    : strValue;

                try
                {
                    return BigInteger.Parse(hexValue, NumberStyles.AllowHexSpecifier);
                }
                catch
                {
                    if (decimal.TryParse(strValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedDecimal))
                        return (BigInteger)(parsedDecimal * 1000000000000000000m);

                    throw new ArgumentException($"Cannot parse string '{strValue}' as hex or decimal");
                }
            }

            if (value is decimal decValue)
                return (BigInteger)(decValue * 1000000000000000000m);

            if (value is int intValue)
                return (BigInteger)intValue * 1000000000000000000;

            if (value is long longValue)
                return (BigInteger)longValue * 1000000000000000000;

            if (value is double doubleValue)
                return (BigInteger)(doubleValue * 1000000000000000000.0);

            if (value is float floatValue)
                return (BigInteger)(floatValue * 1000000000000000000.0f);

            throw new ArgumentException($"Cannot convert value type '{value?.GetType().Name ?? "null"}' to Wei");
        }

        /// <summary>
        /// amount: "max" → uint256.max, "cancel" → 0, или BigInteger строка
        /// </summary>
        public string Approve(string contractAddress, string spender, string amount, string rpc, string proxy = "", bool debug = false)
        {
            contractAddress = contractAddress.NormalizeAddress();
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            string abi = @"[{""inputs"":[{""name"":""spender"",""type"":""address""},{""name"":""amount"",""type"":""uint256""}],""name"":""approve"",""outputs"":[{""name"":"""",""type"":""bool""}],""stateMutability"":""nonpayable"",""type"":""function""}]";

            string[] types = { "address", "uint256" };
            BigInteger amountValue;

            if (amount.ToLower() == "max")
            {
                amountValue = BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007913129639935");
            }
            else if (amount.ToLower() == "cancel")
            {
                amountValue = BigInteger.Zero;
            }
            else
            {
                try
                {
                    amountValue = BigInteger.Parse(amount);
                    if (amountValue < 0)
                        throw new ArgumentException("Amount cannot be negative");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to parse amount '{amount}': {ex.Message}");
                }
            }

            object[] values = { spender, amountValue };
            string encoded = Encoder.EncodeTransactionData(abi, "approve", types, values);

            string txHash = SendTx(rpc, contractAddress, encoded, 0, proxy: proxy, txType: 0, speedup: 3, debug: debug);

            _logger.Send($"[APPROVE] {contractAddress} for spender {spender} with amount {amount}");
            return txHash;
        }

        public string Wrap(string contract, decimal value, string rpc, string proxy = "", bool debug = false)
        {
            contract = contract.NormalizeAddress();
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            string abi = @"[{""inputs"":[],""name"":""deposit"",""outputs"":[],""stateMutability"":""payable"",""type"":""function""}]";

            string[] types = { };
            object[] values = { };
            string encoded = Encoder.EncodeTransactionData(abi, "deposit", types, values);

            string txHash = SendTx(rpc, contract, encoded, value, proxy: proxy, txType: 0, speedup: 3, debug: debug);

            _logger.Send($"[WRAP] {value} native to {contract}");
            return txHash;
        }

        public string SendNative(string to, decimal amount, string rpc, string proxy = "", bool debug = false)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            string encoded = "";

            string txHash = SendTx(rpc, to, encoded, amount, proxy: proxy, txType: 0, speedup: 3, debug: debug);

            _logger.Send($"[NATIVE] sent {amount} to {to}");
            return txHash;
        }

        public string SendErc20(string contract, string to, decimal amount, string rpc, string proxy = "", bool debug = false)
        {
            contract = contract.NormalizeAddress();
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            string abi = @"[{""inputs"":[{""name"":""to"",""type"":""address""},{""name"":""amount"",""type"":""uint256""}],""name"":""transfer"",""outputs"":[{""name"":"""",""type"":""bool""}],""stateMutability"":""nonpayable"",""type"":""function""}]";
            string[] types = { "address", "uint256" };
            decimal scaledAmount = amount * 1000000000000000000m;
            BigInteger amountValue = (BigInteger)Math.Floor(scaledAmount);
            object[] values = { to, amountValue };
            string encoded = Encoder.EncodeTransactionData(abi, "transfer", types, values);

            string txHash = SendTx(rpc, contract, encoded, 0, proxy: proxy, txType: 0, speedup: 3, debug: debug);

            _logger.Send($"[ERC20] sent {amount} of {contract} to {to}");
            return txHash;
        }

        public string SendErc721(string contract, string to, BigInteger tokenId, string rpc, string proxy = "", bool debug = false)
        {
            contract = contract.NormalizeAddress();
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            string key = _project.DbKey("evm");

            string abi = @"[{""inputs"":[{""name"":""from"",""type"":""address""},{""name"":""to"",""type"":""address""},{""name"":""tokenId"",""type"":""uint256""}],""name"":""safeTransferFrom"",""outputs"":[],""stateMutability"":""nonpayable"",""type"":""function""}]";
            string[] types = { "address", "address", "uint256" };
            object[] values = { key.ToEvmAddress(), to, tokenId };
            string encoded = Encoder.EncodeTransactionData(abi, "safeTransferFrom", types, values);

            string txHash = SendTx(rpc, contract, encoded, 0, proxy: proxy, txType: 0, speedup: 3, debug: debug);

            _logger.Send($"[ERC721] sent {contract}/{tokenId} to {to}");
            return txHash;
        }

        #endregion
    }
}