# Disposer Class

The `Disposer` class handles session cleanup and reporting operations for ZennoPoster projects.

---

## Constructor

### public Disposer(IZennoPosterProjectModel project, Instance instance, bool log = false)

**Purpose**: Creates a new Disposer instance for managing session cleanup and reporting.

**Example**:
```csharp
// Create a disposer instance with logging enabled
var disposer = new Disposer(project, instance, log: true);

// Parameters:
// - project: The ZennoPoster project model instance (required)
// - instance: The current Instance object (required)
// - log: Enable detailed logging output (optional, default: false)

// This constructor initializes internal Reporter and Logger objects
// for handling reports and logging operations
```

**Breakdown**:
- **project**: ZennoPoster project model interface for accessing project variables and methods
- **instance**: Current instance object for browser and session management
- **log**: When set to `true`, enables detailed logging of all disposer operations
- **Exceptions**: Throws `ArgumentNullException` if project or instance is null

---

## FinishSession()

### public void FinishSession()

**Purpose**: Completes the current session by generating reports, saving cookies, and cleaning up resources.

**Example**:
```csharp
var disposer = new Disposer(project, instance, log: true);

// Finish the current session
disposer.FinishSession();

// This method performs the following operations:
// 1. Determines if the session was successful by checking 'lastQuery' variable
// 2. Generates success or error reports (logs, telegram, database)
// 3. Saves browser cookies if using Chromium and acc0 is set
// 4. Logs final session status with elapsed time
// 5. Clears global and local variables (acc0)
// 6. Stops the instance

// No return value
// May throw exceptions during cleanup operations, but will attempt emergency stop
```

**Breakdown**:
- **Return value**: void - no return value
- **Side effects**:
  - Generates reports to log, Telegram, and database
  - Saves cookies to the path defined by `PathCookies()` extension
  - Clears `acc{acc0}` global variable and `acc0` local variable
  - Stops the instance
- **Exceptions**: Catches and logs all exceptions during cleanup, attempts emergency instance stop if needed

---

## ErrorReport()

### public string ErrorReport(bool toLog = true, bool toTelegram = false, bool toDb = false, bool screenshot = false)

**Purpose**: Generates an error report with optional logging, Telegram notification, database recording, and screenshot capture.

**Example**:
```csharp
var disposer = new Disposer(project, instance);

// Generate error report with all options enabled
string errorMessage = disposer.ErrorReport(
    toLog: true,        // Log error to ZennoPoster log
    toTelegram: true,   // Send notification to Telegram
    toDb: true,         // Record error in database
    screenshot: true    // Capture screenshot of current state
);

// Returns: Error message string that was reported
// Example: "Error: Task failed - connection timeout"

// Use case: Call this when an error occurs in your project
// to automatically log and notify through configured channels
```

**Breakdown**:
- **toLog**: When `true`, writes error to ZennoPoster log (default: true)
- **toTelegram**: When `true`, sends error notification to configured Telegram bot (default: false)
- **toDb**: When `true`, records error in the database (default: false)
- **screenshot**: When `true`, captures and includes screenshot with error report (default: false)
- **Return value**: String containing the error message that was reported
- **Exceptions**: Internal exception handling by Reporter class

---

## SuccessReport()

### public string SuccessReport(bool toLog = true, bool toTelegram = false, bool toDb = false, string customMessage = null)

**Purpose**: Generates a success report with optional logging, Telegram notification, database recording, and custom message.

**Example**:
```csharp
var disposer = new Disposer(project, instance);

// Generate success report with custom message
string successMessage = disposer.SuccessReport(
    toLog: true,                     // Log success to ZennoPoster log
    toTelegram: true,                // Send notification to Telegram
    toDb: true,                      // Record success in database
    customMessage: "Task completed"  // Custom message to include
);

// Returns: Success message string that was reported
// Example: "Success: Task completed - account processed successfully"

// Use case: Call this when your project completes successfully
// to log the result and send notifications
```

**Breakdown**:
- **toLog**: When `true`, writes success to ZennoPoster log (default: true)
- **toTelegram**: When `true`, sends success notification to configured Telegram bot (default: false)
- **toDb**: When `true`, records success in the database (default: false)
- **customMessage**: Optional custom message to include in report (default: null)
- **Return value**: String containing the success message that was reported
- **Exceptions**: Internal exception handling by Reporter class

---

## Extension Methods

The following extension methods provide convenient access to Disposer functionality without creating an instance.

### project.Finish()

### public static void Finish(this IZennoPosterProjectModel project, Instance instance)

**Purpose**: Extension method to quickly finish a session without creating a Disposer instance.

**Example**:
```csharp
// Use extension method directly on project object
project.Finish(instance);

// Equivalent to:
// new Disposer(project, instance).FinishSession();

// This is a convenience method that creates a Disposer internally
// and calls FinishSession() to clean up and stop the instance

// No return value
```

**Breakdown**:
- **project**: The IZennoPosterProjectModel instance (this parameter)
- **instance**: The Instance object to finish
- **Return value**: void
- **Exceptions**: Same as FinishSession()

---

### project.ReportError()

### public static string ReportError(this IZennoPosterProjectModel project, Instance instance, bool toLog = true, bool toTelegram = false, bool toDb = false, bool screenshot = false)

**Purpose**: Extension method to report errors without creating a Disposer instance.

**Example**:
```csharp
// Use extension method directly on project object
string errorMsg = project.ReportError(
    instance,
    toLog: true,
    toTelegram: true,
    toDb: true,
    screenshot: true
);

// Equivalent to:
// new Disposer(project, instance).ErrorReport(true, true, true, true);

// Returns: Error message string
```

**Breakdown**:
- **project**: The IZennoPosterProjectModel instance (this parameter)
- **instance**: The Instance object for context
- **toLog**, **toTelegram**, **toDb**, **screenshot**: Same as ErrorReport() method
- **Return value**: String containing error message
- **Exceptions**: Same as ErrorReport()

---

### project.ReportSuccess()

### public static string ReportSuccess(this IZennoPosterProjectModel project, Instance instance, bool toLog = true, bool toTelegram = false, bool toDb = false, string customMessage = null)

**Purpose**: Extension method to report success without creating a Disposer instance.

**Example**:
```csharp
// Use extension method directly on project object
string successMsg = project.ReportSuccess(
    instance,
    toLog: true,
    toTelegram: true,
    toDb: true,
    customMessage: "Account verification completed"
);

// Equivalent to:
// new Disposer(project, instance).SuccessReport(true, true, true, "Account verification completed");

// Returns: Success message string
```

**Breakdown**:
- **project**: The IZennoPosterProjectModel instance (this parameter)
- **instance**: The Instance object for context
- **toLog**, **toTelegram**, **toDb**: Same as SuccessReport() method
- **customMessage**: Optional custom message to include
- **Return value**: String containing success message
- **Exceptions**: Same as SuccessReport()
