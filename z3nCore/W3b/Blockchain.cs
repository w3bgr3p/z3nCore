using NBitcoin;
using Nethereum.ABI;
using Nethereum.ABI.ABIDeserialisation;
using Nethereum.ABI.Decoders;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;






namespace z3nCore
{
    #region Nethereum
    public class Blockchain
    {
        public static object SyncObject = new object();

        public string walletKey;
        public int chainId;
        public string jsonRpc;

        public Blockchain(string walletKey, int chainId, string jsonRpc)
        {
            this.walletKey = walletKey;
            this.chainId = chainId;
            this.jsonRpc = jsonRpc;
        }

        public Blockchain(string jsonRpc) : this("", 0, jsonRpc)
        { }

        public Blockchain() { }

        public string GetAddressFromPrivateKey(string privateKey)
        {
            if (!privateKey.StartsWith("0x")) privateKey = "0x" + privateKey;
            var account = new Account(privateKey);
            return account.Address;
        }


        public async Task<string> GetBalance()
        {
            var account = new Account(walletKey, chainId);
            var web3 = new Web3(account, jsonRpc);
            var balance = await web3.Eth.GetBalance.SendRequestAsync(account.Address);
            var bnbAmount = Web3.Convert.FromWei(balance.Value);
            return bnbAmount.ToString();
        }

        public async Task<string> ReadContract(string contractAddress, string functionName, string abi, params object[] parameters)
        {
            var web3 = new Web3(jsonRpc);
            web3.TransactionManager.UseLegacyAsDefault = true;
            var contract = web3.Eth.GetContract(abi, contractAddress);
            var function = contract.GetFunction(functionName);
            var result = await function.CallAsync<object>(parameters);

            if (result is Tuple<BigInteger, BigInteger, BigInteger, BigInteger> structResult)
            {
                return $"0x{structResult.Item1.ToString("X")},{structResult.Item2.ToString("X")},{structResult.Item3.ToString("X")},{structResult.Item4.ToString("X")}";
            }

            if (result is BigInteger bigIntResult) return "0x" + bigIntResult.ToString("X");
            else if (result is bool boolResult) return boolResult.ToString().ToLower();
            else if (result is string stringResult) return stringResult;
            else if (result is byte[] byteArrayResult) return "0x" + BitConverter.ToString(byteArrayResult).Replace("-", "");
            else return result?.ToString() ?? "null";
        }


        public async Task<string> SendTransaction(string addressTo, decimal amount, string data, BigInteger gasLimit, BigInteger gasPrice)
        {
            var account = new Account(walletKey, chainId);
            var web3 = new Web3(account, jsonRpc);
            web3.TransactionManager.UseLegacyAsDefault = true;
            var transaction = new TransactionInput();
            transaction.From = account.Address;
            transaction.To = addressTo;
            transaction.Value = Web3.Convert.ToWei(amount).ToHexBigInteger();
            transaction.Data = data;
            transaction.Gas = new HexBigInteger(gasLimit);
            transaction.GasPrice = new HexBigInteger(gasPrice);
            var hash = await web3.TransactionManager.SendTransactionAsync(transaction);
            return hash;
        }

        public async Task<string> SendTransactionEIP1559(string addressTo, decimal amount, string data, BigInteger gasLimit, BigInteger maxFeePerGas, BigInteger maxPriorityFeePerGas)
        {
            var account = new Account(walletKey, chainId);
            var web3 = new Web3(account, jsonRpc);
            var transaction = new TransactionInput
            {
                From = account.Address,
                To = addressTo,
                Value = Web3.Convert.ToWei(amount).ToHexBigInteger(),
                Data = data,
                Gas = new HexBigInteger(gasLimit),
                MaxFeePerGas = new HexBigInteger(maxFeePerGas),
                MaxPriorityFeePerGas = new HexBigInteger(maxPriorityFeePerGas),
                Type = new HexBigInteger(2) // EIP-1559 транзакция
            };

            var hash = await web3.TransactionManager.SendTransactionAsync(transaction);
            return hash;
        }

        public async Task<string> SendTransaction(string addressTo, HexBigInteger amount, string data, BigInteger gasLimit, BigInteger gasPrice)
        {
            var account = new Account(walletKey, chainId);
            var web3 = new Web3(account, jsonRpc);
            web3.TransactionManager.UseLegacyAsDefault = true;
            var transaction = new TransactionInput();
            transaction.From = account.Address;
            transaction.To = addressTo;
            transaction.Value = amount;
            transaction.Data = data;
            transaction.Gas = new HexBigInteger(gasLimit);
            transaction.GasPrice = new HexBigInteger(gasPrice);
            var hash = await web3.TransactionManager.SendTransactionAsync(transaction);
            return hash;
        }

        public async Task<string> SendTransactionEIP1559(string addressTo, HexBigInteger amount, string data, BigInteger gasLimit, BigInteger maxFeePerGas, BigInteger maxPriorityFeePerGas)
        {
            var account = new Account(walletKey, chainId);
            var web3 = new Web3(account, jsonRpc);
            var transaction = new TransactionInput
            {
                From = account.Address,
                To = addressTo,
                Value = amount,
                Data = data,
                Gas = new HexBigInteger(gasLimit),
                MaxFeePerGas = new HexBigInteger(maxFeePerGas),
                MaxPriorityFeePerGas = new HexBigInteger(maxPriorityFeePerGas),
                Type = new HexBigInteger(2) // EIP-1559 транзакция
            };

            var hash = await web3.TransactionManager.SendTransactionAsync(transaction);
            return hash;
        }


        public async Task<(BigInteger GasLimit, BigInteger GasPrice, BigInteger MaxFeePerGas, BigInteger PriorityFee)> EstimateGasAsync(string contractAddress, string encodedData, string value, int txType, int speedup, Web3 web3, string fromAddress)
        {
            BigInteger gasLimit = 0;
            BigInteger gasPrice = 0;
            BigInteger maxFeePerGas = 0;
            BigInteger priorityFee = 0;

            try
            {
                var gasPriceTask = web3.Eth.GasPrice.SendRequestAsync();
                await gasPriceTask;
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
                    Value = new HexBigInteger(value),
                    GasPrice = txType == 0 ? new HexBigInteger(gasPrice) : null,
                    MaxPriorityFeePerGas = txType == 2 ? new HexBigInteger(priorityFee) : null,
                    MaxFeePerGas = txType == 2 ? new HexBigInteger(maxFeePerGas) : null,
                    Type = txType == 2 ? new HexBigInteger(2) : null
                };

                var gasEstimateTask = web3.Eth.Transactions.EstimateGas.SendRequestAsync(transactionInput);
                await gasEstimateTask;
                var gasEstimate = gasEstimateTask.Result;
                gasLimit = gasEstimate.Value + (gasEstimate.Value / 2);
            }
            catch (AggregateException ae)
            {
                if (ae.InnerException is RpcResponseException rpcEx)
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

            return (gasLimit, gasPrice, maxFeePerGas, priorityFee);
        }

        //btc
        public static string GenerateMnemonic(string wordList = "English", int wordCount = 12)
        {
            Wordlist _wordList;
            WordCount _wordCount;

            switch (wordList)
            {
                case "English":
                    _wordList = Wordlist.English;
                    break;

                case "Japanese":
                    _wordList = Wordlist.Japanese;
                    break;

                case "Chinese Simplified":
                    _wordList = Wordlist.ChineseSimplified;
                    break;

                case "Chinese Traditional":
                    _wordList = Wordlist.ChineseTraditional;
                    break;

                case "Spanish":
                    _wordList = Wordlist.Spanish;
                    break;

                case "French":
                    _wordList = Wordlist.French;
                    break;

                case "Portuguese":
                    _wordList = Wordlist.PortugueseBrazil;
                    break;

                case "Czech":
                    _wordList = Wordlist.Czech;
                    break;

                default:
                    _wordList = Wordlist.English;
                    break;
            }

            switch (wordCount)
            {
                case 12:
                    _wordCount = WordCount.Twelve;
                    break;

                case 15:
                    _wordCount = WordCount.Fifteen;
                    break;

                case 18:
                    _wordCount = WordCount.Eighteen;
                    break;

                case 21:
                    _wordCount = WordCount.TwentyOne;
                    break;

                case 24:
                    _wordCount = WordCount.TwentyFour;
                    break;

                default:
                    _wordCount = WordCount.Twelve;
                    break;
            }

            Mnemonic mnemo = new Mnemonic(_wordList, _wordCount);

            return mnemo.ToString();
        }

        public static Dictionary<string, string> MnemonicToAccountEth(string words, int amount)
        {
            string password = "";
            var accounts = new Dictionary<string, string>();

            var wallet = new Nethereum.HdWallet.Wallet(words, password);

            for (int i = 0; i < amount; i++)
            {
                var recoveredAccount = wallet.GetAccount(i);

                accounts.Add(recoveredAccount.Address, recoveredAccount.PrivateKey);
            }

            return accounts;
        }

        public static Dictionary<string, string> MnemonicToAccountBtc(string mnemonic, int amount, string walletType = "Bech32")
        {
            Func<string, string> GenerateAddress = null;

            switch (walletType)
            {
                case "P2PKH compress":
                    GenerateAddress = PrivateKeyToP2WPKHCompress;
                    break;

                case "P2PKH uncompress":
                    GenerateAddress = PrivateKeyToP2PKHUncompress;
                    break;

                case "P2SH":
                    GenerateAddress = PrivateKeyToP2SH;
                    break;

                case "Bech32":
                    GenerateAddress = PrivateKeyToBech32;
                    break;

                default:
                    GenerateAddress = PrivateKeyToBech32;
                    break;
            }

            var mnemo = new Mnemonic(mnemonic);
            var hdroot = mnemo.DeriveExtKey();
            string derive = "m/84'/0'/0'/0"; // m / purpose' / coin_type' / account' / change / address_index

            var addresses = new Dictionary<string, string>();

            for (int i = 0; i < amount; i++)
            {
                string keyPath = derive + "/" + i;
                var pKey = hdroot.Derive(new NBitcoin.KeyPath(keyPath));
                string privateKey = pKey.PrivateKey.ToHex();

                string address = GenerateAddress?.Invoke(privateKey);

                addresses.Add(address, privateKey);
            }

            return addresses;
        }

        private static string PrivateKeyToBech32(string privateKey)
        {
            var privKey = KeyConverter(privateKey, true);
            var wifCompressed = new BitcoinSecret(privKey, Network.Main);

            var bech32 = wifCompressed.PubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main);

            return bech32.ToString();
        }

        private static string PrivateKeyToP2WPKHCompress(string privateKey)
        {
            var privKey = KeyConverter(privateKey, true);
            var wifCompressed = new BitcoinSecret(privKey, Network.Main);

            var P2PKHCompressed = wifCompressed.PubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main);

            return P2PKHCompressed.ToString();
        }

        private static string PrivateKeyToP2PKHUncompress(string privateKey)
        {
            var privKey = KeyConverter(privateKey, false);
            var wifUncompressed = new BitcoinSecret(privKey, Network.Main);

            var P2PKHUncompressed = wifUncompressed.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main);

            return P2PKHUncompressed.ToString();
        }

        private static string PrivateKeyToP2SH(string privateKey)
        {
            var privKey = KeyConverter(privateKey, true);
            var wifUncompressed = new BitcoinSecret(privKey, Network.Main);

            var P2PKHUncompressed = wifUncompressed.PubKey.GetAddress(ScriptPubKeyType.SegwitP2SH, Network.Main);

            return P2PKHUncompressed.ToString();
        }

        private static Key KeyConverter(string privateKey, bool compress)
        {
            Key key;
            var byteKey = privateKey.HexToByteArray();

            if (compress)
            {
                key = new Key(byteKey);
            }

            else
            {
                key = new Key(byteKey, -1, false);
            }

            return key;
        }

        public static string GetEthAccountBalance(string address, string jsonRpc)
        {
            var web3 = new Web3(jsonRpc);

            var balance = web3.Eth.GetBalance.SendRequestAsync(address).Result;
            return balance.Value.ToString();
        }

    }
    public class Function
    {
        public static string[] GetFuncInputTypes(string abi, string functionName)
        {
            var deserialize = new ABIJsonDeserialiser();
            var abiFunctions = deserialize.DeserialiseContract(abi).Functions;
            int paramsAmount = abiFunctions.Where(n => n.Name == functionName).SelectMany(p => p.InputParameters, (n, p) => new { Type = p.Type }).Count();
            var inputTypes = abiFunctions.Where(n => n.Name == functionName).SelectMany(p => p.InputParameters, (n, p) => new { Type = p.Type });
            string[] types = new string[paramsAmount];
            var typesList = new List<string>();
            foreach (var item in inputTypes) typesList.Add(item.Type);
            types = typesList.ToArray();
            return types;
        }

        public static Dictionary<string, string> GetFuncInputParameters(string abi, string functionName)
        {
            var deserialize = new ABIJsonDeserialiser();
            var abiFunctions = deserialize.DeserialiseContract(abi).Functions;
            var parameters = abiFunctions.Where(n => n.Name == functionName).SelectMany(p => p.InputParameters, (n, p) => new { Name = p.Name, Type = p.Type });
            return parameters.ToDictionary(p => p.Name, p => p.Type);
        }

        public static Dictionary<string, string> GetFuncOutputParameters(string abi, string functionName)
        {
            var deserialize = new ABIJsonDeserialiser();
            var abiFunctions = deserialize.DeserialiseContract(abi).Functions;
            var parameters = abiFunctions.Where(n => n.Name == functionName).SelectMany(p => p.OutputParameters, (n, p) => new { Name = p.Name, Type = p.Type });
            return parameters.ToDictionary(p => p.Name, p => p.Type);
        }

        public static string GetFuncAddress(string abi, string functionName)
        {
            var deserialize = new ABIJsonDeserialiser();
            var abiFunctions = deserialize.DeserialiseContract(abi).Functions;
            var address = abiFunctions.Where(n => n.Name == functionName).Select(f => f.Sha3Signature).First();
            return address;
        }

    }
    public class Decoder
    {
        public static Dictionary<string, string> AbiDataDecode(string abi, string functionName, string data)
        {
            var decodedData = new Dictionary<string, string>();
            if (data.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) data = data.Substring(2);
            if (data.Length < 64) data = data.PadLeft(64, '0'); // Если данные короче 64 символов, дополняем их нулями слева
            List<string> dataChunks = SplitChunks(data).ToList();
            Dictionary<string, string> parametersList = Function.GetFuncOutputParameters(abi, functionName);
            for (int i = 0; i < parametersList.Count && i < dataChunks.Count; i++)
            {
                string key = parametersList.Keys.ElementAt(i);
                string type = parametersList.Values.ElementAt(i);
                string value = TypeDecode(type, dataChunks[i]);
                decodedData.Add(key, value);
            }
            return decodedData;
        }

        private static IEnumerable<string> SplitChunks(string data)
        {
            int chunkSize = 64;
            for (int i = 0; i < data.Length; i += chunkSize) yield return i + chunkSize <= data.Length ? data.Substring(i, chunkSize) : data.Substring(i).PadRight(chunkSize, '0');
        }

        private static string TypeDecode(string type, string dataChunk)
        {
            string decoded = string.Empty;

            var decoderAddr = new AddressTypeDecoder();
            var decoderBool = new BoolTypeDecoder();
            var decoderInt = new IntTypeDecoder();

            switch (type)
            {
                case "address":
                    decoded = decoderAddr.Decode<string>(dataChunk);
                    break;
                case "uint256":
                    decoded = decoderInt.DecodeBigInteger(dataChunk).ToString();
                    break;
                case "uint8":
                    decoded = decoderInt.Decode<int>(dataChunk).ToString();
                    break;
                case "bool":
                    decoded = decoderBool.Decode<bool>(dataChunk).ToString();
                    break;
                default: break;
            }
            return decoded;
        }
    }
    public class Encoder
    {
        public static string EncodeTransactionData(string abi, string functionName, string[] types, object[] values)
        {
            string funcAddress = Function.GetFuncAddress(abi, functionName);
            string encodedParams = EncodeParams(types, values);
            string encodedData = "0x" + funcAddress + encodedParams;
            return encodedData;
        }

        public static string EncodeParam(string type, object value)
        {
            var abiEncode = new ABIEncode();
            string result = abiEncode.GetABIEncoded(new ABIValue(type, value)).ToHex();
            return result;
        }

        public static string EncodeParams(string[] types, object[] values)
        {
            var abiEncode = new ABIEncode();
            var parameters = new ABIValue[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                parameters[i] = new ABIValue(types[i], values[i]);
            }
            return abiEncode.GetABIEncoded(parameters).ToHex();
        }

        public static string EncodeParams(Dictionary<string, string> parameters)
        {
            var abiEncode = new ABIEncode();
            string result = string.Empty;
            foreach (var item in parameters) result += abiEncode.GetABIEncoded(new ABIValue(item.Value, item.Key)).ToHex();
            return result;
        }
    }
    public class Converter
    {
        public static object[] ValuesToArray(params dynamic[] inputValues)
        {
            int valuesAmount = inputValues.Length;
            var valuesList = new List<object>();
            foreach (var item in inputValues) valuesList.Add(item);
            object[] values = new object[valuesAmount];
            values = valuesList.ToArray();
            return values;
        }
    }

    #endregion
}
