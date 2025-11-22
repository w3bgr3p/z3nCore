# ZerionWallet Class

The `ZerionWallet` class provides automation for the Zerion Wallet browser extension, supporting Ethereum and EVM-compatible chains.

---

## Constructor

### ZerionWallet

**Purpose**: Initializes a new instance of the ZerionWallet class with wallet configuration and extension setup.

**Example**:
```csharp
var zerion = new ZerionWallet(project, instance, log: true, key: "key");
```

**Breakdown**:
```csharp
public ZerionWallet(
    IZennoPosterProjectModel project,                    // ZennoPoster project model
    Instance instance,                                    // Browser instance
    bool log = false,                                     // Enable detailed logging (default: false)
    string key = null,                                    // Private key or "seed" (default: null, loads "evm" key from DB)
    string fileName = "Zerion1.21.3.crx"                 // Extension CRX file name (default: "Zerion1.21.3.crx")
)
// - Loads the key from database if key parameter is "key" or "seed"
// - Calculates and stores the expected Ethereum address from the key
// - Retrieves the hardware password from secure storage
```

---

## Public Methods

### Launch

**Purpose**: Installs the Zerion Wallet extension, imports or unlocks the wallet, and returns the active Ethereum address.

**Example**:
```csharp
var zerion = new ZerionWallet(project, instance);
string address = zerion.Launch(source: "key", refCode: "MYREF123");
project.SendInfoToLog($"Wallet address: {address}");
```

**Breakdown**:
```csharp
public string Launch(
    string fileName = null,   // CRX file name (default: uses constructor value)
    bool log = false,         // Enable logging (default: false)
    string source = null,     // Key source: "key" or "seed" (default: "key")
    string refCode = null     // Optional referral code for new wallets
)
// Returns: The active Ethereum address (string)
// - Disables full mouse emulation
// - Switches to and installs the Zerion extension
// - Checks wallet state (onboarding, unlock, or ready)
// - If onboarding: imports wallet with optional referral code
// - If locked: unlocks the wallet
// - If no tab: navigates to Zerion popup
// - Disables testnet mode
// - Retrieves the active address
// - Stores address in project variable
// - Closes extra tabs
// - Restores mouse emulation
```

---

### Sign

**Purpose**: Signs a transaction or message by clicking the "Confirm" or "Sign" button in Zerion.

**Example**:
```csharp
var zerion = new ZerionWallet(project, instance);
bool success = zerion.Sign(deadline: 20);
```

**Breakdown**:
```csharp
public bool Sign(
    bool log = false,     // Enable logging (default: false)
    int deadline = 10     // Maximum time to wait for signing prompt (default: 10 seconds)
)
// Returns: true if signing was successful (bool)
// - Scans for Zerion extension tabs
// - Parses the URL to extract transaction information
// - Attempts to click "Confirm" button first
// - If "Confirm" not found, tries "Sign" button
// - Times out after specified deadline
// - Returns true when button is successfully clicked
// - Throws exception if deadline is exceeded
```

---

### Connect

**Purpose**: Handles connection requests from dApps to the Zerion Wallet, approving all connection steps.

**Example**:
```csharp
var zerion = new ZerionWallet(project, instance);
zerion.Connect();
```

**Breakdown**:
```csharp
public void Connect(
    bool log = false  // Enable logging (default: false)
)
// Returns: void
// - Continuously checks for action buttons in Zerion popup
// - Handles multiple connection steps: "Add", "Close", "Connect", "Sign", "Sign In"
// - Clicks the primary button for each step
// - Logs the current action and site information
// - Uses goto pattern for state machine logic
// - Returns when no wallet tab is found (connection complete)
```

---

### SwitchSource

**Purpose**: Switches between different imported wallets (key-based or seed-based) in Zerion.

**Example**:
```csharp
var zerion = new ZerionWallet(project, instance);
zerion.SwitchSource("seed");  // Switch to seed-based wallet
```

**Breakdown**:
```csharp
public void SwitchSource(
    string addressToUse = "key"  // Address to switch to: "key" or "seed" (default: "key")
)
// Returns: void
// - Resolves "key" or "seed" to actual Ethereum address
// - Navigates to wallet selection page
// - Waits for wallet list to load
// - Iterates through available wallets
// - Parses wallet information (masked address, balance, ENS name)
// - Compares masked addresses with target address
// - If found: clicks to activate the wallet
// - If not found: adds the wallet and retries
// - Closes extra tabs after switching
```

---

### WaitTx

**Purpose**: Waits for a transaction to be confirmed or fail by monitoring the transaction history.

**Example**:
```csharp
var zerion = new ZerionWallet(project, instance);
bool success = zerion.WaitTx(deadline: 120);
if (success)
    project.SendInfoToLog("Transaction confirmed!");
```

**Breakdown**:
```csharp
public bool WaitTx(
    int deadline = 60,  // Maximum time to wait in seconds (default: 60)
    bool log = false    // Enable logging (default: false)
)
// Returns: true if transaction succeeded, false if failed (bool)
// - Opens a new tab and navigates to Zerion transaction history
// - Retrieves the latest transaction status
// - If "Pending": waits and checks again
// - If "Failed": returns false
// - If "Execute": returns true
// - Throws exception if deadline is exceeded
// - Closes extra tabs before returning
```

---

### Claimable

**Purpose**: Retrieves a list of claimable Zerion DNA quests for a given wallet address.

**Example**:
```csharp
var zerion = new ZerionWallet(project, instance);
List<string> questIds = zerion.Claimable("0x1234...5678");
foreach (string questId in questIds)
{
    project.SendInfoToLog($"Claimable quest: {questId}");
}
```

**Breakdown**:
```csharp
public List<string> Claimable(
    string address  // Ethereum address to check for claimable quests
)
// Returns: List of claimable quest IDs (List<string>)
// - Makes HTTP GET request to Zerion DNA API
// - Parses JSON response containing quest information
// - Iterates through all quests
// - Filters quests where claimable count > 0
// - Logs each claimable quest with ID and kind
// - Returns list of claimable quest IDs
// - Handles parsing errors gracefully
```

---

### ActiveAddress

**Purpose**: Retrieves the currently active Ethereum wallet address from Zerion.

**Example**:
```csharp
var zerion = new ZerionWallet(project, instance);
string address = zerion.ActiveAddress();
project.SendInfoToLog($"Active address: {address}");
```

**Breakdown**:
```csharp
public string ActiveAddress()
// Returns: The active Ethereum address (string)
// - Finds the receive link element containing the address
// - Extracts the href attribute
// - Removes the URL prefix to get clean address
// - Logs the retrieved address
// - Returns the address
```

---

## Public Static Methods

### TxFromUrl

**Purpose**: Extracts transaction data from a Zerion URL containing transaction parameters.

**Example**:
```csharp
string url = "chrome-extension://...#/send?transaction={...}";
string txJson = ZerionWallet.TxFromUrl(url);
project.SendInfoToLog($"Transaction: {txJson}");
```

**Breakdown**:
```csharp
public static string TxFromUrl(
    string url  // Zerion URL containing transaction data
)
// Returns: JSON string containing transaction data (string)
// - Parses the URL and extracts the query string or fragment
// - Searches for "transaction=" parameter
// - URL-decodes the transaction data
// - Deserializes JSON to validate it contains "to" and "from" fields
// - Returns the transaction JSON string
// - Throws ArgumentException if URL is invalid or transaction data missing
// - Throws Exception with detailed error message if parsing fails
```

---

### Replace

**Purpose**: Placeholder method for transaction replacement functionality (not yet implemented).

**Example**:
```csharp
string newTx = ZerionWallet.Replace(originalTx);
```

**Breakdown**:
```csharp
public static string Replace(
    string tx  // Transaction data to replace
)
// Returns: Empty string (string)
// - Currently returns empty string
// - Intended for future transaction replacement functionality
```
