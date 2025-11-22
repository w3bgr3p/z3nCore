# DBuilder Class Documentation

The `DBuilder` class provides static utility methods for database table creation and data import forms in ZennoPoster projects.

---

## Public Methods

### 1. Columns

**Purpose**: Returns predefined column names for specific table schemas.

**Example**:
```csharp
// Get column names for a Google accounts table
string[] googleColumns = DBuilder.Columns("_google");
// Result: ["status", "last", "cookies", "login", "password", "otpsecret", "otpbackup", "recoveryemail", "recovery_phone"]
```

**Breakdown**:
```csharp
public static string[] Columns(string tableSchem)
{
    // Parameter: tableSchem - name of the table schema (e.g., "_google", "_twitter", "_discord")
    // Returns: string[] - array of column names for the specified schema
    // Returns empty array if schema is not found
}
```

---

### 2. GetLines

**Purpose**: Opens a dialog window for entering multiple lines of text.

**Example**:
```csharp
// Prompt user to input addresses
List<string> addresses = project.GetLines("Enter wallet addresses");
if (addresses != null)
{
    // Process each address
    foreach (var addr in addresses)
    {
        project.SendInfoToLog(addr);
    }
}
```

**Breakdown**:
```csharp
public static List<string> GetLines(
    this IZennoPosterProjectModel project,  // Extension method for project
    string title = "Input lines")            // Dialog window title
{
    // Returns: List<string> - list of entered lines, or null if user cancels
    // Each line is trimmed and separated by newline
}
```

---

### 3. CreateBasicTable

**Purpose**: Creates a database table with predefined structure based on table name.

**Example**:
```csharp
// Create a Google accounts table
project.CreateBasicTable("_google", log: true);

// Create a settings table
project.CreateBasicTable("_settings", log: false);
```

**Breakdown**:
```csharp
public static void CreateBasicTable(
    this IZennoPosterProjectModel project,  // Extension method for project
    string table,                            // Table name (e.g., "_google", "_twitter")
    bool log = false)                        // Whether to log creation process
{
    // Creates table with ID column (INTEGER or TEXT PRIMARY KEY depending on table type)
    // Adds columns based on DBuilder.Columns(table) with TEXT DEFAULT '' type
    // Does nothing if table already exists
}
```

---

### 4. FormKeyBool

**Purpose**: Opens a dialog for entering key-boolean pairs (checkboxes).

**Example**:
```csharp
// Create form with 5 checkbox options
var keys = new List<string> { "chain1", "chain2", "chain3", "chain4", "chain5" };
var labels = new List<string> { "Enable Ethereum", "Enable BSC", "Enable Polygon", "Enable Arbitrum", "Enable Optimism" };

Dictionary<string, bool> selections = project.FormKeyBool(
    quantity: 5,
    keyPlaceholders: keys,
    valuePlaceholders: labels,
    title: "Select Chains",
    prepareUpd: false
);

if (selections != null)
{
    foreach (var item in selections)
    {
        project.SendInfoToLog($"{item.Key}: {item.Value}");
    }
}
```

**Breakdown**:
```csharp
public static Dictionary<string, bool> FormKeyBool(
    this IZennoPosterProjectModel project,  // Extension method for project
    int quantity,                            // Number of checkbox pairs to display
    List<string> keyPlaceholders = null,     // Key labels (left side)
    List<string> valuePlaceholders = null,   // Checkbox labels (right side)
    string title = "Input Key-Bool Pairs",   // Dialog title
    bool prepareUpd = true)                  // If true, uses numeric keys (1,2,3...), else uses label text
{
    // Returns: Dictionary<string, bool> with selected values, or null if cancelled
    // Key: either numeric ID or label text depending on prepareUpd
    // Value: checkbox state (true/false)
}
```

---

### 5. FormKeyString

**Purpose**: Opens a dialog for entering key-value string pairs.

**Example**:
```csharp
// Create form for API credentials
var keys = new List<string> { "apikey", "secret", "passphrase" };
var placeholders = new List<string> { "Enter API key", "Enter secret", "Optional passphrase" };

Dictionary<string, string> credentials = project.FormKeyString(
    quantity: 3,
    keyPlaceholders: keys,
    valuePlaceholders: placeholders,
    title: "API Configuration",
    prepareUpd: false
);

if (credentials != null)
{
    string apiKey = credentials["apikey"];
    string secret = credentials["secret"];
}
```

**Breakdown**:
```csharp
public static Dictionary<string, string> FormKeyString(
    this IZennoPosterProjectModel project,  // Extension method for project
    int quantity,                            // Number of key-value pairs
    List<string> keyPlaceholders = null,     // Default key names
    List<string> valuePlaceholders = null,   // Placeholder text for value fields
    string title = "Input Key-Value Pairs",  // Dialog title
    bool prepareUpd = true)                  // If true, formats for SQL UPDATE, else returns raw key-value
{
    // Returns: Dictionary<string, string> with entered values, or null if cancelled
    // Empty or placeholder-only values are skipped
    // Single quotes in values are escaped for SQL safety
}
```

---

### 6. FormSocial (Overload 1)

**Purpose**: Opens a dialog for importing social account data with predefined field mapping.

**Example**:
```csharp
// Import Twitter accounts
var fields = new string[] { "", "login", "password", "email", "emailpass", "token", "code2fa" };
var mapping = new Dictionary<string, string>
{
    { "login", "login" },
    { "password", "password" },
    { "email", "email" },
    { "emailpass", "emailpass" },
    { "token", "token" },
    { "CODE2FA", "otpsecret" }
};

string result = project.FormSocial(
    tableName: "_twitter",
    formTitle: "Import Twitter Accounts",
    availableFields: fields,
    columnMapping: mapping
);

project.SendInfoToLog($"Imported {result} accounts");
```

**Breakdown**:
```csharp
public static string FormSocial(
    this IZennoPosterProjectModel project,              // Extension method
    string tableName,                                    // Target database table
    string formTitle,                                    // Dialog title
    string[] availableFields,                            // Available field options for dropdown
    Dictionary<string, string> columnMapping,            // Maps form fields to DB columns
    string message = "Select format (one field per box):") // Instruction message
{
    // Returns: string - number of records imported, or "0" if cancelled/error
    // User selects field format via dropdowns
    // Data is entered one account per line, separated by ":"
    // Updates records in database using UPDATE WHERE id = line_number
}
```

---

### 7. FormSocial (Overload 2)

**Purpose**: Opens a dialog for importing data with custom separator and automatic field mapping.

**Example**:
```csharp
// Import accounts with custom format
var availableFields = new List<string> { "", "login", "password", "email", "token" };

string result = project.FormSocial(
    availableFields: availableFields,
    tableName: "_accounts",
    formTitle: "Import Accounts",
    message: "Select field order"
);

project.SendInfoToLog($"{result} records imported");
```

**Breakdown**:
```csharp
public static string FormSocial(
    this IZennoPosterProjectModel project,      // Extension method
    List<string> availableFields,                // List of available field names
    string tableName,                            // Target table name
    string formTitle,                            // Dialog window title
    string message = "Select format (one field per box):") // User instruction
{
    // Returns: string - count of imported records
    // User selects separator character (default ":")
    // Automatically adds rows to table using AddRange
    // Parses data and updates each row with field values
}
```

---

### 8. FormSocial (Overload 3)

**Purpose**: Opens a dialog for importing data using mask syntax with placeholders.

**Example**:
```csharp
// Import using mask format
string result = project.FormSocial(
    tableName: "_twitter",
    formTitle: "Import Twitter Data",
    message: "Enter format mask with {field} syntax"
);

// User enters mask like: {login}:{password}:{email}
// Then enters data like: user1:pass123:user1@mail.com
```

**Breakdown**:
```csharp
public static string FormSocial(
    this IZennoPosterProjectModel project,     // Extension method
    string tableName,                           // Database table name
    string formTitle,                           // Dialog title
    string message = "Enter mask format using {field} syntax:") // Instructions
{
    // Returns: string - number of imported records
    // Uses mask parsing with {fieldname} placeholders
    // Supports flexible data formats
    // Automatically maps parsed fields to database columns
    // Special handling for CODE2FA field (extracts value after '/')
}
```

---

