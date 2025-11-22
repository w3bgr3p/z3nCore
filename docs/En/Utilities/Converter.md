# Converter Class

Static class for converting data between different formats (hex, base64, bech32, bytes, text).

---

## ConvertFormat

### Purpose
Converts data from one format to another. Supports hex, base64, bech32, bytes, and text formats with automatic validation and error handling.

### Example
```csharp
using z3nCore.Utilities;

// Convert hex to base64
string hex = "0x48656c6c6f";
string base64 = Converer.ConvertFormat(project, hex, "hex", "base64", log: true);
// Result: "SGVsbG8="

// Convert text to hex
string text = "Hello";
string hexResult = Converer.ConvertFormat(project, text, "text", "hex");
// Result: "0x48656c6c6f"

// Convert bech32 address to hex
string bech32Addr = "init1qqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqq";
string hexAddr = Converer.ConvertFormat(project, bech32Addr, "bech32", "hex");
```

### Breakdown
```csharp
public static string ConvertFormat(
    IZennoPosterProjectModel project,  // Project instance for logging
    string toProcess,                  // Input data to convert
    string input,                      // Input format: "hex", "base64", "bech32", "bytes", "text"
    string output,                     // Output format: "hex", "base64", "bech32", "bytes", "text"
    bool log = false)                  // Enable logging (default: false)

// Returns: Converted string in the specified output format
// Returns: null if conversion fails

// Exceptions handled internally:
// - ArgumentException: Unsupported format or invalid input data
// - Exception: General conversion errors (logged to project)

// Notes:
// - Hex strings can start with "0x" (optional)
// - Bech32 format requires exactly 32 bytes and "init" prefix
// - All errors are logged to project, returns null on failure
```

---
