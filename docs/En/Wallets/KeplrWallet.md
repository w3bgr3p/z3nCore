# KeplrWallet Class

The `KeplrWallet` class provides automation for the Keplr Wallet browser extension with custom click handling for specific browser configurations.

---

## Constructor

### KeplrWallet

**Purpose**: Initializes a new instance of the KeplrWallet class for interacting with the Keplr Wallet extension.

**Example**:
```csharp
var keplrWallet = new KeplrWallet(project, instance, log: true);
```

**Breakdown**:
```csharp
public KeplrWallet(
    IZennoPosterProjectModel project,  // ZennoPoster project model
    Instance instance,                  // Browser instance
    bool log = false,                   // Enable detailed logging (default: false)
    string key = null,                  // Private key (optional, loaded from DB if null)
    string seed = null                  // Seed phrase (optional, loaded from DB if null)
)
```

---

## Public Methods

### Launch

**Purpose**: Installs the Keplr Wallet extension, imports a wallet or unlocks it, and sets the active wallet source.

**Example**:
```csharp
var keplrWallet = new KeplrWallet(project, instance);
keplrWallet.Launch(source: "seed");  // Launch with seed phrase wallet
```

**Breakdown**:
```csharp
public void Launch(
    string source = "seed",       // Wallet source: "seed" or "keyEvm" (default: "seed")
    string fileName = null,       // CRX file name (default: "Keplr0.12.223.crx")
    bool log = false              // Enable logging (default: false)
)
// Returns: void
// - Disables full mouse emulation
// - Switches to the Keplr extension
// - If new install: imports wallet using specified source
// - If already installed: unlocks the wallet
// - Sets the active wallet source
// - Closes extra tabs
// - Re-enables previous mouse emulation setting
```

---

### SetSource

**Purpose**: Sets which imported wallet (seed or private key) should be active in Keplr.

**Example**:
```csharp
var keplrWallet = new KeplrWallet(project, instance);
keplrWallet.Launch();
keplrWallet.SetSource("keyEvm");  // Switch to private key wallet
```

**Breakdown**:
```csharp
public void SetSource(
    string source,    // Wallet source to activate: "seed" or "keyEvm"
    bool log = false  // Enable logging (default: false)
)
// Returns: void
// - Navigates to wallet selection page
// - Prunes (removes) wallets that aren't seed or keyEvm
// - Checks if both required wallets are imported
// - If both exist: uses KeplrClick to select the requested source
// - If missing: adds the missing wallet and retries
// - Uses custom KeplrClick method for browser-specific click handling
```

---

### Unlock

**Purpose**: Unlocks the Keplr Wallet using the stored password.

**Example**:
```csharp
var keplrWallet = new KeplrWallet(project, instance);
keplrWallet.Unlock();
```

**Breakdown**:
```csharp
public void Unlock(
    bool log = false  // Enable logging (default: false)
)
// Returns: void
// - Navigates to Keplr popup
// - Checks if already unlocked (by looking for "Copy Address" button)
// - If unlocked: returns immediately
// - If locked: enters password and uses KeplrClick to unlock
// - Verifies password is correct (checks for "Invalid password" message)
// - If wrong password: closes tabs, uninstalls extension, and throws exception
// - Uses retry logic with goto pattern for reliability
// - Uses custom KeplrClick method for clicking unlock button
```

---

### Sign

**Purpose**: Approves a transaction or message signing request in Keplr Wallet.

**Example**:
```csharp
var keplrWallet = new KeplrWallet(project, instance);
keplrWallet.Sign();
```

**Breakdown**:
```csharp
public void Sign(
    bool log = false  // Enable logging (default: false)
)
// Returns: void
// - Waits up to 20 seconds for a Keplr tab to appear
// - Throws exception if no Keplr tab detected within timeout
// - Disables full mouse emulation for faster interaction
// - Clicks the "Approve" button using HeClick
// - Waits for the Keplr tab to close (indicating transaction was approved)
// - Retries approval if tab doesn't close immediately
// - Throws exception if tab remains open after timeout
// - Re-enables full mouse emulation before returning
```

---

### KeplrApprove

**Purpose**: Legacy method for approving transactions. Calls the Sign method internally.

**Example**:
```csharp
var keplrWallet = new KeplrWallet(project, instance);
string result = keplrWallet.KeplrApprove();
```

**Breakdown**:
```csharp
public string KeplrApprove(
    bool log = false  // Enable logging (default: false)
)
// Returns: "done" (string)
// - Marks this method as obsolete (replaced by Sign)
// - Calls Sign(log) internally
// - Returns "done" string after successful approval
// - Provided for backward compatibility
```
