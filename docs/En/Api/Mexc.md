# Mexc Class Documentation

## Overview
The `Mexc` class provides comprehensive MEXC exchange API integration for spot trading, withdrawals, deposits, transfers, and account management.

---

## Constructor

### `Mexc(IZennoPosterProjectModel project, bool log = false)`

**Purpose:** Initializes MEXC API client with credentials from database.

**Example:**
```csharp
var mexc = new Mexc(project, log: true);
var balance = mexc.GetSpotBalance();
```

**Breakdown:**
```csharp
var mexc = new Mexc(
    project,  // IZennoPosterProjectModel - project instance
    true      // bool - enable logging
);
// Note: API key, secret, and proxy loaded from database (_api table, id='mexc')
```

---

## Public Methods

### `GetSpotBalance(bool log = false, bool toJson = false)`

**Purpose:** Retrieves all spot wallet balances with non-zero amounts.

**Example:**
```csharp
var mexc = new Mexc(project);
Dictionary<string, string> balances = mexc.GetSpotBalance(log: true);

foreach (var coin in balances)
{
    Console.WriteLine($"{coin.Key}: {coin.Value}");
}
```

**Breakdown:**
```csharp
Dictionary<string, string> balances = mexc.GetSpotBalance(
    true,   // bool - enable logging
    false   // bool - populate project.Json object
);
// Returns: Dictionary<string, string> - asset → free balance
// Throws: Exception - if API error occurs
```

---

### `GetSpotBalance(string coin)`

**Purpose:** Retrieves balance for a specific coin.

**Example:**
```csharp
var mexc = new Mexc(project);
string usdtBalance = mexc.GetSpotBalance("USDT");
```

**Breakdown:**
```csharp
string balance = mexc.GetSpotBalance(
    "USDT"  // string - coin symbol
);
// Returns: string - available balance or "0" if not found
```

---

### `Withdraw(string coin, string network, string address, string amount, string memo = "", string remark = "")`

**Purpose:** Withdraws cryptocurrency to an external address.

**Example:**
```csharp
var mexc = new Mexc(project);
string withdrawId = mexc.Withdraw(
    "USDT",
    "arbitrum",
    "0x742d35Cc6634C0532925a3b8D45C0532925aAB1",
    "10.5",
    "",           // Optional memo
    "My withdrawal"
);
```

**Breakdown:**
```csharp
string withdrawalId = mexc.Withdraw(
    "USDT",                 // string - coin symbol
    "arbitrum",             // string - network name
    "0x742d35Cc...",       // string - recipient address
    "10.5",                 // string - withdrawal amount
    "",                     // string - optional memo/tag
    "Withdrawal note"       // string - optional remark
);
// Returns: string - withdrawal ID
// Throws: Exception - if withdrawal fails
// Note: Uses InvariantCulture for decimal formatting
```

---

### `GetWithdrawHistory(int limit = 1000)`

**Purpose:** Retrieves withdrawal history.

**Example:**
```csharp
var mexc = new Mexc(project);
List<string> history = mexc.GetWithdrawHistory(50);
```

**Breakdown:**
```csharp
List<string> withdrawals = mexc.GetWithdrawHistory(
    100  // int - maximum records to retrieve
);
// Returns: List<string> - format: "id:coin:amount:status:address:network"
```

---

### `GetWithdrawHistory(string searchId)`

**Purpose:** Searches for a specific withdrawal by ID.

**Example:**
```csharp
var mexc = new Mexc(project);
string withdrawal = mexc.GetWithdrawHistory("1234567890");
```

**Breakdown:**
```csharp
string matchingWithdrawal = mexc.GetWithdrawHistory(
    "1234567890"  // string - withdrawal ID to search
);
// Returns: string - withdrawal record or "NoIdFound: {searchId}"
```

---

### `GetDepositHistory(string coin = "", int limit = 1000)`

**Purpose:** Retrieves deposit history.

**Example:**
```csharp
var mexc = new Mexc(project);

// All deposits
List<string> allDeposits = mexc.GetDepositHistory();

// Specific coin
List<string> usdtDeposits = mexc.GetDepositHistory("USDT", 50);
```

**Breakdown:**
```csharp
List<string> deposits = mexc.GetDepositHistory(
    "USDT",  // string - optional coin filter
    100      // int - max records
);
// Returns: List<string> - format: "txId:coin:amount:status:address:network"
```

---

### `GetDepositAddress(string coin, string network = "")`

**Purpose:** Retrieves deposit address for a specific coin and network.

**Example:**
```csharp
var mexc = new Mexc(project);
string address = mexc.GetDepositAddress("USDT", "arbitrum");
// Returns: "0x742d35Cc..." or "0x742d35Cc...:memo" if memo required
```

**Breakdown:**
```csharp
string depositAddress = mexc.GetDepositAddress(
    "USDT",      // string - coin symbol
    "arbitrum"   // string - network name
);
// Returns: string - deposit address or "address:memo" if memo required
// Throws: Exception - if no address found
```

---

### `GetCoins()`

**Purpose:** Retrieves all coins configuration and populates project.Json.

**Example:**
```csharp
var mexc = new Mexc(project);
mexc.GetCoins();
// Access via project.Json
```

**Breakdown:**
```csharp
mexc.GetCoins();
// Populates project.Json with full coins configuration
// Use GetSupportedCoins() for formatted list
```

---

### `GetSupportedCoins()`

**Purpose:** Gets list of all supported coins and their networks.

**Example:**
```csharp
var mexc = new Mexc(project);
List<string> coins = mexc.GetSupportedCoins();
```

**Breakdown:**
```csharp
List<string> supportedCoins = mexc.GetSupportedCoins();
// Returns: List<string> - format: "coin:network1;network2;network3"
// Example: "USDT:ERC20;TRC20;BEP20(BSC);ARBITRUM"
```

---

### `GetPrice<T>(string symbol)`

**Purpose:** Gets current market price for a trading pair.

**Example:**
```csharp
var mexc = new Mexc(project);
decimal price = mexc.GetPrice<decimal>("BTCUSDT");
string priceStr = mexc.GetPrice<string>("ETHUSDT");
```

**Breakdown:**
```csharp
T price = mexc.GetPrice<T>(
    "BTCUSDT"  // string - trading pair symbol
);
// Returns: T - price as decimal or string
// Throws: Exception - if symbol not found
```

---

### `CancelWithdraw(string withdrawId)`

**Purpose:** Cancels a pending withdrawal.

**Example:**
```csharp
var mexc = new Mexc(project);
string result = mexc.CancelWithdraw("1234567890");
```

**Breakdown:**
```csharp
string cancelResult = mexc.CancelWithdraw(
    "1234567890"  // string - withdrawal ID to cancel
);
// Returns: string - cancelled withdrawal ID
// Throws: Exception - if cancellation fails
```

---

### `InternalTransfer(string asset, string amount, string fromAccountType = "SPOT", string toAccountType = "FUTURES")`

**Purpose:** Transfers funds between account types (SPOT/FUTURES).

**Example:**
```csharp
var mexc = new Mexc(project);
string transferId = mexc.InternalTransfer(
    "USDT",
    "100.50",
    "SPOT",
    "FUTURES"
);
```

**Breakdown:**
```csharp
string transferId = mexc.InternalTransfer(
    "USDT",     // string - asset to transfer
    "100.50",   // string - amount
    "SPOT",     // string - source account type
    "FUTURES"   // string - destination account type
);
// Returns: string - transfer ID
// Throws: Exception - if transfer fails
```

---

### `GetTransferHistory(string fromAccountType = "SPOT", string toAccountType = "FUTURES", int size = 10)`

**Purpose:** Retrieves internal transfer history.

**Example:**
```csharp
var mexc = new Mexc(project);
List<string> transfers = mexc.GetTransferHistory("SPOT", "FUTURES", 50);
```

**Breakdown:**
```csharp
List<string> history = mexc.GetTransferHistory(
    "SPOT",     // string - source account type
    "FUTURES",  // string - destination account type
    10          // int - max records
);
// Returns: List<string> - format: "tranId:asset:amount:fromType:toType:status"
```

---

## Network Mapping

| Input | MEXC Network Code |
|-------|------------------|
| arbitrum | ARBITRUM |
| ethereum | ERC20 |
| base | BASE |
| bsc | BEP20(BSC) |
| avalanche | AVAX-C |
| polygon | POLYGON |
| optimism | OP |
| trc20 | TRC20 |
| zksync | ZKSYNC |
| aptos | APTOS |

---

## Notes

- API credentials loaded from database table `_api` with `id = 'mexc'`
- All requests use HMAC-SHA256 signature authentication
- Uses InvariantCulture for decimal parsing/formatting
- Deposit/withdrawal status codes vary by type
- All timestamps use Unix milliseconds
- User-Agent from project profile
- Responses are parsed into Newtonsoft.Json objects
