# Cryptography

This module provides cryptographic utilities for encryption, hashing, and encoding operations.

---

## AES Class

Static class providing AES encryption/decryption and MD5 hashing functionality.

### EncryptAES

**Purpose**: Encrypts a string using AES encryption with ECB mode and PKCS7 padding.

**Example**:
```csharp
// Encrypt a string with automatic key hashing
string plainText = "Hello World";
string key = "mySecretKey";
string encrypted = AES.EncryptAES(plainText, key);

// Encrypt without key hashing (key must be 32 hex characters)
string encryptedNoHash = AES.EncryptAES(plainText, "0123456789ABCDEF0123456789ABCDEF", false);
```

**Breakdown**:
```csharp
public static string EncryptAES(string phrase, string key, bool hashKey = true)

// Parameters:
// - phrase: The plain text string to encrypt
// - key: The encryption key (will be hashed with MD5 if hashKey is true)
// - hashKey: If true, the key will be hashed with MD5 before use (default: true)

// Returns:
// - Encrypted string in hexadecimal format
// - Returns null if either phrase or key is null

// Notes:
// - Uses AES with ECB mode and PKCS7 padding
// - Result is returned as a hex string
```

---

### DecryptAES

**Purpose**: Decrypts an AES-encrypted hexadecimal string back to plain text.

**Example**:
```csharp
// Decrypt a string encrypted with EncryptAES
string encrypted = "4A6F686E..."; // hex string
string key = "mySecretKey";
string decrypted = AES.DecryptAES(encrypted, key);

// Decrypt without key hashing
string decryptedNoHash = AES.DecryptAES(encrypted, "0123456789ABCDEF0123456789ABCDEF", false);
```

**Breakdown**:
```csharp
public static string DecryptAES(string hash, string key, bool hashKey = true)

// Parameters:
// - hash: The encrypted hexadecimal string to decrypt
// - key: The decryption key (same key used for encryption)
// - hashKey: If true, the key will be hashed with MD5 before use (default: true)

// Returns:
// - Decrypted plain text string
// - Returns null if either hash or key is null

// Notes:
// - Must use the same hashKey setting as encryption
// - Input must be a valid hexadecimal string
```

---

### HashMD5

**Purpose**: Computes the MD5 hash of a string and returns it as a hexadecimal string.

**Example**:
```csharp
// Hash a string
string input = "myPassword123";
string hash = AES.HashMD5(input);
// Result: "5F4DCC3B5AA765D61D8327DEB882CF99" (example)
```

**Breakdown**:
```csharp
public static string HashMD5(string phrase)

// Parameters:
// - phrase: The string to hash

// Returns:
// - MD5 hash as uppercase hexadecimal string (32 characters)
// - Returns null if phrase is null

// Notes:
// - MD5 is NOT cryptographically secure for passwords
// - Use for checksums or non-security-critical hashing only
```

---

## Bech32 Class

Static class providing Bech32 encoding/decoding for blockchain addresses.

### Encode

**Purpose**: Encodes binary data into Bech32 format with a specified prefix.

**Example**:
```csharp
// Encode binary data
byte[] data = new byte[] { 0x01, 0x02, 0x03, 0x04 };
string encoded = Bech32.Encode("init", data);
// Result: "init1qypqxpq9qcrsszg"
```

**Breakdown**:
```csharp
public static string Encode(string prefix, byte[] data)

// Parameters:
// - prefix: Human-readable prefix (e.g., "init", "bc", "eth")
// - data: Binary data to encode

// Returns:
// - Bech32-encoded string with checksum

// Notes:
// - Converts 8-bit data to 5-bit format
// - Appends 6-character checksum
// - Prefix and data separated by '1'
```

---

### Bech32ToHex

**Purpose**: Converts a Bech32 address to hexadecimal format (specifically for "init" prefix).

**Example**:
```csharp
// Convert Bech32 to hex
string bech32 = "init1qypqxpq9qcrsszg2pgxysph3jf3j35z8";
string hex = Bech32.Bech32ToHex(bech32);
// Result: "0x0102030405060708091011121314151617181920"
```

**Breakdown**:
```csharp
public static string Bech32ToHex(string bech32Address)

// Parameters:
// - bech32Address: Valid Bech32 address with "init" prefix

// Returns:
// - Hexadecimal address with "0x" prefix (lowercase)

// Exceptions:
// - ArgumentException: If address is empty, invalid format, wrong prefix,
//   checksum fails, or decoded data is not 20 bytes

// Notes:
// - Only accepts "init" prefix
// - Validates checksum
// - Expects exactly 20 bytes of decoded data
```

---

### HexToBech32

**Purpose**: Converts a hexadecimal address to Bech32 format.

**Example**:
```csharp
// Convert hex to Bech32
string hex = "0x0102030405060708091011121314151617181920";
string bech32 = Bech32.HexToBech32(hex);
// Result: "init1qypqxpq9qcrsszg2pgxysph3jf3j35z8"

// With custom prefix
string customBech32 = Bech32.HexToBech32(hex, "eth");
```

**Breakdown**:
```csharp
public static string HexToBech32(string hexAddress, string prefix = "init")

// Parameters:
// - hexAddress: Hex address with or without "0x" prefix
// - prefix: Bech32 prefix (default: "init")

// Returns:
// - Bech32-encoded address with checksum

// Exceptions:
// - ArgumentException: If hex is empty, not 40 characters, or contains non-hex characters

// Notes:
// - Accepts hex with or without "0x" prefix
// - Requires exactly 40 hex characters (20 bytes)
```

---

### Bech32ToBytes

**Purpose**: Decodes a Bech32 address to raw bytes with prefix validation.

**Example**:
```csharp
// Decode Bech32 to bytes
string bech32 = "init1qypqxpq9qcrsszg2pgxysph3jf3j35z8";
byte[] data = Bech32.Bech32ToBytes(bech32, "init");
// Result: byte array of decoded data
```

**Breakdown**:
```csharp
public static byte[] Bech32ToBytes(string bech32Address, string expectedPrefix)

// Parameters:
// - bech32Address: Valid Bech32 address
// - expectedPrefix: Expected prefix for validation

// Returns:
// - Decoded byte array (checksum removed)

// Exceptions:
// - ArgumentException: If address is empty, invalid format, wrong prefix,
//   checksum fails, or contains invalid characters

// Notes:
// - Validates prefix matches expectedPrefix (case-insensitive)
// - Verifies checksum
// - Returns data without checksum bytes
```

---

## Blake2b Class

Static class providing Blake2b cryptographic hashing.

### ComputeHash

**Purpose**: Computes a Blake2b hash of the input data with configurable output length.

**Example**:
```csharp
// Compute 32-byte hash (default)
byte[] input = Encoding.UTF8.GetBytes("Hello World");
byte[] hash = Blake2b.ComputeHash(input);

// Compute 64-byte hash
byte[] longHash = Blake2b.ComputeHash(input, 64);

// Compute 16-byte hash
byte[] shortHash = Blake2b.ComputeHash(input, 16);
```

**Breakdown**:
```csharp
public static byte[] ComputeHash(byte[] input, int outLen = 32)

// Parameters:
// - input: Byte array to hash
// - outLen: Output hash length in bytes (default: 32)

// Returns:
// - Blake2b hash as byte array of specified length

// Notes:
// - Blake2b is faster than SHA-256 and more secure than MD5
// - Supports variable output lengths (1-64 bytes typically)
// - Uses 12 rounds of compression
// - Result is in little-endian format
```

---
