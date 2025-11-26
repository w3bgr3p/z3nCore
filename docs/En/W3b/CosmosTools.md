# CosmosTools

Tools for working with Cosmos SDK wallets and addresses.

---

## KeyFromSeed

### Purpose
Derives a private key from a mnemonic phrase using Cosmos SDK derivation path.

### Example
```csharp
var cosmosTools = new CosmosTools();
string mnemonic = "word1 word2 word3...";
string privateKey = cosmosTools.KeyFromSeed(mnemonic);
Console.WriteLine($"Private Key: {privateKey}");
```

### Breakdown
```csharp
public string KeyFromSeed(
    string mnemonic    // BIP39 mnemonic phrase (12-24 words)
)
// Returns: Private key in hex format (lowercase, without 0x)
// Uses derivation path: m/44'/118'/0'/0/0 (Cosmos SDK standard)
```

---

## AddressFromSeed

### Purpose
Derives a Bech32 wallet address from a mnemonic phrase.

### Example
```csharp
var cosmosTools = new CosmosTools();
string mnemonic = "word1 word2 word3...";
string address = cosmosTools.AddressFromSeed(mnemonic, "cosmos");
Console.WriteLine($"Cosmos Address: {address}");
// For other chains: AddressFromSeed(mnemonic, "osmo") for Osmosis
```

### Breakdown
```csharp
public string AddressFromSeed(
    string mnemonic,           // BIP39 mnemonic phrase
    string chain = "cosmos"    // Chain prefix (cosmos, osmo, juno, etc.)
)
// Returns: Bech32 encoded address (e.g., cosmos1abc...)
// Default chain: "cosmos"
```

---

## AddressFromKey

### Purpose
Derives a Bech32 wallet address from a private key.

### Example
```csharp
var cosmosTools = new CosmosTools();
string privateKey = "abc123def456..."; // Hex format
string address = cosmosTools.AddressFromKey(privateKey, "cosmos");
Console.WriteLine($"Address: {address}");
```

### Breakdown
```csharp
public string AddressFromKey(
    string privateKey,         // Private key in hex format
    string chain = "cosmos"    // Chain prefix
)
// Returns: Bech32 encoded address
// Process: privateKey -> pubKey -> SHA256 -> RIPEMD160 -> Bech32 encode
```

---

## AccFromSeed

### Purpose
Derives both private key and address from a mnemonic phrase.

### Example
```csharp
var cosmosTools = new CosmosTools();
string mnemonic = "word1 word2 word3...";
string[] account = cosmosTools.AccFromSeed(mnemonic, "osmo");
string privateKey = account[0];
string address = account[1];
Console.WriteLine($"PrivateKey: {privateKey}");
Console.WriteLine($"Address: {address}");
```

### Breakdown
```csharp
public string[] AccFromSeed(
    string mnemonic,           // BIP39 mnemonic phrase
    string chain = "cosmos"    // Chain prefix
)
// Returns: string[2] array [privateKey, address]
// Combines KeyFromSeed and AddressFromSeed functionality
```
