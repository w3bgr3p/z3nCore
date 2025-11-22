# Helper Class

Static utility class providing extension methods for project debugging and XML documentation search.

---

## Help

### Purpose
Searches through ZennoPoster XML documentation files for methods, properties, or classes and displays detailed information in a readable form.

### Example
```csharp
using z3nCore.Utilities;

// Search for specific method
project.Help("SendInfoToLog");

// Search with dialog prompt (if parameter is null)
project.Help();  // Shows input dialog

// Search for browser methods
project.Help("Navigate");

// Search for instance methods
project.Help("FindElement");
```

### Breakdown
```csharp
public static void Help(
    this IZennoPosterProjectModel project,  // Project instance (extension method)
    string toSearch = null)                 // Search term (null prompts for input)

// Returns: void (displays results in a dialog window)

// Throws:
// - ArgumentException: If search term is empty

// Features:
// - Searches in 4 XML files:
//   * ZennoLab.CommandCenter.xml
//   * ZennoLab.InterfacesLibrary.xml
//   * ZennoLab.Macros.xml
//   * ZennoLab.Emulation.xml
//
// - Displays for each match:
//   * Member name
//   * Summary
//   * Parameters with descriptions
//   * Return value description
//   * Remarks
//   * Code examples
//   * Requirements
//   * Related methods (See Also)
//   * Overloads information
//   * Exceptions
//
// - Results shown in copyable text viewer
// - Uses Cascadia Mono font for readability
// - Window supports Ctrl+A (select all), Ctrl+C (copy), Esc (close)
```

### Example Output
```
=== M:ZennoLab.InterfacesLibrary.ProjectModel.IZennoPosterProjectModel.SendInfoToLog ===
Summary: Sends an informational message to the project log
Parameter [message]: The message text to log
Parameter [showDialog]: Whether to show message in a dialog (default: false)
Returns: void
Example 1: project.SendInfoToLog("Process completed successfully");

--------------------------------------------------
```

---
