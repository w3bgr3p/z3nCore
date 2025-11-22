# SQLite Class Documentation

Static class providing SQLite database operations for ZennoPoster projects using ODBC connection.

---

## Public Methods

### lSQL
**Purpose**: Executes SQL queries against SQLite database.

**Example**:
```csharp
// SELECT query
string result = project.lSQL("SELECT email FROM users WHERE id = 1");

// UPDATE query
project.lSQL("UPDATE users SET status = 'active' WHERE id = 1");

// With logging enabled
project.lSQL("SELECT * FROM users", log: true);

// Ignore errors
project.lSQL("INSERT INTO users (id) VALUES (1)", ignoreErrors: true);
```

**Breakdown**:
```csharp
public static string lSQL(
    IZennoPosterProjectModel project,  // ZennoPoster project instance
    string query,                      // SQL query to execute
    bool log = false,                  // Enable logging with colored output
    bool ignoreErrors = false          // Return empty string on error instead of throwing
)
// Returns: Query result string
//   - SELECT: rows separated by \r\n, columns by |
//   - Other: number of affected rows
// Uses ODBC connection with database path from DBsqltPath variable
// Logging shows different colors for SELECT (Gray) vs. modifications (Default)
// Shows special color when no rows affected
// Throws: Exception on error (unless ignoreErrors=true)
```

---

### lSQLMakeTable
**Purpose**: Creates or updates SQLite table with automatic column management.

**Example**:
```csharp
// Create table with structure
var structure = new Dictionary<string, string> {
    {"id", "INTEGER PRIMARY KEY AUTOINCREMENT"},
    {"email", "TEXT DEFAULT ''"},
    {"status", "TEXT DEFAULT ''"},
    {"acc0", "INTEGER"}
};

project.lSQLMakeTable(structure, "users");

// Strict mode - removes columns not in structure
project.lSQLMakeTable(structure, "users", strictMode: true);

// Use projectTable variable
project.lSQLMakeTable(structure);
```

**Breakdown**:
```csharp
public static void lSQLMakeTable(
    IZennoPosterProjectModel project,         // ZennoPoster project instance
    Dictionary<string, string> tableStructure, // Column name -> SQL type definition
    string tableName = "",                     // Table name (default: projectTable variable)
    bool strictMode = false                    // Remove columns not in structure
)
// Creates table if it doesn't exist
// Adds missing columns if table exists
// Removes extra columns if strictMode=true
// Automatically populates acc0 column with range (1 to rangeEnd variable)
// Uses INSERT OR IGNORE to avoid duplicate IDs
```

---

## Notes

- Uses ZennoPoster's built-in ODBC connection
- Database path must be set in DBsqltPath project variable
- Column separator: `|`
- Row separator: `\r\n`
- Logging includes query compression (removes extra whitespace)
- Supports both table creation and schema updates

---
