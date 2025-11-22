# AuroWallet Class

The `AuroWallet` class provides automation for the Auro Wallet browser extension, which is used for managing Mina Protocol accounts.

---

## Constructor

### AuroWallet

**Purpose**: Initializes a new instance of the AuroWallet class with necessary project and browser instance references.

**Example**:
```csharp
var auroWallet = new AuroWallet(project, instance, _showLog: true);
```

**Breakdown**:
```csharp
public AuroWallet(
    IZennoPosterProjectModel project,  // ZennoPoster project model for accessing project functionality
    Instance instance,                  // Browser instance to interact with
    bool _showLog = false              // Whether to show detailed logs (default: false)
)
```

---

## Public Methods

### Launch

**Purpose**: Installs or restores the Auro Wallet extension, imports the seed phrase or unlocks the wallet, and returns the Mina address.

**Example**:
```csharp
var auroWallet = new AuroWallet(project, instance);
string minaAddress = auroWallet.Launch();
project.SendInfoToLog($"Wallet address: {minaAddress}");
```

**Breakdown**:
```csharp
public string Launch()
// Returns: The Mina wallet address (string)
// - Installs the Auro Wallet extension from CRX file if not present
// - If fresh install: Restores wallet using seed phrase from database
// - If already installed: Unlocks the wallet using stored password
// - Navigates to receive page to retrieve the wallet address
// - Updates the database with the address
// - Closes extra tabs
// - Returns the Mina address
```

---

### SwitchChain

**Purpose**: Switches the Auro Wallet to a different Mina network (Testnet, Devnet, or Mainnet).

**Example**:
```csharp
var auroWallet = new AuroWallet(project, instance);
auroWallet.Launch();
auroWallet.SwitchChain("Testnet");  // Switch to Testnet
```

**Breakdown**:
```csharp
public void SwitchChain(
    string chain = "Testnet"  // Target network: "Testnet", "Devnet", or "Mainnet" (default: "Testnet")
)
// Returns: void
// - Activates the Auro Wallet extension
// - Checks if already on the target chain
// - If not, opens the network selector
// - Selects the appropriate network based on the chain parameter
// - Expands additional networks if needed (e.g., for Testnet)
```

---

### Unlock

**Purpose**: Unlocks the Auro Wallet using the stored password.

**Example**:
```csharp
var auroWallet = new AuroWallet(project, instance);
auroWallet.Unlock();
```

**Breakdown**:
```csharp
public void Unlock()
// Returns: void
// - Activates the Auro Wallet extension
// - Retrieves the hardware password from secure storage
// - Checks if the wallet is already unlocked
// - If locked: enters password and clicks unlock button
// - If already unlocked: returns immediately
```
