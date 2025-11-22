# tGit and GHsync Classes Documentation

These classes provide GitHub repository synchronization functionality for ZennoPoster projects.

---

## tGit Class

Modern, refactored implementation for GitHub repository synchronization with improved error handling and validation.

### Constructor

**Purpose**: Initializes a new tGit instance with GitHub authentication credentials.

**Example**:
```csharp
// Create GitHub sync instance
var gitSync = new tGit(
    project: project,
    token: "ghp_xxxxxxxxxxxxxxxxxxxx",
    username: "myusername",
    organization: null  // Optional: use organization instead of user repos
);
```

**Breakdown**:
```csharp
public tGit(
    IZennoPosterProjectModel project,  // ZennoPoster project instance
    string token,                       // GitHub personal access token (must start with "ghp_")
    string username,                    // GitHub username
    string organization = null)         // Optional: GitHub organization name
{
    // Validates token format (must start with "ghp_")
    // Validates username is not empty
    // Throws ArgumentNullException if project is null
    // Throws ArgumentException if token or username is invalid
}
```

---

### SyncRepositories

**Purpose**: Synchronizes local directories to GitHub repositories based on configuration file.

**Example**:
```csharp
var gitSync = new tGit(project, token, username);

// Sync all repositories listed in .sync.txt
gitSync.SyncRepositories(
    baseDir: @"C:\Projects",
    commitMessage: "Updated code"
);

// Use timestamp as commit message
gitSync.SyncRepositories(
    baseDir: @"C:\Projects",
    commitMessage: "ts"  // Will use current UTC timestamp
);
```

**Breakdown**:
```csharp
public void SyncRepositories(
    string baseDir,                    // Base directory containing projects to sync
    string commitMessage = "ts")       // Commit message ("ts" = auto timestamp)
{
    // Reads .sync.txt file from baseDir for list of projects
    // Validates directory exists and token was provided
    // For each project:
    //   - Checks repository size limits (max 100MB per file, 1000MB total)
    //   - Initializes git repository if needed
    //   - Configures remote URL with authentication
    //   - Creates/updates .gitignore
    //   - Commits and pushes changes if any
    // Skips projects marked with "false" in .sync.txt
    // Logs comprehensive statistics at the end
    // Uses delays between operations to avoid rate limiting
}
```

---

### Main (Obsolete)

**Purpose**: Legacy method for backward compatibility.

**Example**:
```csharp
var gitSync = new tGit(project);
gitSync.Main(
    baseDir: @"C:\Projects",
    token: "ghp_token",
    username: "myusername",
    commitMessage: "update"
);
```

**Breakdown**:
```csharp
[Obsolete("Use constructor with parameters and SyncRepositories()")]
public void Main(
    string baseDir,          // Base directory path
    string token,            // GitHub token
    string username,         // GitHub username
    string commitMessage = "ts")  // Commit message
{
    // Creates temporary tGit instance and calls SyncRepositories
    // Kept for backward compatibility only
    // Recommended to use new constructor instead
}
```

---

### GetFileHash

**Purpose**: Computes MD5 hash of a file for change detection.

**Example**:
```csharp
// Get hash of a file
string hash = tGit.GetFileHash(@"C:\path\to\file.txt");
project.SendInfoToLog($"File hash: {hash}");

// Compare files
string hash1 = tGit.GetFileHash("file1.txt");
string hash2 = tGit.GetFileHash("file2.txt");
if (hash1 == hash2)
{
    project.SendInfoToLog("Files are identical");
}
```

**Breakdown**:
```csharp
public static string GetFileHash(string filePath)
{
    // Parameter: filePath - absolute path to file
    // Returns: string - lowercase MD5 hash without dashes
    // Returns: empty string on error
    // Uses MD5 algorithm for fast hash computation
    // Disposes stream and MD5 instance properly
}
```

---

## GHsync Class

Legacy implementation of GitHub synchronization. Use `tGit` for new projects.

### Constructor

**Purpose**: Initializes GHsync instance.

**Example**:
```csharp
var sync = new GHsync(project);
```

**Breakdown**:
```csharp
public GHsync(IZennoPosterProjectModel project)
{
    // Initializes project reference and logger
    // Sets up size limits and file exclusion rules
}
```

---

### Main

**Purpose**: Synchronizes repositories to GitHub (legacy method).

**Example**:
```csharp
var sync = new GHsync(project);
sync.Main(
    baseDir: @"C:\MyProjects",
    token: "ghp_token",
    username: "myuser",
    commmit: "daily update"
);
```

**Breakdown**:
```csharp
public void Main(
    string baseDir,          // Base directory containing projects
    string token,            // GitHub personal access token
    string username,         // GitHub username
    string commmit = "ts")   // Commit message
{
    // Reads .sync.txt configuration file
    // Validates directory, token format, and username
    // Processes each project directory:
    //   - Validates repository size (max 100MB per file, 1000MB total, 10000 files max)
    //   - Excludes binary and large files (.exe, .zip, media files, etc.)
    //   - Initializes git if needed
    //   - Adds remote origin with token authentication
    //   - Creates/updates .gitignore with common patterns
    //   - Commits and force pushes to master branch
    // Logs summary statistics (total, changes, skipped, committed, failed)
}
```

---

### GetFileHash

**Purpose**: Computes MD5 hash of a file.

**Example**:
```csharp
string hash = GHsync.GetFileHash("myfile.txt");
```

**Breakdown**:
```csharp
public static string GetFileHash(string filePath)
{
    // Parameter: filePath - path to file
    // Returns: string - MD5 hash in lowercase hex format
    // Returns: empty string if file cannot be read
    // Same implementation as tGit.GetFileHash
}
```

---

## Configuration File Format

Both classes use `.sync.txt` file in the base directory:

```
ProjectFolder1
ProjectFolder2
ProjectFolder3:false
AnotherProject
```

- Each line contains a project folder name
- Append `:false` to skip synchronization for that project
- Empty lines are ignored

---

## Size and File Limits

Both classes enforce these limits:

- **MAX_FILE_SIZE_MB**: 100 MB per file
- **MAX_TOTAL_SIZE_MB**: 1000 MB total repository size
- **MAX_FILES_COUNT**: 10,000 files maximum
- **Excluded extensions**: .exe, .dll, .zip, .rar, media files, databases, etc.

---

## Best Practices

1. **Use tGit for new projects** - better error handling and validation
2. **Create .sync.txt** before running to specify which folders to sync
3. **Use meaningful commit messages** or "ts" for automatic timestamps
4. **Monitor logs** for size limit warnings and errors
5. **Keep repositories under size limits** to avoid skipping
6. **Use .gitignore** to exclude build artifacts and dependencies

---

