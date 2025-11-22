# UnlockApi Class Documentation

## Overview
The `UnlockApi` class provides integration with Unlock Protocol smart contracts for reading NFT membership data, expiration timestamps, and holder information.

---

## Constructor

### `UnlockApi(IZennoPosterProjectModel project, bool log = false)`

**Purpose:** Initializes Unlock Protocol API client with Optimism network RPC.

**Example:**
```csharp
var unlock = new UnlockApi(project, log: true);
string expiration = unlock.keyExpirationTimestampFor(
    "0x1234567890123456789012345678901234567890",
    1
);
```

**Breakdown:**
```csharp
var unlock = new UnlockApi(
    project,  // IZennoPosterProjectModel - project instance
    true      // bool - enable logging
);
// Note: Uses Optimism RPC by default
```

---

## Public Methods

### `keyExpirationTimestampFor(string addressTo, int tokenId, bool decode = true)`

**Purpose:** Retrieves the expiration timestamp for an NFT membership key.

**Example:**
```csharp
var unlock = new UnlockApi(project);
string expirationTimestamp = unlock.keyExpirationTimestampFor(
    "0x1234567890123456789012345678901234567890",  // Lock contract
    1,                                               // Token ID
    true                                             // Decode result
);
Console.WriteLine($"Expires at: {expirationTimestamp}");
```

**Breakdown:**
```csharp
string expiration = unlock.keyExpirationTimestampFor(
    "0x1234567...",  // string - Unlock Protocol lock contract address
    1,               // int - NFT token ID
    true             // bool - decode hex result to readable format
);
// Returns: string - Unix timestamp (decoded) or hex (raw)
// Throws: Exception - if contract call fails
```

---

### `ownerOf(string addressTo, int tokenId, bool decode = true)`

**Purpose:** Retrieves the owner address of a specific NFT token.

**Example:**
```csharp
var unlock = new UnlockApi(project);
string owner = unlock.ownerOf(
    "0x1234567890123456789012345678901234567890",
    1,
    true
);
Console.WriteLine($"Owner: {owner}");
```

**Breakdown:**
```csharp
string ownerAddress = unlock.ownerOf(
    "0x1234567...",  // string - lock contract address
    1,               // int - token ID
    true             // bool - decode result
);
// Returns: string - owner's Ethereum address
// Throws: Exception - if contract call fails
```

---

### `Decode(string toDecode, string function)`

**Purpose:** Decodes ABI-encoded hex data from contract responses.

**Example:**
```csharp
var unlock = new UnlockApi(project);
string decoded = unlock.Decode(
    "0x000000000000000000000000000000000000000000000000000000006789abcd",
    "keyExpirationTimestampFor"
);
```

**Breakdown:**
```csharp
string decodedValue = unlock.Decode(
    "0x6789abcd...",              // string - hex data to decode
    "keyExpirationTimestampFor"   // string - function name for ABI matching
);
// Returns: string - decoded value
// Automatically pads hex to 64 characters if needed
// Uses internal ABI for decoding
```

---

### `Holders(string contract)`

**Purpose:** Retrieves all NFT holders and their expiration timestamps from a lock contract.

**Example:**
```csharp
var unlock = new UnlockApi(project);
Dictionary<string, string> holders = unlock.Holders(
    "0x1234567890123456789012345678901234567890"
);

foreach (var holder in holders)
{
    Console.WriteLine($"Address: {holder.Key}, Expires: {holder.Value}");
}
```

**Breakdown:**
```csharp
Dictionary<string, string> allHolders = unlock.Holders(
    "0x1234567..."  // string - lock contract address
);
// Returns: Dictionary<string, string> - owner address → expiration timestamp
// Iterates through token IDs starting from 1
// Stops when encountering zero address (0x000...000)
// All addresses and timestamps returned in lowercase
```

---

## ABI Contract Methods

The class uses the following Unlock Protocol ABI methods:

### keyExpirationTimestampFor
- **Input:** uint256 tokenId
- **Output:** uint256 expiration timestamp
- **Purpose:** Get when a key expires

### ownerOf
- **Input:** uint256 tokenId
- **Output:** address owner
- **Purpose:** Get the owner of a specific token

---

## Contract Interaction Flow

1. **Call Contract:** Uses `Blockchain.ReadContract()` to query the smart contract
2. **Get Result:** Receives hex-encoded result
3. **Decode (Optional):** Decodes hex to human-readable format using ABI
4. **Return:** Returns processed data

---

## Data Format

### Raw (decode=false)
```
0x000000000000000000000000000000000000000000000000000000006789abcd
```

### Decoded (decode=true)
```
1736793293  // Unix timestamp
0x742d35Cc6634C0532925a3b8D45C0532925aAB1  // Address
```

---

## Notes

- Default blockchain: Optimism (via `Rpc.Get("optimism")`)
- All contract reads are view/pure functions (no gas cost)
- Uses Blockchain class for Web3 interactions
- ABI is hardcoded for Unlock Protocol v11+ contracts
- Token iteration in `Holders()` starts from ID 1
- Zero address (0x000...000) indicates non-existent token
- All returned addresses are lowercase
- Timestamps are Unix epoch seconds
- Automatic hex padding for short responses
- Logs errors to project log with inner exception details
