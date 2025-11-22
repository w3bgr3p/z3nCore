# RabbyWallet Class

The `RabbyWallet` class provides automation for the Rabby Wallet browser extension, an Ethereum wallet with multi-chain support.

---

## Constructor

### RabbyWallet

**Purpose**: Initializes a new instance of the RabbyWallet class with project references and wallet configuration.

**Example**:
```csharp
var rabby = new RabbyWallet(project, instance, log: true, key: "key");
```

**Breakdown**:
```csharp
public RabbyWallet(
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

### RabbyLnch

**Purpose**: Launches the Rabby Wallet extension by installing it or unlocking it if already installed.

**Example**:
```csharp
var rabby = new RabbyWallet(project, instance);
rabby.RabbyLnch();
```

**Breakdown**:
```csharp
public void RabbyLnch(
    string fileName = null,  // CRX file name (default: uses constructor value)
    bool log = false         // Enable logging (default: false)
)
// Returns: void
// - Enables full mouse emulation for reliable interaction
// - Switches to the Rabby Wallet extension
// - If new install: calls RabbyImport to import the wallet
// - If already installed: calls RabbyUnlock to unlock it
// - Closes extra tabs
// - Restores previous mouse emulation setting
```

---

### RabbyImport

**Purpose**: Imports a wallet into Rabby using a private key and sets up the wallet password.

**Example**:
```csharp
var rabby = new RabbyWallet(project, instance);
rabby.RabbyImport();
```

**Breakdown**:
```csharp
public void RabbyImport(
    bool log = false  // Enable logging (default: false)
)
// Returns: void
// - Clicks "I already have an address" button
// - Selects the private key import method (by clicking the key icon)
// - Enters the private key into the input field
// - Clicks "Confirm" to proceed
// - Sets wallet password (enters it twice)
// - Clicks "Confirm" to finalize import
// - Clicks "Get Started" to complete setup
// - Throws exception if any step fails
```

---

### RabbyUnlock

**Purpose**: Unlocks the Rabby Wallet using the stored password.

**Example**:
```csharp
var rabby = new RabbyWallet(project, instance);
rabby.RabbyUnlock();
```

**Breakdown**:
```csharp
public void RabbyUnlock(
    bool log = false  // Enable logging (default: false)
)
// Returns: void
// - Enables full mouse emulation
// - Checks for offscreen.html tab (background page)
// - If offscreen tab is active: closes it and navigates to unlock page
// - Enters the password into the password field
// - Clicks the "Unlock" button
// - Throws exception if unlock fails
```
