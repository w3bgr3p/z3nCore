# BinanceApi Class Documentation

## Overview
The `BinanceApi` class provides integration with Binance exchange API for cryptocurrency withdrawals and balance management.

---

## Constructor

### `BinanceApi(IZennoPosterProjectModel project, bool log = false)`

**Purpose:** Initializes the Binance API client with credentials from database.

**Example:**
```csharp
var binance = new BinanceApi(project, log: true);
var balance = binance.GetUserAsset("USDT");
```

**Breakdown:**
```csharp
var binance = new BinanceApi(
    project,  // IZennoPosterProjectModel - project instance
    true      // bool - enable logging
);
// Note: API key, secret, and proxy are loaded from database (_api table)
```

---

## Public Methods

### `Withdraw(string coin, string network, string address, string amount)`

**Purpose:** Withdraws cryptocurrency to an external address.

**Example:**
```csharp
var binance = new BinanceApi(project);
string result = binance.Withdraw(
    "USDT",
    "arbitrum",
    "0x742d35Cc6634C0532925a3b8D45C0...AB1",
    "10.5"
);
```

**Breakdown:**
```csharp
string withdrawalResult = binance.Withdraw(
    "USDT",                          // string - coin symbol
    "arbitrum",                      // string - network (arbitrum/ethereum/base/bsc/etc)
    "0x742d35Cc6634C0532...",       // string - recipient address
    "10.5"                          // string - amount to withdraw
);
// Returns: string - API response with withdrawal details
// Note: Network names are automatically mapped (e.g., "arbitrum" → "ARBITRUM")
// Throws: Exception - if withdrawal fails
```

---

### `GetUserAsset()`

**Purpose:** Retrieves all user assets with non-zero balances.

**Example:**
```csharp
var binance = new BinanceApi(project);
Dictionary<string, string> balances = binance.GetUserAsset();

foreach (var asset in balances)
{
    Console.WriteLine($"{asset.Key}: {asset.Value}");
}
// Output: USDT: 1250.50
//         BTC: 0.025
```

**Breakdown:**
```csharp
Dictionary<string, string> allBalances = binance.GetUserAsset();
// Returns: Dictionary<string, string> - key: asset symbol, value: free balance
// Example: {"USDT": "1250.50", "BTC": "0.025", "ETH": "1.5"}
```

---

### `GetUserAsset(string coin)`

**Purpose:** Retrieves balance for a specific coin.

**Example:**
```csharp
var binance = new BinanceApi(project);
string usdtBalance = binance.GetUserAsset("USDT");
Console.WriteLine($"USDT Balance: {usdtBalance}");
```

**Breakdown:**
```csharp
string balance = binance.GetUserAsset(
    "USDT"  // string - coin symbol to check
);
// Returns: string - free balance for the specified coin
// Example: "1250.50"
```

---

### `GetWithdrawHistory()`

**Purpose:** Retrieves complete withdrawal history.

**Example:**
```csharp
var binance = new BinanceApi(project);
List<string> history = binance.GetWithdrawHistory();

foreach (string withdrawal in history)
{
    Console.WriteLine(withdrawal);
}
// Output: 123456:10.5:USDT:6
//         789012:0.001:BTC:1
```

**Breakdown:**
```csharp
List<string> withdrawalHistory = binance.GetWithdrawHistory();
// Returns: List<string> - each entry format: "id:amount:coin:status"
// Status codes: 0=EmailSent, 1=Cancelled, 2=AwaitingApproval, 3=Rejected,
//               4=Processing, 5=Failure, 6=Completed
```

---

### `GetWithdrawHistory(string searchId)`

**Purpose:** Searches for a specific withdrawal by ID or any search term.

**Example:**
```csharp
var binance = new BinanceApi(project);
string withdrawal = binance.GetWithdrawHistory("123456");
Console.WriteLine(withdrawal);
// Output: 123456:10.5:USDT:6
```

**Breakdown:**
```csharp
string matchingWithdrawal = binance.GetWithdrawHistory(
    "123456"  // string - withdrawal ID or search term
);
// Returns: string - matching withdrawal in format "id:amount:coin:status"
// Returns: "NoIdFound: {searchId}" - if no match found
```

---

## Network Mapping

The class automatically maps common network names to Binance network codes:

| Input | Binance Code |
|-------|-------------|
| arbitrum | ARBITRUM |
| ethereum | ETH |
| base | BASE |
| bsc | BSC |
| avalanche | AVAXC |
| polygon | MATIC |
| optimism | OPTIMISM |
| trc20 | TRC20 |
| zksync | ZkSync |
| aptos | APT |

---

## Notes

- API credentials (key, secret, proxy) are loaded from database table `_api` with `id = 'binance'`
- All requests use HMAC-SHA256 signature authentication
- Uses ZennoPoster HTTP methods with cookie container support
- Logging outputs withdrawal details and balance information
- Withdrawal status codes: 0=EmailSent, 1=Cancelled, 2=AwaitingApproval, 3=Rejected, 4=Processing, 5=Failure, 6=Completed
