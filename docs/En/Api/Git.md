# Git Class Documentation

## Overview
The `Git` class provides automated Git repository synchronization to GitHub, supporting both personal and organization accounts with intelligent file filtering and size limits.

---

## Constructors

### `Git(IZennoPosterProjectModel project, string token, string username, string branch = "master", string organization = null)`

**Purpose:** Initializes Git client with GitHub credentials.

**Example:**
```csharp
var git = new Git(
    project,
    "ghp_yourGitHubToken",
    "yourusername",
    "main",
    "your-org"  // Optional
);
git.SyncRepositories("C:/Projects", "Update commit");
```

**Breakdown:**
```csharp
var git = new Git(
    project,         // IZennoPosterProjectModel - project instance
    "ghp_token...",  // string - GitHub personal access token (must start with "ghp_")
    "username",      // string - GitHub username
    "main",          // string - default branch name
    "organization"   // string - optional organization name
);
```

---

## Public Methods

### `SyncRepositories(string baseDir, string commitMessage = "ts")`

**Purpose:** Synchronizes multiple Git repositories from a base directory to GitHub.

**Example:**
```csharp
var git = new Git(project, token, username, "main");
git.SyncRepositories(
    "C:/MyProjects",
    "Updated code and docs"
);
// Syncs all projects listed in C:/MyProjects/.sync.txt
```

**Breakdown:**
```csharp
git.SyncRepositories(
    "C:/Projects",              // string - base directory containing projects
    "Update timestamp"          // string - commit message ("ts" = auto timestamp)
);
// Reads project list from {baseDir}/.sync.txt
// Processes each project: init, add, commit, push
// Logs detailed statistics upon completion
```

---

### Configuration File (.sync.txt)

Create a `.sync.txt` file in the base directory:

```txt
project1
project2:false
project3
# This is a comment
project4
```

- Each line = one project folder name
- Add `:false` to skip syncing
- Lines starting with `#` are comments
- Empty lines are ignored

---

### `GetFileHash(string filePath)` [Static]

**Purpose:** Calculates MD5 hash of a file for change detection.

**Example:**
```csharp
string hash = Git.GetFileHash("C:/file.txt");
Console.WriteLine(hash);  // Output: "098f6bcd4621d373cade4e832627b4f6"
```

**Breakdown:**
```csharp
string md5Hash = Git.GetFileHash(
    "C:/path/to/file.txt"  // string - absolute file path
);
// Returns: string - MD5 hash in lowercase hex format
// Returns: string.Empty - if file reading fails
```

---

## Size and File Limits

The class enforces these limits to prevent issues:

| Limit Type | Value |
|------------|-------|
| Max file size | 100 MB |
| Max total repo size | 1000 MB |
| Max file count | 10,000 files |

**Excluded file types:**
- Executables: `.exe`, `.so`, `.dylib`, `.bin`, `.obj`, `.lib`, `.a`
- Archives: `.zip`, `.rar`, `.7z`, `.tar`, `.gz`, `.bz2`
- Disk images: `.iso`, `.img`, `.dmg`, `.msi`, `.deb`, `.rpm`
- Media: `.mp4`, `.avi`, `.mkv`, `.mov`, `.mp3`, `.wav`
- Design: `.psd`, `.ai`, `.sketch`, `.fig`
- Databases: `.db`, `.sqlite`, `.mdb`, `.accdb`

---

## .gitignore Management

Automatically creates/updates `.gitignore` with common patterns:

```gitignore
*.exe
*.dll
*.zip
node_modules/
.vs/
.vscode/
bin/
obj/
Thumbs.db
.DS_Store
```

---

## Workflow

For each project in `.sync.txt`:

1. **Size Check:** Validates file count and total size
2. **Git Init:** Initializes repository if needed
3. **Safe Directory:** Configures Git safe directory
4. **Remote Setup:** Adds/updates GitHub remote URL
5. **Status Check:** Checks for file changes
6. **Commit:** Commits changes if any exist
7. **Push:** Force pushes to GitHub
8. **Logging:** Logs operation results

---

## Statistics

After completion, logs summary:

```
=======================Summary=======================
Total=10, Changes=5, Skipped=3, SizeSkipped=1,
Committed=5, Failed=1
```

---

## GitHub URL Format

**Personal account:**
```
https://{token}@github.com/{username}/{repo}.git
```

**Organization:**
```
https://{token}@github.com/{organization}/{repo}.git
```

---

## Notes

- Requires Git installed and accessible via command line
- Token must have `repo` scope permissions
- Uses force push (`--force`) - be cautious with existing repositories
- Skips folders with `.git` directory in file counts
- Creates `master` branch by default (configurable)
- All Git operations run in separate processes
- Automatically configures user name and email for commits
- Thread sleeps: 2s between repos, 5s after errors
