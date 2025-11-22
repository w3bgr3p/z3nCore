# Blockchain

Core class for interacting with EVM-compatible blockchains using Nethereum.

---

## Blockchain (Constructor)

### Purpose
Initializes a new Blockchain instance with wallet credentials and RPC endpoint.

### Example
```csharp
string privateKey = "0xabc123...";
int chainId = 1; // Ethereum mainnet
string rpc = "https://eth.llamarpc.com";
var blockchain = new Blockchain(privateKey, chainId, rpc);
```

### Breakdown
```csharp
public Blockchain(
    string walletKey,    // Private key for signing transactions
    int chainId,         // Chain ID (1 for Ethereum, 56 for BSC, etc.)
    string jsonRpc       // RPC endpoint URL
)
// Initializes blockchain connection with wallet credentials
```

---

## GetAddressFromPrivateKey

### Purpose
Derives the Ethereum address from a private key.

### Example
```csharp
var blockchain = new Blockchain();
string privateKey = "abc123..."; // Without 0x prefix
string address = blockchain.GetAddressFromPrivateKey(privateKey);
Console.WriteLine($"Address: {address}");
```

### Breakdown
```csharp
public string GetAddressFromPrivateKey(
    string privateKey    // Private key (with or without 0x prefix)
)
// Returns: Ethereum address (0x...)
// Automatically adds 0x prefix if missing
```

---

## GetBalance

### Purpose
Retrieves the native token balance (ETH, BNB, etc.) for the configured wallet.

### Example
```csharp
var blockchain = new Blockchain(privateKey, chainId, rpc);
string balance = await blockchain.GetBalance();
Console.WriteLine($"Balance: {balance} ETH");
```

### Breakdown
```csharp
public async Task<string> GetBalance()
// Returns: Balance as string in Ether units
// Requires: Blockchain instance initialized with walletKey
```

---

## ReadContract

### Purpose
Calls a read-only smart contract function and retrieves the result.

### Example
```csharp
var blockchain = new Blockchain(rpc);
string contractAddress = "0x123...";
string abi = "[{...}]"; // Contract ABI JSON
string result = await blockchain.ReadContract(
    contractAddress,
    "balanceOf",
    abi,
    "0xUserAddress"
);
```

### Breakdown
```csharp
public async Task<string> ReadContract(
    string contractAddress,    // Smart contract address
    string functionName,       // Function name to call
    string abi,               // Contract ABI in JSON format
    params object[] parameters // Function parameters
)
// Returns: Function result as string (formatted based on return type)
// Supports: BigInteger (hex), bool, string, byte[], tuples
```

---

## SendTransaction

### Purpose
Sends a legacy (Type 0) transaction to the blockchain.

### Example
```csharp
var blockchain = new Blockchain(privateKey, chainId, rpc);
string hash = await blockchain.SendTransaction(
    to: "0x123...",
    amount: 0.1m,  // Can be decimal, BigInteger, HexBigInteger, etc.
    data: "0x",
    gasLimit: new BigInteger(21000),
    gasPrice: new BigInteger(20000000000)
);
```

### Breakdown
```csharp
public async Task<string> SendTransaction(
    string addressTo,        // Recipient address
    object amount,          // Amount to send (supports multiple types)
    string data,            // Transaction data (0x for simple transfers)
    BigInteger gasLimit,    // Gas limit
    BigInteger gasPrice     // Gas price in wei
)
// Returns: Transaction hash
// Amount types: decimal (ETH), BigInteger (wei), HexBigInteger, int, long, double, float, string
```

---

## SendTransactionEIP1559

### Purpose
Sends an EIP-1559 (Type 2) transaction with dynamic fee structure.

### Example
```csharp
var blockchain = new Blockchain(privateKey, chainId, rpc);
string hash = await blockchain.SendTransactionEIP1559(
    to: "0x123...",
    amount: 0.1m,
    data: "0x",
    gasLimit: new BigInteger(21000),
    maxFeePerGas: new BigInteger(30000000000),
    maxPriorityFeePerGas: new BigInteger(2000000000)
);
```

### Breakdown
```csharp
public async Task<string> SendTransactionEIP1559(
    string addressTo,                  // Recipient address
    object amount,                     // Amount to send (supports multiple types)
    string data,                       // Transaction data
    BigInteger gasLimit,               // Gas limit
    BigInteger maxFeePerGas,          // Maximum fee per gas
    BigInteger maxPriorityFeePerGas   // Priority fee (tip) per gas
)
// Returns: Transaction hash
// Uses EIP-1559 fee market mechanism (Type 2 transaction)
```

---

## EstimateGasAsync

### Purpose
Estimates gas parameters (limit and price) for a transaction.

### Example
```csharp
var blockchain = new Blockchain(privateKey, chainId, rpc);
var web3 = new Web3(rpc);
var (gasLimit, gasPrice, maxFee, priority) = await blockchain.EstimateGasAsync(
    contractAddress: "0x123...",
    encodedData: "0xabc...",
    value: "0",
    txType: 0,
    speedup: 100,
    web3: web3,
    fromAddress: "0xYourAddress"
);
```

### Breakdown
```csharp
public async Task<(BigInteger GasLimit, BigInteger GasPrice, BigInteger MaxFeePerGas, BigInteger PriorityFee)> EstimateGasAsync(
    string contractAddress,    // Contract address to interact with
    string encodedData,       // Encoded transaction data
    string value,             // Value to send in wei
    int txType,              // 0 for legacy, 2 for EIP-1559
    int speedup,             // Speedup percentage (100 = normal, 110 = 10% faster)
    Web3 web3,               // Web3 instance
    string fromAddress       // Sender address
)
// Returns: Tuple with gas parameters
// Throws: Exception with detailed RPC error messages
```

---

## GenerateMnemonic

### Purpose
Generates a new BIP39 mnemonic phrase.

### Example
```csharp
string mnemonic = Blockchain.GenerateMnemonic("English", 12);
Console.WriteLine($"Mnemonic: {mnemonic}");
```

### Breakdown
```csharp
public static string GenerateMnemonic(
    string wordList = "English",    // Language: English, Japanese, ChineseSimplified, etc.
    int wordCount = 12             // Word count: 12, 15, 18, 21, or 24
)
// Returns: Space-separated mnemonic phrase
// Supported languages: English, Japanese, Chinese (Simplified/Traditional), Spanish, French, Portuguese, Czech
```

---

## MnemonicToAccountEth

### Purpose
Derives multiple Ethereum accounts from a mnemonic phrase.

### Example
```csharp
string mnemonic = "word1 word2 word3...";
var accounts = Blockchain.MnemonicToAccountEth(mnemonic, 5);
foreach (var account in accounts)
{
    Console.WriteLine($"Address: {account.Key}, PrivateKey: {account.Value}");
}
```

### Breakdown
```csharp
public static Dictionary<string, string> MnemonicToAccountEth(
    string words,    // BIP39 mnemonic phrase
    int amount      // Number of accounts to derive
)
// Returns: Dictionary<address, privateKey>
// Uses standard Ethereum derivation path: m/44'/60'/0'/0/i
```

---

## MnemonicToAccountBtc

### Purpose
Derives multiple Bitcoin accounts from a mnemonic phrase.

### Example
```csharp
string mnemonic = "word1 word2 word3...";
var accounts = Blockchain.MnemonicToAccountBtc(mnemonic, 5, "Bech32");
foreach (var account in accounts)
{
    Console.WriteLine($"Address: {account.Key}, PrivateKey: {account.Value}");
}
```

### Breakdown
```csharp
public static Dictionary<string, string> MnemonicToAccountBtc(
    string mnemonic,                    // BIP39 mnemonic phrase
    int amount,                         // Number of accounts to derive
    string walletType = "Bech32"       // Address type
)
// Returns: Dictionary<address, privateKey>
// Wallet types: "Bech32" (native SegWit), "P2PKH compress", "P2PKH uncompress", "P2SH"
// Uses derivation path: m/84'/0'/0'/0/i
```

---

## GetEthAccountBalance

### Purpose
Gets the ETH balance for any address (static utility method).

### Example
```csharp
string balance = Blockchain.GetEthAccountBalance(
    "0x123...",
    "https://eth.llamarpc.com"
);
Console.WriteLine($"Balance: {balance} wei");
```

### Breakdown
```csharp
public static string GetEthAccountBalance(
    string address,    // Address to check
    string jsonRpc    // RPC endpoint URL
)
// Returns: Balance in wei as string
// Note: Result is in wei, not ether
```

---

# Function Class

Helper class for working with smart contract ABIs.

## GetFuncInputTypes

### Purpose
Extracts input parameter types from a contract function.

### Example
```csharp
string abi = "[{...}]";
string[] types = Function.GetFuncInputTypes(abi, "transfer");
// Returns: ["address", "uint256"]
```

### Breakdown
```csharp
public static string[] GetFuncInputTypes(
    string abi,           // Contract ABI JSON
    string functionName   // Function name
)
// Returns: Array of Solidity type strings
```

---

## GetFuncInputParameters

### Purpose
Gets input parameter names and types as a dictionary.

### Example
```csharp
var params = Function.GetFuncInputParameters(abi, "transfer");
// Returns: {"to": "address", "amount": "uint256"}
```

### Breakdown
```csharp
public static Dictionary<string, string> GetFuncInputParameters(
    string abi,           // Contract ABI JSON
    string functionName   // Function name
)
// Returns: Dictionary<parameterName, type>
```

---

## GetFuncOutputParameters

### Purpose
Gets output parameter names and types.

### Example
```csharp
var outputs = Function.GetFuncOutputParameters(abi, "balanceOf");
// Returns: {"": "uint256"}
```

### Breakdown
```csharp
public static Dictionary<string, string> GetFuncOutputParameters(
    string abi,           // Contract ABI JSON
    string functionName   // Function name
)
// Returns: Dictionary<parameterName, type>
```

---

## GetFuncAddress

### Purpose
Gets the function selector (first 4 bytes of keccak256 hash).

### Example
```csharp
string selector = Function.GetFuncAddress(abi, "transfer");
// Returns: "0xa9059cbb"
```

### Breakdown
```csharp
public static string GetFuncAddress(
    string abi,           // Contract ABI JSON
    string functionName   // Function name
)
// Returns: Function selector (0x + 8 hex characters)
```

---

# Decoder Class

Decodes smart contract data.

## AbiDataDecode

### Purpose
Decodes transaction output data using ABI.

### Example
```csharp
var decoded = Decoder.AbiDataDecode(abi, "balanceOf", "0x0000000000000000000000000000000000000000000000000000000000000064");
// Returns: {"": "100"}
```

### Breakdown
```csharp
public static Dictionary<string, string> AbiDataDecode(
    string abi,           // Contract ABI JSON
    string functionName,  // Function name
    string data          // Hex encoded data (with or without 0x)
)
// Returns: Dictionary<parameterName, decodedValue>
// Supports: address, uint256, uint8, bool
```

---

# Encoder Class

Encodes smart contract transaction data.

## EncodeTransactionData

### Purpose
Encodes a complete function call with parameters.

### Example
```csharp
string[] types = {"address", "uint256"};
object[] values = {"0x123...", new BigInteger(100)};
string encoded = Encoder.EncodeTransactionData(abi, "transfer", types, values);
// Returns: "0xa9059cbb000000000000000000000000123...0000000000000064"
```

### Breakdown
```csharp
public static string EncodeTransactionData(
    string abi,           // Contract ABI JSON
    string functionName,  // Function name to call
    string[] types,       // Parameter types array
    object[] values      // Parameter values array
)
// Returns: Encoded transaction data (0x + selector + encoded params)
```

---

## EncodeParam

### Purpose
Encodes a single parameter.

### Example
```csharp
string encoded = Encoder.EncodeParam("uint256", new BigInteger(100));
// Returns: "0000000000000000000000000000000000000000000000000000000000000064"
```

### Breakdown
```csharp
public static string EncodeParam(
    string type,    // Solidity type
    object value   // Value to encode
)
// Returns: ABI-encoded parameter (hex, without 0x)
```

---

## EncodeParams

### Purpose
Encodes multiple parameters.

### Example
```csharp
string[] types = {"address", "uint256"};
object[] values = {"0x123...", new BigInteger(100)};
string encoded = Encoder.EncodeParams(types, values);
```

### Breakdown
```csharp
public static string EncodeParams(
    string[] types,    // Array of Solidity types
    object[] values   // Array of values
)
// Returns: Concatenated encoded parameters (hex, without 0x)
```

---

# Converter Class

Utility for converting values.

## ValuesToArray

### Purpose
Converts dynamic parameters to object array.

### Example
```csharp
object[] values = Converter.ValuesToArray("0x123...", 100, true);
// Can be used with EncodeParams
```

### Breakdown
```csharp
public static object[] ValuesToArray(
    params dynamic[] inputValues    // Variable number of dynamic values
)
// Returns: object[] array of values
// Useful for preparing parameters for encoding
```
