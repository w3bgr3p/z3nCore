# DailyReport Classes

Provides tools for generating comprehensive HTML daily reports with heatmaps and statistics for project execution tracking.

---

## ProjectData Class

### `ProjectData.CollectData(IZennoPosterProjectModel project, string tableName)` [static]

**Purpose**
Collects and parses execution data from a project table, extracting completion status, timestamps, and execution details for all touched accounts.

**Example**
```csharp
// Collect data from a specific project table
var projectData = DailyReport.ProjectData.CollectData(project, "__myProject");

// Access collected data
foreach (var account in projectData.All.Keys)
{
    var data = projectData.All[account];
    string status = data[0];      // "+" or "-"
    string timestamp = data[1];    // ISO timestamp
    string execTime = data[2];     // Execution time in seconds
    string report = data[3];       // Detailed report
}
```

**Breakdown**
```csharp
// Parameters:
// - project: IZennoPosterProjectModel instance for database operations
// - tableName: Name of the project table to collect data from
var projectData = DailyReport.ProjectData.CollectData(project, "__projectName");

// Returns: ProjectData instance with:
//   - ProjectName: Cleaned table name (without "__" prefix)
//   - All: Dictionary<string, string[]> where:
//       Key: Account ID
//       Value: string[] with 4 elements:
//         [0] = Completion status ("+" success, "-" error)
//         [1] = UTC timestamp (ISO 8601 format)
//         [2] = Execution time in seconds
//         [3] = Full report text (multi-line)
// Data source: Queries "id, last" columns where last LIKE '+ %' OR '- %'
// Exceptions: None (invalid entries are skipped)
```

---

## HtmlEncoder Class

### `HtmlEncoder.HtmlEncode(string text)` [static]

**Purpose**
Encodes text for safe HTML output by escaping special characters.

**Example**
```csharp
// Encode text with HTML special characters
string rawText = "<div>Account & 'Data'</div>";
string encoded = DailyReport.HtmlEncoder.HtmlEncode(rawText);
// Result: "&lt;div&gt;Account &amp; &#39;Data&#39;&lt;/div&gt;"
```

**Breakdown**
```csharp
// Parameter:
// - text: String to encode for HTML output
string encoded = DailyReport.HtmlEncoder.HtmlEncode("<script>alert('XSS')</script>");

// Returns: HTML-safe encoded string
// Character replacements:
//   & → &amp;
//   < → &lt;
//   > → &gt;
//   " → &quot;
//   ' → &#39;
// Returns original string if null or empty
// Exceptions: None
```

---

### `HtmlEncoder.HtmlAttributeEncode(string text)` [static]

**Purpose**
Encodes text for safe use in HTML attributes (data-*, title, etc.).

**Example**
```csharp
// Encode text for HTML attribute
string tooltipData = "Account: acc1 | Balance: 5.23";
string encoded = DailyReport.HtmlEncoder.HtmlAttributeEncode(tooltipData);

// Use in HTML attribute
string html = $"<div data-tooltip='{encoded}'>Cell</div>";
```

**Breakdown**
```csharp
// Parameter:
// - text: String to encode for HTML attribute
string encoded = DailyReport.HtmlEncoder.HtmlAttributeEncode("value=\"test\"");

// Returns: Attribute-safe encoded string
// Character replacements:
//   & → &amp;
//   " → &quot;
//   ' → &#39;
//   < → &lt;
//   > → &gt;
// Returns original string if null or empty
// Exceptions: None
```

---

## FarmReportGenerator Class

### `FarmReportGenerator.GenerateHtmlReport(List<ProjectData> projects, string userId = null)` [static]

**Purpose**
Generates a comprehensive HTML daily report with heatmaps, statistics, and execution summaries for multiple projects.

**Example**
```csharp
// Collect data from multiple projects
var projects = new List<DailyReport.ProjectData>();
projects.Add(DailyReport.ProjectData.CollectData(project, "__project1"));
projects.Add(DailyReport.ProjectData.CollectData(project, "__project2"));

// Generate HTML report
string htmlContent = DailyReport.FarmReportGenerator.GenerateHtmlReport(
    projects,
    userId: "admin"
);

// Save to file
File.WriteAllText("report.html", htmlContent, Encoding.UTF8);
```

**Breakdown**
```csharp
// Parameters:
// - projects: List of ProjectData instances to include in report
// - userId: Optional user identifier to display in report header (default: null)
string html = DailyReport.FarmReportGenerator.GenerateHtmlReport(
    projects: projectsList,
    userId: "john_doe"
);

// Returns: Complete HTML document as string
// Report includes:
//   - Summary cards: Total attempts, successful, failed (with percentages)
//   - Interactive heatmap for each project with:
//       * Color-coded cells by completion date (today, yesterday, 2 days, 3+ days)
//       * Success (green tones) vs Error (red tones) indicators
//       * Hover tooltips with account details, timestamps, execution time
//       * Click-to-copy functionality
//   - Project statistics: Min/Max/Avg execution times, success rates
//   - ZennoProcesses sidebar (basic version without PID details)
//   - Idle projects section (projects with no execution data)
// Heatmap color coding:
//   - Today: Bright colors (green/red)
//   - Yesterday: Medium tones
//   - 2 days ago: Darker tones
//   - 3+ days: Very dark tones
//   - Not touched: Transparent
// Exceptions: None (empty lists handled gracefully)
```

---

### `FarmReportGenerator.GenerateHtmlReportWithPid(List<ProjectData> projects, string userId = null)` [static]

**Purpose**
Generates an enhanced HTML daily report with additional process ID tracking and account binding information.

**Example**
```csharp
// Collect data from multiple projects
var projects = new List<DailyReport.ProjectData>();
projects.Add(DailyReport.ProjectData.CollectData(project, "__swapper"));
projects.Add(DailyReport.ProjectData.CollectData(project, "__bridge"));

// Generate enhanced report with PID tracking
string htmlContent = DailyReport.FarmReportGenerator.GenerateHtmlReportWithPid(
    projects,
    userId: "operator_1"
);

// Save and open
File.WriteAllText("dailyReport.html", htmlContent, Encoding.UTF8);
Process.Start("dailyReport.html");
```

**Breakdown**
```csharp
// Parameters:
// - projects: List of ProjectData instances to include in report
// - userId: Optional user identifier for report header (default: null)
string html = DailyReport.FarmReportGenerator.GenerateHtmlReportWithPid(
    projects: projectDataList,
    userId: "admin"
);

// Returns: Complete HTML document as string
// Additional features compared to GenerateHtmlReport:
//   - ZennoProcesses sidebar with enhanced details:
//       * Process ID (PID)
//       * Memory usage in MB
//       * Process age in minutes
//       * Project name binding
//       * Account binding (acc#, unbinded, or unknown)
//   - Same heatmap and statistics as standard report
// Data source: Uses ProcAcc.PidReport() for process tracking
// Display format: "acc123 [projectName]" or "unbinded"
// Exceptions: None (gracefully handles ProcAcc.PidReport() failures)
```
