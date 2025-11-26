# BackpackWallet Class

The `BackpackWallet` class provides automation for the Backpack Wallet browser extension, supporting both Solana and Ethereum chains.

---

## Constructor

### BackpackWallet

**Purpose**: Initializes a new instance of the BackpackWallet class with project references and optional configuration.

**Example**:
```csharp
var backpack = new BackpackWallet(project, instance, log: true);
```

**Breakdown**:
```csharp
public BackpackWallet(
    IZennoPosterProjectModel project,                    // ZennoPoster project model
    Instance instance,                                    // Browser instance
    bool log = false,                                     // Enable detailed logging (default: false)
    string key = null,                                    // Private key or seed phrase (default: null, loads from DB)
    string fileName = "Backpack0.10.94.crx"              // Extension CRX file name (default: "Backpack0.10.94.crx")
)
```

---

## Public Methods

### Launch

**Purpose**: Installs the Backpack Wallet extension, imports the wallet or unlocks it, and returns the active address.

**Example**:
```csharp
var backpack = new BackpackWallet(project, instance);
string address = backpack.Launch();
project.SendInfoToLog($"Wallet address: {address}");
```

**Breakdown**:
```csharp
public string Launch(
    string fileName = null,  // CRX file name (default: uses constructor value)
    bool log = false         // Enable logging (default: false)
)
// Returns: The active wallet address (string)
// - Switches to the Backpack extension
// - If new install: imports wallet using private key or seed phrase
// - If already installed: unlocks the wallet
// - Returns the active address
// - Closes extra tabs
```

---

### Unlock

**Purpose**: Unlocks the Backpack Wallet using the stored password.

**Example**:
```csharp
var backpack = new BackpackWallet(project, instance);
backpack.Unlock();
```

**Breakdown**:
```csharp
public void Unlock(
    bool log = false  // Enable logging (default: false)
)
// Returns: void
// - Navigates to the Backpack popup if not already there
// - Waits for the unlock screen or unlocked state
// - If locked: enters password and clicks unlock button
// - If already unlocked: returns immediately
// - Uses goto pattern for state machine logic
```

---

### Approve

**Purpose**: Approves a transaction or connection request in Backpack Wallet.

**Example**:
```csharp
var backpack = new BackpackWallet(project, instance);
backpack.Approve();
```

**Breakdown**:
```csharp
public void Approve(
    bool log = false  // Enable logging (default: false)
)
// Returns: void
// - Attempts to click the "Approve" button directly
// - If fails: unlocks the wallet first, then approves
// - Closes extra tabs after approval
// - Catches exceptions to handle locked state
```

---

### Connect

**Purpose**: Handles connection requests from dApps to the Backpack Wallet.

**Example**:
```csharp
var backpack = new BackpackWallet(project, instance);
backpack.Connect();
```

**Breakdown**:
```csharp
public void Connect(
    bool log = false  // Enable logging (default: false)
)
// Returns: void
// - Waits for connection approval prompt
// - Unlocks wallet if needed
// - Clicks "Approve" button to establish connection
// - Uses state machine pattern with goto for reliable handling
// - Times out after 30 seconds if no wallet tab detected
```

---

### ActiveAddress

**Purpose**: Retrieves the currently active wallet address from Backpack.

**Example**:
```csharp
var backpack = new BackpackWallet(project, instance);
string address = backpack.ActiveAddress();
project.SendInfoToLog($"Active address: {address}");
```

**Breakdown**:
```csharp
public string ActiveAddress(
    bool log = false  // Enable logging (default: false)
)
// Returns: The active wallet address (string)
// - Navigates to Backpack popup
// - Closes extra tabs
// - Opens wallet details by clicking navigation elements
// - Extracts address from the page
// - Closes the details view
// - Returns the address
// - Throws exception if address retrieval fails
```

---

### CurrentChain

**Purpose**: Determines which blockchain network Backpack is currently connected to.

**Example**:
```csharp
var backpack = new BackpackWallet(project, instance);
string chain = backpack.CurrentChain();
project.SendInfoToLog($"Current chain: {chain}");
```

**Breakdown**:
```csharp
public string CurrentChain(
    bool log = true  // Enable logging (default: true)
)
// Returns: Current chain name: "mainnet", "devnet", "testnet", or "ethereum" (string)
// - Retrieves the chain selector element's HTML
// - Checks for chain-specific image references
// - Returns the detected chain name
// - Retries until a valid chain is detected
```

---

### Devmode

**Purpose**: Enables or disables developer mode in Backpack Wallet to access test networks.

**Example**:
```csharp
var backpack = new BackpackWallet(project, instance);
backpack.Devmode(enable: true);  // Enable dev mode
```

**Breakdown**:
```csharp
public void Devmode(
    bool enable = true  // Enable (true) or disable (false) dev mode (default: true)
)
// Returns: void
// - Navigates to Backpack popup
// - Opens settings menu if not already open
// - Checks current dev mode state
// - Toggles dev mode checkbox if state doesn't match desired state
// - Verifies the toggle was successful
```

---

### DevChain

**Purpose**: Switches Backpack Wallet to a specific Solana development network (devnet, testnet, or mainnet).

**Example**:
```csharp
var backpack = new BackpackWallet(project, instance);
backpack.DevChain("devnet");  // Switch to Solana Devnet
```

**Breakdown**:
```csharp
public void DevChain(
    string reqmode = "devnet"  // Target network: "devnet", "testnet", or "mainnet" (default: "devnet")
)
// Returns: void
// - Switches to Solana chain first
// - Checks current chain
// - If not on target network: opens network selector
// - Enables devmode if test networks are not available
// - Selects the requested network
// - Handles bridge transfers if needed
```

---

### Add

**Purpose**: Adds a new wallet (Solana or Ethereum) to Backpack using a private key or seed phrase.

**Example**:
```csharp
var backpack = new BackpackWallet(project, instance);
backpack.Add(type: "Ethereum", source: "key");
```

**Breakdown**:
```csharp
public void Add(
    string type = "Ethereum",  // Blockchain type: "Ethereum" or "Solana" (default: "Ethereum")
    string source = "key"      // Import source: "key" (private key) or "phrase" (seed) (default: "key")
)
// Returns: void
// - Retrieves the appropriate key from database based on type
// - Navigates to add account URL
// - Clicks through import flow
// - Selects blockchain type
// - Selects import method (key or phrase)
// - Enters the key or seed phrase
// - Completes import and closes extra tabs
```

---

### Switch

**Purpose**: Switches between Solana and Ethereum wallets in Backpack.

**Example**:
```csharp
var backpack = new BackpackWallet(project, instance);
backpack.Switch("Ethereum");  // Switch to Ethereum wallet
```

**Breakdown**:
```csharp
public void Switch(
    string type  // Wallet type to switch to: "Solana" or "Ethereum"
)
// Returns: void
// - Navigates to Backpack popup if not already there
// - Opens wallet selector menu
// - Counts available wallets
// - If missing required wallet: adds it automatically
// - Selects the target wallet by index (0 for Solana, 1 for Ethereum)
```

---

### Current

**Purpose**: Determines which blockchain (Solana or Ethereum) is currently active in Backpack.

**Example**:
```csharp
var backpack = new BackpackWallet(project, instance);
string currentChain = backpack.Current();
project.SendInfoToLog($"Current blockchain: {currentChain}");
```

**Breakdown**:
```csharp
public string Current()
// Returns: Current blockchain: "Solana", "Ethereum", or "Undefined" (string)
// - Navigates to Backpack popup
// - Retrieves the chain selector element's HTML
// - Checks for blockchain-specific identifiers
// - Returns the detected blockchain name
// - Logs the result
```
