# Db Class Documentation

Static class providing high-level database operations for ZennoPoster projects with support for both SQLite and PostgreSQL.

---

## Public Methods

### DbGet
**Purpose**: Retrieves a single value from the database table.

**Example**:
```csharp
// Get proxy value for current account
string proxy = project.DbGet("proxy");

// Get specific column with custom key
string email = project.DbGet("email", "users", key: "user_id", acc: "123");
```

**Breakdown**:
```csharp
public static string DbGet(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    string toGet,                           // Column name to retrieve
    string tableName = null,                // Table name (default: projectTable variable)
    bool log = false,                       // Enable logging
    bool thrw = false,                      // Throw exceptions if true
    string key = "id",                      // Key column name
    string acc = null,                      // Account ID (default: acc0 variable)
    string where = ""                       // Custom WHERE clause
)
// Returns: String value from database
```

---

### DbGetColumns
**Purpose**: Retrieves a row from database as a dictionary with column names as keys.

**Example**:
```csharp
// Get multiple columns for current account
var userData = project.DbGetColumns("email, username, status");

// Access individual values
string email = userData["email"];
string username = userData["username"];
```

**Breakdown**:
```csharp
public static Dictionary<string, string> DbGetColumns(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    string toGet,                           // Comma-separated column names
    string tableName = null,                // Table name (default: projectTable variable)
    bool log = false,                       // Enable logging
    bool thrw = false,                      // Throw exceptions if true
    string key = "id",                      // Key column name
    object id = null,                       // Record ID (default: acc0 variable)
    string where = ""                       // Custom WHERE clause
)
// Returns: Dictionary with column names as keys and values as strings
```

---

### DbGetLine
**Purpose**: Retrieves a row from database as an array of values.

**Example**:
```csharp
// Get multiple columns as array
string[] userData = project.DbGetLine("email, username, status");

// Access by index
string email = userData[0];
string username = userData[1];
```

**Breakdown**:
```csharp
public static string[] DbGetLine(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    string toGet,                           // Comma-separated column names
    string tableName = null,                // Table name (default: projectTable variable)
    bool log = false,                       // Enable logging
    bool thrw = false,                      // Throw exceptions if true
    string key = "id",                      // Key column name
    object id = null,                       // Record ID (default: acc0 variable)
    string where = ""                       // Custom WHERE clause
)
// Returns: Array of string values
```

---

### DbGetLines
**Purpose**: Retrieves multiple rows from database as a list of strings.

**Example**:
```csharp
// Get all email addresses
var emails = project.DbGetLines("email", where: "status = 'active'");

// Sync to ZennoPoster list
var emails = project.DbGetLines("email", toList: "emailList");
```

**Breakdown**:
```csharp
public static List<string> DbGetLines(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    string toGet,                           // Column name to retrieve
    string tableName = null,                // Table name (default: projectTable variable)
    bool log = false,                       // Enable logging
    bool thrw = false,                      // Throw exceptions if true
    string key = "id",                      // Key column name
    object id = null,                       // Record ID (default: acc0 variable)
    string where = "",                      // Custom WHERE clause
    string toList = null                    // ZennoPoster list name to sync results
)
// Returns: List of string values
```

---

### DbToVars
**Purpose**: Retrieves database row and automatically sets ZennoPoster variables.

**Example**:
```csharp
// Get columns and set as variables
project.DbToVars("email, username, proxy");

// Now variables are available
string email = project.Variables["email"].Value;
```

**Breakdown**:
```csharp
public static Dictionary<string, string> DbToVars(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    string toGet,                           // Comma-separated column names
    string tableName = null,                // Table name (default: projectTable variable)
    bool log = false,                       // Enable logging
    bool thrw = false,                      // Throw exceptions if true
    string key = "id",                      // Key column name
    object id = null,                       // Record ID (default: acc0 variable)
    string where = ""                       // Custom WHERE clause
)
// Returns: Dictionary of retrieved data
// Side effect: Sets project variables with column names
```

---

### JsonToDb
**Purpose**: Parses JSON and writes data to database.

**Example**:
```csharp
string json = "{\"email\":\"test@example.com\",\"status\":\"active\"}";
project.JsonToDb(json, "users");
```

**Breakdown**:
```csharp
public static void JsonToDb(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    string json,                            // JSON string to parse
    string tableName = null,                // Table name (default: projectTable variable)
    bool log = false,                       // Enable logging
    bool thrw = false                       // Throw exceptions if true
)
// Converts JSON to dictionary and updates database
```

---

### DicToDb
**Purpose**: Writes dictionary data to database.

**Example**:
```csharp
var data = new Dictionary<string, string> {
    {"email", "test@example.com"},
    {"status", "active"}
};
project.DicToDb(data, "users");
```

**Breakdown**:
```csharp
public static void DicToDb(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    Dictionary<string,string> dataDic,      // Data to write
    string tableName = null,                // Table name (default: projectTable variable)
    bool log = false,                       // Enable logging
    bool thrw = false                       // Throw exceptions if true
)
// Automatically adds missing columns and updates database
```

---

### DbUpd
**Purpose**: Updates database record with specified values.

**Example**:
```csharp
// Update single column
project.DbUpd("status = 'completed'");

// Update multiple columns
project.DbUpd("email = 'new@example.com', status = 'active'");

// Custom WHERE clause
project.DbUpd("status = 'processed'", where: "status = 'pending'");
```

**Breakdown**:
```csharp
public static void DbUpd(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    string toUpd,                           // Update expression (e.g., "col = 'value'")
    string tableName = null,                // Table name (default: projectTable variable)
    bool log = false,                       // Enable logging
    bool thrw = false,                      // Throw exceptions if true
    string key = "id",                      // Key column name
    object acc = null,                      // Account ID (default: acc0 variable)
    string where = ""                       // Custom WHERE clause
)
// Updates database record and stores query in lastQuery variable
```

---

### DbSettings
**Purpose**: Loads settings from _settings table and optionally sets project variables.

**Example**:
```csharp
// Load and set variables
project.DbSettings();

// Load without setting variables
project.DbSettings(set: false, log: true);
```

**Breakdown**:
```csharp
public static void DbSettings(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    bool set = true,                        // Set project variables from settings
    bool log = false                        // Enable logging
)
// Reads id and value from _settings table
// Sets project variables when set=true
```

---

### MigrateTable
**Purpose**: Copies table structure and data to a new table, renaming legacy columns.

**Example**:
```csharp
// Migrate old table to new structure
project.MigrateTable("old_users", "users");
```

**Breakdown**:
```csharp
public static void MigrateTable(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    string source,                          // Source table name
    string dest                             // Destination table name
)
// Validates table names (alphanumeric and underscores only)
// Copies all data from source to destination
// Renames acc0/key columns to id if they exist
// Throws: ArgumentException if table names are invalid
```

---

### DbGetRandom
**Purpose**: Retrieves random non-empty value(s) from database.

**Example**:
```csharp
// Get random proxy
string proxy = project.DbGetRandom("proxy");

// Get multiple random values
string proxies = project.DbGetRandom("proxy", single: false);

// Get random empty value (invert logic)
string empty = project.DbGetRandom("email", invert: true);
```

**Breakdown**:
```csharp
public static string DbGetRandom(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    string toGet,                           // Column name to retrieve
    string tableName = null,                // Table name (default: projectTable variable)
    bool log = false,                       // Enable logging
    bool acc = false,                       // Include id column in result
    bool thrw = false,                      // Throw exceptions if true
    int range = 0,                          // Max ID to consider (default: from range variable)
    bool single = true,                     // Return single value or multiple
    bool invert = false                     // Get empty values instead of non-empty
)
// Returns: Random value(s) from database
// Uses ORDER BY RANDOM() for selection
```

---

### DbKey
**Purpose**: Retrieves cryptographic key from _wallets table with optional decryption.

**Example**:
```csharp
// Get EVM private key
string evmKey = project.DbKey("evm");

// Get Solana key
string solKey = project.DbKey("sol");

// Get seed phrase
string seed = project.DbKey("seed");
```

**Breakdown**:
```csharp
public static string DbKey(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    string chainType = "evm"                // Chain type: evm, sol, or seed
)
// Supported chainType values:
//   "evm" -> retrieves secp256k1 column
//   "sol" -> retrieves base58 column
//   "seed" -> retrieves bip39 column
// Decrypts value if cfgPin variable is set
// Throws: Exception for unsupported chainType
```

---

### TblAdd
**Purpose**: Creates a new table if it doesn't exist.

**Example**:
```csharp
var structure = new Dictionary<string, string> {
    {"id", "INTEGER PRIMARY KEY"},
    {"email", "TEXT DEFAULT ''"},
    {"status", "TEXT DEFAULT ''"}
};
project.TblAdd(structure, "users");
```

**Breakdown**:
```csharp
public static void TblAdd(
    this IZennoPosterProjectModel project,       // ZennoPoster project instance
    Dictionary<string, string> tableStructure,   // Column definitions
    string tblName,                              // Table name
    bool log = false                             // Enable logging
)
// Does nothing if table already exists
// Automatically converts AUTOINCREMENT to SERIAL for PostgreSQL
// Creates quoted column names for safety
```

---

### TblExist
**Purpose**: Checks if a table exists in the database.

**Example**:
```csharp
if (project.TblExist("users"))
{
    // Table exists
}
```

**Breakdown**:
```csharp
public static bool TblExist(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    string tblName,                         // Table name to check
    bool log = false                        // Enable logging
)
// Returns: true if table exists, false otherwise
// Queries information_schema for PostgreSQL, sqlite_master for SQLite
```

---

### TblList
**Purpose**: Retrieves list of all tables in the database.

**Example**:
```csharp
var tables = project.TblList();
foreach (var table in tables)
{
    project.SendInfoToLog(table, true);
}
```

**Breakdown**:
```csharp
public static List<string> TblList(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    bool log = false                        // Enable logging
)
// Returns: Alphabetically sorted list of table names
// Excludes system tables
```

---

### TblColumns
**Purpose**: Retrieves list of column names for a table.

**Example**:
```csharp
var columns = project.TblColumns("users");
foreach (var column in columns)
{
    project.SendInfoToLog(column, true);
}
```

**Breakdown**:
```csharp
public static List<string> TblColumns(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    string tblName,                         // Table name
    bool log = false                        // Enable logging
)
// Returns: List of column names
// Queries information_schema for PostgreSQL, pragma_table_info for SQLite
```

---

### TblForProject
**Purpose**: Creates table structure definition for project with standard columns.

**Example**:
```csharp
// Basic structure
var structure = project.TblForProject();

// With custom columns
var columns = new List<string> { "email", "username", "proxy" };
var structure = project.TblForProject(columns);
```

**Breakdown**:
```csharp
public static Dictionary<string, string> TblForProject(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    List<string> projectColumns = null,     // Custom columns to include
    string defaultType = "TEXT DEFAULT ''"  // Default column type
)
// Returns: Dictionary with column definitions
// Always includes: id (PRIMARY KEY), status, last
// Adds columns from projectColumns parameter
// Adds columns from cfgToDo variable
// Useful for creating standardized project tables
```

---

### TblPrepareDefault
**Purpose**: Creates and prepares default project table with standard structure.

**Example**:
```csharp
// Creates table with id, status, last columns and range
project.TblPrepareDefault();
```

**Breakdown**:
```csharp
public static void TblPrepareDefault(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    bool log = false                        // Enable logging
)
// Creates table using projectTable variable
// Adds standard columns (id, status, last)
// Adds columns from cfgToDo variable
// Populates table with range from rangeEnd variable
```

---

### PrepareProjectTable
**Purpose**: Creates or updates project table with custom columns and options.

**Example**:
```csharp
var columns = new List<string> { "email", "username", "proxy" };
project.PrepareProjectTable(columns, "users", prune: true);
```

**Breakdown**:
```csharp
public static void PrepareProjectTable(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    List<string> projectColumns = null,     // Custom columns to include
    string tblName = null,                  // Table name (default: projectTable variable)
    bool log = false,                       // Enable logging
    bool prune = false,                     // Remove columns not in structure
    bool rearrange = false                  // Reorder columns to match structure
)
// Creates table if it doesn't exist
// Adds missing columns
// Populates with range if needed
// Optionally removes extra columns (prune)
// Optionally reorders columns (rearrange)
```

---

### ClmnExist
**Purpose**: Checks if a column exists in a table.

**Example**:
```csharp
if (project.ClmnExist("email", "users"))
{
    // Column exists
}
```

**Breakdown**:
```csharp
public static bool ClmnExist(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    string clmnName,                        // Column name to check
    string tblName,                         // Table name
    bool log = false                        // Enable logging
)
// Returns: true if column exists, false otherwise
// Case-insensitive for PostgreSQL
```

---

### ClmnAdd
**Purpose**: Adds a new column to a table if it doesn't exist.

**Example**:
```csharp
// Add single column
project.ClmnAdd("email", "users");

// Add with custom type
project.ClmnAdd("age", "users", defaultValue: "INTEGER DEFAULT 0");

// Add multiple columns from structure
var structure = new Dictionary<string, string> {
    {"email", "TEXT DEFAULT ''"},
    {"phone", "TEXT DEFAULT ''"}
};
project.ClmnAdd(structure, "users");
```

**Breakdown**:
```csharp
public static void ClmnAdd(
    this IZennoPosterProjectModel project,       // ZennoPoster project instance
    string clmnName,                             // Column name
    string tblName = null,                       // Table name (default: projectTable variable)
    bool log = false,                            // Enable logging
    string defaultValue = "TEXT DEFAULT ''"      // Column type and default
)
// Does nothing if column already exists
// Automatically quotes column name
```

---

### ClmnDrop
**Purpose**: Removes a column from a table.

**Example**:
```csharp
// Drop single column
project.ClmnDrop("old_email", "users");
```

**Breakdown**:
```csharp
public static void ClmnDrop(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    string clmnName,                        // Column name to drop
    string tblName,                         // Table name
    bool log = false                        // Enable logging
)
// Does nothing if column doesn't exist
// Adds CASCADE for PostgreSQL to drop dependent objects
```

---

### ClmnPrune
**Purpose**: Removes all columns not present in the table structure.

**Example**:
```csharp
var structure = new Dictionary<string, string> {
    {"id", "INTEGER PRIMARY KEY"},
    {"email", "TEXT DEFAULT ''"}
};
// Removes all columns except id and email
project.ClmnPrune(structure, "users");
```

**Breakdown**:
```csharp
public static void ClmnPrune(
    this IZennoPosterProjectModel project,       // ZennoPoster project instance
    Dictionary<string, string> tableStructure,   // Desired table structure
    string tblName,                              // Table name
    bool log = false                             // Enable logging
)
// Removes any column not in tableStructure
// Useful for cleaning up old/unused columns
// Use with caution - drops data permanently
```

---

### ClmnRearrange
**Purpose**: Reorders table columns to match specified structure.

**Example**:
```csharp
var structure = new Dictionary<string, string> {
    {"id", "INTEGER PRIMARY KEY"},
    {"email", "TEXT DEFAULT ''"},
    {"status", "TEXT DEFAULT ''"}
};
project.ClmnRearrange(structure, "users");
```

**Breakdown**:
```csharp
public static void ClmnRearrange(
    this IZennoPosterProjectModel project,       // ZennoPoster project instance
    Dictionary<string, string> tableStructure,   // Desired column order
    string tblName,                              // Table name
    bool log = false                             // Enable logging
)
// Creates temporary table with new column order
// Copies all data to temporary table
// Drops original table and renames temporary table
// Preserves id column type and primary key
// Throws: Exception if table name is invalid or operation fails
```

---

### AddRange
**Purpose**: Inserts ID records into table up to specified range.

**Example**:
```csharp
// Add IDs 1-100
project.AddRange("users", 100);

// Add up to rangeEnd variable
project.AddRange("users");
```

**Breakdown**:
```csharp
public static void AddRange(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    string tblName,                         // Table name
    int range = 0,                          // Maximum ID (default: rangeEnd variable)
    bool log = false                        // Enable logging
)
// Inserts missing ID records from (current max + 1) to range
// Uses ON CONFLICT DO NOTHING to avoid duplicates
// Defaults to range=10 if rangeEnd variable is empty
```

---

### MigrateAllTables
**Purpose**: Migrates all tables between SQLite and PostgreSQL databases.

**Example**:
```csharp
// Migrates all tables based on current DBmode
project.MigrateAllTables();
```

**Breakdown**:
```csharp
public static void MigrateAllTables(
    this IZennoPosterProjectModel project   // ZennoPoster project instance
)
// Determines migration direction from DBmode variable
// If DBmode is PostgreSQL, migrates to SQLite
// If DBmode is SQLite, migrates to PostgreSQL
// Migrates table structures and all data
// Logs progress and results
// Throws: ArgumentException if DBmode is not PostgreSQL or SQLite
```

---

## DbCore Class

### DbQ
**Purpose**: Executes raw SQL queries against the database.

**Example**:
```csharp
// SELECT query
string result = project.DbQ("SELECT email FROM users WHERE id = 1");

// UPDATE query
project.DbQ("UPDATE users SET status = 'active' WHERE id = 1");

// With logging
project.DbQ("SELECT * FROM users", log: true);
```

**Breakdown**:
```csharp
public static string DbQ(
    this IZennoPosterProjectModel project,  // ZennoPoster project instance
    string query,                           // SQL query to execute
    bool log = false,                       // Enable logging with emoji prefix
    string sqLitePath = null,               // SQLite database path (default: DBsqltPath variable)
    string pgHost = null,                   // PostgreSQL host (default: localhost)
    string pgPort = null,                   // PostgreSQL port (default: 5432)
    string pgDbName = null,                 // PostgreSQL database (default: postgres)
    string pgUser = null,                   // PostgreSQL user (default: postgres)
    string pgPass = null,                   // PostgreSQL password (default: DBpstgrPass variable)
    bool thrw = false,                      // Throw exceptions if true
    bool unSafe = false                     // Allow unsafe operations
)
// Returns: Query result as string
//   - For SELECT: formatted result with separators
//   - For other queries: number of affected rows
// Automatically detects query type (SELECT vs. write operations)
// Logs with database-specific emoji (🐘 for PostgreSQL, SQLite for SQLite)
// Returns empty string on error unless thrw=true
```

---
