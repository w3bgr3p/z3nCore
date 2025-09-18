
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Signer;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Newtonsoft.Json;

namespace z3nCore
{
    public class W3b_obsolete
    {
        protected readonly IZennoPosterProjectModel _project;
        protected readonly Logger _logger;

        public W3b_obsolete(IZennoPosterProjectModel project, bool log = false, string key = null)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            _project = project;
            _logger = new Logger(project, log: log, classEmoji: "💠");
        }
        
        public string SendLegacy(string chainRpc, string contractAddress, string encodedData, decimal value, string walletKey, int speedup = 1)
        {

            return SendTx(chainRpc, contractAddress, encodedData, value, walletKey,0, speedup);

        }
        public string Send1559(string chainRpc, string contractAddress, string encodedData, decimal value, string walletKey, int speedup = 1)
        {
            return SendTx(chainRpc, contractAddress, encodedData, value, walletKey, 2, speedup);
        }
        public string SendTx(string chainRpc, string contractAddress, string encodedData, decimal value, string walletKey, int txType = 2, int speedup = 1)
        {
            if (string.IsNullOrEmpty(chainRpc))
                throw new ArgumentException("Chain RPC is null or empty");

            if (string.IsNullOrEmpty(walletKey))
                throw new ArgumentException("Wallet key is null or empty");

            var web3 = new Web3(chainRpc);
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

            BigInteger _value = (BigInteger)(value * 1000000000000000000m);
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
                }
                else
                {
                    priorityFee = baseGasPrice / 100 * speedup + gasPriceTask.Result.Value;
                    maxFeePerGas = baseGasPrice / 100 * speedup + gasPriceTask.Result.Value;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to estimate gas price: {ex.Message}, InnerException: {ex.InnerException?.Message}", ex);
            }

            try
            {
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
            }
            catch (AggregateException ae)
            {
                if (ae.InnerException is Nethereum.JsonRpc.Client.RpcResponseException rpcEx)
                {
                    var error = $"Code: {rpcEx.RpcError.Code}, Message: {rpcEx.RpcError.Message}, Data: {rpcEx.RpcError.Data}";
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
                    ? blockchain.SendTransaction(contractAddress, value, encodedData, gasLimit, gasPrice).Result
                    : blockchain.SendTransactionEIP1559(contractAddress, value, encodedData, gasLimit, maxFeePerGas, priorityFee).Result;
                return hash;
            }
            catch (AggregateException ae)
            {
                throw new Exception($"Transaction send failed: {ae.Message}, InnerException: {ae.InnerException?.Message}", ae);
            }
            catch (Exception ex)
            {
                throw new Exception($"Transaction send failed: {ex.Message}, InnerException: {ex.InnerException?.Message}", ex);
            }
        }

        public string TxFromHex(string chainRpc, string contractAddress, string encodedData, string value, string walletKey, int txType = 2, int speedup = 1)
        {
            if (string.IsNullOrEmpty(chainRpc))
                throw new ArgumentException("Chain RPC is null or empty");

            if (string.IsNullOrEmpty(walletKey))
                throw new ArgumentException("Wallet key is null or empty");

            var web3 = new Web3(chainRpc);
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

            string fromAddress;
            try
            {
                var ethECKey = new EthECKey(walletKey);
                fromAddress = ethECKey.GetPublicAddress();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to initialize EthECKey: length={walletKey.Length}, startsWith={walletKey.Substring(0, Math.Min(6, walletKey.Length))}..., Message={ex.Message}, InnerException={ex.InnerException?.Message}", ex);
            }
            HexBigInteger HexValue = new HexBigInteger(value);

            var gasParamsTask = new Blockchain(walletKey, chainId, chainRpc).EstimateGasAsync(contractAddress, encodedData, value, txType, speedup, web3, fromAddress);
            gasParamsTask.Wait();
            var (gasLimit, gasPrice, maxFeePerGas, priorityFee) = gasParamsTask.Result;

            try
            {
                var blockchain = new Blockchain(walletKey, chainId, chainRpc);
                string hash = txType == 0
                    ? blockchain.SendTransaction(contractAddress, HexValue, encodedData, gasLimit, gasPrice).Result
                    : blockchain.SendTransactionEIP1559(contractAddress, HexValue, encodedData, gasLimit, maxFeePerGas, priorityFee).Result;
                return hash;
            }
            catch (AggregateException ae)
            {
                throw new Exception($"Transaction send failed: {ae.Message}, InnerException: {ae.InnerException?.Message}", ae);
            }
            catch (Exception ex)
            {
                throw new Exception($"Transaction send failed: {ex.Message}, InnerException: {ex.InnerException?.Message}", ex);
            }
        }

        public string Tx(string chainRpc, string contractAddress, string encodedData, string hexValue, string walletKey, int txType = 2, int speedup = 1)
        {
            if (string.IsNullOrEmpty(chainRpc))
                throw new ArgumentException("Chain RPC is null or empty");

            if (string.IsNullOrEmpty(walletKey))
                throw new ArgumentException("Wallet key is null or empty");

            var web3 = new Web3(chainRpc);
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

            string fromAddress;
            try
            {
                var ethECKey = new EthECKey(walletKey);
                fromAddress = ethECKey.GetPublicAddress();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to initialize EthECKey: length={walletKey.Length}, startsWith={walletKey.Substring(0, Math.Min(6, walletKey.Length))}..., Message={ex.Message}, InnerException={ex.InnerException?.Message}", ex);
            }
            HexBigInteger HexValue = new HexBigInteger(hexValue);

            var gasParamsTask = new Blockchain(walletKey, chainId, chainRpc).EstimateGasAsync(contractAddress, encodedData, hexValue, txType, speedup, web3, fromAddress);
            gasParamsTask.Wait();
            var (gasLimit, gasPrice, maxFeePerGas, priorityFee) = gasParamsTask.Result;

            try
            {
                var blockchain = new Blockchain(walletKey, chainId, chainRpc);
                string hash = txType == 0
                    ? blockchain.SendTransaction(contractAddress, HexValue, encodedData, gasLimit, gasPrice).Result
                    : blockchain.SendTransactionEIP1559(contractAddress, HexValue, encodedData, gasLimit, maxFeePerGas, priorityFee).Result;
                return hash;
            }
            catch (AggregateException ae)
            {
                throw new Exception($"Transaction send failed: {ae.Message}, InnerException: {ae.InnerException?.Message}", ae);
            }
            catch (Exception ex)
            {
                throw new Exception($"Transaction send failed: {ex.Message}, InnerException: {ex.InnerException?.Message}", ex);
            }
        }
        public string Tx(string chainRpc, string transactionData, string walletKey, int txType = 2, int speedup = 1)
        {
            if (string.IsNullOrEmpty(chainRpc))
                throw new ArgumentException("Chain RPC is null or empty");

            if (string.IsNullOrEmpty(walletKey))
                throw new ArgumentException("Wallet key is null or empty");

            var transaction = JsonConvert.DeserializeObject<Dictionary<string, string>>(transactionData);
            if (transaction == null || !transaction.ContainsKey("to") || !transaction.ContainsKey("from"))
                throw new ArgumentException("Invalid transaction data");

            string contractAddress = transaction["to"];
            string hexValue = transaction.ContainsKey("value") ? transaction["value"] : "0x0";


            string encodedData = transaction.ContainsKey("data") ? transaction["data"] : null;


            return Tx(chainRpc, contractAddress, encodedData, hexValue, walletKey, txType, speedup);
        }
    
    
    }


}
