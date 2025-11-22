# Running Class

**Namespace:** `z3nCore`

Inter-process shared memory storage for tracking running processes across all ZennoPoster instances. All methods are thread-safe and process-safe using mutex synchronization.

**Data Structure:** Each process is stored as `Dictionary<int, List<object>>` where:
- **Key**: Process ID (int)
- **Value**: List containing `[memory(MB), uptime(minutes), port, project_name, account]`

---

## Add

### Purpose
Adds or updates information about a process in shared memory.

### Example
```csharp
int pid = 12345;
var data = new List<object>
{
    150,        // Memory in MB
    5,          // Uptime in minutes
    8080,       // Port
    "MyProject",// Project name
    "acc123"    // Account name
};

Running.Add(pid, data);
Console.WriteLine($"Added process {pid} to shared storage");
```

### Breakdown
```csharp
public static void Add(int pid, List<object> data)
```
- **Parameter `pid`**: Process ID to add/update
- **Parameter `data`**: List of process information: `[memory, uptime, port, project, account]`
- **Thread-safe**: Uses mutex lock for inter-process synchronization
- **Overwrites**: If PID already exists, data is updated
- **Timeout**: 5 second timeout for mutex acquisition
- **Exception**: Throws `TimeoutException` if mutex cannot be acquired

---

## Get

### Purpose
Retrieves information about a specific process from shared memory.

### Example
```csharp
int pid = 12345;
List<object> data = Running.Get(pid);

if (data != null)
{
    int mem = Convert.ToInt32(data[0]);
    int age = Convert.ToInt32(data[1]);
    int port = Convert.ToInt32(data[2]);
    string proj = data[3].ToString();
    string acc = data[4].ToString();

    Console.WriteLine($"PID {pid}: {acc}, {mem}MB, {age}min, Port:{port}");
}
else
{
    Console.WriteLine($"Process {pid} not found");
}
```

### Breakdown
```csharp
public static List<object> Get(int pid)
```
- **Parameter `pid`**: Process ID to retrieve
- **Returns**: List of process data if found, `null` if not found
- **Data format**: `[memory(int), uptime(int), port(int), project(string), account(string)]`
- **Thread-safe**: Uses mutex lock
- **Timeout**: 5 second timeout

---

## Remove

### Purpose
Removes a process from shared memory storage.

### Example
```csharp
int pid = 12345;
bool removed = Running.Remove(pid);

if (removed)
{
    Console.WriteLine($"Process {pid} removed from storage");
}
else
{
    Console.WriteLine($"Process {pid} not found");
}
```

### Breakdown
```csharp
public static bool Remove(int pid)
```
- **Parameter `pid`**: Process ID to remove
- **Returns**: `true` if process was removed, `false` if it didn't exist
- **Thread-safe**: Uses mutex lock
- **Atomic operation**: Read, remove, and write happen under single lock

---

## ContainsKey

### Purpose
Checks if a process exists in shared memory storage.

### Example
```csharp
int pid = 12345;

if (Running.ContainsKey(pid))
{
    Console.WriteLine($"Process {pid} is being tracked");
}
else
{
    Console.WriteLine($"Process {pid} is not in storage");
}
```

### Breakdown
```csharp
public static bool ContainsKey(int pid)
```
- **Parameter `pid`**: Process ID to check
- **Returns**: `true` if process exists in storage, `false` otherwise
- **Thread-safe**: Uses mutex lock
- **Fast check**: Only reads, doesn't modify data

---

## Clear

### Purpose
Removes all processes from shared memory storage.

### Example
```csharp
Running.Clear();
Console.WriteLine("All process data cleared from shared memory");

// Verify
int count = Running.Count;
Console.WriteLine($"Processes in storage: {count}"); // Output: 0
```

### Breakdown
```csharp
public static void Clear()
```
- **No parameters**
- **No return value**
- **Thread-safe**: Uses mutex lock
- **Warning**: This affects all ZennoPoster processes sharing the same memory
- **Use case**: Cleanup, testing, or resetting state

---

## Count

### Purpose
Gets the total number of processes stored in shared memory.

### Example
```csharp
int totalProcesses = Running.Count;
Console.WriteLine($"Total processes being tracked: {totalProcesses}");

// Use in conditions
if (Running.Count > 10)
{
    Console.WriteLine("Warning: Many processes running");
}
```

### Breakdown
```csharp
public static int Count { get; }
```
- **Property** (read-only)
- **Returns**: Number of processes in storage
- **Thread-safe**: Uses mutex lock
- **Performance**: Loads all data to count, consider caching if called frequently

---

## ToLocal

### Purpose
Creates a local copy of all process data from shared memory for reading and analysis. Optionally filters by data member count.

### Example
```csharp
// Get all processes
Dictionary<int, List<object>> allProcesses = Running.ToLocal();

foreach (var proc in allProcesses)
{
    int pid = proc.Key;
    var data = proc.Value;
    Console.WriteLine($"PID: {pid}, Data elements: {data.Count}");
}

// Get only processes with exactly 5 data members
var filtered = Running.ToLocal(dataMembers: 5);
Console.WriteLine($"Processes with 5 data members: {filtered.Count}");
```

### Breakdown
```csharp
public static Dictionary<int, List<object>> ToLocal(int dataMembers = 0)
```
- **Parameter `dataMembers`**: If `0`, returns all processes. If > 0, returns only processes with exactly that many data elements
- **Returns**: Dictionary copy of process data (safe to modify locally)
- **Thread-safe**: Uses mutex lock
- **Use case**: Analyzing process data without blocking other processes
- **Note**: Returns a copy, modifications won't affect shared memory

---

## FromLocal

### Purpose
Writes a local dictionary to shared memory, completely replacing all existing data.

### Example
```csharp
// Get current data
var local = Running.ToLocal();

// Modify locally
local.Remove(12345); // Remove a process
local[99999] = new List<object> { 100, 10, 8080, "Test", "acc999" }; // Add new

// Write back to shared memory
Running.FromLocal(local);
Console.WriteLine("Updated shared memory with modified data");
```

### Breakdown
```csharp
public static void FromLocal(Dictionary<int, List<object>> localDict)
```
- **Parameter `localDict`**: Dictionary to write to shared memory
- **No return value**
- **Thread-safe**: Uses mutex lock
- **Warning**: COMPLETELY OVERWRITES all shared data
- **Use case**: Batch updates, restoring from backup
- **Caution**: This affects all processes, use carefully

---

## PruneAndUpdate

### Purpose
Removes dead processes and updates memory/uptime data for alive processes. This is a maintenance method that should be called periodically.

### Example
```csharp
// Add some processes
Running.Add(12345, new List<object> { 100, 5, 8080, "Proj1", "acc1" });
Running.Add(99999, new List<object> { 200, 10, 8081, "Proj2", "acc2" });

Console.WriteLine($"Before cleanup: {Running.Count} processes");

// Process 99999 is killed externally
// ...

// Clean up and update
Running.PruneAndUpdate();
Console.WriteLine($"After cleanup: {Running.Count} processes");
// Dead processes removed, alive processes have updated memory/uptime
```

### Breakdown
```csharp
public static void PruneAndUpdate()
```
- **No parameters**
- **No return value**
- **Thread-safe**: Uses mutex lock
- **Actions performed**:
  1. Checks each stored PID using `Process.GetProcessById()`
  2. **Dead processes**: Removed from storage
  3. **Alive processes**: Updates `data[0]` (memory) and `data[1]` (uptime in minutes)
  4. Preserves `data[2+]` (port, project, account)
- **Use case**: Periodic cleanup to prevent storage bloat
- **Recommendation**: Call this method regularly (e.g., every 5-10 seconds)

---

## Dispose

### Purpose
Releases the memory-mapped file handle. Should be called when application is shutting down.

### Example
```csharp
// At application shutdown
try
{
    Running.Dispose();
    Console.WriteLine("Shared memory resources released");
}
catch (Exception ex)
{
    Console.WriteLine($"Error during cleanup: {ex.Message}");
}
```

### Breakdown
```csharp
public static void Dispose()
```
- **No parameters**
- **No return value**
- **Thread-safe**: Uses internal lock
- **Optional**: The OS will clean up when process exits, but explicit disposal is good practice
- **Idempotent**: Safe to call multiple times
- **Note**: After disposal, next access will re-open the memory-mapped file
- **Use case**: Application shutdown, cleanup in finally blocks

---

## Architecture Notes

### Memory-Mapped File (MMF)
- **Name**: "ZennoRunningProcesses"
- **Size**: 1 MB (1024 * 1024 bytes)
- **Format**: First 4 bytes = data length, remaining bytes = UTF-8 JSON
- **Shared**: Accessible across all ZennoPoster process instances

### Mutex Synchronization
- **Name**: "ZennoRunningProcessesMutex"
- **Timeout**: 5 seconds
- **Purpose**: Prevents race conditions when multiple processes access shared memory
- **Error**: `TimeoutException` if lock cannot be acquired (indicates deadlock or hung process)

### Data Persistence
- **Lifetime**: Data persists as long as any process holds reference to MMF
- **Survival**: Survives individual process crashes
- **Reset**: Cleared only by explicit `Clear()` call or system restart

### Best Practices
1. **Call `PruneAndUpdate()` regularly** to remove dead processes
2. **Use `ToLocal()` for read-heavy operations** to minimize lock time
3. **Keep data size small** (1 MB limit total for all processes)
4. **Handle `TimeoutException`** on all operations
5. **Call `Dispose()` on application shutdown** (optional but recommended)
