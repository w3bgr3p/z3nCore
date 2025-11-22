# Debugger Class

Static utility class for debugging and system diagnostics.

---

## AssemblyVer

### Purpose
Retrieves version information and public key token for a loaded assembly by name.

### Example
```csharp
using z3nCore.Utilities;

// Get version info for ZennoLab assembly
string info = Debugger.AssemblyVer("ZennoLab.InterfacesLibrary");
// Result: "ZennoLab.InterfacesLibrary 7.8.0.0, PublicKeyToken: AB-CD-EF-..."

// Check if an assembly is loaded
string result = Debugger.AssemblyVer("MyCustomLib");
// Result: "MyCustomLib not loaded" (if not loaded)
```

### Breakdown
```csharp
public static string AssemblyVer(
    string dllName)  // Name of the assembly (without .dll extension)

// Returns: Version string with format "Name Version, PublicKeyToken: XXX"
// Returns: "dllName not loaded" if assembly not found in current AppDomain

// Example outputs:
// - "ZennoLab.InterfacesLibrary 7.8.0.0, PublicKeyToken: AB-CD-EF-12"
// - "MyLib not loaded"
```

---

## ZennoProcesses

### Purpose
Gets information about all running ZennoPoster and ZennoBox processes including memory usage and uptime.

### Example
```csharp
using z3nCore.Utilities;

// Get all Zenno processes
List<string[]> processes = Debugger.ZennoProcesses();

// Display process information
foreach (var proc in processes)
{
    string name = proc[0];        // Process name
    string memoryMB = proc[1];    // Memory usage in MB
    string runtimeMin = proc[2];  // Running time in minutes

    project.SendInfoToLog($"{name}: {memoryMB}MB, running {runtimeMin} min");
}

// Check if any processes are running
if (processes.Count == 0)
{
    project.SendInfoToLog("No Zenno processes found");
}
```

### Breakdown
```csharp
public static List<string[]> ZennoProcesses()

// Returns: List of string arrays, each containing:
//   [0] - Process name ("ZennoPoster" or "zbe1")
//   [1] - Memory usage in MB (WorkingSet64)
//   [2] - Running time in minutes (from process start)

// Returns: Empty list if no ZennoPoster/ZennoBox processes found

// Example result:
// [
//   ["ZennoPoster", "1024", "45"],
//   ["zbe1", "512", "30"]
// ]
```

---
