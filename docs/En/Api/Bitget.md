# Bitget Class Documentation

## Overview
The `Bitget` class provides comprehensive integration with Bitget exchange API for trading, withdrawals, deposits, transfers, and account management.

---

## Constructor

### `Bitget(IZennoPosterProjectModel project, bool log = false)`

**Purpose:** Initializes the Bitget API client with credentials from database.

**Example:**
```csharp
var bitget = new Bitget(project, log: true);
var balance = bitget.GetSpotBalance();
```

**Breakdown:**
```csharp
var bitget = new Bitget(
    project,  // IZennoPosterProjectModel - project instance
    true      // bool - enable logging
);
// Note: API key, secret, passphrase, and proxy loaded from database
```

---

## Public Methods

### `GetSpotBalance(bool log = false, bool toJson = false)`

**Purpose:** Retrieves all spot wallet balances with non-zero amounts.

**Example:**
```csharp
var bitget = new Bitget(project);
Dictionary<string, string> balances = bitget.GetSpotBalance(log: true);

foreach (var coin in balances)
{
    Console.WriteLine($"{coin.Key}: {coin.Value}");
}
```

**Breakdown:**
```csharp
Dictionary<string, string> balances = bitget.GetSpotBalance(
    true,   // bool - enable logging
    false   // bool - populate project.Json object
);
// Returns: Dictionary<string, string> - coin name → available balance
// Example: {"USDT": "1250.50", "BTC": "0.025"}
```

---

### `GetSpotBalance(string coin)`

**Purpose:** Retrieves balance for a specific coin.

**Example:**
```csharp
var bitget = new Bitget(project);
string usdtBalance = bitget.GetSpotBalance("USDT");
```

**Breakdown:**
```csharp
string balance = bitget.GetSpotBalance(
    "USDT"  // string - coin symbol
);
// Returns: string - available balance or "0" if not found
```

---

### `Withdraw(string coin, string chain, string address, string amount, string tag = "", string remark = "", string clientOid = "")`

**Purpose:** Withdraws cryptocurrency to an external address.

**Example:**
```csharp
var bitget = new Bitget(project);
string orderId = bitget.Withdraw(
    "USDT",
    "arbitrum",
    "0x742d35Cc6634C0532925a3b8D45C0...AB1",
    "10.5"
);
```

**Breakdown:**
```csharp
string withdrawalOrderId = bitget.Withdraw(
    "USDT",                    // string - coin symbol
    "arbitrum",                // string - network name
    "0x742d35Cc...",          // string - recipient address
    "10.5",                    // string - withdrawal amount
    "",                        // string - optional memo/tag
    "",                        // string - optional remark
    ""                         // string - optional client order ID
);
// Returns: string - withdrawal order ID
// Throws: Exception - if withdrawal fails
// Note: Uses InvariantCulture for decimal formatting
```

---

### `GetWithdrawHistory(int limit = 100)`

**Purpose:** Retrieves withdrawal history.

**Example:**
```csharp
var bitget = new Bitget(project);
List<string> history = bitget.GetWithdrawHistory(50);

foreach (string withdrawal in history)
{
    var parts = withdrawal.Split(':');
    Console.WriteLine($"Order: {parts[0]}, Coin: {parts[1]}, Amount: {parts[2]}");
}
```

**Breakdown:**
```csharp
List<string> withdrawals = bitget.GetWithdrawHistory(
    100  // int - maximum number of records to retrieve
);
// Returns: List<string> - format: "orderId:coin:amount:status:address:chain"
```

---

### `GetWithdrawHistory(string searchId)`

**Purpose:** Searches for a specific withdrawal by order ID.

**Example:**
```csharp
var bitget = new Bitget(project);
string withdrawal = bitget.GetWithdrawHistory("1234567890");
```

**Breakdown:**
```csharp
string matchingWithdrawal = bitget.GetWithdrawHistory(
    "1234567890"  // string - order ID to search
);
// Returns: string - withdrawal record or "NoIdFound: {searchId}"
```

---

### `GetSupportedCoins()`

**Purpose:** Retrieves list of all supported coins and their available chains.

**Example:**
```csharp
var bitget = new Bitget(project);
List<string> coins = bitget.GetSupportedCoins();

foreach (string coin in coins)
{
    var parts = coin.Split(':');
    Console.WriteLine($"{parts[0]}: {parts[1]}");
}
```

**Breakdown:**
```csharp
List<string> supportedCoins = bitget.GetSupportedCoins();
// Returns: List<string> - format: "coinName:chain1;chain2;chain3"
// Example: "USDT:ERC20;TRC20;BSC"
```

---

### `GetPrice<T>(string symbol)`

**Purpose:** Gets current market price for a trading pair.

**Example:**
```csharp
var bitget = new Bitget(project);
decimal priceDecimal = bitget.GetPrice<decimal>("BTCUSDT");
string priceString = bitget.GetPrice<string>("ETHUSDT");
```

**Breakdown:**
```csharp
T price = bitget.GetPrice<T>(
    "BTCUSDT"  // string - trading pair symbol
);
// Returns: T - price as decimal or string type
// Example: 45000.50 (decimal) or "45000.50" (string)
// Throws: Exception - if API error
```

---

### `GetSubAccountsAssets()`

**Purpose:** Retrieves assets from all sub-accounts with non-zero balances.

**Example:**
```csharp
var bitget = new Bitget(project);
List<string> subAssets = bitget.GetSubAccountsAssets();

foreach (string asset in subAssets)
{
    var parts = asset.Split(':');
    Console.WriteLine($"User: {parts[0]}, Coin: {parts[1]}, Available: {parts[2]}");
}
```

**Breakdown:**
```csharp
List<string> assets = bitget.GetSubAccountsAssets();
// Returns: List<string> - format: "userId:coinName:available:frozen:locked"
```

---

### `GetAccountInfo()`

**Purpose:** Retrieves main account information including user ID and authorities.

**Example:**
```csharp
var bitget = new Bitget(project);
Dictionary<string, object> info = bitget.GetAccountInfo();

string userId = info["userId"].ToString();
string authorities = info["authorities"].ToString();
```

**Breakdown:**
```csharp
Dictionary<string, object> accountInfo = bitget.GetAccountInfo();
// Returns: Dictionary with keys: userId, inviterId, parentId,
//          isTrader, isSpotTrader, authorities
```

---

### `SubTransfer(string fromUserId, string toUserId, string coin, string amount, string fromType = "spot", string toType = "spot", string clientOid = null)`

**Purpose:** Transfers funds between sub-accounts or from sub to main account.

**Example:**
```csharp
var bitget = new Bitget(project);
bitget.SubTransfer(
    "sub_user_123",
    "main_user_456",
    "USDT",
    "100.50"
);
```

**Breakdown:**
```csharp
string result = bitget.SubTransfer(
    "123456",     // string - source user ID
    "789012",     // string - destination user ID
    "USDT",       // string - coin to transfer
    "100.50",     // string - amount
    "spot",       // string - source account type
    "spot",       // string - destination account type
    null          // string - optional client order ID
);
// Returns: "Success" on successful transfer
// Throws: Exception - if transfer fails
```

---

### `InternalTransfer(string coin, string amount, string fromType = "spot", string toType = "spot", string clientOid = null)`

**Purpose:** Transfers funds within the same account between different account types.

**Example:**
```csharp
var bitget = new Bitget(project);
string transferId = bitget.InternalTransfer(
    "USDT",
    "50.25",
    "spot",
    "futures"
);
```

**Breakdown:**
```csharp
string transferId = bitget.InternalTransfer(
    "USDT",      // string - coin to transfer
    "50.25",     // string - amount
    "spot",      // string - source account type
    "futures",   // string - destination account type
    null         // string - optional client order ID
);
// Returns: string - transfer ID
// Throws: Exception - if transfer fails
```

---

### `DrainSubAccounts()`

**Purpose:** Automatically transfers all assets from all sub-accounts to main account.

**Example:**
```csharp
var bitget = new Bitget(project);
bitget.DrainSubAccounts();
// All sub-account funds will be transferred to main account
```

**Breakdown:**
```csharp
bitget.DrainSubAccounts();
// Iterates through all sub-accounts and transfers all positive balances
// Uses 1 second delay between transfers to avoid rate limits
// Logs transfer count and any failures
```

---

### `GetDepositAddress(string coin, string chain)`

**Purpose:** Retrieves deposit address for a specific coin and chain.

**Example:**
```csharp
var bitget = new Bitget(project);
string address = bitget.GetDepositAddress("USDT", "arbitrum");
// Returns: "0x742d35Cc..." or "0x742d35Cc...:memo123" if tag/memo required
```

**Breakdown:**
```csharp
string depositAddress = bitget.GetDepositAddress(
    "USDT",      // string - coin symbol
    "arbitrum"   // string - network name
);
// Returns: string - deposit address or "address:tag" if memo required
// Throws: Exception - if error getting address
```

---

### `GetTransferHistory(string coinId, string fromType, string after, string before, int limit = 100)`

**Purpose:** Retrieves internal transfer history between account types.

**Example:**
```csharp
var bitget = new Bitget(project);
List<string> transfers = bitget.GetTransferHistory(
    "USDT",
    "spot",
    "0",
    "9999999999999",
    50
);
```

**Breakdown:**
```csharp
List<string> history = bitget.GetTransferHistory(
    "USDT",        // string - coin ID
    "spot",        // string - source account type
    "0",           // string - start timestamp
    "99999999",    // string - end timestamp
    100            // int - max records
);
// Returns: List<string> - format: "transferId:coin:amount:fromType:toType:status:time"
```

---

## Network Mapping

| Input | Bitget Chain Name |
|-------|------------------|
| arbitrum | Arbitrum One |
| ethereum | ERC20 |
| base | Base |
| bsc | BEP20 |
| avalanche | AVAX-C |
| polygon | Polygon |
| optimism | Optimism |
| trc20 | TRC20 |
| zksync | zkSync Era |
| aptos | Aptos |

---

## Notes

- API credentials loaded from database table `_api` with `id = 'bitget'`
- All requests use HMAC-SHA256 Base64 signature authentication
- Uses InvariantCulture for decimal parsing to avoid locale issues
- Includes 1-second delays between sub-account operations to respect rate limits
- All timestamps use Unix milliseconds format
