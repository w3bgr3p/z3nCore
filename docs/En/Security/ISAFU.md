# ISAFU (Security & Authentication Functions Utility)

This module provides security and authentication utilities for encrypting/decrypting sensitive data in ZennoPoster projects.

---

## FunctionStorage Class

Static class providing a thread-safe storage for security-related functions.

### Functions Field

**Purpose**: Stores security function delegates in a thread-safe concurrent dictionary.

**Example**:
```csharp
// Access stored functions
if (FunctionStorage.Functions.ContainsKey("SAFU_Encode"))
{
    var encodeFunc = (Func<IZennoPosterProjectModel, string, bool, string>)
        FunctionStorage.Functions["SAFU_Encode"];
}

// Add custom function
FunctionStorage.Functions.TryAdd("CustomFunc", myCustomFunction);
```

**Breakdown**:
```csharp
public static ConcurrentDictionary<string, object> Functions

// Type: ConcurrentDictionary<string, object>
// - Key: Function name (e.g., "SAFU_Encode", "SAFU_Decode")
// - Value: Function delegate (stored as object, needs casting)

// Notes:
// - Thread-safe for concurrent access
// - Used by SAFU class to store encryption/decryption functions
// - Functions can be replaced with custom implementations
```

---

## ISAFU Interface

Public interface defining the contract for security and authentication functions.

**Purpose**: Defines methods for encryption, decryption, and hardware-based password generation.

**Example**:
```csharp
// Implement custom SAFU
public class CustomSAFU : ISAFU
{
    public string Encode(IZennoPosterProjectModel project, string toEncrypt, bool log)
    {
        // Custom encryption logic
        return encrypted;
    }

    public string EncodeV2(IZennoPosterProjectModel project, string toEncrypt, bool log)
    {
        // Enhanced encryption logic
        return encrypted;
    }

    public string Decode(IZennoPosterProjectModel project, string toDecrypt, bool log)
    {
        // Custom decryption logic
        return decrypted;
    }

    public string HWPass(IZennoPosterProjectModel project, bool v2)
    {
        // Hardware-based password
        return password;
    }
}
```

**Breakdown**:
```csharp
public interface ISAFU

// Methods:
// - Encode: Encrypts a string
// - EncodeV2: Enhanced version of encryption
// - Decode: Decrypts a string
// - HWPass: Generates hardware-based password

// Notes:
// - Implemented by SimpleSAFU internally
// - Can be implemented for custom security logic
// - All methods accept IZennoPosterProjectModel for context
```

---

## SAFU Class

Static class providing secure encryption/decryption for sensitive project data.

### Initialize

**Purpose**: Initializes the SAFU system with default fallback functions if custom implementations are not registered.

**Example**:
```csharp
// Initialize SAFU (typically called at project start)
SAFU.Initialize(project);

// After initialization, you can use Encode/Decode methods
string encrypted = SAFU.Encode(project, "sensitive data");
```

**Breakdown**:
```csharp
public static void Initialize(IZennoPosterProjectModel project)

// Parameters:
// - project: ZennoPoster project model for logging

// Notes:
// - Checks if SAFU functions are already registered in FunctionStorage
// - If not registered, loads SimpleSAFU as fallback with warning
// - Logs warning: "⚠️ SAFU fallback: script kiddie security level!"
// - Registers: SAFU_Encode, SAFU_Decode, SAFU_HWPass functions
// - Safe to call multiple times (only initializes once)
```

---

### Encode

**Purpose**: Encrypts a string using the project's PIN configuration and registered SAFU function.

**Example**:
```csharp
// Encrypt sensitive data
project.Variables["cfgPin"].Value = "mySecretPin123";
string plainText = "myPassword123";
string encrypted = SAFU.Encode(project, plainText);

// With logging enabled
string encryptedWithLog = SAFU.Encode(project, plainText, true);

// If cfgPin is empty, returns original string
project.Variables["cfgPin"].Value = "";
string notEncrypted = SAFU.Encode(project, plainText); // Returns plainText
```

**Breakdown**:
```csharp
public static string Encode(IZennoPosterProjectModel project, string toEncrypt, bool log = false)

// Parameters:
// - project: ZennoPoster project model (accesses cfgPin variable)
// - toEncrypt: Plain text string to encrypt
// - log: Enable logging for debugging (default: false)

// Returns:
// - Encrypted string
// - Returns original string if cfgPin is empty

// Notes:
// - Uses AES encryption with MD5-hashed PIN as key
// - Requires cfgPin variable to be set in project
// - If cfgPin is empty, no encryption is performed (returns input)
```

---

### EncodeV2

**Purpose**: Enhanced encryption using the V2 function if available, falls back to standard Encode.

**Example**:
```csharp
// Encrypt with V2 (if available)
project.Variables["cfgPin"].Value = "mySecretPin123";
string encrypted = SAFU.EncodeV2(project, "sensitive data");

// If EncodeV2 not registered, uses Encode automatically
// Logs warning: "EncodeV2 not available, using fallback"
```

**Breakdown**:
```csharp
public static string EncodeV2(IZennoPosterProjectModel project, string toEncrypt, bool log = false)

// Parameters:
// - project: ZennoPoster project model (accesses cfgPin variable)
// - toEncrypt: Plain text string to encrypt
// - log: Enable logging for debugging (default: false)

// Returns:
// - Encrypted string using V2 method
// - Falls back to standard Encode if V2 not available

// Notes:
// - Checks for SAFU_EncodeV2 function in FunctionStorage
// - If not found, logs warning and uses SAFU_Encode
// - Provides enhanced security if V2 implementation is registered
```

---

### Decode

**Purpose**: Decrypts a string encrypted with SAFU.Encode or SAFU.EncodeV2.

**Example**:
```csharp
// Decrypt data
project.Variables["cfgPin"].Value = "mySecretPin123";
string encrypted = "AB12CD34..."; // encrypted string
string decrypted = SAFU.Decode(project, encrypted);

// With logging
string decryptedWithLog = SAFU.Decode(project, encrypted, true);

// If cfgPin is empty, returns original string
project.Variables["cfgPin"].Value = "";
string notDecrypted = SAFU.Decode(project, encrypted); // Returns encrypted
```

**Breakdown**:
```csharp
public static string Decode(IZennoPosterProjectModel project, string toDecrypt, bool log = false)

// Parameters:
// - project: ZennoPoster project model (accesses cfgPin variable)
// - toDecrypt: Encrypted string to decrypt
// - log: Enable logging for debugging (default: false)

// Returns:
// - Decrypted plain text string
// - Returns original string if cfgPin is empty

// Exceptions:
// - May throw if encrypted string is corrupted or key is wrong
// - SimpleSAFU logs error and re-throws exception

// Notes:
// - Must use same cfgPin value as encryption
// - Uses AES decryption with MD5-hashed PIN
```

---

### HWPass

**Purpose**: Generates a hardware-based password using motherboard serial number and project data.

**Example**:
```csharp
// Generate hardware-based password
project.Variables["acc0"].Value = "myAccountID";
string hwPassword = SAFU.HWPass(project);

// V2 mode (default)
string hwPasswordV2 = SAFU.HWPass(project, true);

// V1 mode
string hwPasswordV1 = SAFU.HWPass(project, false);
```

**Breakdown**:
```csharp
public static string HWPass(IZennoPosterProjectModel project, bool v2 = true)

// Parameters:
// - project: ZennoPoster project model (accesses acc0 variable)
// - v2: Use version 2 of password generation (default: true)

// Returns:
// - Hardware-based password string
// - Combines motherboard serial + acc0 variable value

// Notes:
// - Password is unique to hardware (motherboard serial)
// - Requires WMI access to read Win32_BaseBoard serial
// - Uses project.Variables["acc0"] for additional entropy
// - Password changes if hardware or acc0 changes
// - SimpleSAFU implementation: serial + acc0 concatenation
```

---

## Security Considerations

1. **PIN Security**: The `cfgPin` variable is the master key for all encryption. Protect it carefully.

2. **AES ECB Mode**: SimpleSAFU uses ECB mode, which is less secure than CBC/GCM for large data. Consider custom implementation for enhanced security.

3. **MD5 Hashing**: MD5 is used for key derivation. While acceptable for this use case, consider SHA-256 for enhanced security.

4. **Hardware Binding**: HWPass ties data to specific hardware. Data cannot be decrypted on different machines.

5. **Custom Implementation**: Replace SimpleSAFU by registering custom functions in FunctionStorage before calling SAFU.Initialize().

---
