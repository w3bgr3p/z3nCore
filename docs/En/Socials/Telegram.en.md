# Telegram Class Documentation

## Class: Telegram

### Constructor

#### Telegram(IZennoPosterProjectModel project, bool log = false)

**Purpose**: Initializes a new Telegram bot instance for sending notifications and reports.

**Example**:
```csharp
var telegram = new Telegram(project, log: true);
```

**Breakdown**:
```csharp
// Parameters:
// - project: IZennoPosterProjectModel - Project model for accessing variables and logging
// - log: bool - Enable detailed logging (default: false)
// Returns: Telegram instance
// Note: Automatically loads bot token, group ID, and topic ID from project variables
// Does not require Instance parameter (uses HTTP API instead of browser automation)
```

---

## Notification Methods

### Report()

**Purpose**: Sends execution report to Telegram group with success or failure details.

**Example**:
```csharp
telegram.Report();
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: void
// Note: Checks "failReport" project variable for failure message
// If failReport is empty, sends success message with project name and account info
// Sends formatted message to specified Telegram group/topic
// Uses MarkdownV2 formatting for message text
// Logs final execution time and status to ZennoPoster log with colors (green for success, orange for failure)
```
