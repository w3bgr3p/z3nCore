# Guild Class Documentation

## Class: Guild

### Constructor

#### Guild(IZennoPosterProjectModel project, Instance instance, bool log = false)

**Purpose**: Initializes a new Guild automation instance for parsing Guild.xyz platform data.

**Example**:
```csharp
var guild = new Guild(project, instance, log: true);
```

**Breakdown**:
```csharp
// Parameters:
// - project: IZennoPosterProjectModel - Project model for database operations
// - instance: Instance - Browser instance for automation
// - log: bool - Enable logging (default: false)
// Returns: Guild instance
// Note: Used for extracting role requirements and connection status from Guild.xyz
```

---

## Data Parsing Methods

### ParseRoles(string tablename, bool append = true)

**Purpose**: Parses Guild.xyz roles, extracting completed and incomplete roles with their requirements.

**Example**:
```csharp
guild.ParseRoles("myproject", append: true);
string doneRoles = project.Variables["guildDone"].Value;
string undoneRoles = project.Variables["guildUndone"].Value;
```

**Breakdown**:
```csharp
// Parameters:
// - tablename: string - Database table name to store results
// - append: bool - Append to existing data instead of replacing (default: true)
// Returns: void
// Note: Creates "guild_done" and "guild_undone" columns in specified table
// Stores completed roles with total count in guild_done
// Stores incomplete roles with task lists or reconnect status in guild_undone
// Saves JSON formatted data to project variables and database
```

### ParseConnections()

**Purpose**: Extracts social platform connection status from Guild.xyz profile.

**Example**:
```csharp
Dictionary<string, string> connections = guild.ParseConnections();
foreach (var conn in connections) {
    project.SendInfoToLog($"{conn.Key}: {conn.Value}");
}
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: Dictionary<string, string> - Platform name as key, connection status/data as value
// Note: Identifies platforms by SVG icons (discord, twitter, github, email, telegram, farcaster, world, google)
// Returns "none" for unconnected platforms
// Returns connection data for connected platforms
```

---

## Helper Methods

### Svg(string d)

**Purpose**: Identifies social platform type by SVG path data.

**Example**:
```csharp
string svgPath = "M108,136a16,16,0,1,1-16-16A16,16,0,0,1,108,136Zm56-16a16...";
string platform = guild.Svg(svgPath);
// Returns: "discord"
```

**Breakdown**:
```csharp
// Parameters:
// - d: string - SVG path data string
// Returns: string - Platform name ("discord", "twitter", "github", "google", "email", "telegram", "farcaster", "world") or empty string
// Note: Matches SVG path against known platform icons
```

### Svg(HtmlElement he)

**Purpose**: Identifies social platform type from HtmlElement's SVG content.

**Example**:
```csharp
HtmlElement element = instance.GetHe(("svg", "class", "icon", "regexp", 0));
string platform = guild.Svg(element);
```

**Breakdown**:
```csharp
// Parameters:
// - he: HtmlElement - HTML element containing SVG
// Returns: string - Platform name or empty string
// Note: Extracts InnerHtml from element and passes to Svg(string d) method
```

### MainButton()

**Purpose**: Retrieves the main action button element from Guild.xyz interface.

**Example**:
```csharp
HtmlElement button = guild.MainButton();
instance.HeClick(button);
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: HtmlElement - Main button element
// Note: Finds button by complex class attribute pattern
// Used for primary actions on Guild.xyz pages
```
