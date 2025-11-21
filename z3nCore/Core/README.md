# Core - Independent Database and Logging Layer

This namespace provides ZennoPoster-independent implementations of database access and logging functionality.

## Overview

The `Core` namespace contains standalone classes that can be used in any .NET Framework application without ZennoPoster dependencies:

- **Variables** - Simple key-value storage for configuration and state
- **Logger** - Flexible logging with Console, File, and UI output modes
- **Db** - Full-featured database access layer supporting SQLite and PostgreSQL

## Quick Start

### 1. Basic Setup

```csharp
using Core;
using z3nCore; // For dSql only

// Create variables storage
var variables = new Variables();

// Create logger (Console mode by default)
var logger = new Logger(variables);

// Create database instance
var db = new Db(
    variables,
    logger,
    dbMode: "SQLite",
    dbPath: @"C:\path\to\database.db"
);
```

### 2. Using Variables

```csharp
// Set values
variables["projectName"] = "MyApp";
variables.Set("acc0", "123");

// Get values
string name = variables.Get("projectName");
int accountId = variables.GetInt("acc0");
bool isDebug = variables.GetBool("debug");

// Working with ranges
variables["cfgAccRange"] = "1-100";
var range = variables.Range();
```

### 3. Using Logger

```csharp
// Console logger
var consoleLogger = new Logger(variables);
consoleLogger.Send("Hello from console");

// File logger
var fileLogger = new Logger(variables, @"C:\logs\app.log");
fileLogger.Send("Logged to file");

// UI logger (for WinForms/WPF)
var uiLogger = new Logger(variables, (msg) => {
    textBox.AppendText(msg);
});
uiLogger.Send("Logged to UI");

// With configuration
variables["cfgLog"] = "acc,time,memory,caller";
logger.Send("Detailed log entry", show: true);

// Warnings
logger.Warn("Something went wrong", show: true);
```

### 4. Using Database

```csharp
// Setup
variables["projectTable"] = "__myapp";
variables["acc0"] = "1";

// Create table
var tableStructure = new Dictionary<string, string>
{
    { "id", "INTEGER PRIMARY KEY" },
    { "name", "TEXT DEFAULT ''" },
    { "status", "TEXT DEFAULT ''" }
};
db.TblAdd(tableStructure, "__myapp");

// Insert/Update
db.DbUpd("name = 'John', status = 'active'", "__myapp");

// Query
string name = db.DbGet("name", "__myapp");
var data = db.DbGetColumns("name,status", "__myapp");

// Get all columns as dictionary
var allData = db.DbToVars("name,status,email", "__myapp");
// Now variables contain: name, status, email

// Advanced queries
string result = db.DbQ("SELECT * FROM __myapp WHERE id = 1");
```

### 5. Complete Example

```csharp
using System;
using Core;

class Program
{
    static void Main()
    {
        // Initialize
        var variables = new Variables();
        variables["debug"] = "True";
        variables["cfgLog"] = "time,memory";
        variables["projectTable"] = "__users";
        variables["acc0"] = "1";
        variables["rangeEnd"] = "10";

        var logger = new Logger(variables, @"app.log");
        var db = new Db(
            variables,
            logger,
            dbMode: "SQLite",
            dbPath: @"myapp.db"
        );

        // Prepare database
        var columns = new[] { "username", "email", "created" };
        db.PrepareProjectTable(columns, "__users");

        // Insert data
        db.DbUpd("username = 'alice', email = 'alice@example.com'", "__users");
        logger.Send("User created successfully");

        // Query data
        var userData = db.DbGetColumns("username,email", "__users");
        foreach (var kvp in userData)
        {
            logger.Send($"{kvp.Key}: {kvp.Value}");
        }

        // List tables
        var tables = db.TblList();
        logger.Send($"Tables: {string.Join(", ", tables)}");
    }
}
```

## Logger Output Modes

### Console (Default)
```csharp
var logger = new Logger(variables);
```

### File
```csharp
var logger = new Logger(variables, @"C:\logs\app.log");
```

### UI (WinForms)
```csharp
var logger = new Logger(variables, (msg) => {
    if (textBox.InvokeRequired)
    {
        textBox.Invoke(new Action(() => textBox.AppendText(msg)));
    }
    else
    {
        textBox.AppendText(msg);
    }
});
```

## Database Configuration

### SQLite
```csharp
var db = new Db(
    variables,
    logger,
    dbMode: "SQLite",
    dbPath: @"C:\data\myapp.db"
);
```

### PostgreSQL
```csharp
var db = new Db(
    variables,
    logger,
    dbMode: "PostgreSQL",
    dbPath: null,
    pgHost: "localhost",
    pgPort: "5432",
    pgDbName: "mydb",
    pgUser: "postgres",
    pgPass: "password"
);
```

## Migration from ZennoPoster

If you're migrating from ZennoPoster code:

**Before (with ZennoPoster):**
```csharp
using z3nCore;

// Extension methods on IZennoPosterProjectModel
project.DbGet("name", "users");
project.DbUpd("status = 'active'", "users");
project.log("Hello");
```

**After (standalone):**
```csharp
using Core;

// Instance methods on Db and Logger objects
db.DbGet("name", "users");
db.DbUpd("status = 'active'", "users");
logger.Send("Hello");
```

## API Compatibility

All method signatures and behaviors are preserved from the original `z3nCore` implementation:

- `DbGet()`, `DbUpd()`, `DbQ()` - Database operations
- `TblAdd()`, `TblExist()`, `TblList()` - Table management
- `ClmnAdd()`, `ClmnExist()`, `ClmnDrop()` - Column operations
- `MigrateTable()`, `MigrateAllTables()` - Migration utilities

## Dependencies

- **z3nCore.dSql** - Database abstraction layer (from z3nCore namespace)
- Dapper - SQL object mapper
- Npgsql - PostgreSQL provider
- Microsoft.Data.Sqlite - SQLite provider

## License

Same as parent z3nCore project.
