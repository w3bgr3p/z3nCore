# SolTools

Tools for interacting with Solana blockchain.

---

## GetSolanaBalance

### Purpose
Gets the native SOL balance for a Solana address.

### Example
```csharp
var solTools = new SolTools();
string rpc = "https://api.mainnet-beta.solana.com";
string address = "DYw8jCTfwHNRJhhmFcbXvVDTqWMEVFBX6ZKUmG5CNSKK";
decimal balance = await solTools.GetSolanaBalance(rpc, address);
Console.WriteLine($"SOL Balance: {balance}");
```

### Breakdown
```csharp
public async Task<decimal> GetSolanaBalance(
    string rpc,       // Solana RPC endpoint URL
    string address    // Solana wallet address (base58)
)
// Returns: Balance in SOL (converted from lamports, 9 decimals)
// 1 SOL = 1,000,000,000 lamports
```

---

## GetSplTokenBalance

### Purpose
Gets the balance of an SPL token for a Solana address.

### Example
```csharp
var solTools = new SolTools();
string rpc = "https://api.mainnet-beta.solana.com";
string walletAddress = "DYw8jCTfwHNRJhhmFcbXvVDTqWMEVFBX6ZKUmG5CNSKK";
string tokenMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v"; // USDC
decimal balance = await solTools.GetSplTokenBalance(rpc, walletAddress, tokenMint);
Console.WriteLine($"Token Balance: {balance}");
```

### Breakdown
```csharp
public async Task<decimal> GetSplTokenBalance(
    string rpc,            // Solana RPC endpoint URL
    string walletAddress,  // Wallet address
    string tokenMint      // SPL token mint address
)
// Returns: Token balance (using token's decimal configuration)
// Uses getTokenAccountsByOwner RPC method
// Returns 0 if no token account found
```

---

## SolFeeByTx

### Purpose
Gets the transaction fee (in SOL) for a completed Solana transaction.

### Example
```csharp
var solTools = new SolTools();
string txHash = "4xKsZN...";
decimal fee = await solTools.SolFeeByTx(txHash);
Console.WriteLine($"Transaction fee: {fee} SOL");
```

### Breakdown
```csharp
public async Task<decimal> SolFeeByTx(
    string transactionHash,              // Transaction signature/hash
    string rpc = null,                   // RPC URL (default: mainnet)
    string tokenDecimal = "9"           // Decimals for conversion (default: 9 for SOL)
)
// Returns: Fee in SOL (converted from lamports)
// Uses getTransaction RPC method
// Defaults to Solana mainnet if rpc is null
```
