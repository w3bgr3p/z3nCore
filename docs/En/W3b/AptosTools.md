# AptosTools

Tools for working with Aptos blockchain.

---

## GetAptBalance

### Purpose
Retrieves the native APT token balance for a given address.

### Example
```csharp
var aptosTools = new AptosTools();
string rpc = "https://fullnode.mainnet.aptoslabs.com/v1";
string address = "0x1234...";
decimal balance = await aptosTools.GetAptBalance(rpc, address, log: true);
Console.WriteLine($"Balance: {balance} APT");
```

### Breakdown
```csharp
public async Task<decimal> GetAptBalance(
    string rpc,          // RPC endpoint URL (default: Aptos mainnet)
    string address,      // Wallet address to check balance
    string proxy = "",   // Optional proxy in format "user:pass:host:port"
    bool log = false     // Enable console logging
)
// Returns: Balance in APT (converted from octas with 8 decimals)
// Throws: HttpRequestException on network errors
```

---

## GetAptTokenBalance

### Purpose
Retrieves the balance of a specific token (coin) for a given address on Aptos.

### Example
```csharp
var aptosTools = new AptosTools();
string rpc = "https://fullnode.mainnet.aptoslabs.com/v1";
string address = "0x1234...";
string coinType = "0x1::aptos_coin::AptosCoin";
decimal balance = await aptosTools.GetAptTokenBalance(coinType, rpc, address, log: true);
Console.WriteLine($"Token balance: {balance}");
```

### Breakdown
```csharp
public async Task<decimal> GetAptTokenBalance(
    string coinType,     // Coin type identifier (e.g., "0x1::aptos_coin::AptosCoin")
    string rpc,          // RPC endpoint URL (default: Aptos mainnet)
    string address,      // Wallet address to check balance
    string proxy = "",   // Optional proxy in format "user:pass:host:port"
    bool log = false     // Enable console logging
)
// Returns: Token balance (converted with 6 decimals assumed)
// Throws: HttpRequestException on network errors
```
