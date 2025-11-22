# Sql Class Documentation

Instance-based class providing unified database operations for both SQLite and PostgreSQL with automatic detection.

---

## Constructor

### Sql
**Purpose**: Creates a new Sql instance with automatic database mode detection.

**Example**:
```csharp
// Create instance with logging
var sql = new Sql(project, log: true);

// Without logging
var sql = new Sql(project);
```

**Breakdown**:
```csharp
public Sql(
    IZennoPosterProjectModel project,  // ZennoPoster project instance
    bool log = false                   // Enable logging with database-specific emoji
)
// Automatically detects database mode from DBmode variable
// Sets up logger with emoji: 🐘 for PostgreSQL, ✒ for SQLite
// Reads password from DBpstgrPass variable
```

---

## Public Methods

### MkTable
**Purpose**: Creates or updates table with automatic column management.

**Example**:
```csharp
var sql = new Sql(project);
var structure = new Dictionary<string, string> {
    {"id", "INTEGER PRIMARY KEY"},
    {"email", "TEXT DEFAULT ''"},
    {"status", "TEXT DEFAULT ''"}
};

sql.MkTable(structure, "users");

// Strict mode removes extra columns
sql.MkTable(structure, "users", strictMode: true);
```

**Breakdown**:
```csharp
public void MkTable(
    Dictionary<string, string> tableStructure,  // Column definitions
    string tableName = null,                    // Table name (default: from project settings)
    bool strictMode = false,                    // Remove columns not in structure
    bool insertData = false,                    // Insert initial data range
    string host = "localhost:5432",             // PostgreSQL host
    string dbName = "postgres",                 // Database name
    string dbUser = "postgres",                 // Username
    string dbPswd = "",                         // Password
    string schemaName = "projects",             // PostgreSQL schema
    bool log = false                            // Enable logging
)
// Works with both SQLite and PostgreSQL
// Automatically converts types for target database
// Uses makeTable variable to determine if table should be created
```

---

### Write
**Purpose**: Writes key-value pairs to table with upsert logic.

**Example**:
```csharp
var sql = new Sql(project);
var data = new Dictionary<string, string> {
    {"api_key", "abc123"},
    {"api_secret", "secret456"}
};

sql.Write(data, "settings");
```

**Breakdown**:
```csharp
public void Write(
    Dictionary<string, string> toWrite,  // Key-value pairs to write
    string tableName = null,             // Table name (default: projectTable variable)
    bool log = false,                    // Enable logging
    bool throwOnEx = false,              // Throw exceptions if true
    bool last = true                     // Update last column with timestamp
)
// Uses ON CONFLICT to update existing records
// Escapes single quotes in keys and values
// Each key-value pair is a separate INSERT/UPDATE
```

---

### Upd (String Version)
**Purpose**: Updates database records with SQL update expression.

**Example**:
```csharp
var sql = new Sql(project);

// Update by id
sql.Upd("status = 'completed', result = 'success'", "users");

// Custom WHERE clause
sql.Upd("status = 'active'", "users", where: "status = 'pending'");

// Update by custom key
sql.Upd("email = 'new@example.com'", "users", key: "username", acc: "john");
```

**Breakdown**:
```csharp
public void Upd(
    string toUpd,               // Update expression (e.g., "col = 'value'")
    string tableName = null,    // Table name (default: projectTable variable)
    bool log = false,           // Enable logging
    bool throwOnEx = false,     // Throw exceptions if true
    bool last = true,           // Automatically update last column
    string key = "id",          // Key column name
    object acc = null,          // Key value (default: acc0 variable)
    string where = ""           // Custom WHERE clause
)
// Automatically adds timestamp to last column if last=true
// Quotes column names for safety
// Skips last update for special tables (blockchain, browser, etc.)
```

---

### Upd (Dictionary Version)
**Purpose**: Updates database using dictionary of column-value pairs.

**Example**:
```csharp
var sql = new Sql(project);
var updates = new Dictionary<string, string> {
    {"email", "test@example.com"},
    {"status", "active"}
};

sql.Upd(updates, "users");
```

**Breakdown**:
```csharp
public void Upd(
    Dictionary<string, string> toWrite,  // Column-value pairs
    string tableName = null,             // Table name
    bool log = false,                    // Enable logging
    bool throwOnEx = false,              // Throw exceptions if true
    bool last = true,                    // Update last column
    string key = "id",                   // Key column name
    object acc = null,                   // Key value
    string where = ""                    // Custom WHERE clause
)
// Converts dictionary to SQL update expression
// Calls string version of Upd method
```

---

### Upd (List Version)
**Purpose**: Updates table with list of values for a specific column.

**Example**:
```csharp
var sql = new Sql(project);
var emails = new List<string> {
    "user1@example.com",
    "user2@example.com",
    "user3@example.com"
};

sql.Upd(emails, "email", "users");
```

**Breakdown**:
```csharp
public void Upd(
    List<string> toWrite,     // List of values to write
    string columnName,        // Column name to update
    string tableName = null,  // Table name
    bool log = false,         // Enable logging
    bool throwOnEx = false,   // Throw exceptions if true
    bool last = true,         // Update last column
    bool byKey = false        // Reserved parameter
)
// Automatically adds range to accommodate list size
// Updates sequentially by id (1, 2, 3, ...)
// Escapes single quotes in values
```

---

### Get
**Purpose**: Retrieves data from database.

**Example**:
```csharp
var sql = new Sql(project);

// Get single value
string email = sql.Get("email", "users");

// Get multiple columns
string data = sql.Get("email, username", "users");

// Custom WHERE
string email = sql.Get("email", "users", where: "status = 'active'");
```

**Breakdown**:
```csharp
public string Get(
    string toGet,            // Comma-separated column names
    string tableName = null, // Table name (default: projectTable variable)
    bool log = false,        // Enable logging
    bool throwOnEx = false,  // Throw exceptions if true
    string key = "id",       // Key column name
    string acc = null,       // Key value (default: acc0 variable)
    string where = ""        // Custom WHERE clause
)
// Returns: Formatted string with column separator
// Quotes column names automatically
// Throws: ArgumentException if toGet is empty
```

---

### GetRandom
**Purpose**: Retrieves random non-empty value(s) from database.

**Example**:
```csharp
var sql = new Sql(project);

// Get random proxy
string proxy = sql.GetRandom("proxy");

// Get multiple random values
string proxies = sql.GetRandom("proxy", single: false);

// Include id in result
string data = sql.GetRandom("proxy", acc: true);
```

**Breakdown**:
```csharp
public string GetRandom(
    string toGet,            // Column name
    string tableName = null, // Table name
    bool log = false,        // Enable logging
    bool acc = false,        // Include id column in result
    bool throwOnEx = false,  // Throw exceptions if true
    int range = 0,           // Max id to consider (default: from range variable)
    bool single = true,      // Return single row or multiple
    bool invert = false      // Get empty values instead
)
// Returns: Random value(s) using ORDER BY RANDOM()
// Filters out empty values (unless invert=true)
// Limits by range to avoid scanning entire table
```

---

### GetColumns
**Purpose**: Gets comma-separated list of column names.

**Example**:
```csharp
var sql = new Sql(project);
string columns = sql.GetColumns("users");
// Result: "id, email, username, status"
```

**Breakdown**:
```csharp
public string GetColumns(
    string tableName,  // Table name
    bool log = false   // Enable logging
)
// Returns: Comma-separated column names
// Queries information_schema for PostgreSQL
// Queries pragma_table_info for SQLite
```

---

### GetColumnList
**Purpose**: Gets list of column names for a table.

**Example**:
```csharp
var sql = new Sql(project);
List<string> columns = sql.GetColumnList("users");
foreach (var col in columns)
{
    project.SendInfoToLog(col, true);
}
```

**Breakdown**:
```csharp
public List<string> GetColumnList(
    string tableName,  // Table name
    bool log = false   // Enable logging
)
// Returns: List of column names
// Useful for iteration or processing
```

---

### TblExist
**Purpose**: Checks if a table exists.

**Example**:
```csharp
var sql = new Sql(project);
if (sql.TblExist("users"))
{
    // Table exists
}
```

**Breakdown**:
```csharp
public bool TblExist(
    string tblName  // Table name to check
)
// Returns: true if table exists, false otherwise
```

---

### TblAdd
**Purpose**: Creates a new table if it doesn't exist.

**Example**:
```csharp
var sql = new Sql(project);
var structure = new Dictionary<string, string> {
    {"id", "INTEGER PRIMARY KEY"},
    {"email", "TEXT DEFAULT ''"}
};
sql.TblAdd("users", structure);
```

**Breakdown**:
```csharp
public void TblAdd(
    string tblName,                             // Table name
    Dictionary<string, string> tableStructure   // Column definitions
)
// Does nothing if table already exists
// Automatically converts AUTOINCREMENT to SERIAL for PostgreSQL
```

---

### ClmnExist
**Purpose**: Checks if a column exists in a table.

**Example**:
```csharp
var sql = new Sql(project);
if (sql.ClmnExist("users", "email"))
{
    // Column exists
}
```

**Breakdown**:
```csharp
public bool ClmnExist(
    string tblName,  // Table name
    string clmnName  // Column name
)
// Returns: true if column exists, false otherwise
// Case-insensitive for PostgreSQL
```

---

### ClmnAdd (Single Column)
**Purpose**: Adds a column to a table if it doesn't exist.

**Example**:
```csharp
var sql = new Sql(project);
sql.ClmnAdd("users", "phone_number");

// With custom type
sql.ClmnAdd("users", "age", "INTEGER DEFAULT 0");
```

**Breakdown**:
```csharp
public void ClmnAdd(
    string tblName,                          // Table name
    string clmnName,                         // Column name
    string defaultValue = "TEXT DEFAULT ''"  // Column type and default
)
// Does nothing if column already exists
```

---

### ClmnAdd (Dictionary)
**Purpose**: Adds multiple columns from dictionary.

**Example**:
```csharp
var sql = new Sql(project);
var structure = new Dictionary<string, string> {
    {"email", "TEXT DEFAULT ''"},
    {"phone", "TEXT DEFAULT ''"}
};
sql.ClmnAdd("users", structure);
```

**Breakdown**:
```csharp
public void ClmnAdd(
    string tblName,                             // Table name
    Dictionary<string, string> tableStructure   // Columns to add
)
// Adds only columns that don't exist
// Logs which columns are being added
```

---

### AddRange
**Purpose**: Inserts ID records into table.

**Example**:
```csharp
var sql = new Sql(project);
sql.AddRange("users", 100);  // Adds IDs 1-100
```

**Breakdown**:
```csharp
public void AddRange(
    string tblName,  // Table name
    int range = 0    // Max ID (default: rangeEnd variable)
)
// Inserts missing IDs from (current max + 1) to range
// Uses ON CONFLICT DO NOTHING
// Defaults to 100 if rangeEnd is not set
```

---

### Settings
**Purpose**: Loads settings from _settings table.

**Example**:
```csharp
var sql = new Sql(project);

// Load and set variables
var config = sql.Settings();

// Load without setting variables
var config = sql.Settings(set: false);
```

**Breakdown**:
```csharp
public Dictionary<string, string> Settings(
    bool set = true  // Set project variables from settings
)
// Returns: Dictionary of settings
// Reads key-value pairs from _settings table
// Optionally sets project variables
```

---

### Address
**Purpose**: Retrieves blockchain address for specified chain.

**Example**:
```csharp
var sql = new Sql(project);

// Get EVM address
string evmAddr = sql.Address("evm");

// Get Solana address
string solAddr = sql.Address("sol");
```

**Breakdown**:
```csharp
public string Address(
    string chainType = "evm"  // Chain type column name
)
// Returns: Address from _addresses table
// Sets project variable address{CHAINTYPE} with result
// Reads from current account (acc0 variable)
```

---

### Key
**Purpose**: Retrieves cryptographic private key with optional decryption.

**Example**:
```csharp
var sql = new Sql(project);

// Get EVM private key
string evmKey = sql.Key("evm");

// Get Solana key
string solKey = sql.Key("sol");

// Get seed phrase
string seed = sql.Key("seed");
```

**Breakdown**:
```csharp
public string Key(
    string chainType = "evm"  // Key type: evm, sol, or seed
)
// Supported types:
//   "evm" -> secp256k1 column
//   "sol" -> base58 column
//   "seed" -> bip39 column
// Decrypts using cfgPin variable if set
// Throws: Exception for unsupported chainType
```

---

## Public Fields

### columnsDefault
Default columns for standard tables.

```csharp
public string[] columnsDefault = {
    "status",
    "last",
};
```

### columnsSocial
Standard columns for social media tables.

```csharp
public string[] columnsSocial = {
    "status",
    "last",
    "cookie",
    "login",
    "pass",
    "otpsecret",
    "email",
    "recovery",
};
```

---
