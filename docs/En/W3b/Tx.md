# Tx

High-level class for executing blockchain transactions with automatic gas estimation and error handling.

---

## Tx (Constructor)

### Purpose
Initializes a new transaction handler with logging support.

### Example
```csharp
var tx = new Tx(project, log: true);
```

### Breakdown
```csharp
public Tx(
    IZennoPosterProjectModel project,    // Project instance
    bool log = false                     // Enable logging
)
// Initializes transaction handler with logger
```

---

## Read

### Purpose
Calls a read-only contract function (view/pure functions).

### Example
```csharp
var tx = new Tx(project);
string abi = "[{...}]";
string result = tx.Read(
    contract: "0x123...",
    functionName: "balanceOf",
    abi: abi,
    rpc: "https://eth.llamarpc.com",
    parameters: new object[] { "0xUserAddress" }
);
```

### Breakdown
```csharp
public string Read(
    string contract,              // Contract address
    string functionName,          // Function name
    string abi,                   // Contract ABI JSON
    string rpc,                   // RPC endpoint URL
    params object[] parameters    // Function parameters
)
// Returns: Function result as string
// Does not require gas or signing
// Throws: Exception on RPC errors
```

---

## ReadErc20Balance

### Purpose
Reads ERC20 token balance (convenience method).

### Example
```csharp
var tx = new Tx(project);
BigInteger balance = tx.ReadErc20Balance(
    tokenContract: "0xdac17f958d2ee523a2206206994597c13d831ec7",
    ownerAddress: "0x123...",
    rpc: "https://eth.llamarpc.com"
);
```

### Breakdown
```csharp
public BigInteger ReadErc20Balance(
    string tokenContract,    // ERC20 token contract address
    string ownerAddress,     // Owner address
    string rpc              // RPC endpoint URL
)
// Returns: Balance as BigInteger (raw token units)
// Calls balanceOf(address) function
```

---

## ReadErc20Allowance

### Purpose
Reads ERC20 token allowance for a spender.

### Example
```csharp
var tx = new Tx(project);
BigInteger allowance = tx.ReadErc20Allowance(
    tokenContract: "0xdac17f958d2ee523a2206206994597c13d831ec7",
    ownerAddress: "0x123...",
    spenderAddress: "0x456...",
    rpc: "https://eth.llamarpc.com"
);
```

### Breakdown
```csharp
public BigInteger ReadErc20Allowance(
    string tokenContract,    // ERC20 token contract address
    string ownerAddress,     // Token owner address
    string spenderAddress,   // Spender address
    string rpc              // RPC endpoint URL
)
// Returns: Allowance as BigInteger
// Calls allowance(owner, spender) function
```

---

## SendTx

### Purpose
Sends a transaction with automatic gas estimation and signing.

### Example
```csharp
var tx = new Tx(project, log: true);
string hash = tx.SendTx(
    chainRpc: "https://eth.llamarpc.com",
    contractAddress: "0x123...",
    encodedData: "0xa9059cbb000...",
    value: 0,
    walletKey: null,  // Uses DbKey("evm") if null
    txType: 2,        // 0 = legacy, 2 = EIP-1559
    speedup: 100      // 100 = normal, 110 = 10% faster
);
```

### Breakdown
```csharp
public string SendTx(
    string chainRpc,           // RPC endpoint URL
    string contractAddress,    // Contract or recipient address
    string encodedData,        // Encoded transaction data (0x for native transfers)
    object value,             // Value to send (supports multiple types)
    string walletKey,         // Private key (null = use DbKey("evm"))
    int txType = 2,          // 0 = legacy, 2 = EIP-1559
    int speedup = 1,         // Gas price multiplier percentage
    bool debug = false       // Enable debug logging
)
// Returns: Transaction hash
// Automatically estimates gas limit and gas price
// Value types: string (hex), BigInteger, HexBigInteger, decimal, int, long, double, float
// Throws: Exception with detailed error messages
```

---

## Approve

### Purpose
Approves an ERC20 token allowance for a spender.

### Example
```csharp
var tx = new Tx(project, log: true);

// Approve specific amount
string hash1 = tx.Approve(
    contractAddress: "0xTokenContract",
    spender: "0xSpenderAddress",
    amount: "1000000000000000000", // 1 token with 18 decimals
    rpc: "https://eth.llamarpc.com"
);

// Approve maximum
string hash2 = tx.Approve("0xToken", "0xSpender", "max", rpc);

// Cancel approval
string hash3 = tx.Approve("0xToken", "0xSpender", "cancel", rpc);
```

### Breakdown
```csharp
public string Approve(
    string contractAddress,    // ERC20 token contract address
    string spender,           // Spender address
    string amount,            // Amount in raw units, or "max"/"cancel"
    string rpc,               // RPC endpoint URL
    bool debug = false        // Enable debug logging
)
// Returns: Transaction hash
// Special values: "max" = uint256.max, "cancel" = 0
// Sets: project.Variables["blockchainHash"]
```

---

## Wrap

### Purpose
Wraps native tokens (ETH -> WETH, BNB -> WBNB, etc.).

### Example
```csharp
var tx = new Tx(project, log: true);
string hash = tx.Wrap(
    contract: "0xC02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2", // WETH
    value: 0.1m, // 0.1 ETH
    rpc: "https://eth.llamarpc.com"
);
```

### Breakdown
```csharp
public string Wrap(
    string contract,     // Wrapped token contract (WETH, WBNB, etc.)
    decimal value,      // Amount of native tokens to wrap
    string rpc,         // RPC endpoint URL
    bool debug = false  // Enable debug logging
)
// Returns: Transaction hash
// Calls deposit() function on wrapped token contract
// Sets: project.Variables["blockchainHash"]
```

---

## SendNative

### Purpose
Sends native tokens (ETH, BNB, MATIC, etc.) to an address.

### Example
```csharp
var tx = new Tx(project, log: true);
string hash = tx.SendNative(
    to: "0x456...",
    amount: 0.01m,
    rpc: "https://eth.llamarpc.com"
);
```

### Breakdown
```csharp
public string SendNative(
    string to,           // Recipient address
    decimal amount,      // Amount in native tokens (e.g., ETH)
    string rpc,         // RPC endpoint URL
    bool debug = false  // Enable debug logging
)
// Returns: Transaction hash
// Simple value transfer with no data
// Sets: project.Variables["blockchainHash"]
```

---

## SendErc20

### Purpose
Sends ERC20 tokens to an address.

### Example
```csharp
var tx = new Tx(project, log: true);
string hash = tx.SendErc20(
    contract: "0xdac17f958d2ee523a2206206994597c13d831ec7", // USDT
    to: "0x456...",
    amount: 10.5m, // 10.5 tokens (will be converted to raw units)
    rpc: "https://eth.llamarpc.com"
);
```

### Breakdown
```csharp
public string SendErc20(
    string contract,     // ERC20 token contract address
    string to,          // Recipient address
    decimal amount,     // Amount in token units (not raw units)
    string rpc,         // RPC endpoint URL
    bool debug = false  // Enable debug logging
)
// Returns: Transaction hash
// Automatically converts amount to raw units (amount * 10^18)
// Calls transfer(address, uint256) function
// Sets: project.Variables["blockchainHash"]
```

---

## SendErc721

### Purpose
Sends an ERC721 NFT to an address.

### Example
```csharp
var tx = new Tx(project, log: true);
string hash = tx.SendErc721(
    contract: "0xbc4ca0eda7647a8ab7c2061c2e118a18a936f13d", // BAYC
    to: "0x456...",
    tokenId: new BigInteger(1234),
    rpc: "https://eth.llamarpc.com"
);
```

### Breakdown
```csharp
public string SendErc721(
    string contract,      // ERC721 contract address
    string to,           // Recipient address
    BigInteger tokenId,  // NFT token ID
    string rpc,          // RPC endpoint URL
    bool debug = false   // Enable debug logging
)
// Returns: Transaction hash
// Uses safeTransferFrom(from, to, tokenId)
// From address automatically determined from wallet key
// Sets: project.Variables["blockchainHash"]
```
