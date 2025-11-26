# SuiTools

Tools for interacting with Sui blockchain. Note: Main class is internal, but public extension methods are available in W3bTools.

---

## SuiKeyGen.Generate

### Purpose
Generates Sui wallet keys and address from a mnemonic phrase.

### Example
```csharp
string mnemonic = "word1 word2 word3...";
var keys = SuiKeyGen.Generate(mnemonic);
Console.WriteLine($"Private Key (HEX): {SuiKeyGen.ToHex(keys.Priv32)}");
Console.WriteLine($"Private Key (Bech32): {keys.PrivateKeyBech32}");
Console.WriteLine($"Public Key: {SuiKeyGen.ToHex(keys.Pub32)}");
Console.WriteLine($"Address: {keys.Address}");
```

### Breakdown
```csharp
public static SuiKeys Generate(
    string mnemonic,                     // BIP39 mnemonic phrase
    string passphrase = "",              // Optional passphrase
    string path = "m/44'/784'/0'/0'/0'" // Derivation path (Sui standard)
)
// Returns: SuiKeys object with:
//   - Priv32: 32-byte private key
//   - PrivExpanded64: 64-byte expanded private key
//   - Pub32: 32-byte public key (Ed25519)
//   - Address: Sui address (0x...)
//   - PrivateKeyBech32: Bech32 encoded private key (suiprivkey1...)
```

---

## Extension Methods (W3bTools)

### SuiKey

### Purpose
Extracts key material from a mnemonic.

### Example
```csharp
string mnemonic = "word1 word2...";
string privateKeyHex = mnemonic.SuiKey("HEX");
string bech32Key = mnemonic.SuiKey("Bech32");
string publicKey = mnemonic.SuiKey("PubHEX");
string address = mnemonic.SuiKey("Address");
```

### Breakdown
```csharp
public static string SuiKey(
    this string mnemonic,    // Mnemonic phrase
    string keyType = "HEX"  // Key type: "HEX", "Bech32", "PubHEX", "Address"
)
// Returns: Key in requested format
```

---

### SuiAddress

### Purpose
Derives Sui address from mnemonic, private key, or converts existing address.

### Example
```csharp
// From mnemonic
string address1 = mnemonic.SuiAddress();

// From hex private key
string hexKey = "abc123...";
string address2 = hexKey.SuiAddress();

// From Bech32 private key
string bech32Key = "suiprivkey1...";
string address3 = bech32Key.SuiAddress();
```

### Breakdown
```csharp
public static string SuiAddress(
    this string input    // Mnemonic, private key (hex or bech32), or address
)
// Returns: Sui address (0x...)
// Auto-detects input type: seed, keySui, keyEvm, addressSui
```

---

### SuiNative

### Purpose
Gets the native SUI token balance for an address.

### Example
```csharp
string rpc = "https://fullnode.mainnet.sui.io";
string address = "0x123...";
decimal balance = W3bTools.SuiNative(rpc, address);
Console.WriteLine($"SUI Balance: {balance}");
```

### Breakdown
```csharp
public static decimal SuiNative(
    string rpc,       // Sui RPC endpoint URL
    string address    // Sui wallet address
)
// Returns: Balance in SUI (converted from MIST, 9 decimals)
// 1 SUI = 1,000,000,000 MIST
```

---

### SuiTokenBalance

### Purpose
Gets the balance of a specific coin type on Sui.

### Example
```csharp
string coinType = "0x2::sui::SUI";
decimal balance = W3bTools.SuiTokenBalance(coinType, rpc, address);
```

### Breakdown
```csharp
public static decimal SuiTokenBalance(
    string coinType,    // Coin type identifier
    string rpc,        // Sui RPC endpoint URL
    string address     // Sui wallet address
)
// Returns: Token balance (6 decimals assumed)
```

---

### SendNativeSui

### Purpose
Sends native SUI tokens to another address (extension method for IZennoPosterProjectModel).

### Example
```csharp
string to = "0x456...";
decimal amount = 0.5m; // 0.5 SUI
string rpc = "https://fullnode.mainnet.sui.io";
string txHash = project.SendNativeSui(to, amount, rpc);
Console.WriteLine($"Transaction: {txHash}");
```

### Breakdown
```csharp
public static string SendNativeSui(
    this IZennoPosterProjectModel project,    // Project instance
    string to,                                 // Recipient address
    decimal amount,                            // Amount in SUI
    string rpc = null,                        // RPC URL (default: mainnet)
    bool debug = false                        // Debug mode
)
// Returns: Transaction digest (hash)
// Requires: Private key in database (DbKey("evm"))
// Sets: project.Variables["blockchainHash"] = txHash
// Throws: Exception on failure
```

---

### SuiFaucet

### Purpose
Requests testnet SUI from the faucet (extension method for Instance).

### Example
```csharp
string address = "0x123...";
instance.SuiFaucet(project, address, successRequired: 3, tableToUpdate: "wallets");
```

### Breakdown
```csharp
public static void SuiFaucet(
    this Instance instance,                    // Browser instance
    IZennoPosterProjectModel project,          // Project instance
    string address,                            // Sui address to fund
    int successRequired = 3,                   // Number of successful requests needed
    string tableToUpdate = null               // Database table to update balance
)
// Requests SUI from testnet faucet
// Attempts up to 3 times
// Updates database with final balance if tableToUpdate specified
// Handles Cloudflare challenges automatically
```
