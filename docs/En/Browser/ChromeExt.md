# ChromeExt

Chrome browser extension management utilities for installing, enabling, disabling, and removing extensions.

## Class: Extension

Modern extension manager with logging and support for both Chromium and ChromiumFromZB browsers.

### Constructor (without instance)

```csharp
public Extension(IZennoPosterProjectModel project, bool log = false)
```

**Purpose**: Creates extension manager without browser instance (for operations not requiring browser).

**Example**:
```csharp
var ext = new Extension(project, log: true);
```

**Breakdown**:
- `project` - ZennoPoster project model
- `log` - Enable detailed logging (default: false)

---

### Constructor (with instance)

```csharp
public Extension(IZennoPosterProjectModel project, Instance instance, bool log = false)
```

**Purpose**: Creates extension manager with browser instance for full extension operations.

**Example**:
```csharp
var ext = new Extension(project, instance, log: true);
```

**Breakdown**:
- `project` - ZennoPoster project model
- `instance` - Browser instance
- `log` - Enable detailed logging (default: false)

---

### GetVer()

```csharp
public string GetVer(string extId)
```

**Purpose**: Retrieves the version of an installed extension from browser profile.

**Example**:
```csharp
var ext = new Extension(project, instance);
string version = ext.GetVer("pbgjpgbpljobkekbhnnmlikbbfhbhmem");
// Returns: "1.2.3"
```

**Breakdown**:
- `extId` - Chrome extension ID (32-character identifier)
- Returns version string from extension manifest
- Reads from browser's Secure Preferences file
- Throws exception if extension not found

---

### InstallFromStore()

```csharp
public bool InstallFromStore(string url, bool log = false)
```

**Purpose**: Installs Chrome extension directly from Chrome Web Store.

**Example**:
```csharp
var ext = new Extension(project, instance);
bool installed = ext.InstallFromStore("https://chromewebstore.google.com/detail/extension-id");
// Returns: true if newly installed, false if already installed
```

**Breakdown**:
- `url` - Chrome Web Store extension URL
- `log` - Enable operation logging (default: false)
- Navigates to store page
- Clicks "Add to Chrome" button if not installed
- Enables extension if already installed but disabled
- Returns true if installation performed, false if already present

---

### InstallFromCrx()

```csharp
public bool InstallFromCrx(string extId, string fileName, bool log = false)
```

**Purpose**: Installs extension from local CRX file.

**Example**:
```csharp
var ext = new Extension(project, instance);
bool installed = ext.InstallFromCrx("pbgjpgbpljobkekbhnnmlikbbfhbhmem", "MyExtension.crx");
```

**Breakdown**:
- `extId` - Expected extension ID after installation
- `fileName` - CRX file name (must be in `ProjectPath\.crx\` folder)
- `log` - Enable operation logging (default: false)
- Checks if extension already installed
- Installs from CRX file if not present
- Returns true if installed, false if already present
- Throws FileNotFoundException if CRX file not found

---

### Switch()

```csharp
public bool Switch(string toUse = "", bool log = false)
```

**Purpose**: Enables specified extensions and disables all others using One-Click Extensions Manager.

**Example**:
```csharp
var ext = new Extension(project, instance);
ext.Switch("MetaMask,Phantom"); // Enable only MetaMask and Phantom
ext.Switch(""); // Disable all extensions
```

**Breakdown**:
- `toUse` - Comma-separated list of extension names or IDs to enable
- `log` - Enable operation logging (default: false)
- Installs One-Click Extensions Manager if needed
- Opens extension manager popup
- Enables extensions matching names/IDs in `toUse`
- Disables all other extensions
- Returns true if any switching performed
- Works only with Chromium and ChromiumFromZB browsers

---

### Rm()

```csharp
public void Rm(string[] ExtToRemove)
```

**Purpose**: Removes (uninstalls) specified extensions from browser.

**Example**:
```csharp
var ext = new Extension(project, instance);
ext.Rm(new[] { "ext-id-1", "ext-id-2", "ext-id-3" });
```

**Breakdown**:
- `ExtToRemove` - Array of extension IDs to remove
- Uninstalls each extension from browser
- Silently catches and logs removal errors
- No return value

---

## Class: ChromeExt

Legacy extension manager with similar functionality (maintained for compatibility).

### Constructor (without instance)

```csharp
public ChromeExt(IZennoPosterProjectModel project, bool log = false)
```

**Purpose**: Creates legacy extension manager without browser instance.

**Example**:
```csharp
var ext = new ChromeExt(project, log: true);
```

**Breakdown**:
- `project` - ZennoPoster project model
- `log` - Enable logging (default: false)

---

### Constructor (with instance)

```csharp
public ChromeExt(IZennoPosterProjectModel project, Instance instance, bool log = false)
```

**Purpose**: Creates legacy extension manager with browser instance.

**Example**:
```csharp
var ext = new ChromeExt(project, instance);
```

**Breakdown**:
- `project` - ZennoPoster project model
- `instance` - Browser instance
- `log` - Enable logging (default: false)

---

### GetVer()

```csharp
public string GetVer(string extId)
```

**Purpose**: Retrieves installed extension version (same as Extension.GetVer).

**Example**:
```csharp
var ext = new ChromeExt(project);
string version = ext.GetVer("pbgjpgbpljobkekbhnnmlikbbfhbhmem");
```

**Breakdown**:
- See Extension.GetVer() documentation

---

### Install()

```csharp
public bool Install(string extId, string fileName, bool log = false)
```

**Purpose**: Installs extension from CRX file (same as Extension.InstallFromCrx).

**Example**:
```csharp
var ext = new ChromeExt(project, instance);
bool installed = ext.Install("ext-id", "Extension.crx");
```

**Breakdown**:
- See Extension.InstallFromCrx() documentation

---

### Switch()

```csharp
public bool Switch(string toUse = "", bool log = false)
```

**Purpose**: Enables/disables extensions (same as Extension.Switch).

**Example**:
```csharp
var ext = new ChromeExt(project, instance);
ext.Switch("MetaMask");
```

**Breakdown**:
- See Extension.Switch() documentation
- Works only with Chromium browser type

---

### Rm()

```csharp
public void Rm(string[] ExtToRemove)
```

**Purpose**: Removes extensions (same as Extension.Rm).

**Example**:
```csharp
var ext = new ChromeExt(project, instance);
ext.Rm(new[] { "ext-id-1", "ext-id-2" });
```

**Breakdown**:
- See Extension.Rm() documentation
