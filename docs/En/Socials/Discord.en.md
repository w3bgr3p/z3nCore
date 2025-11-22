# Discord Class Documentation

## Class: Discord

### Constructor

#### Discord(IZennoPosterProjectModel project, Instance instance, bool log = false)

**Purpose**: Initializes a new Discord automation instance with project context, browser instance, and optional logging.

**Example**:
```csharp
var discord = new Discord(project, instance, log: true);
```

**Breakdown**:
```csharp
// Parameters:
// - project: IZennoPosterProjectModel - The ZennoPoster project model for database and macro operations
// - instance: Instance - Browser instance for web automation
// - log: bool - Enable detailed logging (default: false)
// Returns: Discord instance
// Note: Automatically loads credentials from database on initialization
```

---

## Authentication Methods

### GetState(bool log = false)

**Purpose**: Detects the current state of the Discord page (logged in, captcha, 2FA required, etc.).

**Example**:
```csharp
string state = discord.GetState();
if (state == "logged") {
    // Proceed with actions
}
```

**Breakdown**:
```csharp
// Parameters:
// - log: bool - Enable logging for this operation (default: false)
// Returns: string - Current page state ("logged", "capctha", "input_otp", "input_credentials", "appDetected", etc.)
// Note: Uses element detection to determine page state
```

### Load(bool log = false)

**Purpose**: Performs complete Discord authentication using token or credentials with automatic state handling.

**Example**:
```csharp
string result = discord.Load();
if (result == "logged") {
    project.SendInfoToLog("Discord authenticated successfully");
}
```

**Breakdown**:
```csharp
// Parameters:
// - log: bool - Enable logging (default: false)
// Returns: string - Final authentication state
// Throws: Exception if authentication fails
// Note: Attempts token authentication first, falls back to credentials
// Handles captcha, 2FA, and other authentication challenges automatically
```

### Auth()

**Purpose**: Performs authorization action by scrolling and clicking the authorize button on Discord OAuth pages.

**Example**:
```csharp
discord.Auth();
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: void
// Note: Used for OAuth authorization flows
// Scrolls page until authorization button is visible and clicks it
```

---

## Server & Role Methods

### Servers(bool toDb = false, bool log = false)

**Purpose**: Retrieves list of all Discord servers the account has joined, including servers in folders.

**Example**:
```csharp
List<string> serverList = discord.Servers(toDb: true);
foreach (var server in serverList) {
    project.SendInfoToLog($"Server: {server}");
}
```

**Breakdown**:
```csharp
// Parameters:
// - toDb: bool - Save server list to database (default: false)
// - log: bool - Enable logging (default: false)
// Returns: List<string> - List of server names
// Note: Expands folders to get all servers, saves to "__discord" table if toDb is true
```

### GetRoles(string gmChannelLink, string gmMessage = "gm", bool log = false)

**Purpose**: Extracts user roles from a Discord server by triggering a message and reading the user popup.

**Example**:
```csharp
List<string> roles = discord.GetRoles("https://discord.com/channels/123/456", "hello");
```

**Breakdown**:
```csharp
// Parameters:
// - gmChannelLink: string - Discord channel URL to send message
// - gmMessage: string - Message to send (default: "gm")
// - log: bool - Enable logging (default: false)
// Returns: List<string> - List of role names
// Note: Sends message if username not visible, clicks username to open popup
```

### GM(string gmChannelLink, string message = "gm")

**Purpose**: Sends a message to a specified Discord channel.

**Example**:
```csharp
discord.GM("https://discord.com/channels/123/456", "Hello everyone!");
```

**Breakdown**:
```csharp
// Parameters:
// - gmChannelLink: string - Full Discord channel URL
// - message: string - Message text to send (default: "gm")
// Returns: void
// Throws: Exception if channel not accessible or no permissions
// Note: Navigates to channel, checks permissions, sends message
```

### UpdateServerInfo(string gmChannelLink)

**Purpose**: Updates database with server name and user roles from a channel.

**Example**:
```csharp
discord.UpdateServerInfo("https://discord.com/channels/123/456");
```

**Breakdown**:
```csharp
// Parameters:
// - gmChannelLink: string - Discord channel URL
// Returns: void
// Note: Creates new column with server name and stores comma-separated roles
// Combines GetRoles() and database update operations
```
