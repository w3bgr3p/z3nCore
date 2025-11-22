# EvmTools

Tools for interacting with EVM-compatible blockchains (Ethereum, BSC, Polygon, etc.).

---

## WaitTx

### Purpose
Waits for a transaction to be confirmed on the blockchain.

### Example
```csharp
var evmTools = new EvmTools();
string rpc = "https://eth.llamarpc.com";
string hash = "0xabc123...";
bool success = await evmTools.WaitTx(rpc, hash, deadline: 120, log: true);
if (success) Console.WriteLine("Transaction succeeded!");
```

### Breakdown
```csharp
public async Task<bool> WaitTx(
    string rpc,          // RPC endpoint URL
    string hash,         // Transaction hash
    int deadline = 60,   // Timeout in seconds
    string proxy = "",   // Optional proxy "user:pass:host:port"
    bool log = false     // Enable console logging
)
// Returns: true if transaction succeeded (status = 1), false if failed
// Throws: Exception on timeout
// Polls every 2-3 seconds until transaction is confirmed or timeout
```

---

## WaitTxExtended

### Purpose
Waits for transaction with detailed logging (shows pending state, gas info, nonce).

### Example
```csharp
var evmTools = new EvmTools();
bool success = await evmTools.WaitTxExtended(rpc, hash, deadline: 180, log: true);
```

### Breakdown
```csharp
public async Task<bool> WaitTxExtended(
    string rpc,          // RPC endpoint URL
    string hash,         // Transaction hash
    int deadline = 60,   // Timeout in seconds
    string proxy = "",   // Optional proxy
    bool log = false     // Enable detailed logging
)
// Returns: true if succeeded, false if failed
// Additional logging: shows pending status, gasLimit, gasPrice, nonce, value
// Useful for debugging stuck transactions
```

---

## Native

### Purpose
Gets the native token balance (ETH, BNB, MATIC, etc.) for an address.

### Example
```csharp
var evmTools = new EvmTools();
string hexBalance = await evmTools.Native(rpc, "0x123...");
BigInteger weiBalance = BigInteger.Parse(hexBalance, NumberStyles.AllowHexSpecifier);
```

### Breakdown
```csharp
public async Task<string> Native(
    string rpc,       // RPC endpoint URL
    string address    // Wallet address (0x...)
)
// Returns: Balance in hex format (without 0x prefix)
// To convert to decimal: Use BigInteger.Parse with AllowHexSpecifier
```

---

## Erc20

### Purpose
Gets the ERC20 token balance for an address.

### Example
```csharp
var evmTools = new EvmTools();
string tokenContract = "0xdac17f958d2ee523a2206206994597c13d831ec7"; // USDT
string hexBalance = await evmTools.Erc20(tokenContract, rpc, "0x123...");
```

### Breakdown
```csharp
public async Task<string> Erc20(
    string tokenContract,    // ERC20 token contract address
    string rpc,             // RPC endpoint URL
    string address          // Wallet address
)
// Returns: Balance in hex format (raw token units)
// Calls balanceOf(address) function on the token contract
```

---

## Erc721

### Purpose
Gets the number of ERC721 NFTs owned by an address.

### Example
```csharp
var evmTools = new EvmTools();
string nftContract = "0xbc4ca0eda7647a8ab7c2061c2e118a18a936f13d"; // BAYC
string hexCount = await evmTools.Erc721(nftContract, rpc, "0x123...");
```

### Breakdown
```csharp
public async Task<string> Erc721(
    string tokenContract,    // ERC721 NFT contract address
    string rpc,             // RPC endpoint URL
    string address          // Wallet address
)
// Returns: Number of NFTs owned (hex format)
// Calls balanceOf(address) for ERC721
```

---

## Erc1155

### Purpose
Gets the ERC1155 token balance for a specific token ID.

### Example
```csharp
var evmTools = new EvmTools();
string hexBalance = await evmTools.Erc1155(
    tokenContract: "0x123...",
    tokenId: "1",
    rpc: rpc,
    address: "0x456..."
);
```

### Breakdown
```csharp
public async Task<string> Erc1155(
    string tokenContract,    // ERC1155 contract address
    string tokenId,         // Token ID (decimal format)
    string rpc,             // RPC endpoint URL
    string address          // Wallet address
)
// Returns: Balance in hex format
// Calls balanceOf(address, tokenId)
```

---

## Nonce

### Purpose
Gets the transaction count (nonce) for an address.

### Example
```csharp
var evmTools = new EvmTools();
string nonceHex = await evmTools.Nonce(rpc, "0x123...", log: true);
int nonce = Convert.ToInt32(nonceHex, 16);
```

### Breakdown
```csharp
public async Task<string> Nonce(
    string rpc,          // RPC endpoint URL
    string address,      // Wallet address
    string proxy = "",   // Optional proxy
    bool log = false     // Enable logging
)
// Returns: Nonce in hex format (without 0x prefix)
// Uses eth_getTransactionCount with "latest" parameter
```

---

## ChainId

### Purpose
Gets the chain ID from an RPC endpoint.

### Example
```csharp
var evmTools = new EvmTools();
string chainIdHex = await evmTools.ChainId(rpc);
int chainId = Convert.ToInt32(chainIdHex.Replace("0x", ""), 16);
// 1 = Ethereum, 56 = BSC, 137 = Polygon, etc.
```

### Breakdown
```csharp
public async Task<string> ChainId(
    string rpc,          // RPC endpoint URL
    string proxy = "",   // Optional proxy
    bool log = false     // Enable logging
)
// Returns: Chain ID in hex format (with 0x prefix)
// Uses eth_chainId method
```

---

## GasPrice

### Purpose
Gets the current gas price from the network.

### Example
```csharp
var evmTools = new EvmTools();
string gasPriceHex = await evmTools.GasPrice(rpc);
BigInteger gasPrice = BigInteger.Parse(gasPriceHex, NumberStyles.AllowHexSpecifier);
```

### Breakdown
```csharp
public async Task<string> GasPrice(
    string rpc,          // RPC endpoint URL
    string proxy = "",   // Optional proxy
    bool log = false     // Enable logging
)
// Returns: Gas price in hex format (wei, without 0x prefix)
// Uses eth_gasPrice method
```
