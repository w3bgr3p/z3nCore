using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Threading;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Nethereum.Web3;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;

namespace z3nCore
{
    public class Tx_
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;
        public Tx_(IZennoPosterProjectModel project,  bool log = false)   
        {
            _project = project;
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
            return BigInteger.Parse(result.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
        }
        public BigInteger ReadErc20Allowance(string tokenContract, string ownerAddress, string spenderAddress, string rpc)
        {
            string abi = @"[{""inputs"":[{""name"":""owner"",""type"":""address""},{""name"":""spender"",""type"":""address""}],""name"":""allowance"",""outputs"":[{""name"":"""",""type"":""uint256""}],""stateMutability"":""view"",""type"":""function""}]";
            var result = Read(tokenContract, "allowance", abi, rpc, ownerAddress, spenderAddress);
            return BigInteger.Parse(result.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
        }
        
        #endregion
        
        #region SEND
        public string SendTx(string chainRpc, string contractAddress, string encodedData, object value, string walletKey, int txType = 2, int speedup = 1, bool debug = false)
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
                
                var web3 = new Web3(chainRpc);
                report.AppendLine($"rpc: {chainRpc}");
                int chainId;
                
                try
                {
                    var chainIdTask = web3.Eth.ChainId.SendRequestAsync();
                    chainIdTask.Wait();
                    chainId = (int)chainIdTask.Result.Value;

                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to get chain ID: {ex.Message}, InnerException: {ex.InnerException?.Message}", ex);
                }
                report.AppendLine($"chainId: {chainId}");
                
                string fromAddress;
                try
                {
                    var ethECKey = new Nethereum.Signer.EthECKey(walletKey);
                    fromAddress = ethECKey.GetPublicAddress();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to initialize EthECKey: length={walletKey.Length}, startsWith={walletKey.Substring(0, Math.Min(6, walletKey.Length))}..., Message={ex.Message}, InnerException={ex.InnerException?.Message}", ex);
                }
                report.AppendLine($"from: {fromAddress}");
                
                BigInteger _value = ConvertValueToWei(value);
                report.AppendLine($"_value: {_value}");
                
                BigInteger gasLimit = 0;
                BigInteger gasPrice = 0;
                BigInteger maxFeePerGas = 0;
                BigInteger priorityFee = 0;
                
                try
                {
                    var gasPriceTask = web3.Eth.GasPrice.SendRequestAsync();
                    gasPriceTask.Wait();
                    BigInteger baseGasPrice = gasPriceTask.Result.Value / 100 + gasPriceTask.Result.Value;
                    if (txType == 0)
                    {
                        gasPrice = baseGasPrice / 100 * speedup + gasPriceTask.Result.Value;
                        report.AppendLine($"gasPrice: {gasPrice}");
                    }
                    else
                    {
                        priorityFee = baseGasPrice / 100 * speedup + gasPriceTask.Result.Value;
                        report.AppendLine($"priorityFee: {priorityFee}");
                        maxFeePerGas = baseGasPrice / 100 * speedup + gasPriceTask.Result.Value;
                        report.AppendLine($"maxFeePerGas: {maxFeePerGas}");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to estimate gas price: {ex.Message}, InnerException: {ex.InnerException?.Message}", ex);
                }
                
                try
                {
                    report.AppendLine($"data: {encodedData}");
                    report.AppendLine($"txType: {txType}");
                    var transactionInput = new TransactionInput
                    {
                        To = contractAddress,
                        From = fromAddress,
                        Data = encodedData,
                        Value = new HexBigInteger(_value),
                        GasPrice = txType == 0 ? new HexBigInteger(gasPrice) : null,
                        MaxPriorityFeePerGas = txType == 2 ? new HexBigInteger(priorityFee) : null,
                        MaxFeePerGas = txType == 2 ? new HexBigInteger(maxFeePerGas) : null,
                        Type = txType == 2 ? new HexBigInteger(2) : null
                    };

                    var gasEstimateTask = web3.Eth.Transactions.EstimateGas.SendRequestAsync(transactionInput);
                    gasEstimateTask.Wait();
                    var gasEstimate = gasEstimateTask.Result;
                    gasLimit = gasEstimate.Value + (gasEstimate.Value / 2);
                    report.AppendLine($"gasLimit: {gasLimit}");
                }
                catch (AggregateException ae)
                {
                    if (ae.InnerException is Nethereum.JsonRpc.Client.RpcResponseException rpcEx)
                    {
                        var error = $"Code: {rpcEx.RpcError.Code}, Message: {rpcEx.RpcError.Message}, Data: {rpcEx.RpcError.Data}";
                        report.AppendLine($"error: {error}");
                        throw new Exception($"RPC error during gas estimation: {error}, InnerException: {ae.InnerException?.Message}", ae);
                    }
                    throw new Exception($"Gas estimation failed: {ae.Message}, InnerException: {ae.InnerException?.Message}", ae);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Gas estimation failed: {ex.Message}, InnerException: {ex.InnerException?.Message}", ex);
                }
                
                try
                {
                    var blockchain = new Blockchain(walletKey, chainId, chainRpc);
                    string hash = txType == 0
                        ? blockchain.SendTransaction(contractAddress, _value, encodedData, gasLimit, gasPrice).Result
                        : blockchain.SendTransactionEIP1559(contractAddress, _value, encodedData, gasLimit, maxFeePerGas, priorityFee).Result;
                    report.AppendLine($"hash: {hash}");
                    return hash;
                }
                catch (AggregateException ae)
                {
                    string errorMsg = ae.InnerException?.Message ?? ae.Message;
                    throw new Exception(errorMsg, ae);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.InnerException?.Message ?? ex.Message, ex);
                }
            }
            catch //(Exception ex)
            {
                if (debug) 
                {
                    _project.warn(report.ToString());
                }
                throw;
            }
        }
        internal string SendTx(string chainRpc, string zerionJson, string walletKey = null, int txType = 2, int speedup = 1, bool debug = false)
        {
            var report = new StringBuilder();
            
            try
            {
                if (string.IsNullOrEmpty(chainRpc))
                    throw new ArgumentException("Chain RPC is null or empty");

                if (string.IsNullOrEmpty(zerionJson))
                    throw new ArgumentException("Transaction JSON is null or empty");
                
                // Parse JSON to extract transaction parameters
                dynamic txData;
                try
                {
                    txData = Newtonsoft.Json.JsonConvert.DeserializeObject(zerionJson);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Failed to parse transaction JSON: {ex.Message}", ex);
                }
                
                // Extract parameters from JSON
                string contractAddress = txData.to;
                string encodedData = txData.data;
                string valueHex = txData.value;
                string fromAddress = txData.from;
                
                // Convert hex value to BigInteger
                BigInteger _value = 0;
                if (!string.IsNullOrEmpty(valueHex) && valueHex != "0x0")
                {
                    _value = BigInteger.Parse(valueHex.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                }
                
                // If walletKey not provided, use default
                if (string.IsNullOrEmpty(walletKey))
                    walletKey = _project.DbKey("evm");
                
                if (string.IsNullOrEmpty(walletKey))
                    throw new ArgumentException("Wallet key is null or empty");
                
                var web3 = new Web3(chainRpc);
                report.AppendLine($"rpc: {chainRpc}");
                int chainId;
                
                try
                {
                    var chainIdTask = web3.Eth.ChainId.SendRequestAsync();
                    chainIdTask.Wait();
                    chainId = (int)chainIdTask.Result.Value;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to get chain ID: {ex.Message}, InnerException: {ex.InnerException?.Message}", ex);
                }
                report.AppendLine($"chainId: {chainId}");
                
                // Verify wallet address matches the from address in JSON
                string walletFromAddress;
                try
                {
                    var ethECKey = new Nethereum.Signer.EthECKey(walletKey);
                    walletFromAddress = ethECKey.GetPublicAddress();
                    
                    // Check if addresses match (case-insensitive)
                    if (!string.Equals(walletFromAddress, fromAddress, StringComparison.OrdinalIgnoreCase))
                    {
                        report.AppendLine($"Warning: Wallet address {walletFromAddress} does not match JSON from address {fromAddress}");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to initialize EthECKey: length={walletKey.Length}, startsWith={walletKey.Substring(0, Math.Min(6, walletKey.Length))}..., Message={ex.Message}, InnerException={ex.InnerException?.Message}", ex);
                }
                report.AppendLine($"from: {walletFromAddress}");
                report.AppendLine($"_value: {_value}");
                
                BigInteger gasLimit = 0;
                BigInteger gasPrice = 0;
                BigInteger maxFeePerGas = 0;
                BigInteger priorityFee = 0;
                
                try
                {
                    var gasPriceTask = web3.Eth.GasPrice.SendRequestAsync();
                    gasPriceTask.Wait();
                    BigInteger baseGasPrice = gasPriceTask.Result.Value / 100 + gasPriceTask.Result.Value;
                    if (txType == 0)
                    {
                        gasPrice = baseGasPrice / 100 * speedup + gasPriceTask.Result.Value;
                        report.AppendLine($"gasPrice: {gasPrice}");
                    }
                    else
                    {
                        priorityFee = baseGasPrice / 100 * speedup + gasPriceTask.Result.Value;
                        report.AppendLine($"priorityFee: {priorityFee}");
                        maxFeePerGas = baseGasPrice / 100 * speedup + gasPriceTask.Result.Value;
                        report.AppendLine($"maxFeePerGas: {maxFeePerGas}");
                    }
                }
                catch (Exception ex)
                {
                    report.AppendLine($"Failed to estimate gas price: {ex.Message}, InnerException: {ex.InnerException?.Message}");
                    throw new Exception($"Failed to estimate gas price: {ex.Message}, InnerException: {ex.InnerException?.Message}", ex);
                }
                
                try
                {
                    report.AppendLine($"data: {encodedData}");
                    report.AppendLine($"txType: {txType}");
                    var transactionInput = new TransactionInput
                    {
                        To = contractAddress,
                        From = walletFromAddress,
                        Data = encodedData,
                        Value = new HexBigInteger(_value),
                        GasPrice = txType == 0 ? new HexBigInteger(gasPrice) : null,
                        MaxPriorityFeePerGas = txType == 2 ? new HexBigInteger(priorityFee) : null,
                        MaxFeePerGas = txType == 2 ? new HexBigInteger(maxFeePerGas) : null,
                        Type = txType == 2 ? new HexBigInteger(2) : null
                    };

                    var gasEstimateTask = web3.Eth.Transactions.EstimateGas.SendRequestAsync(transactionInput);
                    gasEstimateTask.Wait();
                    var gasEstimate = gasEstimateTask.Result;
                    gasLimit = gasEstimate.Value + (gasEstimate.Value / 2);
                    report.AppendLine($"gasLimit: {gasLimit}");
                }
                catch (AggregateException ae)
                {
                    if (ae.InnerException is Nethereum.JsonRpc.Client.RpcResponseException rpcEx)
                    {
                        var error = $"Code: {rpcEx.RpcError.Code}, Message: {rpcEx.RpcError.Message}, Data: {rpcEx.RpcError.Data}";
                        report.AppendLine($"error: {error}");
                        throw new Exception($"RPC error during gas estimation: {error}, InnerException: {ae.InnerException?.Message}", ae);
                    }
                    throw new Exception($"Gas estimation failed: {ae.Message}, InnerException: {ae.InnerException?.Message}", ae);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Gas estimation failed: {ex.Message}, InnerException: {ex.InnerException?.Message}", ex);
                }

                try
                {
                    var blockchain = new Blockchain(walletKey, chainId, chainRpc);
                    string hash = txType == 0
                        ? blockchain.SendTransaction(contractAddress, _value, encodedData, gasLimit, gasPrice).Result
                        : blockchain.SendTransactionEIP1559(contractAddress, _value, encodedData, gasLimit, maxFeePerGas, priorityFee).Result;
                    report.AppendLine($"hash: {hash}");
                    return hash;
                }
                catch (AggregateException ae)
                {
                    string errorMsg = ae.InnerException?.Message ?? ae.Message;
                    throw new Exception(errorMsg, ae);
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.InnerException?.Message ?? ex.Message, ex);
                }
            }
            catch //(Exception ex)
            {
                if (debug) 
                {
                    _project.warn(report.ToString());
                }
                throw;
            }
        }       //FOR ZERION

        private BigInteger ConvertValueToWei(object value)
        {
            // Уже готовое значение в Wei
            if (value is BigInteger bigInt)
                return bigInt;
    
            if (value is HexBigInteger hexBigInt)
                return hexBigInt.Value;
    
            // Строка - пытаемся распарсить как hex (с или без 0x)
            if (value is string strValue)
            {
                if (string.IsNullOrWhiteSpace(strValue))
                    throw new ArgumentException("Value string is null or empty");
        
                // Убираем 0x если есть
                string hexValue = strValue.StartsWith("0x") || strValue.StartsWith("0X") 
                    ? strValue.Substring(2) 
                    : strValue;
        
                try
                {
                    return BigInteger.Parse(hexValue, NumberStyles.AllowHexSpecifier);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Невозможно распарсить строку '{strValue}' как hex значение: {ex.Message}", ex);
                }
            }
    
            // Числовые типы - конвертируем в Wei (умножаем на 10^18)
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
    
            throw new ArgumentException(
                $"Невозможно конвертировать value типа '{value?.GetType().Name ?? "null"}' в Wei. " +
                "Поддерживаемые типы: decimal, int, long, double, float, BigInteger, HexBigInteger, string (hex)"
            );
        }
        public string Approve(string contractAddress, string spender, string amount, string rpc, bool debug = false)
        {
            contractAddress = contractAddress.NormalizeAddress();
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            string key = _project.DbKey("evm");

            string abi = @"[{""inputs"":[{""name"":""spender"",""type"":""address""},{""name"":""amount"",""type"":""uint256""}],""name"":""approve"",""outputs"":[{""name"":"""",""type"":""bool""}],""stateMutability"":""nonpayable"",""type"":""function""}]";

            string txHash = null;

            string[] types = { "address", "uint256" };
            BigInteger amountValue;


            if (amount.ToLower() == "max")
            {
                amountValue = BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007913129639935"); // max uint256
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


            try
            {
                txHash = SendTx(rpc, contractAddress, encoded, 0, key, 0, 3, debug:debug);
                try
                {
                    _project.Variables["blockchainHash"].Value = txHash;
                }
                catch (Exception ex)
                {
                    _logger.Send($"!W:{ex.Message}");
                }

            }
            catch (Exception ex)
            {
                _logger.Send($"!W:{ex.Message}");
                throw;
            }

            _logger.Send($"[APPROVE] {contractAddress} for spender {spender} with amount {amount}...");
            return txHash;
        }
        public string Wrap(string contract, decimal value, string rpc , bool debug = false)
        {
            contract = contract.NormalizeAddress();
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            string key = _project.DbKey("evm");

            string abi = @"[{""inputs"":[],""name"":""deposit"",""outputs"":[],""stateMutability"":""payable"",""type"":""function""}]";

            string txHash = null;

            string[] types = { };
            object[] values = { };
            string encoded = Encoder.EncodeTransactionData(abi, "deposit", types, values);


            try
            {
                txHash = SendTx(rpc, contract, encoded, value, key, 0, 3, debug:debug);

                try
                {
                    _project.Variables["blockchainHash"].Value = txHash;
                }
                catch (Exception ex)
                {
                    _logger.Send($"!W:{ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.Send($"!W:{ex.Message}");
                throw;
            }

            _logger.Send($"[WRAP] {value} native to {contract}...");
            return txHash;
        }
        public string SendNative(string to, decimal amount, string rpc, bool debug = false)
        {
            
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            string key = _project.DbKey("evm");
            string txHash = null;
            string encoded = "";
            try
            {
                txHash = SendTx(rpc, to, encoded, amount, key, 0, 3, debug:debug);
                try
                {
                    _project.Variables["blockchainHash"].Value = txHash;
                }
                catch (Exception ex)
                {
                    _logger.Send($"!W:{ex.Message}",show:true);
                }
            }
            catch (Exception ex)
            {
                _logger.Send($"!W:{ex.Message}", show: true);
                throw;
            }
            _logger.Send($"sent [{amount}] to [{to}] by [{rpc}] [{txHash}]");

            return txHash;
        }
        public string SendErc20(string contract, string to, decimal amount, string rpc, bool debug = false)
        {
            contract = contract.NormalizeAddress();
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            string key = _project.DbKey("evm");
            string txHash = null;

            try
            {

                string abi = @"[{""inputs"":[{""name"":""to"",""type"":""address""},{""name"":""amount"",""type"":""uint256""}],""name"":""transfer"",""outputs"":[{""name"":"""",""type"":""bool""}],""stateMutability"":""nonpayable"",""type"":""function""}]";
                string[] types = { "address", "uint256" };
                decimal scaledAmount = amount * 1000000000000000000m;
                BigInteger amountValue = (BigInteger)Math.Floor(scaledAmount);
                object[] values = { to, amountValue };
                string encoded = z3nCore.Encoder.EncodeTransactionData(abi, "transfer", types, values);
                txHash = SendTx(rpc, contract, encoded, 0, key, 0, 3, debug:debug);
                try
                {
                    _project.Variables["blockchainHash"].Value = txHash;
                }
                catch (Exception ex)
                {
                    _logger.Send($"!W:{ex.Message}", show:true);
                }
            }
            catch (Exception ex)
            {
                _logger.Send($"!W:{ex.Message}", show:true);
                throw;
            }

            _logger.Send($"sent [{amount}] of [{contract}]  to [{to}] by [{rpc}] [{txHash}]");
            return txHash;
        }
        public string SendErc721(string contract, string to, BigInteger tokenId, string rpc, bool debug = false)
        {
            contract = contract.NormalizeAddress();
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            string txHash = null;
            string key = _project.DbKey("evm");
            try
            {
                string abi = @"[{""inputs"":[{""name"":""from"",""type"":""address""},{""name"":""to"",""type"":""address""},{""name"":""tokenId"",""type"":""uint256""}],""name"":""safeTransferFrom"",""outputs"":[],""stateMutability"":""nonpayable"",""type"":""function""}]";
                string[] types = { "address", "address", "uint256" };
                object[] values = { key.ToEvmAddress(), to, tokenId };
                string encoded = z3nCore.Encoder.EncodeTransactionData(abi, "safeTransferFrom", types, values);


                txHash = SendTx(rpc, contract, encoded, 0, key, 0, 3, debug:debug);
                try
                {
                    _project.Variables["blockchainHash"].Value = txHash;
                }
                catch (Exception ex)
                {
                    _logger.Send($"!W:{ex.Message}", show: true);
                }
            }
            catch (Exception ex)
            {
                _logger.Send($"!W:{ex.Message}", show: true);
                throw;
            }

            _logger.Send($"sent [{contract}/{tokenId}] to [{to}] by [{rpc}] [{txHash}]");
            return txHash;
        }
        
        #endregion

    }
    
}
