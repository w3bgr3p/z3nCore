# OTP Class

Static class for generating One-Time Passwords (OTP) using TOTP algorithm.

---

## Offline

### Purpose
Generates a TOTP code from a secret key, with optional waiting for a fresh code if time remaining is low.

### Example
```csharp
using z3nCore;

// Generate OTP from secret key
string secret = "JBSWY3DPEHPK3PXP";
string code = OTP.Offline(secret);
// Result: "123456" (6-digit code)

// Wait for fresh code if less than 10 seconds remaining
string freshCode = OTP.Offline(secret, waitIfTimeLess: 10);

// Use in automation
project.SendInfoToLog($"OTP Code: {code}");
instance.ActiveTab.FillTextBox("input[name='otp']", code);
```

### Breakdown
```csharp
public static string Offline(
    string keyString,           // Base32-encoded secret key
    int waitIfTimeLess = 5)    // Wait for new code if remaining seconds < this value

// Returns: 6-digit TOTP code as string

// Throws:
// - Exception: If keyString is null or empty

// How it works:
// 1. Decodes Base32 secret key
// 2. Generates current TOTP code
// 3. Checks remaining seconds in current time window
// 4. If remaining seconds <= waitIfTimeLess:
//    - Waits for next time window
//    - Generates fresh code
// 5. Returns the code

// Notes:
// - Uses 30-second time windows (standard TOTP)
// - Base32 encoding required for secret key
// - Code refreshes every 30 seconds
// - waitIfTimeLess prevents using codes about to expire
```

---

## FirstMail

### Purpose
Retrieves OTP code from FirstMail email service.

### Example
```csharp
using z3nCore;

// Get OTP from email
string email = "user@firstmail.ltd";
string code = OTP.FirstMail(project, email);

// Use the code
if (!string.IsNullOrEmpty(code))
{
    project.SendInfoToLog($"Received OTP: {code}");
    instance.ActiveTab.FillTextBox("#otp-input", code);
}
```

### Breakdown
```csharp
public static string FirstMail(
    IZennoPosterProjectModel project,  // Project instance for logging
    string email)                      // Email address to check

// Returns: OTP code extracted from email
// Returns: null or empty if no OTP found

// Throws:
// - Exception: If email parameter is null or empty

// Notes:
// - Requires FirstMail class implementation
// - Email must be accessible via FirstMail service
// - Automatically parses OTP from email content
```

---
