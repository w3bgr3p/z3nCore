# BrowserScan

Browser fingerprinting and timezone configuration utilities using browserscan.net.

## Class: BrowserScan

### Constructor

```csharp
public BrowserScan(IZennoPosterProjectModel project, Instance instance, bool log = false)
```

**Purpose**: Creates a new BrowserScan instance for analyzing browser fingerprints.

**Example**:
```csharp
var scanner = new BrowserScan(project, instance, log: true);
```

**Breakdown**:
- `project` - ZennoPoster project model
- `instance` - Browser instance to analyze
- `log` - Enable logging (default: false)

---

### ParseStats()

```csharp
public void ParseStats()
```

**Purpose**: Navigates to browserscan.net, parses browser fingerprint statistics, and saves them to the database.

**Example**:
```csharp
var scanner = new BrowserScan(project, instance);
scanner.ParseStats(); // Saves WebGL, Audio, Fonts, etc. to _browserscan table
```

**Breakdown**:
- Loads browserscan.net and waits for scan completion
- Extracts hardware data: WebGL, WebGLReport, Audio, ClientRects, WebGPUReport
- Extracts software data: Fonts, TimeZoneBasedonIP, TimeFromIP
- Saves all data to `_browserscan` database table
- No parameters
- No return value
- May throw timeout exception if page loading exceeds 60 seconds

---

### GetScore()

```csharp
public string GetScore()
```

**Purpose**: Gets the browser fingerprint score from browserscan.net with problem details if not 100%.

**Example**:
```csharp
var scanner = new BrowserScan(project, instance);
string score = scanner.GetScore();
// Returns: "[100%] " or "[85%] WebGL: Suspicious; Audio: Mismatched"
```

**Breakdown**:
- Loads browserscan.net and reads the score
- Returns formatted string: `[score%] problems`
- If score is 100%, returns `[100%] ` (no problems)
- If score < 100%, includes detected issues
- Returns string with score percentage and problem details

---

### FixTime()

```csharp
public void FixTime()
```

**Purpose**: Automatically fixes browser timezone to match IP geolocation according to browserscan.net.

**Example**:
```csharp
var scanner = new BrowserScan(project, instance, log: true);
scanner.FixTime(); // Sets timezone to match IP location
```

**Breakdown**:
- Reads timezone data from browserscan.net
- Extracts GMT offset and IANA timezone name
- Sets browser timezone using `instance.SetTimezone()`
- Sets IANA timezone using `instance.SetIanaTimezone()`
- No parameters
- No return value

---

## Extension Method: ProjectExtensions.FixTime()

```csharp
public static void FixTime(this IZennoPosterProjectModel project, Instance instance, bool log = false)
```

**Purpose**: Quick extension method to fix timezone without creating BrowserScan instance.

**Example**:
```csharp
project.FixTime(instance, log: true);
```

**Breakdown**:
- `project` - ZennoPoster project model
- `instance` - Browser instance to fix
- `log` - Enable logging (default: false)
- Navigates to browserscan.net
- Automatically adjusts timezone to match IP
- Catches and logs exceptions without throwing
