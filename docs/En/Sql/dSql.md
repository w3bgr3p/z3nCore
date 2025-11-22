# dSql Class Documentation

Universal database class supporting both SQLite and PostgreSQL with async operations and migration capabilities.

---

## Constructor Overloads

### Constructor (SQLite)
**Purpose**: Creates SQLite database connection.

**Example**:
```csharp
var db = new dSql("/path/to/database.db", null);
db.Dispose();
```

**Breakdown**:
```csharp
public dSql(
    string dbPath,   // Path to SQLite database file
    string dbPass    // Password (not used for SQLite, pass null)
)
// Opens connection immediately
// Connection is ready for queries after construction
```

---

### Constructor (PostgreSQL with Details)
**Purpose**: Creates PostgreSQL connection with individual parameters.

**Example**:
```csharp
var db = new dSql("localhost", "5432", "mydb", "postgres", "password");
db.Dispose();
```

**Breakdown**:
```csharp
public dSql(
    string hostname,  // Server hostname
    string port,      // Port number
    string database,  // Database name
    string user,      // Username
    string password   // Password
)
// Opens connection immediately
// Uses connection pooling
```

---

### Constructor (PostgreSQL with Connection String)
**Purpose**: Creates PostgreSQL connection from connection string.

**Example**:
```csharp
string connStr = "Host=localhost;Port=5432;Database=mydb;Username=postgres;Password=pass";
var db = new dSql(connStr);
db.Dispose();
```

**Breakdown**:
```csharp
public dSql(
    string connectionstring  // Full PostgreSQL connection string
)
// Opens connection immediately
```

---

### Constructor (Existing Connection)
**Purpose**: Wraps existing database connection.

**Example**:
```csharp
IDbConnection existingConn = GetConnection();
var db = new dSql(existingConn);
```

**Breakdown**:
```csharp
public dSql(
    IDbConnection connection  // Existing open connection
)
// Opens connection if not already open
// Does not dispose the original connection
// Throws: ArgumentNullException if connection is null
```

---

## Properties

### ConnectionType
**Purpose**: Gets the database type of current connection.

**Example**:
```csharp
var db = new dSql("/path/to/db.db", null);
if (db.ConnectionType == DatabaseType.SQLite)
{
    // SQLite-specific logic
}
```

**Breakdown**:
```csharp
public DatabaseType ConnectionType { get; }
// Returns: DatabaseType enum value
//   - DatabaseType.SQLite
//   - DatabaseType.PostgreSQL
//   - DatabaseType.Unknown
```

---

## Read/Write Methods

### DbReadAsync
**Purpose**: Executes SELECT query asynchronously and returns formatted results.

**Example**:
```csharp
var db = new dSql("/path/to/db.db", null);
string result = await db.DbReadAsync("SELECT email, username FROM users");
// Result: "email1¦username1·email2¦username2"

// Custom separators
string result = await db.DbReadAsync("SELECT * FROM users", "|", "\n");
```

**Breakdown**:
```csharp
public async Task<string> DbReadAsync(
    string sql,                      // SQL SELECT query
    string columnSeparator = "|",    // Separator between columns
    string rawSeparator = "\r\n"     // Separator between rows
)
// Returns: Task<string> with formatted results
// Empty cells are returned as empty strings
// Throws: NotSupportedException for unknown connection type
//         ObjectDisposedException if disposed
```

---

### DbRead
**Purpose**: Executes SELECT query synchronously (wrapper for DbReadAsync).

**Example**:
```csharp
var db = new dSql("/path/to/db.db", null);
string result = db.DbRead("SELECT email FROM users");
```

**Breakdown**:
```csharp
public string DbRead(
    string sql,              // SQL SELECT query
    string separator = "|"   // Column separator
)
// Returns: Formatted query results
// Synchronous wrapper around DbReadAsync
// Uses \r\n as row separator
```

---

### DbWriteAsync
**Purpose**: Executes INSERT, UPDATE, DELETE or DDL commands asynchronously.

**Example**:
```csharp
var db = new dSql("/path/to/db.db", null);

// Simple update
int affected = await db.DbWriteAsync("UPDATE users SET status = 'active'");

// With parameters
var param = db.CreateParameter("@email", "test@example.com");
int affected = await db.DbWriteAsync("UPDATE users SET email = @email", param);
```

**Breakdown**:
```csharp
public async Task<int> DbWriteAsync(
    string sql,                          // SQL command
    params IDbDataParameter[] parameters // Optional parameters
)
// Returns: Task<int> with number of rows affected
// Supports parameterized queries
// Throws: Exception with query details on error
//         NotSupportedException for unknown connection type
```

---

### DbWrite
**Purpose**: Executes write command synchronously (wrapper for DbWriteAsync).

**Example**:
```csharp
var db = new dSql("/path/to/db.db", null);
int affected = db.DbWrite("DELETE FROM users WHERE status = 'inactive'");
```

**Breakdown**:
```csharp
public int DbWrite(
    string sql,                          // SQL command
    params IDbDataParameter[] parameters // Optional parameters
)
// Returns: Number of rows affected
// Synchronous wrapper around DbWriteAsync
```

---

## Parameter Helpers

### CreateParameter
**Purpose**: Creates database-specific parameter for safe queries.

**Example**:
```csharp
var db = new dSql("/path/to/db.db", null);
var param = db.CreateParameter("@email", "test@example.com");
db.DbWrite("UPDATE users SET email = @email WHERE id = 1", param);
```

**Breakdown**:
```csharp
public IDbDataParameter CreateParameter(
    string name,   // Parameter name (with @ prefix)
    object value   // Parameter value (null becomes DBNull.Value)
)
// Returns: Database-specific parameter object
//   - SqliteParameter for SQLite
//   - NpgsqlParameter for PostgreSQL
// Throws: NotSupportedException for unknown connection type
```

---

### CreateParameters
**Purpose**: Creates multiple parameters from tuples.

**Example**:
```csharp
var db = new dSql("/path/to/db.db", null);
var parameters = db.CreateParameters(
    ("@email", "test@example.com"),
    ("@status", "active")
);
db.DbWrite("UPDATE users SET email = @email, status = @status", parameters);
```

**Breakdown**:
```csharp
public IDbDataParameter[] CreateParameters(
    params (string name, object value)[] parameters  // Name-value tuples
)
// Returns: Array of database parameters
// Convenient for multiple parameters
```

---

## Table Operations

### CopyTableAsync
**Purpose**: Copies table structure and data to a new table.

**Example**:
```csharp
var db = new dSql("/path/to/db.db", null);

// Copy within same database
int rows = await db.CopyTableAsync("old_users", "new_users");

// PostgreSQL with schemas
int rows = await db.CopyTableAsync("public.old_users", "archive.users");
```

**Breakdown**:
```csharp
public async Task<int> CopyTableAsync(
    string sourceTable,       // Source table name (optionally schema.table)
    string destinationTable   // Destination table name
)
// Returns: Task<int> with number of rows copied
// Copies complete table structure including:
//   - Column types
//   - NOT NULL constraints
//   - DEFAULT values
//   - PRIMARY KEY constraints
// Creates destination table automatically
// Supports schema qualification for PostgreSQL
// Throws: ArgumentException for invalid table names or formats
//         Exception with detailed error messages
```

---

### MigrateAllTablesAsync (Static)
**Purpose**: Migrates all tables between SQLite and PostgreSQL databases.

**Example**:
```csharp
var sqliteDb = new dSql("/path/to/sqlite.db", null);
var pgDb = new dSql("Host=localhost;Database=postgres;Username=postgres;Password=pass");

int totalRows = await dSql.MigrateAllTablesAsync(sqliteDb, pgDb);
// All tables migrated from SQLite to PostgreSQL

sqliteDb.Dispose();
pgDb.Dispose();
```

**Breakdown**:
```csharp
public static async Task<int> MigrateAllTablesAsync(
    dSql sourceDb,       // Source database connection
    dSql destinationDb   // Destination database connection
)
// Returns: Task<int> with total rows migrated
// Migrates all user tables (excludes system tables)
// Automatically handles:
//   - Type conversion between databases
//   - Schema detection
//   - Primary key preservation
//   - DEFAULT value conversion
// Skips tables that already exist in destination
// Continues migration even if individual tables fail
// Throws: ArgumentNullException if either db is null
//         ArgumentException if source and destination are same type
//         NotSupportedException for unsupported database types
```

---

## CRUD Operations with Dapper

### Upd (Update Record)
**Purpose**: Updates database record with Dapper integration.

**Example**:
```csharp
var db = new dSql("/path/to/db.db", null);

// Update by id
await db.Upd("email = 'new@example.com', status = 'active'",
             id: 123,
             tableName: "users");

// With last timestamp
await db.Upd("status = 'completed'",
             id: 123,
             tableName: "users",
             last: true);

// Custom WHERE
await db.Upd("status = 'processed'",
             id: null,
             tableName: "users",
             where: "status = 'pending'");
```

**Breakdown**:
```csharp
public async Task<int> Upd(
    string toUpd,            // Update expression
    object id,               // Record ID
    string tableName = null, // Table name (required)
    string where = null,     // Custom WHERE clause
    bool last = false        // Add last timestamp column
)
// Returns: Task<int> with number of rows affected
// Automatically quotes column names
// Adds last column with UTC timestamp if last=true
// Throws: Exception if tableName is null
//         Exception with formatted query on error
```

---

### Upd (Update Multiple Records from List)
**Purpose**: Updates multiple records from a list of values.

**Example**:
```csharp
var db = new dSql("/path/to/db.db", null);
var values = new List<string> { "value1", "value2", "value3" };
await db.Upd(values, tableName: "users");
```

**Breakdown**:
```csharp
public async Task Upd(
    List<string> toWrite,    // Values to write
    string tableName = null, // Table name
    string where = null,     // Custom WHERE
    bool last = false        // Add timestamp
)
// Updates records with id = 0, 1, 2, ... sequentially
```

---

### Get
**Purpose**: Retrieves value from database using Dapper.

**Example**:
```csharp
var db = new dSql("/path/to/db.db", null);
string email = await db.Get("email", "123", "users");

// Custom WHERE
string email = await db.Get("email", null, "users", where: "status = 'active'");
```

**Breakdown**:
```csharp
public async Task<string> Get(
    string toGet,            // Column name(s)
    string id,               // Record ID
    string tableName = null, // Table name (required)
    string where = null      // Custom WHERE clause
)
// Returns: Task<string> with value
// Automatically quotes column names
// Throws: Exception if tableName is null
```

---

### AddRange
**Purpose**: Inserts range of ID records into table.

**Example**:
```csharp
var db = new dSql("/path/to/db.db", null);
await db.AddRange(100, "users");  // Inserts IDs 1-100
```

**Breakdown**:
```csharp
public async Task AddRange(
    int range,               // Maximum ID to insert
    string tableName = null  // Table name (required)
)
// Inserts missing IDs from (current max + 1) to range
// Uses ON CONFLICT DO NOTHING to avoid duplicates
// Throws: Exception if tableName is null
```

---

## Resource Management

### Dispose
**Purpose**: Closes connection and releases resources.

**Example**:
```csharp
var db = new dSql("/path/to/db.db", null);
// ... use database ...
db.Dispose();

// Or use with using statement (recommended)
using (var db = new dSql("/path/to/db.db", null))
{
    // Connection automatically disposed
}
```

**Breakdown**:
```csharp
public void Dispose()
// Closes database connection
// Releases all resources
// Safe to call multiple times
// Implements IDisposable pattern
```

---

## Enums

### DatabaseType
```csharp
public enum DatabaseType
{
    Unknown,      // Unknown or unsupported database
    SQLite,       // SQLite database
    PostgreSQL    // PostgreSQL database
}
```

---
