# GitHub Class Documentation

## Class: GitHub

### Constructor

#### GitHub(IZennoPosterProjectModel project, Instance instance, bool log = false)

**Purpose**: Initializes a new GitHub automation instance with project context and browser instance.

**Example**:
```csharp
var github = new GitHub(project, instance, log: true);
```

**Breakdown**:
```csharp
// Parameters:
// - project: IZennoPosterProjectModel - Project model for database operations
// - instance: Instance - Browser instance for automation
// - log: bool - Enable logging (default: false)
// Returns: GitHub instance
// Note: Automatically loads credentials from "_github" table
```

---

## Credential Management

### LoadCreds()

**Purpose**: Loads GitHub credentials from the database into instance variables.

**Example**:
```csharp
github.LoadCreds();
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: void
// Throws: Exception if login or password is empty
// Note: Loads status, login, password, 2FA secret, email, and cookies from "_github" table
// Also populates project variables with loaded credentials
```

---

## Authentication Methods

### Go()

**Purpose**: Navigates to GitHub login page and handles cookie consent.

**Example**:
```csharp
github.Go();
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: void
// Note: Navigates to https://github.com/login
// Automatically clicks "Accept" button if present (2 second timeout)
```

### InputCreds()

**Purpose**: Inputs login credentials into GitHub login form.

**Example**:
```csharp
github.InputCreds();
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: void
// Throws: Exception if alert/error message appears
// Note: Enters email and password, clicks submit button
// Automatically handles 2FA input if required
```

### Load()

**Purpose**: Performs complete GitHub authentication with automatic state handling and validation.

**Example**:
```csharp
string currentUser = github.Load();
project.SendInfoToLog($"Logged in as: {currentUser}");
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: string - Current logged-in username
// Throws: Exception if wrong account detected or credentials invalid
// Note: Navigates to login, inputs credentials, handles 2FA verification
// Validates logged-in account matches expected username
// Automatically saves cookies on successful login
```

### Verify2fa()

**Purpose**: Handles 2FA verification process when prompted during login.

**Example**:
```csharp
github.Verify2fa();
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: void
// Note: Clicks "Verify 2FA now" button, waits 20 seconds
// Generates and inputs OTP code from 2FA secret
// Clicks primary button and "Done" button to complete verification
```

### Current()

**Purpose**: Retrieves the currently logged-in GitHub username.

**Example**:
```csharp
string username = github.Current();
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: string - Current username
// Note: Clicks avatar, gets username from user navigation area
// Returns to home page by clicking GitHub logo
```

---

## Account Management

### ChangePass(string password = null)

**Purpose**: Initiates password reset process via email.

**Example**:
```csharp
github.ChangePass();
```

**Breakdown**:
```csharp
// Parameters:
// - password: string - New password (optional, uses project variable if null)
// Returns: void
// Note: Requests password reset, solves captcha if needed
// Retrieves reset link from email, sets new password
// Handles 2FA during password change if enabled
```

### SaveCookies()

**Purpose**: Saves current browser cookies to database for session persistence.

**Example**:
```csharp
github.SaveCookies();
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: void
// Note: Uses Cookies helper class to save cookies to project database
```
