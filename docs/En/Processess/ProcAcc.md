# ProcAcc Class

**Namespace:** `z3nCore.Utilities`

Management of associations between process IDs (PID) and account names (ACC). This class provides methods to track, query, and manage browser processes associated with specific accounts.

---

## GetAllPidAcc

### Purpose
Retrieves all PID → ACC associations with caching support. The cache is valid for 2 seconds by default.

### Example
```csharp
// Get all PID-ACC associations (uses cache if available)
var allPids = ProcAcc.GetAllPidAcc();
foreach (var pair in allPids)
{
    int pid = pair.Key;         // Process ID
    string acc = pair.Value;    // Account name
    Console.WriteLine($"PID {pid} → ACC {acc}");
}

// Force refresh cache
var freshPids = ProcAcc.GetAllPidAcc(forceRefresh: true);
```

### Breakdown
```csharp
public static Dictionary<int, string> GetAllPidAcc(bool forceRefresh = false)
```
- **Parameter `forceRefresh`**: Set to `true` to bypass cache and scan all processes immediately
- **Returns**: Dictionary where key is PID (int) and value is account name (string)
- **Thread-safe**: Uses internal locking mechanism
- **Caching**: Results cached for 2 seconds unless `forceRefresh = true`

---

## GetPids

### Purpose
Retrieves all process IDs associated with a specific account name.

### Example
```csharp
// Get all PIDs for account "acc123"
List<int> pids = ProcAcc.GetPids("acc123");

if (pids.Count > 0)
{
    Console.WriteLine($"Account acc123 has {pids.Count} running processes");
    foreach (int pid in pids)
    {
        Console.WriteLine($"  - PID: {pid}");
    }
}
else
{
    Console.WriteLine("Account not running");
}
```

### Breakdown
```csharp
public static List<int> GetPids(string acc)
```
- **Parameter `acc`**: Account name to search for (normalized automatically)
- **Returns**: List of process IDs associated with the account (empty list if none found)
- **Note**: Account name is normalized (removes "acc"/"ACC" prefix)
- **Uses cache**: Calls `GetAllPidAcc()` internally

---

## GetAcc

### Purpose
Retrieves the account name associated with a specific process ID.

### Example
```csharp
int pid = 12345;
string acc = ProcAcc.GetAcc(pid);

if (acc != null)
{
    Console.WriteLine($"PID {pid} belongs to account: {acc}");
}
else
{
    Console.WriteLine($"PID {pid} not found or not associated with any account");
}
```

### Breakdown
```csharp
public static string GetAcc(int pid)
```
- **Parameter `pid`**: Process ID to look up
- **Returns**: Account name (string) if found, `null` if PID doesn't exist or has no association
- **Uses cache**: Calls `GetAllPidAcc()` internally

---

## IsRunning

### Purpose
Checks if an account has any running processes.

### Example
```csharp
if (ProcAcc.IsRunning("acc123"))
{
    Console.WriteLine("Account acc123 is currently running");
}
else
{
    Console.WriteLine("Account acc123 is not running");
}
```

### Breakdown
```csharp
public static bool IsRunning(string acc)
```
- **Parameter `acc`**: Account name to check
- **Returns**: `true` if account has at least one running process, `false` otherwise
- **Implementation**: Checks if `GetPids(acc).Count > 0`

---

## ClearCache

### Purpose
Clears the internal cache of PID-ACC associations. Should be called after killing processes to ensure fresh data on next query.

### Example
```csharp
// Kill a process
ProcAcc.KillPid(12345);

// Clear cache to ensure next query scans processes
ProcAcc.ClearCache();

// Now this will scan fresh data
var pids = ProcAcc.GetAllPidAcc();
```

### Breakdown
```csharp
public static void ClearCache()
```
- **No parameters**
- **No return value**
- **Thread-safe**: Uses internal locking
- **When to use**: After killing processes, or when you need absolutely fresh data

---

## GetNewlyLaunchedPid

### Purpose
Fast search for a newly launched browser process. Searches only among processes that didn't exist before launch.

### Example
```csharp
// Before launching browser, take a snapshot
HashSet<int> pidsBefore = ProcAcc.GetPidSnapshot();

// Launch browser for account acc123
// ... browser launch code ...

// Find the new PID (waits up to 1 second with 10 attempts)
int newPid = ProcAcc.GetNewlyLaunchedPid("acc123", pidsBefore, maxAttempts: 10, delayMs: 100);

if (newPid > 0)
{
    Console.WriteLine($"Browser launched successfully with PID: {newPid}");
}
else
{
    Console.WriteLine("Failed to detect new browser process");
}
```

### Breakdown
```csharp
public static int GetNewlyLaunchedPid(string acc, HashSet<int> pidsBeforeLaunch, int maxAttempts = 10, int delayMs = 100)
```
- **Parameter `acc`**: Account name to search for
- **Parameter `pidsBeforeLaunch`**: Set of PIDs that existed before browser launch (use `GetPidSnapshot()`)
- **Parameter `maxAttempts`**: Maximum number of search attempts (default: 10)
- **Parameter `delayMs`**: Delay between attempts in milliseconds (default: 100)
- **Returns**: New process ID if found, `0` if not found
- **Side effect**: Clears cache when process is found
- **Performance**: Only scans new processes, not all processes

---

## GetPidSnapshot

### Purpose
Creates a snapshot of all current zbe1 process IDs. Used to detect newly launched processes.

### Example
```csharp
// Take snapshot before launching
HashSet<int> before = ProcAcc.GetPidSnapshot();

// Launch browser
// ...

// Take snapshot after launching
HashSet<int> after = ProcAcc.GetPidSnapshot();

// Find new PIDs
var newPids = after.Except(before);
foreach (int pid in newPids)
{
    Console.WriteLine($"New process detected: {pid}");
}
```

### Breakdown
```csharp
public static HashSet<int> GetPidSnapshot()
```
- **No parameters**
- **Returns**: HashSet of all current zbe1 process IDs
- **Fast operation**: Only gets process IDs, doesn't read command lines
- **Use case**: Detecting newly launched processes

---

## FindFirstNewPid

### Purpose
Alternative method for finding newly launched browser. Searches for the first process matching the account that started after a specific time. Stops immediately after finding.

### Example
```csharp
// Record launch time
DateTime launchTime = DateTime.Now;

// Launch browser
// ... browser launch code ...

// Find new PID (waits up to 2 seconds)
int newPid = ProcAcc.FindFirstNewPid("acc123", launchTime, maxWaitMs: 2000);

if (newPid > 0)
{
    Console.WriteLine($"Found new browser process: {newPid}");
}
```

### Breakdown
```csharp
public static int FindFirstNewPid(string acc, DateTime launchedAfter, int maxWaitMs = 2000)
```
- **Parameter `acc`**: Account name to search for
- **Parameter `launchedAfter`**: Only consider processes started after this time
- **Parameter `maxWaitMs`**: Maximum wait time in milliseconds (default: 2000)
- **Returns**: Process ID if found, `0` if not found within timeout
- **Side effect**: Clears cache when process is found
- **Performance**: Stops immediately after finding first match

---

## GetNewest

### Purpose
Gets the newest (most recently started) process ID for an account.

### Example
```csharp
int newestPid = ProcAcc.GetNewest("acc123");

if (newestPid > 0)
{
    Console.WriteLine($"Newest process for acc123: {newestPid}");
}
else
{
    Console.WriteLine("No processes found for acc123");
}
```

### Breakdown
```csharp
public static int GetNewest(string acc)
```
- **Parameter `acc`**: Account name
- **Returns**: PID of newest process, `0` if no processes found
- **Selection criteria**: Process with highest `StartTime`

---

## GetOldest

### Purpose
Gets the oldest (longest running) process ID for an account.

### Example
```csharp
int oldestPid = ProcAcc.GetOldest("acc123");

if (oldestPid > 0)
{
    Console.WriteLine($"Oldest process for acc123: {oldestPid}");
}
```

### Breakdown
```csharp
public static int GetOldest(string acc)
```
- **Parameter `acc`**: Account name
- **Returns**: PID of oldest process, `0` if no processes found
- **Selection criteria**: Process with lowest `StartTime`

---

## GetHeaviest

### Purpose
Gets the process ID using the most memory for an account.

### Example
```csharp
int heaviestPid = ProcAcc.GetHeaviest("acc123");

if (heaviestPid > 0)
{
    Console.WriteLine($"Process using most memory: {heaviestPid}");
}
```

### Breakdown
```csharp
public static int GetHeaviest(string acc)
```
- **Parameter `acc`**: Account name
- **Returns**: PID of process with highest memory usage, `0` if no processes found
- **Selection criteria**: Process with highest `WorkingSet64` (memory usage)

---

## GetLightest

### Purpose
Gets the process ID using the least memory for an account.

### Example
```csharp
int lightestPid = ProcAcc.GetLightest("acc123");

if (lightestPid > 0)
{
    Console.WriteLine($"Process using least memory: {lightestPid}");
}
```

### Breakdown
```csharp
public static int GetLightest(string acc)
```
- **Parameter `acc`**: Account name
- **Returns**: PID of process with lowest memory usage, `0` if no processes found
- **Selection criteria**: Process with lowest `WorkingSet64` (memory usage)

---

## Kill

### Purpose
Terminates all processes associated with an account.

### Example
```csharp
int killedCount = ProcAcc.Kill("acc123");

Console.WriteLine($"Terminated {killedCount} process(es) for acc123");

// Cache is automatically cleared after successful kill
```

### Breakdown
```csharp
public static int Kill(string acc)
```
- **Parameter `acc`**: Account name
- **Returns**: Number of processes successfully terminated
- **Side effect**: Automatically clears cache if any processes were killed
- **Error handling**: Silently catches exceptions for individual process kills

---

## KillOld

### Purpose
Terminates all processes for an account EXCEPT the newest one. Useful for cleaning up duplicate/old browser instances.

### Example
```csharp
// Account has 3 processes running
int killedCount = ProcAcc.KillOld("acc123");

Console.WriteLine($"Terminated {killedCount} old process(es), keeping the newest");
```

### Breakdown
```csharp
public static int KillOld(string acc)
```
- **Parameter `acc`**: Account name
- **Returns**: Number of old processes terminated
- **Keeps alive**: The newest process (highest `StartTime`)
- **Side effect**: Automatically clears cache if any processes were killed
- **Use case**: Preventing multiple browser instances for same account

---

## KillPid

### Purpose
Terminates a specific process by its process ID.

### Example
```csharp
int pidToKill = 12345;
bool success = ProcAcc.KillPid(pidToKill);

if (success)
{
    Console.WriteLine($"Process {pidToKill} terminated successfully");
}
else
{
    Console.WriteLine($"Failed to terminate process {pidToKill}");
}
```

### Breakdown
```csharp
public static bool KillPid(int pid)
```
- **Parameter `pid`**: Process ID to terminate
- **Returns**: `true` if process was terminated, `false` if failed or process doesn't exist
- **Side effect**: Automatically clears cache on success
- **Error handling**: Returns `false` on any exception

---

## GetDetails

### Purpose
Retrieves detailed information about all processes associated with an account (memory usage, uptime, thread count).

### Example
```csharp
List<string> details = ProcAcc.GetDetails("acc123");

Console.WriteLine($"Details for acc123:");
foreach (string info in details)
{
    Console.WriteLine(info);
    // Output example: "PID:12345, Mem:150MB, Up:25min, Threads:42"
}
```

### Breakdown
```csharp
public static List<string> GetDetails(string acc)
```
- **Parameter `acc`**: Account name
- **Returns**: List of formatted strings with process details
- **Format**: `"PID:{pid}, Mem:{memory}MB, Up:{uptime}min, Threads:{count}"`
- **Information included**: Process ID, memory in MB, uptime in minutes, thread count
- **Error handling**: Silently skips processes that throw exceptions

---

## PidReport

### Purpose
Generates a comprehensive report of all running processes, including both bound (associated with accounts) and unbound processes.

### Example
```csharp
Dictionary<int, List<object>> report = ProcAcc.PidReport();

foreach (var entry in report)
{
    int pid = entry.Key;
    int mem = Convert.ToInt32(entry.Value[0]);      // Memory in MB
    int age = Convert.ToInt32(entry.Value[1]);      // Age in minutes
    string proj = entry.Value[2].ToString();         // Project name
    string acc = entry.Value[3].ToString();          // Account name

    Console.WriteLine($"PID: {pid}, ACC: {acc}, Mem: {mem}MB, Age: {age}min");
}
```

### Breakdown
```csharp
public static Dictionary<int, List<object>> PidReport()
```
- **No parameters**
- **Returns**: Dictionary where key is PID, value is list containing: `[memory(MB), age(minutes), project, account]`
- **Data structure**: `List<object>` with 4 elements: `[int mem, int age, string proj, string acc]`
- **Integration**: Updates `Running` class data
- **Side effect**: Calls `Running.PruneAndUpdate()` to clean up dead processes

---

## zbe1

### Purpose
Gets a list of all current zbe1 process IDs. This is a low-level method used internally.

### Example
```csharp
List<int> allZbe1Pids = ProcAcc.zbe1();

Console.WriteLine($"Total zbe1 processes: {allZbe1Pids.Count}");
foreach (int pid in allZbe1Pids)
{
    Console.WriteLine($"  - {pid}");
}
```

### Breakdown
```csharp
public static List<int> zbe1()
```
- **No parameters**
- **Returns**: List of all zbe1 process IDs
- **Process name**: Searches for processes named "zbe1"
- **Resource management**: Properly disposes Process objects in finally block
- **Performance**: Fast operation, only retrieves PIDs without additional data
