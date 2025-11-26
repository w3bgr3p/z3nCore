# Cookies

High-performance cookie management with JSON export/import and Base64 database storage.

## Class: Cookies

### Constructor

```csharp
public Cookies(IZennoPosterProjectModel project, Instance instance, bool log = false)
```

**Purpose**: Creates cookie manager for saving, loading, and manipulating browser cookies.

**Example**:
```csharp
var cookies = new Cookies(project, instance, log: true);
```

**Breakdown**:
- `project` - ZennoPoster project model
- `instance` - Browser instance
- `log` - Enable detailed logging (default: false)

---

### Save()

```csharp
public void Save(string source = null, string jsonPath = null)
```

**Purpose**: Saves cookies to database or file in optimized format with Base64 encoding.

**Example**:
```csharp
var cookies = new Cookies(project, instance);
cookies.Save("project"); // Save current domain cookies to DB
cookies.Save("all"); // Save all cookies to DB
cookies.Save("all", "C:\\cookies.json"); // Save all cookies to DB and JSON file
```

**Breakdown**:
- `source` - Save mode: "project" (current domain only) or "all" (all domains)
- `jsonPath` - Optional file path to also save cookies as JSON
- Saves to `_instance` table with Base64 encoding for database safety
- "project" mode filters cookies by current domain
- "all" mode saves complete cookie container
- Throws exception if source parameter is invalid

---

### SaveAllFast()

```csharp
public void SaveAllFast(string jsonPath = null)
```

**Purpose**: High-performance method to save all browser cookies (all domains).

**Example**:
```csharp
var cookies = new Cookies(project, instance, log: true);
cookies.SaveAllFast(); // Save to DB only
cookies.SaveAllFast("C:\\backup\\cookies.json"); // Save to DB and file
```

**Breakdown**:
- `jsonPath` - Optional file path to also save JSON copy
- Uses native browser export for speed
- Converts to JSON format
- Stores in database with Base64 encoding
- Logs performance metrics if logging enabled
- Automatically cleans up temporary files

---

### SaveProjectFast()

```csharp
public void SaveProjectFast()
```

**Purpose**: High-performance method to save cookies for current domain only.

**Example**:
```csharp
var cookies = new Cookies(project, instance);
instance.Go("https://example.com");
cookies.SaveProjectFast(); // Saves only example.com cookies
```

**Breakdown**:
- No parameters
- Filters cookies by current `instance.ActiveTab.MainDomain`
- Faster than Save("project") for domain-specific operations
- Stores in database with Base64 encoding
- No return value

---

### CookieFix()

```csharp
public static string CookieFix(string brokenJson)
```

**Purpose**: Fixes common JSON formatting issues in cookie strings.

**Example**:
```csharp
string broken = "{\"\"value\":\"abc\" =123}";
string fixed = Cookies.CookieFix(broken);
// Returns properly formatted JSON
```

**Breakdown**:
- `brokenJson` - Malformed cookie JSON string
- Removes double quotes in "value" field
- Fixes "=" spacing issues
- Normalizes cookie IDs to 1
- Returns corrected JSON string
- Returns error message if parsing fails

---

### Set()

```csharp
public void Set(string cookieSource = null, string jsonPath = null)
```

**Purpose**: Loads cookies from database or file into browser.

**Example**:
```csharp
var cookies = new Cookies(project, instance);
cookies.Set(); // Load from _instance table (default)
cookies.Set("dbProject"); // Load from project table
cookies.Set("fromFile", "C:\\cookies.json"); // Load from JSON file
```

**Breakdown**:
- `cookieSource` - Source: "dbMain" (default), "dbProject", or "fromFile"
- `jsonPath` - File path when using "fromFile" source
- Automatically decodes Base64 from database
- "dbMain" reads from `_instance` table
- "dbProject" reads from project-specific table
- "fromFile" reads from specified JSON file path
- Applies cookies to browser instance

---

### Get()

```csharp
public string Get(string domainFilter = "")
```

**Purpose**: Exports current browser cookies as JSON string with optional domain filtering.

**Example**:
```csharp
var cookies = new Cookies(project, instance, log: true);
string allCookies = cookies.Get(); // All cookies
string googleCookies = cookies.Get("google.com"); // Only google.com
string currentDomain = cookies.Get("."); // Current page domain
```

**Breakdown**:
- `domainFilter` - Domain filter: empty (all), "domain.com", or "." (current)
- "." automatically resolves to `instance.ActiveTab.MainDomain`
- Returns JSON array of cookie objects
- Includes all cookie properties: name, value, domain, path, expiry, etc.
- Logs detailed statistics if logging enabled
- Returns JSON string with cookies

---

### GetByJs()

```csharp
public string GetByJs(string domainFilter = "", bool log = false)
```

**Purpose**: Gets cookies using JavaScript (lighter but less complete than Get()).

**Example**:
```csharp
var cookies = new Cookies(project, instance);
string jsCookies = cookies.GetByJs();
project.log(jsCookies);
```

**Breakdown**:
- `domainFilter` - Domain filter (currently not implemented in JS version)
- `log` - Enable logging of result
- Uses JavaScript `document.cookie` API
- Faster but only gets HTTP-accessible cookies
- Cannot access HttpOnly cookies
- Returns JSON string with cookie data

---

### SetByJs()

```csharp
public void SetByJs(string cookiesJson, bool log = false)
```

**Purpose**: Sets cookies using JavaScript for current domain.

**Example**:
```csharp
var cookies = new Cookies(project, instance);
string cookieJson = "[{\"name\":\"token\",\"value\":\"abc123\",\"domain\":\".example.com\"}]";
cookies.SetByJs(cookieJson);
```

**Breakdown**:
- `cookiesJson` - JSON array of cookie objects
- `log` - Enable logging
- Filters cookies by current domain automatically
- Uses JavaScript to set cookies
- Only sets cookies matching current domain or parent domain
- Handles cookie expiration dates
- Deduplicates cookies (last occurrence wins)

---

## Extension Method: ProjectExtensions.DbCookies()

```csharp
public static string DbCookies(this IZennoPosterProjectModel project)
```

**Purpose**: Retrieves and fixes cookies from database in one call.

**Example**:
```csharp
string cookies = project.DbCookies();
// Returns cleaned cookie JSON from database
```

**Breakdown**:
- Extension method on project
- Reads cookies from `_instance` table
- Automatically decodes Base64
- Applies CookieFix() to clean format
- Returns ready-to-use cookie JSON
- Handles old non-Base64 format for backward compatibility
