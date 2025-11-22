# _Accountant and Test Classes Documentation

These classes provide balance reporting and utility functions for ZennoPoster projects.

---

## _Accountant Class

Generates HTML reports for cryptocurrency balance visualization with heatmaps and statistics.

### Constructor

**Purpose**: Initializes the Accountant instance with optional logging.

**Example**:
```csharp
// Create accountant with logging enabled
var accountant = new _Accountant(project, log: true);

// Create accountant without logging
var accountant = new _Accountant(project);
```

**Breakdown**:
```csharp
public _Accountant(
    IZennoPosterProjectModel project,  // ZennoPoster project instance
    bool log = false)                  // Enable detailed logging
{
    // Initializes project reference
    // Creates logger with "$" class emoji
    // Sets up balance thresholds and formatting constants
}
```

---

### ShowBalanceTable

**Purpose**: Generates an HTML table report with balance data from database and optionally opens it in browser.

**Example**:
```csharp
var accountant = new _Accountant(project);

// Show balances for all chains from _native table
accountant.ShowBalanceTable(chains: null, call: true);

// Show specific chains only
accountant.ShowBalanceTable(
    chains: "ethereum,bsc,polygon",
    single: false,
    call: true
);

// Generate report without opening browser
accountant.ShowBalanceTable(chains: "arbitrum,optimism", call: false);
```

**Breakdown**:
```csharp
public void ShowBalanceTable(
    string chains = null,      // Comma-separated chain names, null = all chains from _native table
    bool single = false,       // Force single-column view even for small datasets
    bool call = false)         // Open report in default browser
{
    // Queries _native table for balance data
    // Uses project variable "rangeEnd" to limit rows
    // Auto-selects layout: multi-column for ≤3 chains and ≥100 rows, else single-column
    // Applies color coding based on balance thresholds:
    //   - Blue (≥0.1), Green (≥0.01), Yellow-Green (≥0.001)
    //   - Khaki (≥0.0001), Salmon (≥0.00001), Red (>0), White (=0)
    // Saves HTML to .data/balanceReport.html
    // Includes pagination (50 rows per page)
    // Shows statistics: total accounts, active accounts, accounts ≥0.1, grand total
}
```

---

### ShowBalanceTableHeatmap

**Purpose**: Generates an interactive heatmap visualization of balances across chains.

**Example**:
```csharp
var accountant = new _Accountant(project);

// Show heatmap for all chains
accountant.ShowBalanceTableHeatmap(call: true);

// Show heatmap for specific chains
accountant.ShowBalanceTableHeatmap(
    chains: "ethereum,bsc,polygon,arbitrum",
    call: true
);
```

**Breakdown**:
```csharp
public void ShowBalanceTableHeatmap(
    string chains = null,     // Comma-separated chain names, null = all non-id columns
    bool call = false)        // Open in browser
{
    // Queries _native table for all account balances
    // Creates grid of colored cells (heatmap) for each account+chain combination
    // Each cell color represents balance level (same color scheme as table)
    // Hover shows tooltip with account number, chain, and exact balance
    // Click cell to copy info to clipboard
    // Displays statistics per chain: accounts with balance, min/max/avg, total, coverage%
    // Saves to .data/balanceHeatmap.html
    // Uses GitHub Dark theme styling
    // Includes summary cards: total accounts, active accounts, total balance
}
```

---

### ShowBalanceTableFromList

**Purpose**: Generates a balance report from a list of "account:balance" strings.

**Example**:
```csharp
var accountant = new _Accountant(project);

var balanceData = new List<string>
{
    "Account #1:0.5432",
    "Account #2:1.2345",
    "Account #3:0.0001",
    "Account #4:0"
};

accountant.ShowBalanceTableFromList(balanceData, call: true);
```

**Breakdown**:
```csharp
public void ShowBalanceTableFromList(
    List<string> data,     // List of "account:balance" strings
    bool call = false)     // Open in browser
{
    // Parses each line expecting format: "account_name:balance_value"
    // Applies balance color coding
    // Calculates total sum
    // Saves to .data/balanceListReport.html
    // Includes pagination for large datasets
    // Shows summary row with total balance
}
```

---

## Test Class

Static utility methods for string manipulation.

### GetFileNameFromUrl

**Purpose**: Extracts filename from URL or HTML attribute, with optional extension removal.

**Example**:
```csharp
// Extract filename from URL
string url = "https://example.com/images/photo.jpg?version=1";
string filename = url.GetFileNameFromUrl(withExtension: true);
// Result: "photo.jpg"

// Extract filename without extension
string filenameNoExt = url.GetFileNameFromUrl(withExtension: false);
// Result: "photo"

// Extract from HTML src attribute
string html = "<img src='https://cdn.example.com/assets/logo.png' />";
string logoFile = html.GetFileNameFromUrl(withExtension: true);
// Result: "logo.png"

// Extract from HTML href
string link = "href=\"/downloads/file.pdf\"";
string pdfName = link.GetFileNameFromUrl(withExtension: false);
// Result: "file"
```

**Breakdown**:
```csharp
public static string GetFileNameFromUrl(
    this string input,           // URL string or HTML with src/href attribute
    bool withExtension = false)  // Include file extension in result
{
    // Searches for src or href attributes in HTML (case-insensitive)
    // Extracts URL from attribute or uses input as-is if no attribute found
    // Extracts last path segment (filename) from URL
    // Removes query parameters (everything after ?)
    // If withExtension is false, removes file extension
    // Returns original input on parsing error
    // Handles paths with / or \ separators
}
```

---

## Balance Color Coding Thresholds

Both `ShowBalanceTable` and `ShowBalanceTableHeatmap` use these thresholds:

| Balance Range | Color | CSS Class |
|--------------|-------|-----------|
| ≥ 0.1 | Blue (#4682B4) | `balance-highest` |
| ≥ 0.01 | Green (#228B22) | `balance-high` |
| ≥ 0.001 | Yellow-Green (#9ACD32) | `balance-medium` |
| ≥ 0.0001 | Khaki (#F0E68C) | `balance-low` |
| ≥ 0.00001 | Salmon (#FFA07A) | `balance-verylow` |
| > 0 | Red (#CD5C5C) | `balance-minimal` |
| = 0 | Transparent/Gray | `balance-zero` |

---

## Report Features

All HTML reports include:

- **GitHub Dark Theme** - consistent styling with modern dark UI
- **Responsive Design** - adapts to different screen sizes
- **Pagination** - navigable with arrow keys or buttons
- **Monospace Font** - Iosevka/Consolas for precise number alignment
- **Statistics Summary** - quick overview of key metrics
- **Interactive Elements** - hover effects, clickable cells (heatmap)

---

## File Locations

Generated reports are saved to:
- `{project.Path}/.data/balanceReport.html` - standard table report
- `{project.Path}/.data/balanceHeatmap.html` - heatmap visualization
- `{project.Path}/.data/balanceListReport.html` - list-based report

---

## Best Practices

1. **Set rangeEnd variable** before calling balance reports to limit query scope
2. **Use specific chains** parameter for faster reports when you only need certain chains
3. **Disable call parameter** when generating multiple reports programmatically
4. **Enable logging** during development to troubleshoot data issues
5. **Use heatmap** for quick visual overview of many accounts across chains
6. **Use table view** for detailed numerical analysis

---

