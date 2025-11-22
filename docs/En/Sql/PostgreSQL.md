# PostgresDB Class Documentation

Class for direct PostgreSQL database operations using Npgsql driver.

---

## Public Methods

### Constructor
**Purpose**: Creates a new PostgreSQL database connection.

**Example**:
```csharp
// Basic connection
var db = new PostgresDB("localhost", "mydb", "postgres", "password");

// With port specified
var db = new PostgresDB("localhost:5433", "mydb", "postgres", "password");
```

**Breakdown**:
```csharp
public PostgresDB(
    string host,        // Host address with optional port (e.g., "localhost:5432")
    string database,    // Database name
    string user,        // Username
    string password     // Password
)
// Creates connection string but doesn't open connection
// Call Open() method to establish connection
// Supports host:port format for host parameter
```

---

### Open
**Purpose**: Opens the database connection.

**Example**:
```csharp
var db = new PostgresDB("localhost", "mydb", "postgres", "password");
db.Open();
// Connection is now ready for queries
```

**Breakdown**:
```csharp
public void Open()
// Opens the PostgreSQL connection
// Throws: Exception with "DB connection failed" message on error
// Disposes connection if open fails
```

---

### DbRead
**Purpose**: Executes SELECT query and returns formatted results.

**Example**:
```csharp
db.Open();
string result = db.DbRead("SELECT email, username FROM users");
// Result format: "email1|username1\r\nemail2|username2"

// Custom separator
string result = db.DbRead("SELECT * FROM users", separator: ",");
```

**Breakdown**:
```csharp
public string DbRead(
    string sql,              // SQL SELECT query
    string separator = "|"   // Column separator in results
)
// Returns: Formatted string with rows separated by \r\n
// Each row has columns separated by the separator parameter
// Throws: InvalidOperationException if connection is not open
```

---

### DbWrite
**Purpose**: Executes INSERT, UPDATE, DELETE or other non-query SQL commands.

**Example**:
```csharp
db.Open();

// Simple update
int affected = db.DbWrite("UPDATE users SET status = 'active' WHERE id = 1");

// With parameters (safer against SQL injection)
var param = new NpgsqlParameter("@email", "test@example.com");
int affected = db.DbWrite("UPDATE users SET email = @email WHERE id = 1", param);
```

**Breakdown**:
```csharp
public int DbWrite(
    string sql,                        // SQL command (INSERT, UPDATE, DELETE, etc.)
    params NpgsqlParameter[] parameters // Optional parameters for parameterized queries
)
// Returns: Number of rows affected
// Supports parameterized queries for security
// Throws: Exception with SQL error message and query
// InvalidOperationException if connection is not open
```

---

### Raw (Static)
**Purpose**: Executes a query with automatic connection management.

**Example**:
```csharp
// No need to manage connection lifecycle
string result = PostgresDB.Raw(
    "SELECT email FROM users WHERE id = 1",
    host: "localhost:5432",
    dbName: "mydb",
    dbUser: "postgres",
    dbPswd: "password"
);

// Connection is automatically opened and disposed
```

**Breakdown**:
```csharp
public static string Raw(
    string query,                    // SQL query to execute
    bool throwOnEx = false,          // Throw exceptions if true
    string host = "localhost:5432",  // Host with optional port
    string dbName = "postgres",      // Database name
    string dbUser = "postgres",      // Username
    string dbPswd = ""               // Password (required)
)
// Returns: Query result as string
//   - SELECT queries: formatted result
//   - Other queries: number of affected rows
// Automatically opens connection, executes query, and disposes
// Returns error message instead of throwing (unless throwOnEx=true)
// Throws: Exception if password is null or empty
```

---

### DbQueryPostgre (Static)
**Purpose**: Executes query using ZennoPoster project variables for connection.

**Example**:
```csharp
// Uses project variables for connection details
string result = PostgresDB.DbQueryPostgre(
    project,
    "SELECT * FROM users",
    throwOnEx: false
);
```

**Breakdown**:
```csharp
public static string DbQueryPostgre(
    IZennoPosterProjectModel project,  // ZennoPoster project instance
    string query,                      // SQL query to execute
    bool throwOnEx = false,            // Throw exceptions if true
    string host = "localhost:5432",    // Host with optional port
    string dbName = "postgres",        // Database name (default: DBpstgrName variable)
    string dbUser = "postgres",        // Username (default: DBpstgrUser variable)
    string dbPswd = ""                 // Password (default: DBpstgrPass variable)
)
// Returns: Query result as string
// Uses project variables when parameters are null:
//   - DBpstgrName for database name
//   - DBpstgrUser for username
//   - DBpstgrPass for password
// Logs warnings on error
// Returns empty string on error unless throwOnEx=true
// Throws: Exception if password is null or empty
```

---

### MkTablePostgre (Static)
**Purpose**: Creates or updates PostgreSQL table with automatic column management.

**Example**:
```csharp
var structure = new Dictionary<string, string> {
    {"id", "INTEGER PRIMARY KEY"},
    {"email", "TEXT DEFAULT ''"},
    {"acc0", "INTEGER"}
};

PostgresDB.MkTablePostgre(
    project,
    structure,
    tableName: "users",
    strictMode: false,
    insertData: true
);
```

**Breakdown**:
```csharp
public static void MkTablePostgre(
    IZennoPosterProjectModel project,       // ZennoPoster project instance
    Dictionary<string, string> tableStructure, // Column definitions
    string tableName = "",                   // Table name (default: projectTable variable)
    bool strictMode = false,                 // Remove columns not in structure
    bool insertData = true,                  // Insert initial data range
    string host = null,                      // Host address
    string dbName = "postgres",              // Database name
    string dbUser = "postgres",              // Username
    string dbPswd = "",                      // Password (default: DBpstgrPass variable)
    string schemaName = "projects",          // Schema name
    bool log = false                         // Enable logging
)
// Creates table if it doesn't exist
// Adds missing columns
// Removes extra columns if strictMode=true
// Inserts range of acc0 values if acc0 column exists and insertData=true
// Automatically converts AUTOINCREMENT to SERIAL
// Uses rangeEnd variable for data range
// Throws: Exception if password is null
```

---

### Dispose
**Purpose**: Closes and disposes the database connection.

**Example**:
```csharp
var db = new PostgresDB("localhost", "mydb", "postgres", "password");
db.Open();
// ... perform operations ...
db.Dispose();

// Or use with using statement (recommended)
using (var db = new PostgresDB("localhost", "mydb", "postgres", "password"))
{
    db.Open();
    // Connection automatically disposed
}
```

**Breakdown**:
```csharp
public void Dispose()
// Closes the database connection
// Releases all resources
// Safe to call multiple times
// Automatically called when using 'using' statement
```

---

## Private Methods (For Reference)

### CheckAndCreateTable
Creates table if it doesn't exist in the specified schema.

### ManageColumns
Adds new columns and optionally removes columns not in structure (strictMode).

### InsertInitialData
Inserts range of acc0 values from current max to rangeEnd.

---
