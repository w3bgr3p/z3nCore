# F0rms Class

Class for creating interactive Windows Forms dialogs to collect user input during automation.

---

## Constructor

### Purpose
Initializes the F0rms class with a project instance for logging and interaction.

### Example
```csharp
using z3nCore;

var forms = new F0rms(project);
```

---

## InputBox

### Purpose
Displays a simple input dialog box to collect text from the user.

### Example
```csharp
using z3nCore;

// Simple input
string code = F0rms.InputBox("Enter verification code:");

// Custom size dialog
string apiKey = F0rms.InputBox("Enter API Key:", width: 800, height: 400);

// Use the input
if (!string.IsNullOrEmpty(code))
{
    project.Variables["code"].Value = code;
}
```

### Breakdown
```csharp
public static string InputBox(
    string message = "input data please",  // Prompt message displayed to user
    int width = 600,                       // Dialog width in pixels
    int height = 600)                      // Dialog height in pixels

// Returns: User input text
// Returns: Empty string if user closes dialog without input

// Features:
// - Multiline text input
// - OK button to confirm
// - Cascadia Mono SemiBold font
```

---

## GetLinesByKey

### Purpose
Displays a form to collect multiple lines of data with a custom key column name, formatted for database updates.

### Example
```csharp
var forms = new F0rms(project);

// Collect wallet addresses
Dictionary<string, string> data = forms.GetLinesByKey("address", "Import Wallet Addresses");

// Result format:
// {
//   "1": "address = '0x123...'",
//   "2": "address = '0x456...'",
//   "3": "address = '0x789...'"
// }

// Use in database update
foreach (var kvp in data)
{
    project.SendInfoToLog($"Row {kvp.Key}: {kvp.Value}");
}
```

### Breakdown
```csharp
public Dictionary<string, string> GetLinesByKey(
    string keycolumn = "input Column Name",     // Column name for the data
    string title = "Input data line per line")  // Dialog title

// Returns: Dictionary with numeric keys (1, 2, 3...) and formatted values
//   Key: Sequential number as string ("1", "2", "3"...)
//   Value: Formatted as "columnName = 'escaped_value'"

// Returns: null if user cancels or inputs are empty

// Features:
// - Automatically escapes single quotes in values
// - Skips empty lines
// - Logs warnings for empty lines
// - Values are trimmed
```

---

## GetLines

### Purpose
Similar to GetLinesByKey but returns a List instead of Dictionary, formatted for SQL updates.

### Example
```csharp
var forms = new F0rms(project);

// Collect email addresses
List<string> emails = forms.GetLines("email", "Import Email List");

// Result format:
// [
//   "email = 'user1@example.com'",
//   "email = 'user2@example.com'"
// ]

// Use for batch updates
foreach (string emailUpdate in emails)
{
    // Execute SQL update with emailUpdate
}
```

### Breakdown
```csharp
public List<string> GetLines(
    string keycolumn = "input Column Name",     // Column name for the data
    string title = "Input data line per line")  // Dialog title

// Returns: List of formatted strings "columnName = 'escaped_value'"
// Returns: null if user cancels or inputs are empty

// Features:
// - Returns List<string> instead of Dictionary
// - Same formatting as GetLinesByKey
// - Single quotes are escaped
```

---

## GetKeyValuePairs

### Purpose
Displays a form with multiple key-value input fields for collecting structured data.

### Example
```csharp
var forms = new F0rms(project);

// Simple usage
Dictionary<string, string> config = forms.GetKeyValuePairs(
    quantity: 3,
    title: "Enter Configuration"
);

// With placeholders
List<string> keys = new List<string> { "apiKey", "secretKey", "endpoint" };
List<string> values = new List<string> { "your-api-key", "your-secret", "https://api.example.com" };

Dictionary<string, string> params = forms.GetKeyValuePairs(
    quantity: 3,
    keyPlaceholders: keys,
    valuePlaceholders: values,
    title: "API Configuration",
    prepareUpd: true
);

// Result with prepareUpd=true:
// {
//   "1": "apikey = 'abc123'",
//   "2": "secretkey = 'xyz789'",
//   "3": "endpoint = 'https://api.example.com'"
// }

// Result with prepareUpd=false:
// {
//   "apikey": "abc123",
//   "secretkey": "xyz789",
//   "endpoint": "https://api.example.com"
// }
```

### Breakdown
```csharp
public Dictionary<string, string> GetKeyValuePairs(
    int quantity,                          // Number of key-value pairs to collect
    List<string> keyPlaceholders = null,   // Default key names
    List<string> valuePlaceholders = null, // Placeholder values shown in gray
    string title = "Input Key-Value Pairs", // Dialog title
    bool prepareUpd = true)                // Format for SQL UPDATE (true) or raw pairs (false)

// Returns: Dictionary of key-value pairs
//   If prepareUpd=true: Keys are "1","2","3"..., Values are "key = 'value'"
//   If prepareUpd=false: Keys are actual input keys, Values are actual input values

// Returns: null if user cancels or no valid pairs entered

// Features:
// - Dynamic form with specified number of fields
// - Placeholder support for keys and values
// - Keys are converted to lowercase
// - Skips empty or placeholder-only values
// - Escapes single quotes in values
```

---

## GetKeyBoolPairs

### Purpose
Displays a form with checkboxes to collect boolean values for predefined keys.

### Example
```csharp
var forms = new F0rms(project);

List<string> features = new List<string> { "AutoSave", "DarkMode", "Notifications" };
List<string> descriptions = new List<string> {
    "Enable auto-save",
    "Use dark theme",
    "Show notifications"
};

Dictionary<string, bool> settings = forms.GetKeyBoolPairs(
    quantity: 3,
    keyPlaceholders: features,
    valuePlaceholders: descriptions,
    title: "Feature Settings",
    prepareUpd: false
);

// Result:
// {
//   "autosave": true,
//   "darkmode": false,
//   "notifications": true
// }

// Apply settings
foreach (var kvp in settings)
{
    project.SendInfoToLog($"{kvp.Key}: {kvp.Value}");
}
```

### Breakdown
```csharp
public Dictionary<string, bool> GetKeyBoolPairs(
    int quantity,                           // Number of checkboxes to display
    List<string> keyPlaceholders = null,    // Labels for checkboxes
    List<string> valuePlaceholders = null,  // Checkbox descriptions
    string title = "Input Key-Bool Pairs",  // Dialog title
    bool prepareUpd = true)                 // Use numeric keys (true) or label keys (false)

// Returns: Dictionary with string keys and boolean values
//   If prepareUpd=true: Keys are "1","2","3"...
//   If prepareUpd=false: Keys are lowercase labels

// Returns: null if user cancels or no valid pairs

// Features:
// - Checkbox interface for boolean input
// - All checkboxes default to unchecked (false)
// - Keys converted to lowercase
```

---

## GetKeyValueString

### Purpose
Collects key-value pairs and returns them as a single formatted string suitable for SQL SET clauses.

### Example
```csharp
var forms = new F0rms(project);

List<string> keys = new List<string> { "name", "email", "age" };
List<string> values = new List<string> { "John Doe", "john@example.com", "30" };

string updateString = forms.GetKeyValueString(
    quantity: 3,
    keyPlaceholders: keys,
    valuePlaceholders: values,
    title: "Update User Data"
);

// Result: "name='John Doe', email='john@example.com', age='30'"

// Use in SQL
string sql = $"UPDATE users SET {updateString} WHERE id = 1";
```

### Breakdown
```csharp
public string GetKeyValueString(
    int quantity,                          // Number of key-value pairs
    List<string> keyPlaceholders = null,   // Default key names
    List<string> valuePlaceholders = null, // Placeholder values
    string title = "Input Key-Value Pairs") // Dialog title

// Returns: String formatted as "key1='value1', key2='value2', key3='value3'"
// Returns: null if user cancels or no valid pairs entered

// Features:
// - Perfect for SQL SET clauses
// - Escapes single quotes in values
// - Keys converted to lowercase
// - Comma-separated format
```

---

## GetSelectedItem

### Purpose
Displays a dropdown list for selecting a single item from a list of options.

### Example
```csharp
var forms = new F0rms(project);

List<string> networks = new List<string> { "Mainnet", "Testnet", "Devnet" };

string selected = forms.GetSelectedItem(
    items: networks,
    title: "Select Network",
    labelText: "Choose network:"
);

// Result: "Mainnet" (or whichever user selected)

// Use selection
if (selected == "Mainnet")
{
    project.Variables["rpcUrl"].Value = "https://mainnet.example.com";
}
```

### Breakdown
```csharp
public string GetSelectedItem(
    List<string> items,              // List of items to choose from
    string title = "Select an Item", // Dialog title
    string labelText = "Select:")    // Label text above dropdown

// Returns: Selected item as string
// Returns: null if user cancels or items list is empty

// Features:
// - Dropdown (ComboBox) interface
// - First item selected by default
// - DropDownList style (no typing, selection only)
// - Validates items list is not null or empty
```

---
