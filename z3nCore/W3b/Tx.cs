using NBitcoin;
using System;
using System.Globalization;
using System.Numerics;
using System.Threading;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Nethereum.Web3;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;

namespace z3nCore
{
    public class Tx
    {
        protected readonly IZennoPosterProjectModel _project;
        protected readonly Logger _logger;
        public Tx(IZennoPosterProjectModel project,  bool log = false)   
        {
            _project = project;
            _logger = new Logger(project, log: log, classEmoji: "💠");
        }
        public string SendTx(string chainRpc, string contractAddress, string encodedData, decimal value, string walletKey, int txType = 2, int speedup = 1)
        {
            if (string.IsNullOrEmpty(chainRpc))
                throw new ArgumentException("Chain RPC is null or empty");

            if (string.IsNullOrEmpty(walletKey))
                walletKey = _project.DbKey("evm");            
            
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
        public string Approve(string contract, string spender, string amount, string rpc)
        {
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
                txHash = SendTx(rpc, contract, encoded, 0, key, 0, 3);        
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

            _logger.Send($"[APPROVE] {contract} for spender {spender} with amount {amount}...");
            return txHash;
        }
        public string Wrap(string contract, decimal value, string rpc )
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            string key = _project.DbKey("evm");

            string abi = @"[{""inputs"":[],""name"":""deposit"",""outputs"":[],""stateMutability"":""payable"",""type"":""function""}]";

            string txHash = null;

            string[] types = { };
            object[] values = { };
            string encoded = Encoder.EncodeTransactionData(abi, "deposit", types, values);


            try
            {
                txHash = SendTx(rpc, contract, encoded, value, key, 0, 3);

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
        public string SendNative(string to, decimal amount, string rpc)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            string key = _project.DbKey("evm");
            string txHash = null;
            string encoded = "";
            try
            {
                txHash = SendTx(rpc, to, encoded, amount, key, 0, 3);
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
        public string SendERC20(string contract, string to, decimal amount, string rpc)
        {
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
                txHash = SendTx(rpc, contract, encoded, 0, key, 0, 3);      
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
        public string SendERC721(string contract, string to, BigInteger tokenId, string rpc)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            string txHash = null;
            string key = _project.DbKey("evm");
            try
            {
                string abi = @"[{""inputs"":[{""name"":""from"",""type"":""address""},{""name"":""to"",""type"":""address""},{""name"":""tokenId"",""type"":""uint256""}],""name"":""safeTransferFrom"",""outputs"":[],""stateMutability"":""nonpayable"",""type"":""function""}]";
                string[] types = { "address", "address", "uint256" };
                object[] values = { key.ToPubEvm(), to, tokenId };
                string encoded = z3nCore.Encoder.EncodeTransactionData(abi, "safeTransferFrom", types, values);
                

                txHash = SendTx(rpc, contract, encoded, 0, key, 0, 3);
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
    }
}
