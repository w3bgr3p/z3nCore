# Init Class

The `Init` class handles project initialization, browser setup, account selection, and wallet loading for ZennoPoster projects.

---

## Constructor

### public Init(IZennoPosterProjectModel project, Instance instance, bool log = false)

**Purpose**: Creates a new Init instance for managing project initialization and browser setup.

**Example**:
```csharp
// Create an Init instance with logging enabled
var init = new Init(project, instance, log: true);

// Parameters:
// - project: The ZennoPoster project model instance (required)
// - instance: The current Instance object (required)
// - log: Enable detailed logging output (optional, default: false)

// This constructor initializes internal Logger for tracking
// initialization and browser setup operations
```

**Breakdown**:
- **project**: ZennoPoster project model interface for accessing project variables and methods
- **instance**: Current instance object for browser and session management
- **log**: When set to `true`, enables detailed logging of all initialization operations
- **Exceptions**: None thrown by constructor

---

## PrepareInstance()

### public void PrepareInstance(string browserToLaunch = null, bool getscore = false)

**Purpose**: Prepares the instance by launching the browser and configuring it with proxy, cookies, and WebGL settings.

**Example**:
```csharp
var init = new Init(project, instance, log: true);

// Prepare instance with Chromium browser
init.PrepareInstance(
    browserToLaunch: "Chromium",  // Browser type to launch
    getscore: false               // Skip browser fingerprint score check
);

// This method performs:
// 1. Launches browser based on browserToLaunch parameter or cfgBrowser variable
// 2. For Chromium: Sets proxy, loads cookies, configures WebGL fingerprint
// 3. For WithoutBrowser mode: Just launches without browser
// 4. Optionally runs browser scan to check and fix fingerprint issues
// 5. Closes extra tabs leaving only one active

// No return value
// Throws exception if browser launch or setup fails after 3 retry attempts
```

**Breakdown**:
- **browserToLaunch**: Browser type to launch - "Chromium", "ZB", "WithoutBrowser", or null to use cfgBrowser variable (default: null)
- **getscore**: When `true`, runs browser fingerprint scan and fixes time-related issues if detected (default: false)
- **Return value**: void
- **Side effects**:
  - Launches browser with specified profile folder
  - Sets proxy and verifies it's working
  - Loads cookies from database or file
  - Configures WebGL fingerprint from database
  - Closes extra tabs
- **Exceptions**: Throws after 3 failed retry attempts with browser setup

---

## RunProject()

### public bool RunProject(List<string> additionalVars = null, bool add = true)

**Purpose**: Executes another ZennoPoster project with mapped variables from the current project.

**Example**:
```csharp
var init = new Init(project, instance);

// Run another project with additional custom variables
var additionalVariables = new List<string> { "customVar1", "customVar2" };
bool success = init.RunProject(
    additionalVars: additionalVariables,  // Custom variables to pass
    add: true                              // Add to default variable list
);

// Parameters:
// - additionalVars: Additional variables to pass to the child project
// - add: If true, adds to default vars; if false, replaces default vars

// Returns: true if project executed successfully, false otherwise

// Default variables passed to child project:
// acc0, accRnd, cfgChains, cfgRefCode, cfgDelay, cfgLog, cfgPin, cfgToDo,
// cfgAccRange, DBmode, DBpstgrPass, DBpstgrUser, DBsqltPath, failReport,
// humanNear, instancePort, ip, lastQuery, pathCookies, projectName,
// projectTable, projectScript, proxy, requiredSocial, requiredWallets,
// toDo, varSessionId, wkMode
```

**Breakdown**:
- **additionalVars**: List of additional variable names to map to child project (default: null)
- **add**: When `true`, adds additionalVars to default list; when `false`, uses only additionalVars (default: true)
- **Return value**: `bool` - true if project execution succeeded, false otherwise
- **Side effects**: Executes child project specified in `projectScript` variable
- **Exceptions**: Exceptions from child project execution may propagate

---

## LoadWallets()

### public string LoadWallets(string walletsToUse)

**Purpose**: Loads and initializes specified cryptocurrency wallet extensions in the browser.

**Example**:
```csharp
var init = new Init(project, instance, log: true);

// Load multiple wallets - Backpack for Solana and Zerion for EVM chains
string loadedWallets = init.LoadWallets("Backpack,Zerion,Keplr");

// Parameters:
// - walletsToUse: Comma-separated wallet names to load

// Returns: The same string that was passed in, or "noBrowser" if not using Chromium

// Supported wallets:
// - Backpack: Solana wallet, stores address in 'addressSol' variable
// - Zerion: EVM wallet, stores address in 'addressEvm' variable
// - Keplr: Cosmos ecosystem wallet

// If accRnd is set, uses random seed for wallet initialization
// Retries up to 3 times if wallet loading fails
// Closes extra tabs after successful load
```

**Breakdown**:
- **walletsToUse**: Comma-separated string of wallet names - "Backpack", "Zerion", "Keplr"
- **Return value**: Returns input string if successful, "noBrowser" if browser type is not Chromium
- **Side effects**:
  - Loads wallet extensions into browser
  - Sets `addressSol` variable for Backpack wallet
  - Sets `addressEvm` variable for Zerion wallet
  - Sets `refSeed` variable with random seed if accRnd is set
  - Closes extra tabs after loading
- **Exceptions**: Throws after 3 failed retry attempts

---

## InitVariables()

### public void InitVariables(string author = "")

**Purpose**: Initializes project variables, disables logs, sets up session ID, and displays project logo.

**Example**:
```csharp
var init = new Init(project, instance);

// Initialize project variables with author name
init.InitVariables(author: "w3bgr3p");

// This method performs:
// 1. Disables ZennoPoster logs by creating symlink to NUL
// 2. Runs SAFU security check project
// 3. Sets session ID, project name, and project table name
// 4. Initializes captcha module if configured
// 5. Validates required variables (cfgAccRange)
// 6. Processes account range configuration
// 7. Initializes SAFU system
// 8. Displays project logo with version info and author

// No return value
// Throws exception if required variables are missing
```

**Breakdown**:
- **author**: Author name to display in project logo (default: empty string)
- **Return value**: void
- **Side effects**:
  - Disables ZennoPoster logging system
  - Sets `varSessionId`, `projectName`, `projectTable` variables
  - Initializes captcha module if configured
  - Processes `cfgAccRange` variable
  - Displays formatted logo in project log
- **Exceptions**: Throws if required variables like `cfgAccRange` are null or empty

---

## AccountManager.ChooseAccountByCondition()

### public static void ChooseAccountByCondition(this IZennoPosterProjectModel project, string condition, string sortByTaskAge = null, bool useRange = true, bool filterTwitter = false, bool filterDiscord = false, bool debugLog = false)

**Purpose**: Selects an account from the database based on specified conditions, filters, and priority groups.

**Example**:
```csharp
// Select account with status 'ready' from configured range
project.ChooseAccountByCondition(
    condition: "status = 'ready'",     // SQL WHERE condition
    sortByTaskAge: "last_run",         // Sort by this date column (oldest first)
    useRange: true,                    // Use cfgAccRange for filtering
    filterTwitter: true,               // Require Twitter account with status 'ok'
    filterDiscord: false,              // Don't filter by Discord
    debugLog: false                    // Disable debug logging
);

// After execution:
// - Sets 'acc0' variable to selected account ID
// - Updates account status to 'working...' in database
// - Logs selection info: account ID, remaining accounts, condition

// Priority groups in cfgAccRange:
// "1-10:11-20:21-30" - tries 1-10 first, then 11-20, then 21-30
// "5,10,15" - tries only accounts 5, 10, 15
// "1-100" - tries accounts from 1 to 100

// No return value
// Throws exception if no accounts found in any priority group
```

**Breakdown**:
- **condition**: SQL WHERE clause for filtering accounts (e.g., "status = 'ready'")
- **sortByTaskAge**: Column name to sort by (oldest date first), null for random selection (default: null)
- **useRange**: When `true`, applies cfgAccRange filtering to limit account IDs (default: true)
- **filterTwitter**: When `true`, requires Twitter account with status = 'ok' in _twitter table (default: false)
- **filterDiscord**: When `true`, requires Discord account with status = 'ok' in _discord table (default: false)
- **debugLog**: When `true`, logs detailed SQL queries and results (default: false)
- **Return value**: void
- **Side effects**:
  - Sets `acc0` variable with selected account ID
  - Updates database: sets status to 'working...' for selected account
  - Removes selected account from 'accs' list if using sortByTaskAge
  - Logs selection information to project log
- **Exceptions**: Logs warning and doesn't set acc0 if no accounts found

---

## Extension Methods

The following extension methods provide convenient access to Init functionality.

### project.InitVariables()

### public static void InitVariables(this IZennoPosterProjectModel project, Instance instance, string author = "w3bgr3p")

**Purpose**: Extension method to initialize project variables without creating an Init instance.

**Example**:
```csharp
// Initialize variables with custom author
project.InitVariables(instance, author: "myusername");

// Equivalent to:
// new Init(project, instance).InitVariables("myusername");

// This is a convenience method that creates Init internally
// and calls InitVariables() to set up the project

// No return value
```

**Breakdown**:
- **project**: The IZennoPosterProjectModel instance (this parameter)
- **instance**: The Instance object
- **author**: Author name for project logo (default: "w3bgr3p")
- **Return value**: void
- **Exceptions**: Same as InitVariables()

---

### project.RunBrowser()

### public static void RunBrowser(this IZennoPosterProjectModel project, Instance instance, string browserToLaunch = "Chromium", bool debug = false)

**Purpose**: Extension method to launch browser if not already running.

**Example**:
```csharp
// Launch Chromium browser if not already running
project.RunBrowser(
    instance,
    browserToLaunch: "Chromium",  // Browser type to launch
    debug: false                   // Debug mode flag (currently unused)
);

// This method checks if browser is already running
// If browser type is not Chromium or ChromiumFromZB,
// it will call PrepareInstance() to launch the specified browser

// Use case: Ensure browser is running before executing browser-dependent code

// No return value
```

**Breakdown**:
- **project**: The IZennoPosterProjectModel instance (this parameter)
- **instance**: The Instance object to check and launch browser
- **browserToLaunch**: Browser type to launch if needed (default: "Chromium")
- **debug**: Debug mode flag (currently not used in implementation) (default: false)
- **Return value**: void
- **Side effects**: Launches browser if not already running as Chromium or ChromiumFromZB
- **Exceptions**: Same as PrepareInstance()
