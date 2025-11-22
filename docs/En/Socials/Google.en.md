# Google Class Documentation

## Class: Google

### Constructor

#### Google(IZennoPosterProjectModel project, Instance instance, bool log = false)

**Purpose**: Initializes a new Google automation instance for handling Google account operations.

**Example**:
```csharp
var google = new Google(project, instance, log: true);
```

**Breakdown**:
```csharp
// Parameters:
// - project: IZennoPosterProjectModel - Project model for database operations
// - instance: Instance - Browser instance for automation
// - log: bool - Enable detailed logging (default: false)
// Returns: Google instance
// Note: Automatically loads credentials from "_google" table on initialization
```

---

## Authentication Methods

### Load(bool log = false, bool cookieBackup = true)

**Purpose**: Performs complete Google account authentication with automatic state handling.

**Example**:
```csharp
string result = google.Load(cookieBackup: true);
if (result == "ok") {
    project.SendInfoToLog("Google authenticated successfully");
}
```

**Breakdown**:
```csharp
// Parameters:
// - log: bool - Enable logging (default: false)
// - cookieBackup: bool - Save cookies to database on success (default: true)
// Returns: string - Final authentication state ("ok", "phoneVerify", "badBrowser", etc.)
// Throws: Exception for critical errors (CAPTCHA, wrong account, disabled account)
// Note: Handles login, password, OTP, recovery phone prompts automatically
// Navigates to myaccount.google.com if not already on Google domain
```

### State(bool log = false)

**Purpose**: Detects current Google account page state and authentication status.

**Example**:
```csharp
string state = google.State();
if (state == "ok") {
    // Account is signed in and verified
}
```

**Breakdown**:
```csharp
// Parameters:
// - log: bool - Enable logging (default: false)
// Returns: string - Current page state ("ok", "inputLogin", "inputPassword", "inputOtp", "CAPTCHA", "Disabled", etc.)
// Note: Validates that logged-in account matches expected credentials
// Returns "!WrongAcc" if different account is logged in
```

### GAuth(bool log = false)

**Purpose**: Handles Google OAuth authentication on third-party sites using existing Google session.

**Example**:
```csharp
string result = google.GAuth();
project.SendInfoToLog($"OAuth result: {result}");
```

**Breakdown**:
```csharp
// Parameters:
// - log: bool - Enable logging (default: false)
// Returns: string - Authentication result ("SUCCESS with continue", "SUCCESS. without confirmation", "FAIL. Wrong account", "FAIL. No loggined Users Found")
// Note: Clicks on account container if correct user found
// Handles OTP if required, clicks Continue button
// Clears cookies if wrong account detected
```

---

## Data Management

### SaveCookies()

**Purpose**: Saves Google session cookies to database for persistence across sessions.

**Example**:
```csharp
google.SaveCookies();
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: void
// Note: Navigates to YouTube first to ensure all Google cookies are set
// Saves cookies for all Google domains (using "." wildcard)
// Updates "_google" table with status and cookies
```

### ParseSecurity()

**Purpose**: Extracts and stores Google account security settings information.

**Example**:
```csharp
google.ParseSecurity();
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: void
// Note: Navigates to https://myaccount.google.com/security if needed
// Extracts status of: 2-Step Verification, Password, Skip password, Authenticator, Recovery phone, Recovery email, Backup codes
// Saves formatted security info to "__google" table
```
