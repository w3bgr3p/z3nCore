# FirstMail Class Documentation

## Overview
The `FirstMail` class provides integration with FirstMail API service for temporary email management, including reading emails, extracting OTPs, and finding verification links.

---

## Constructors

### `FirstMail(IZennoPosterProjectModel project, bool log = false)`

**Purpose:** Initializes FirstMail client with credentials from database.

**Example:**
```csharp
var mail = new FirstMail(project, log: true);
string message = mail.GetOne("test@example.com");
```

**Breakdown:**
```csharp
var mail = new FirstMail(
    project,  // IZennoPosterProjectModel - project instance
    true      // bool - enable logging
);
// Note: API key, login, and password loaded from database (_api table)
```

---

### `FirstMail(IZennoPosterProjectModel project, string mail, string password, bool log = false)`

**Purpose:** Initializes FirstMail client with specific email credentials.

**Example:**
```csharp
var mail = new FirstMail(
    project,
    "test@firstmail.ltd",
    "password123",
    log: true
);
```

**Breakdown:**
```csharp
var mail = new FirstMail(
    project,            // IZennoPosterProjectModel - project instance
    "test@first.com",   // string - email address
    "password",         // string - email password
    true                // bool - enable logging
);
// Note: Credentials are URI-encoded automatically
```

---

## Public Methods

### `Delete(string email, bool seen = false)`

**Purpose:** Deletes emails from the mailbox.

**Example:**
```csharp
var mail = new FirstMail(project);
string result = mail.Delete("test@firstmail.ltd", seen: true);
```

**Breakdown:**
```csharp
string deleteResult = mail.Delete(
    "test@firstmail.ltd",  // string - email address
    true                    // bool - delete only seen/read emails
);
// Returns: string - API response (JSON)
// Note: If seen=false, deletes all emails
```

---

### `GetOne(string email)`

**Purpose:** Retrieves the most recent email message.

**Example:**
```csharp
var mail = new FirstMail(project);
string result = mail.GetOne("test@firstmail.ltd");
project.Json.FromString(result);

string sender = project.Json.from;
string subject = project.Json.subject;
string text = project.Json.text;
```

**Breakdown:**
```csharp
string latestEmail = mail.GetOne(
    "test@firstmail.ltd"  // string - email address
);
// Returns: string - JSON response with email data
// Response fields: from, to, subject, text, html, date
```

---

### `GetAll(string email)`

**Purpose:** Retrieves all messages from the mailbox.

**Example:**
```csharp
var mail = new FirstMail(project);
string allEmails = mail.GetAll("test@firstmail.ltd");
project.Json.FromString(allEmails);

foreach (var email in project.Json)
{
    Console.WriteLine($"Subject: {email.subject}");
}
```

**Breakdown:**
```csharp
string allMessages = mail.GetAll(
    "test@firstmail.ltd"  // string - email address
);
// Returns: string - JSON array of all email messages
```

---

### `GetOTP(string email)`

**Purpose:** Extracts a 6-digit OTP code from the latest email.

**Example:**
```csharp
var mail = new FirstMail(project);
string otp = mail.GetOTP("test@firstmail.ltd");
Console.WriteLine($"OTP Code: {otp}");  // Output: "123456"
```

**Breakdown:**
```csharp
string otpCode = mail.GetOTP(
    "test@firstmail.ltd"  // string - email address
);
// Returns: string - 6-digit OTP code
// Searches in order: subject → text → html
// Throws: Exception - if email not found or OTP not found
```

---

### `GetLink(string email)`

**Purpose:** Extracts the first HTTP/HTTPS link from the latest email.

**Example:**
```csharp
var mail = new FirstMail(project);
string verificationLink = mail.GetLink("test@firstmail.ltd");
Console.WriteLine($"Link: {verificationLink}");
// Output: https://example.com/verify?token=abc123
```

**Breakdown:**
```csharp
string extractedLink = mail.GetLink(
    "test@firstmail.ltd"  // string - email address
);
// Returns: string - first valid HTTP/HTTPS URL found
// Throws: Exception - if email doesn't match or no link found
```

---

## Extension Methods

### `Otp(this IZennoPosterProjectModel project, string source)`

**Purpose:** Extension method to extract OTP from email or offline source.

**Example:**
```csharp
// From email
string otp = project.Otp("test@firstmail.ltd");

// From text (offline extraction)
string otp = project.Otp("Your code is 123456");
```

**Breakdown:**
```csharp
string otpCode = project.Otp(
    "test@example.com"  // string - email or text containing OTP
);
// Returns: string - 6-digit OTP code
// Note: Routes to FirstMail if source contains @, otherwise uses offline extraction
```

---

## API Endpoints

The class uses these FirstMail API endpoints:

| Method | Endpoint | Purpose |
|--------|----------|---------|
| DELETE | /v1/mail/delete | Delete emails |
| GET | /v1/get/messages | Get all messages |
| GET | /v1/mail/one | Get latest message |

---

## Email Response Structure

```json
{
  "from": "sender@example.com",
  "to": ["recipient@firstmail.ltd"],
  "subject": "Verification Code",
  "text": "Your code is 123456",
  "html": "<p>Your code is 123456</p>",
  "date": "2025-01-15T10:30:00Z"
}
```

---

## Notes

- API key stored in database table `_api` with `id = 'firstmail'`
- All credentials are automatically URI-encoded
- Supports proxy configuration from database
- OTP extraction uses regex pattern `\b\d{6}\b`
- Link extraction supports both http:// and https://
- Automatically populates `project.Json` for easy data access
- Headers include API key in `X-API-KEY` format
