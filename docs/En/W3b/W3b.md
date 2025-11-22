# W3b

Utility classes and extension methods for cryptocurrency price checking and Web3 operations.

---

# CoinGecko API

## PriceById

### Purpose
Gets the current USD price of a cryptocurrency by CoinGecko ID.

### Example
```csharp
decimal ethPrice = CoinGecco.PriceById("ethereum");
decimal btcPrice = CoinGecco.PriceById("bitcoin");
decimal solPrice = CoinGecco.PriceById("solana");
Console.WriteLine($"ETH: ${ethPrice}");
```

### Breakdown
```csharp
public static decimal PriceById(
    string CGid = "ethereum",           // CoinGecko coin ID
    [CallerMemberName] string callerName = ""  // Auto-filled caller name for errors
)
// Returns: Current USD price as decimal
// Throws: Exception with caller context on errors
// Note: Uses free CoinGecko API
```

---

## PriceByTiker

### Purpose
Gets cryptocurrency price by ticker symbol (ETH, BNB, SOL).

### Example
```csharp
decimal ethPrice = CoinGecco.PriceByTiker("ETH");
decimal bnbPrice = CoinGecco.PriceByTiker("BNB");
decimal solPrice = CoinGecco.PriceByTiker("SOL");
```

### Breakdown
```csharp
public static decimal PriceByTiker(
    string tiker,                              // Ticker symbol (ETH, BNB, SOL)
    [CallerMemberName] string callerName = ""  // Auto-filled caller name
)
// Returns: Current USD price
// Supported tickers: ETH, BNB, SOL
// Throws: Exception for unsupported tickers
```

---

# KuCoin API

## KuPrice

### Purpose
Gets cryptocurrency price from KuCoin exchange.

### Example
```csharp
decimal ethPrice = KuCoin.KuPrice("ETH");
decimal btcPrice = KuCoin.KuPrice("BTC");
```

### Breakdown
```csharp
public static decimal KuPrice(
    string tiker = "ETH",                      // Ticker symbol
    [CallerMemberName] string callerName = ""  // Auto-filled caller name
)
// Returns: Current USD price from KuCoin orderbook
// Uses KuCoin public API (symbol-USDT pair)
```

---

# DexScreener API

## DSPrice

### Purpose
Gets token price from DexScreener (for DEX tokens).

### Example
```csharp
string solMint = "So11111111111111111111111111111111111111112"; // SOL
decimal price = W3bTools.DSPrice(solMint, "solana");
```

### Breakdown
```csharp
public static decimal DSPrice(
    string contract = "So11111111111111111111111111111111111111112",  // Token contract address
    string chain = "solana",                                         // Chain name
    [CallerMemberName] string callerName = ""                       // Auto-filled caller name
)
// Returns: Token price in native chain currency
// Useful for DEX tokens not listed on CEX
```

---

# W3bTools Extension Methods

## CGPrice

### Purpose
Gets CoinGecko price (static method).

### Example
```csharp
decimal price = W3bTools.CGPrice("ethereum");
```

### Breakdown
```csharp
public static decimal CGPrice(
    string CGid = "ethereum",                  // CoinGecko ID
    [CallerMemberName] string callerName = ""  // Auto-filled caller name
)
// Returns: USD price from CoinGecko
// Same as CoinGecco.PriceById
```

---

## OKXPrice

### Purpose
Gets cryptocurrency price from OKX exchange (project extension).

### Example
```csharp
decimal ethPrice = project.OKXPrice("ETH");
decimal btcPrice = project.OKXPrice("BTC");
```

### Breakdown
```csharp
public static decimal OKXPrice(
    this IZennoPosterProjectModel project,    // Project instance
    string tiker                               // Ticker symbol
)
// Returns: USD price from OKX
// Uses OkxApi integration
// Ticker is auto-converted to uppercase
```

---

## UsdToToken

### Purpose
Converts USD amount to token amount using real-time price.

### Example
```csharp
// Convert $100 to ETH using KuCoin price
decimal ethAmount = project.UsdToToken(100, "ETH", "KuCoin");

// Convert $50 to BTC using OKX price
decimal btcAmount = project.UsdToToken(50, "BTC", "OKX");

// Convert $200 to SOL using CoinGecko
decimal solAmount = project.UsdToToken(200, "SOL", "CoinGecco");
```

### Breakdown
```csharp
public static decimal UsdToToken(
    this IZennoPosterProjectModel project,    // Project instance
    decimal usdAmount,                         // USD amount to convert
    string tiker,                              // Token ticker
    string apiProvider = "KuCoin"             // Price provider: "KuCoin", "OKX", "CoinGecco"
)
// Returns: Token amount (usdAmount / price)
// Supports multiple price providers
// Throws: ArgumentException for unknown provider
```

---

# Static Helper Methods (W3bTools)

## EvmNative

### Purpose
Gets native EVM token balance (static method).

### Example
```csharp
decimal balance = W3bTools.EvmNative(
    rpc: "https://eth.llamarpc.com",
    address: "0x123..."
);
```

### Breakdown
```csharp
public static decimal EvmNative(
    string rpc,       // RPC endpoint URL
    string address    // Wallet address
)
// Returns: Balance in native tokens (ETH, BNB, etc.)
// Converts from wei to ether (18 decimals)
```

---

## ERC20

### Purpose
Gets ERC20 token balance (static method).

### Example
```csharp
string usdtContract = "0xdac17f958d2ee523a2206206994597c13d831ec7";
decimal balance = W3bTools.ERC20(
    tokenContract: usdtContract,
    rpc: "https://eth.llamarpc.com",
    address: "0x123...",
    tokenDecimal: "18"
);
```

### Breakdown
```csharp
public static decimal ERC20(
    string tokenContract,     // ERC20 contract address
    string rpc,              // RPC endpoint URL
    string address,          // Wallet address
    string tokenDecimal = "18"  // Token decimals (default: 18)
)
// Returns: Token balance in decimal format
// Automatically converts from raw units
```

---

## ERC721

### Purpose
Gets ERC721 NFT count (static method).

### Example
```csharp
decimal nftCount = W3bTools.ERC721(
    tokenContract: "0xbc4ca0eda7647a8ab7c2061c2e118a18a936f13d",
    rpc: "https://eth.llamarpc.com",
    address: "0x123..."
);
```

### Breakdown
```csharp
public static decimal ERC721(
    string tokenContract,    // ERC721 contract address
    string rpc,             // RPC endpoint URL
    string address          // Wallet address
)
// Returns: Number of NFTs owned
```

---

## ERC1155

### Purpose
Gets ERC1155 token balance (static method).

### Example
```csharp
decimal balance = W3bTools.ERC1155(
    tokenContract: "0x123...",
    tokenId: "1",
    rpc: "https://eth.llamarpc.com",
    address: "0x456..."
);
```

### Breakdown
```csharp
public static decimal ERC1155(
    string tokenContract,    // ERC1155 contract address
    string tokenId,         // Token ID
    string rpc,             // RPC endpoint URL
    string address          // Wallet address
)
// Returns: Token balance for specific ID
```

---

## Nonce

### Purpose
Gets transaction nonce for an address (static method).

### Example
```csharp
int nonce = W3bTools.Nonce(
    rpc: "https://eth.llamarpc.com",
    address: "0x123..."
);
```

### Breakdown
```csharp
public static int Nonce(
    string rpc,       // RPC endpoint URL
    string address    // Wallet address
)
// Returns: Current transaction count (nonce)
```

---

## ChainId

### Purpose
Gets chain ID from RPC endpoint (static method).

### Example
```csharp
int chainId = W3bTools.ChainId("https://eth.llamarpc.com");
// Returns: 1 (Ethereum mainnet)
```

### Breakdown
```csharp
public static int ChainId(
    string rpc    // RPC endpoint URL
)
// Returns: Chain ID as integer
```

---

## GasPrice

### Purpose
Gets current gas price (static method).

### Example
```csharp
decimal gasPrice = W3bTools.GasPrice("https://eth.llamarpc.com");
```

### Breakdown
```csharp
public static decimal GasPrice(
    string rpc    // RPC endpoint URL
)
// Returns: Current gas price in wei
```

---

## WaitTx

### Purpose
Waits for transaction confirmation (static method).

### Example
```csharp
bool success = W3bTools.WaitTx(
    rpc: "https://eth.llamarpc.com",
    hash: "0xabc123...",
    deadline: 120,
    log: true,
    extended: true  // Use extended logging
);
```

### Breakdown
```csharp
public static bool WaitTx(
    string rpc,          // RPC endpoint URL
    string hash,         // Transaction hash
    int deadline = 60,   // Timeout in seconds
    string proxy = "",   // Optional proxy
    bool log = false,    // Enable logging
    bool extended = false // Use extended logging (shows pending state)
)
// Returns: true if transaction succeeded, false if failed
// Throws: Exception on timeout
```

---

## SolNative

### Purpose
Gets Solana native balance (static method).

### Example
```csharp
decimal balance = W3bTools.SolNative(
    address: "DYw8jCTfwHNRJhhmFcbXvVDTqWMEVFBX6ZKUmG5CNSKK",
    rpc: "https://api.mainnet-beta.solana.com"
);
```

### Breakdown
```csharp
public static decimal SolNative(
    string address,                                 // Solana address
    string rpc = "https://api.mainnet-beta.solana.com"  // RPC URL
)
// Returns: SOL balance (converted from lamports)
```

---

## SPL

### Purpose
Gets SPL token balance on Solana (static method).

### Example
```csharp
string usdcMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v";
decimal balance = W3bTools.SPL(
    tokenMint: usdcMint,
    walletAddress: "DYw8jCTfwHNRJhhmFcbXvVDTqWMEVFBX6ZKUmG5CNSKK",
    rpc: "https://api.mainnet-beta.solana.com"
);
```

### Breakdown
```csharp
public static decimal SPL(
    string tokenMint,     // SPL token mint address
    string walletAddress, // Wallet address
    string rpc = "https://api.mainnet-beta.solana.com"  // RPC URL
)
// Returns: SPL token balance
```

---

## SolTxFee

### Purpose
Gets Solana transaction fee (static method).

### Example
```csharp
decimal fee = W3bTools.SolTxFee(
    transactionHash: "4xKsZN...",
    rpc: null,  // Uses default mainnet
    tokenDecimal: "9"
);
```

### Breakdown
```csharp
public static decimal SolTxFee(
    string transactionHash,        // Transaction signature
    string rpc = null,            // RPC URL (default: mainnet)
    string tokenDecimal = "9"     // Decimals (9 for SOL)
)
// Returns: Transaction fee in SOL
```
