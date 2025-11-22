# Reporter Class

Handles creation, formatting, and delivery of error and success reports across multiple channels (logs, Telegram, database).

---

## Constructor

### `Reporter(IZennoPosterProjectModel project, Instance instance)`

**Purpose**
Initializes a new Reporter instance with project and browser instance context.

**Example**
```csharp
// Create a Reporter instance
var reporter = new Reporter(project, instance);
```

**Breakdown**
```csharp
// Parameters:
// - project: IZennoPosterProjectModel instance for project operations
// - instance: Browser instance for screenshot capture and URL tracking
var reporter = new Reporter(project, instance);

// Returns: Instance of Reporter class
// Exceptions: ArgumentNullException if project or instance is null
```

---

## Public Methods

### `ReportError(bool toLog = true, bool toTelegram = false, bool toDb = true, bool screenshot = false)`

**Purpose**
Creates and sends a formatted error report to specified destinations (log, Telegram, database) with optional screenshot capture.

**Example**
```csharp
var reporter = new Reporter(project, instance);

// Report error to log and database only
reporter.ReportError(toLog: true, toDb: true);

// Report error to all channels with screenshot
reporter.ReportError(toLog: true, toTelegram: true, toDb: true, screenshot: true);

// Report error to Telegram only
reporter.ReportError(toLog: false, toTelegram: true, toDb: false);
```

**Breakdown**
```csharp
// Parameters:
// - toLog: Send error report to project log (default: true)
// - toTelegram: Send error report to Telegram (default: false)
// - toDb: Update database with error info (default: true)
// - screenshot: Capture and save screenshot with watermark (default: false)
string errorReport = reporter.ReportError(
    toLog: true,        // Log to project
    toTelegram: true,   // Send to Telegram bot
    toDb: true,         // Update account status in DB
    screenshot: true    // Save screenshot to .failed folder
);

// Returns: string containing the formatted log report
// Side effects:
// - Logs error with orange color to project log (if toLog=true)
// - Sends Telegram message with Markdown formatting (if toTelegram=true)
// - Updates database: status='dropped', last='{error details}' (if toDb=true)
// - Saves screenshot to: {project.Path}/.failed/{projectName}/[timestamp].jpg (if screenshot=true)
// Error data includes:
//   - Account ID (acc0 variable)
//   - Action ID and comment
//   - Exception type and message
//   - Stack trace
//   - Inner exception message
//   - Current URL
// Exceptions: None (warnings logged if no error data available)
```

---

### `ReportSuccess(bool toLog = true, bool toTelegram = false, bool toDb = true, string customMessage = null)`

**Purpose**
Creates and sends a formatted success report to specified destinations with optional custom message.

**Example**
```csharp
var reporter = new Reporter(project, instance);

// Report success to log and database
reporter.ReportSuccess(toLog: true, toDb: true);

// Report success with custom message to all channels
reporter.ReportSuccess(
    toLog: true,
    toTelegram: true,
    toDb: true,
    customMessage: "Transaction completed successfully"
);

// Report success to Telegram with custom details
reporter.ReportSuccess(
    toLog: false,
    toTelegram: true,
    customMessage: "Balance updated: 5.23 ETH"
);
```

**Breakdown**
```csharp
// Parameters:
// - toLog: Send success report to project log (default: true)
// - toTelegram: Send success report to Telegram (default: false)
// - toDb: Update database with success info (default: true)
// - customMessage: Optional custom message to include in report (default: null)
string successReport = reporter.ReportSuccess(
    toLog: true,                              // Log to project
    toTelegram: true,                         // Send to Telegram
    toDb: true,                               // Update DB status
    customMessage: "Swap completed: 0.5 ETH" // Custom info
);

// Returns: string containing the formatted log report
// Side effects:
// - Logs success with light blue color to project log (if toLog=true)
// - Sends Telegram message with success emoji and Markdown (if toTelegram=true)
// - Updates database: status='idle', last='{success details}' (if toDb=true)
// Success data includes:
//   - Script name (filename only)
//   - Account ID (acc0 variable)
//   - Last query executed (lastQuery variable)
//   - Elapsed execution time in seconds
//   - Custom message (if provided)
//   - UTC timestamp
// Exceptions: None
```
