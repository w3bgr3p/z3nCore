# ZB (ZennoBrowser)

Utilities for working with ZennoBrowser profiles and executing ZennoBrowser-specific operations.

## Extension Methods

### ZBids()

```csharp
public static Dictionary<string, string> ZBids(this IZennoPosterProjectModel project)
```

**Purpose**: Retrieves mapping of ZennoBrowser profile IDs to account names from ZennoBrowser database.

**Example**:
```csharp
var zbProfiles = project.ZBids();

foreach (var profile in zbProfiles)
{
    project.log($"Profile ID: {profile.Key}, Account: {profile.Value}");
}

// Check if specific account exists
if (zbProfiles.ContainsValue("myAccount123"))
{
    project.log("Profile found!");
}
```

**Breakdown**:
- Extension method on project
- Reads from ZennoBrowser's ProfileManagement.db
- Database location: `%LocalAppData%\ZennoLab\ZP8\.zp8\ProfileManagement.db`
- Returns Dictionary<string, string> with profile_id => account_name mapping
- Skips template profiles automatically
- Thread-safe with database locking
- Temporarily switches to SQLite mode and restores original settings
- Throws FileNotFoundException if database not found

---

### ZB()

```csharp
public static bool ZB(this IZennoPosterProjectModel project, string toDo)
```

**Purpose**: Executes ZennoBrowser-specific operations by running internal ZB.zp project.

**Example**:
```csharp
// Execute ZennoBrowser operation
bool success = project.ZB("createProfile");

if (success)
{
    project.log("ZennoBrowser operation completed successfully");
}
else
{
    project.log("ZennoBrowser operation failed");
}
```

**Breakdown**:
- `toDo` - Operation to perform (passed to ZB.zp project)
- Executes `ProjectPath\.internal\ZB.zp` project
- Automatically maps required variables between projects:
  - acc0, cfgLog, cfgPin
  - DBmode, DBpstgrPass, DBpstgrUser, DBsqltPath
  - instancePort, lastQuery, cookies
  - projectScript, varSessionId, toDo
- Sets "toDo" variable with operation name
- Returns true if ZB.zp execution succeeded
- Returns false if ZB.zp execution failed
- Requires ZB.zp project file in `.internal` folder
- Passes variables bidirectionally (to and from ZB.zp)
- Waits for ZB.zp completion before continuing
