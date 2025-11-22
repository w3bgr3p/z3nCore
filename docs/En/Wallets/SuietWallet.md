# SuietWallet Class

The `SuietWallet` class provides automation for the Suiet Wallet browser extension, designed for the Sui blockchain.

---

## Constructor

### SuietWallet

**Purpose**: Initializes a new instance of the SuietWallet class for managing Sui wallets.

**Example**:
```csharp
var suiet = new SuietWallet(project, instance, log: true);
```

**Breakdown**:
```csharp
public SuietWallet(
    IZennoPosterProjectModel project,                              // ZennoPoster project model
    Instance instance,                                              // Browser instance
    bool log = false,                                               // Enable detailed logging (default: false)
    string key = null,                                              // Private key or seed (optional)
    string fileName = "Suiet-Sui-Wallet-Chrome.crx"                // Extension CRX file name (default: "Suiet-Sui-Wallet-Chrome.crx")
)
```

---

## Public Methods

### Launch

**Purpose**: Installs the Suiet Wallet extension, imports the wallet or unlocks it, and returns the Sui address.

**Example**:
```csharp
var suiet = new SuietWallet(project, instance);
string address = suiet.Launch(source: "key");
project.SendInfoToLog($"Sui address: {address}");
```

**Breakdown**:
```csharp
public string Launch(
    string source = null  // Key source: "key" (private key) or "seed" (seed phrase) (default: "key")
)
// Returns: The Sui wallet address (string)
// - Determines the key type and converts seed to Sui-compatible key if needed
// - Disables full mouse emulation for faster interaction
// - Switches to the Suiet extension
// - If new install: imports wallet using the defined key
// - If already installed: unlocks the wallet
// - Retrieves the active wallet address
// - Closes extra tabs
// - Restores mouse emulation setting
// - Returns the Sui address
```

---

### Sign

**Purpose**: Signs a transaction or message in Suiet Wallet by clicking the primary button.

**Example**:
```csharp
var suiet = new SuietWallet(project, instance);
suiet.Sign(deadline: 15, delay: 5);
```

**Breakdown**:
```csharp
public void Sign(
    int deadline = 10,  // Maximum time to wait for the button (default: 10 seconds)
    int delay = 3       // Delay after clicking (default: 3 seconds)
)
// Returns: void
// - Waits for the primary button to appear (up to deadline seconds)
// - Clicks the primary button (usually "Approve" or "Sign")
// - Waits for the specified delay after clicking
// - Uses CSS class selector to find the button
```

---

### Unlock

**Purpose**: Unlocks the Suiet Wallet using the stored password.

**Example**:
```csharp
var suiet = new SuietWallet(project, instance);
suiet.Unlock();
```

**Breakdown**:
```csharp
public void Unlock()
// Returns: void
// - Navigates to the Suiet popup
// - Retrieves the hardware password from secure storage
// - Enters the password into the password field (first input:password element)
// - Clicks the "Unlock" button
```

---

### SwitchChain

**Purpose**: Switches the Suiet Wallet to a different Sui network (Mainnet, Testnet, or Devnet).

**Example**:
```csharp
var suiet = new SuietWallet(project, instance);
suiet.SwitchChain("Testnet");  // Switch to Sui Testnet
```

**Breakdown**:
```csharp
public void SwitchChain(
    string mode = "Mainnet"  // Target network: "Mainnet", "Testnet", or "Devnet" (default: "Mainnet")
)
// Returns: void
// - Determines the network index (0 for Mainnet, 1 for Testnet, 2 for Devnet)
// - Navigates to the networks settings page
// - Clicks the network selection container at the calculated index
// - Clicks "Save" to apply the network change
```

---

### ActiveAddress

**Purpose**: Retrieves the currently active Sui wallet address.

**Example**:
```csharp
var suiet = new SuietWallet(project, instance);
string address = suiet.ActiveAddress();
project.SendInfoToLog($"Active Sui address: {address}");
```

**Breakdown**:
```csharp
public string ActiveAddress()
// Returns: The active Sui wallet address (string)
// - Finds the payment link element containing the wallet address
// - Extracts the href attribute
// - Removes the "https://pay.suiet.app/?wallet_address=" prefix
// - Returns the clean wallet address
```
