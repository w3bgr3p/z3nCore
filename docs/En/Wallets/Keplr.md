# Keplr Class

The `Keplr` class provides automation for the Keplr Wallet browser extension, which supports Cosmos-based blockchains.

---

## Constructor

### Keplr

**Purpose**: Initializes a new instance of the Keplr class for interacting with the Keplr Wallet extension.

**Example**:
```csharp
var keplr = new Keplr(project, instance, log: true);
```

**Breakdown**:
```csharp
public Keplr(
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
var keplr = new Keplr(project, instance);
keplr.Launch(source: "seed");  // Launch with seed phrase wallet
```

**Breakdown**:
```csharp
public void Launch(
    string source = "seed",       // Wallet source: "seed" or "keyEvm" (default: "seed")
    string fileName = null,       // CRX file name (default: uses class constant)
    bool log = false              // Enable logging (default: false)
)
// Returns: void
// - Detects browser type (Chromium or ChromiumFromZB)
// - Installs Keplr extension from CRX or Chrome Web Store
// - Disables full mouse emulation for faster interaction
// - Navigates to Keplr popup
// - Checks wallet state (onboarding, unlock, or ready)
// - If onboarding: imports wallet using specified source
// - If locked: unlocks the wallet
// - Sets the active wallet source
// - Closes extra tabs
// - Navigates back to popup
```

---

### SetSource

**Purpose**: Sets which imported wallet (seed or private key) should be active in Keplr.

**Example**:
```csharp
var keplr = new Keplr(project, instance);
keplr.Launch();
keplr.SetSource("keyEvm");  // Switch to private key wallet
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
// - If both exist: clicks on the requested source to activate it
// - If missing: adds the missing wallet and retries
// - Ensures both "seed" and "keyEvm" wallets are always available
```

---

### Unlock

**Purpose**: Unlocks the Keplr Wallet using the stored password.

**Example**:
```csharp
var keplr = new Keplr(project, instance);
keplr.Unlock();
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
// - If locked: enters password and clicks unlock button
// - Verifies password is correct (checks for "Invalid password" message)
// - If wrong password: uninstalls extension and throws exception
// - Uses retry logic with goto pattern for reliability
// - Displays total available balance when unlocked
```

---

### Sign

**Purpose**: Approves a transaction or message signing request in Keplr Wallet.

**Example**:
```csharp
var keplr = new Keplr(project, instance);
keplr.Sign();
```

**Breakdown**:
```csharp
public void Sign(
    bool log = false  // Enable logging (default: false)
)
// Returns: void
// - Waits up to 20 seconds for a Keplr tab to appear
// - Throws exception if no Keplr tab detected within timeout
// - Disables full mouse emulation for faster clicking
// - Clicks the "Approve" button
// - Waits for the Keplr tab to close (indicating transaction was approved)
// - Retries approval if tab doesn't close immediately
// - Throws exception if tab remains open after timeout
// - Re-enables full mouse emulation before returning
```
