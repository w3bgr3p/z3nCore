# Accountant Class

Provides tools for generating and displaying balance reports in HTML format.

---

## Constructor

### `Accountant(IZennoPosterProjectModel project, bool log = false)`

**Purpose**
Initializes a new instance of the Accountant class with project context and optional logging.

**Example**
```csharp
// Create an Accountant instance with logging enabled
var accountant = new Accountant(project, log: true);
```

**Breakdown**
```csharp
// Parameters:
// - project: IZennoPosterProjectModel instance for project operations
// - log: Optional boolean to enable/disable logging (default: false)
var accountant = new Accountant(project, log: true);

// Returns: Instance of Accountant class
// Exceptions: ArgumentNullException if project is null
```

---

## Public Methods

### `ShowBalanceTable(string chains = null, bool single = false, bool call = false)`

**Purpose**
Generates and displays an HTML balance table for specified blockchain chains. Supports both single-column and multi-column layouts based on data volume.

**Example**
```csharp
var accountant = new Accountant(project);

// Show balance table for specific chains
accountant.ShowBalanceTable(chains: "eth,bsc,polygon", call: true);

// Show balance for all chains without opening
accountant.ShowBalanceTable();
```

**Breakdown**
```csharp
// Parameters:
// - chains: Comma-separated list of chain names (null = all chains from "_native" table)
// - single: Force single-column layout even for large datasets (default: false)
// - call: Open the HTML file in browser after generation (default: false)
accountant.ShowBalanceTable(chains: "eth,bsc", single: false, call: true);

// Returns: void
// Side effects:
// - Generates HTML file at: {project.Path}/.data/balanceReport.html
// - Logs progress to project log
// Exceptions: None (warnings logged if no data found)
```

---

### `ShowBalanceTableHeatmap(string chains = null, bool call = false)`

**Purpose**
Generates a visual heatmap representation of account balances across blockchain chains with color-coded balance indicators.

**Example**
```csharp
var accountant = new Accountant(project);

// Generate heatmap for all chains
accountant.ShowBalanceTableHeatmap(call: true);

// Generate heatmap for specific chains
accountant.ShowBalanceTableHeatmap(chains: "eth,bsc,arbitrum", call: false);
```

**Breakdown**
```csharp
// Parameters:
// - chains: Comma-separated chain names (null = all chains except 'id')
// - call: Open HTML file in browser after generation (default: false)
accountant.ShowBalanceTableHeatmap(chains: "eth,polygon", call: true);

// Returns: void
// Side effects:
// - Generates HTML file at: {project.Path}/.data/balanceHeatmap.html
// - Logs account count and chain information
// Color coding:
//   - Blue (≥0.1): Highest balance
//   - Green (≥0.01): High balance
//   - Yellow-Green (≥0.001): Medium balance
//   - Khaki (≥0.0001): Low balance
//   - Salmon (≥0.00001): Very low balance
//   - Red (>0): Minimal balance
//   - Transparent (0): Empty
// Exceptions: None (warnings logged if no data found)
```

---

### `ShowBalanceTableFromList(List<string> data, bool call = false)`

**Purpose**
Generates a simple two-column balance table from a list of account:balance pairs.

**Example**
```csharp
var accountant = new Accountant(project);

// Prepare data as List<string>
var balanceData = new List<string>
{
    "account1: 0.5",
    "account2: 1.23",
    "account3: 0.001"
};

// Generate balance table from list
accountant.ShowBalanceTableFromList(balanceData, call: true);
```

**Breakdown**
```csharp
// Parameters:
// - data: List of strings in format "accountName: balanceValue"
// - call: Open HTML file in browser after generation (default: false)
var data = new List<string> { "acc1: 0.5", "acc2: 1.2" };
accountant.ShowBalanceTableFromList(data, call: true);

// Returns: void
// Side effects:
// - Generates HTML file at: {project.Path}/.data/balanceListReport.html
// - Includes pagination for large datasets (50 rows per page)
// - Displays total sum and statistics
// Input format: Each string must be "name: value" (colon-separated)
// Exceptions: None (invalid entries are skipped)
```
