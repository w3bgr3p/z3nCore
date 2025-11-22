# GazZip Class Documentation

## Overview
The `GazZip` class provides integration with GazZip bridge service for cross-chain gas refueling using EVM wallets.

---

## Constructor

### `GazZip(IZennoPosterProjectModel project, string key = null, bool log = false)`

**Purpose:** Initializes the GazZip refuel client.

**Example:**
```csharp
var gazzip = new GazZip(project, log: true);
string txHash = gazzip.Refuel("sepolia", 0.001m, "https://rpc.eth.com");
```

**Breakdown:**
```csharp
var gazzip = new GazZip(
    project,  // IZennoPosterProjectModel - project instance
    null,     // string - optional EVM private key (loaded from DB if null)
    true      // bool - enable logging
);
```

---

## Public Methods

### `Refuel(string chainTo, decimal value, string rpc, bool log = false)`

**Purpose:** Refuels a specific destination chain from a source chain.

**Example:**
```csharp
var gazzip = new GazZip(project);
string txHash = gazzip.Refuel(
    "sepolia",                      // Destination chain
    0.001m,                         // Amount in ETH
    "https://rpc.ethereum.org",     // Source RPC
    log: true
);
```

**Breakdown:**
```csharp
string transactionHash = gazzip.Refuel(
    "sepolia",              // string - destination chain name or hex ID
    0.001m,                 // decimal - amount to send (in native token)
    "https://rpc.eth...",   // string - source chain RPC URL
    true                    // bool - enable logging
);
// Returns: string - transaction hash
// Throws: Exception - if balance insufficient or transaction fails
// Note: Checks balance before sending (requires value + 0.00005 ETH fee)
```

---

### `Refuel(string chainTo, decimal value, string[] ChainsFrom = null, bool log = false)`

**Purpose:** Automatically finds a chain with sufficient balance and refuels.

**Example:**
```csharp
var gazzip = new GazZip(project);
string[] sourceChains = { "ethereum", "arbitrum", "optimism", "base" };

string txHash = gazzip.Refuel(
    "sepolia",
    0.001m,
    sourceChains,
    log: true
);
```

**Breakdown:**
```csharp
string transactionHash = gazzip.Refuel(
    "sepolia",                           // string - destination chain
    0.001m,                              // decimal - amount
    new[] { "ethereum", "arbitrum" },    // string[] - chains to check
    true                                 // bool - logging
);
// Returns: string - transaction hash from the chosen source chain
// Throws: Exception - if no chain has sufficient balance
// Note: Iterates through chains until finding one with balance > amount
```

---

## Supported Chains

| Chain Name | Hex ID |
|------------|--------|
| ethereum | 0x0100ff |
| sepolia | 0x010066 |
| soneum | 0x01019e |
| bsc | 0x01000e |
| gravity | 0x0100f0 |
| zero | 0x010169 |
| opbnb | 0x01003a |

You can also provide hex ID directly if the chain is not in the mapping.

---

## Contract Details

- **Contract Address:** `0x391E7C679d29bD940d63be94AD22A25d25b5A604`
- **Method:** Direct ETH transfer with chain ID in data field
- **Gas Multipliers:** Base fee × 2, Priority fee × 3
- **Data Encoding:** Destination chain hex ID (e.g., "0x010066")

---

## Transaction Flow

1. **Validation:** Checks if source chain has sufficient balance
2. **Encoding:** Encodes destination chain ID as transaction data
3. **Execution:** Sends transaction via `Tx` class
4. **Confirmation:** Waits for transaction confirmation
5. **Return:** Returns transaction hash

---

## Notes

- EVM private key loaded from database if not provided in constructor
- Automatically checks native balance before sending
- Uses `InvariantCulture` for decimal formatting
- Transaction hash stored in `project.Var("blockchainHash")`
- Waits for transaction confirmation using `W3bTools.WaitTx()`
- Requires minimum balance of (value + 0.00005 ETH) on source chain
- Supports any EVM-compatible chain via RPC URL
