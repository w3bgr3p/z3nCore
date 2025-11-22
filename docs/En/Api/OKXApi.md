# OkxApi Class Documentation

## Overview
The `OkxApi` class provides comprehensive OKX exchange API integration for trading, withdrawals, sub-account management, and asset operations.

---

## Constructor

### `OkxApi(IZennoPosterProjectModel project, bool log = false)`

**Purpose:** Initializes OKX API client with credentials from database.

**Example:**
```csharp
var okx = new OkxApi(project, log: true);
List<string> subs = okx.OKXGetSubAccs();
```

**Breakdown:**
```csharp
var okx = new OkxApi(
    project,  // IZennoPosterProjectModel - project instance
    true      // bool - enable logging
);
// Note: API key, secret, and passphrase loaded from database
```

---

## Public Methods

### `OKXGetSubAccs(string proxy = null, bool log = false)`

**Purpose:** Retrieves list of all sub-accounts.

**Example:**
```csharp
var okx = new OkxApi(project);
List<string> subAccounts = okx.OKXGetSubAccs();

foreach (string subAcct in subAccounts)
{
    Console.WriteLine($"Sub-account: {subAcct}");
}
```

**Breakdown:**
```csharp
List<string> subAccountsList = okx.OKXGetSubAccs(
    null,   // string - optional proxy
    false   // bool - enable logging
);
// Returns: List<string> - sub-account names
```

---

### `OKXGetSubMax(string accName, string proxy = null, bool log = false)`

**Purpose:** Gets maximum withdrawable balances for a sub-account (Trading account).

**Example:**
```csharp
var okx = new OkxApi(project);
List<string> maxBalances = okx.OKXGetSubMax("sub_account_1");

foreach (string balance in maxBalances)
{
    var parts = balance.Split(':');
    Console.WriteLine($"{parts[0]}: {parts[1]}");  // Currency: Max Withdrawal
}
```

**Breakdown:**
```csharp
List<string> balances = okx.OKXGetSubMax(
    "sub_account_1",  // string - sub-account name
    null,             // string - optional proxy
    false             // bool - logging
);
// Returns: List<string> - format: "currency:maxWithdrawable"
// Example: ["USDT:1250.50", "BTC:0.025"]
```

---

### `OKXGetSubTrading(string accName, string proxy = null, bool log = false)`

**Purpose:** Gets trading account equity for a sub-account.

**Example:**
```csharp
var okx = new OkxApi(project);
List<string> tradingBalances = okx.OKXGetSubTrading("sub_account_1");
```

**Breakdown:**
```csharp
List<string> equity = okx.OKXGetSubTrading(
    "sub_account_1",  // string - sub-account name
    null,             // string - proxy
    false             // bool - logging
);
// Returns: List<string> - adjusted equity values
```

---

### `OKXGetSubFunding(string accName, string proxy = null, bool log = false)`

**Purpose:** Gets funding account balances for a sub-account.

**Example:**
```csharp
var okx = new OkxApi(project);
List<string> fundingBalances = okx.OKXGetSubFunding("sub_account_1");

foreach (string balance in fundingBalances)
{
    var parts = balance.Split(':');
    Console.WriteLine($"{parts[0]}: {parts[1]}");  // Currency: Available
}
```

**Breakdown:**
```csharp
List<string> balances = okx.OKXGetSubFunding(
    "sub_account_1",  // string - sub-account name
    null,             // string - proxy
    false             // bool - logging
);
// Returns: List<string> - format: "currency:availableBalance"
```

---

### `OKXGetSubsBal(string proxy = null, bool log = false)`

**Purpose:** Gets balances from all sub-accounts (both funding and trading).

**Example:**
```csharp
var okx = new OkxApi(project);
List<string> allBalances = okx.OKXGetSubsBal();

foreach (string balance in allBalances)
{
    var parts = balance.Split(':');
    Console.WriteLine($"Sub: {parts[0]}, Currency: {parts[1]}, Balance: {parts[2]}");
}
```

**Breakdown:**
```csharp
List<string> allSubBalances = okx.OKXGetSubsBal(
    null,   // string - proxy
    false   // bool - logging
);
// Returns: List<string> - format: "subAccountName:currency:balance"
// Iterates all subs, checking both funding and trading accounts
```

---

### `OKXWithdraw(string toAddress, string currency, string chain, decimal amount, decimal fee, string proxy = null, bool log = false)`

**Purpose:** Withdraws cryptocurrency to an external address.

**Example:**
```csharp
var okx = new OkxApi(project);
okx.OKXWithdraw(
    "0x742d35Cc6634C0532925a3b8D45C0532925aAB1",
    "USDT",
    "arbitrum",
    10.5m,
    0.1m
);
```

**Breakdown:**
```csharp
okx.OKXWithdraw(
    "0x742d35Cc...",   // string - recipient address
    "USDT",            // string - currency symbol
    "arbitrum",        // string - network name
    10.5m,             // decimal - withdrawal amount
    0.1m,              // decimal - withdrawal fee
    null,              // string - proxy
    false              // bool - logging
);
// Note: Uses InvariantCulture for decimal formatting
// Throws: Exception - if withdrawal fails
```

---

### `OKXCreateSub(string subName, string accountType = "1", string proxy = null, bool log = false)`

**Purpose:** Creates a new sub-account.

**Example:**
```csharp
var okx = new OkxApi(project);
okx.OKXCreateSub("new_sub_account_1", "1");
```

**Breakdown:**
```csharp
okx.OKXCreateSub(
    "sub_account_name",  // string - name for new sub-account
    "1",                 // string - account type (1=standard)
    null,                // string - proxy
    false                // bool - logging
);
// Throws: Exception - if creation fails
```

---

### `OKXDrainSubs()`

**Purpose:** Transfers all balances from all sub-accounts to main account.

**Example:**
```csharp
var okx = new OkxApi(project);
okx.OKXDrainSubs();
// All sub-account funds transferred to main account
```

**Breakdown:**
```csharp
okx.OKXDrainSubs();
// Iterates all sub-accounts
// Transfers funding balances (type "6")
// Transfers trading balances (type "18")
// Includes 500ms delay between transfers
// Logs all operations and failures
```

---

### `OKXAddMaxSubs()`

**Purpose:** Creates the maximum number of sub-accounts allowed.

**Example:**
```csharp
var okx = new OkxApi(project);
okx.OKXAddMaxSubs();
// Creates sub-accounts until limit reached
```

**Breakdown:**
```csharp
okx.OKXAddMaxSubs();
// Creates sub-accounts with auto-generated names: "sub{i}t{timestamp}"
// Continues until exception thrown (limit reached)
// 1.5 second delay between creations
```

---

### `OKXPrice<T>(string pair, string proxy = null, bool log = false)`

**Purpose:** Gets current market price for a trading pair.

**Example:**
```csharp
var okx = new OkxApi(project);
decimal price = okx.OKXPrice<decimal>("BTC-USDT");
string priceStr = okx.OKXPrice<string>("ETH-USDT");
```

**Breakdown:**
```csharp
T price = okx.OKXPrice<T>(
    "BTC-USDT",  // string - trading pair (use hyphen separator)
    null,        // string - proxy
    false        // bool - logging
);
// Returns: T - price as decimal or string
// Throws: Exception - if pair not found
```

---

## Network Mapping

| Input | OKX Chain Name |
|-------|---------------|
| arbitrum | Arbitrum One |
| ethereum | ERC20 |
| base | Base |
| bsc | BSC |
| avalanche | Avalanche C-Chain |
| polygon | Polygon |
| optimism | Optimism |
| trc20 | TRC20 |
| zksync | zkSync Era |
| aptos | Aptos |

---

## Account Types

| Type | Description |
|------|-------------|
| 6 | Funding account |
| 18 | Trading account |
| 1 | Standard sub-account |

---

## Notes

- API credentials loaded from database table `_api` with `id = 'okx'`
- Uses HMAC-SHA256 Base64 signature authentication
- All timestamps use UTC ISO 8601 format
- Passphrase required in addition to key/secret
- Uses InvariantCulture for all decimal operations
- Sub-account transfers have 500-1500ms delays to respect rate limits
- All API responses parsed to `project.Json`
