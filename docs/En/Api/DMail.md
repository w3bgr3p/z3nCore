# DMail Class Documentation

## Overview
The `DMail` class provides integration with DMail decentralized email service for reading, managing, and authenticating with Web3 email accounts.

---

## Constructor

### `DMail(IZennoPosterProjectModel project, string key = null, bool log = false)`

**Purpose:** Initializes the DMail client with Web3 authentication.

**Example:**
```csharp
var dmail = new DMail(project, log: true);
dynamic mails = dmail.GetAll();
```

**Breakdown:**
```csharp
var dmail = new DMail(
    project,  // IZennoPosterProjectModel - project instance
    null,     // string - optional EVM private key (loaded from DB if null)
    true      // bool - enable logging
);
// Note: Automatically authenticates using EVM wallet signature
```

---

## Public Methods

### `CheckAuth()`

**Purpose:** Checks and establishes authentication with DMail service.

**Example:**
```csharp
var dmail = new DMail(project);
dmail.CheckAuth();
// Authentication tokens are now available
```

**Breakdown:**
```csharp
dmail.CheckAuth();
// Checks for existing auth tokens in project variables
// If not found, performs authentication using EVM wallet
// Sets headers with dm-encstring and dm-pid tokens
```

---

### `GetAll()`

**Purpose:** Retrieves all emails from inbox with their content.

**Example:**
```csharp
var dmail = new DMail(project);
dynamic allMails = dmail.GetAll();

foreach (var mail in allMails)
{
    Console.WriteLine($"From: {mail.dm_salias}");
    Console.WriteLine($"Subject: {mail.content.subject}");
}
```

**Breakdown:**
```csharp
dynamic mailList = dmail.GetAll();
// Returns: dynamic - array of email objects with full content
// Each email contains: dm_salias (sender), dm_date, dm_scid, dm_smid, content (subject, html)
// Default page size: 20 emails
```

---

### `ReadMsg(int index = 0, dynamic mail = null, bool markAsRead = true, bool trash = true)`

**Purpose:** Reads a specific email message and optionally marks it as read or moves to trash.

**Example:**
```csharp
var dmail = new DMail(project);
var mailList = dmail.GetAll();
Dictionary<string, string> message = dmail.ReadMsg(
    0,           // Read first email
    mailList,    // Mail list
    true,        // Mark as read
    false        // Don't trash
);

Console.WriteLine($"Sender: {message["sender"]}");
Console.WriteLine($"Subject: {message["subj"]}");
Console.WriteLine($"Body: {message["html"]}");
```

**Breakdown:**
```csharp
Dictionary<string, string> email = dmail.ReadMsg(
    0,        // int - email index in list (0-based)
    null,     // dynamic - mail list (auto-fetched if null)
    true,     // bool - mark as read after reading
    false     // bool - move to trash after reading
);
// Returns: Dictionary with keys: sender, date, subj, html, dm_scid, dm_smid
```

---

### `GetUnread(bool parse = false, string key = null)`

**Purpose:** Gets count of unread emails or specific mail statistics.

**Example:**
```csharp
var dmail = new DMail(project);
string unreadCount = dmail.GetUnread(key: "mail_unread_count");
Console.WriteLine($"Unread emails: {unreadCount}");

// Or get full JSON
string fullStats = dmail.GetUnread();
```

**Breakdown:**
```csharp
string unreadInfo = dmail.GetUnread(
    false,               // bool - parse to project.Json
    "mail_unread_count"  // string - specific key to extract
);
// Returns: string - unread count or full JSON response
// Available keys: mail_unread_count, message_unread_count, not_read_count, used_total_size
```

---

### `Trash(int index = 0, string dm_scid = null, string dm_smid = null)`

**Purpose:** Moves an email to trash.

**Example:**
```csharp
var dmail = new DMail(project);
dmail.Trash(0);  // Trash first email

// Or trash specific email by ID
dmail.Trash(dm_scid: "scid123", dm_smid: "smid456");
```

**Breakdown:**
```csharp
dmail.Trash(
    0,           // int - email index
    "scid123",   // string - optional specific dm_scid
    "smid456"    // string - optional specific dm_smid
);
// Moves email from inbox to trash folder
// If IDs not provided, fetches from ReadMsg at specified index
```

---

### `MarkAsRead(int index = 0, string dm_scid = null, string dm_smid = null)`

**Purpose:** Marks an email as read.

**Example:**
```csharp
var dmail = new DMail(project);
dmail.MarkAsRead(0);  // Mark first email as read

// Or mark specific email
dmail.MarkAsRead(dm_scid: "scid123", dm_smid: "smid456");
```

**Breakdown:**
```csharp
dmail.MarkAsRead(
    0,           // int - email index
    "scid123",   // string - optional specific dm_scid
    "smid456"    // string - optional specific dm_smid
);
// Sets dm_is_read flag to 1 for the specified email
```

---

## Authentication Flow

The class uses Web3 authentication with the following process:

1. **Get Nonce:** Requests a unique nonce from DMail API
2. **Sign Message:** Signs a message with EVM wallet including:
   - App name: "dmail"
   - Wallet address
   - Nonce
   - Current timestamp
3. **Verify Signature:** Sends signature to DMail for verification
4. **Receive Tokens:** Gets authentication tokens (encstring and pid)
5. **Set Headers:** Stores tokens in headers for subsequent requests

---

## Email Structure

Each email object contains:

```csharp
{
    "sender": "user@dmail.ai",              // Sender email
    "date": "2025-01-15T10:30:00Z",        // Email date
    "subj": "Email Subject",                // Subject line
    "html": "<p>Email body</p>",            // HTML content
    "dm_scid": "conversation_id",           // Conversation ID
    "dm_smid": "message_id"                 // Message ID
}
```

---

## Notes

- Uses Ethereum message signing for authentication (EIP-191)
- Authentication tokens stored in project variables for reuse
- Supports automatic re-authentication if tokens expire
- All requests use NetHttp class for HTTP operations
- Private key loaded from database if not provided in constructor
- Integrates with Internet Computer Protocol (ICP) backend
