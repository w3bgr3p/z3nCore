# Snapper Class

Class for creating snapshots and backups of ZennoPoster projects and core DLL files.

---

## Constructor

### Purpose
Initializes the Snapper class with a project instance.

### Example
```csharp
using z3nCore.Utilities;

var snapper = new Snapper(project);
```

---

## SnapDir

### Purpose
Creates snapshots of all project files in the current directory, tracking changes via file hashes and maintaining version history.

### Example
```csharp
using z3nCore.Utilities;

var snapper = new Snapper(project);

// Create snapshot with default path (from project variable)
snapper.SnapDir();

// Create snapshot with custom path
snapper.SnapDir(pathSnaps: @"W:\backups\projects");
```

### Breakdown
```csharp
public void SnapDir(
    string pathSnaps = null)  // Snapshot directory path (null = use project variable "snapsDir")

// Returns: void

// Process:
// 1. Gets all files in project directory (*.zp, etc.)
// 2. For each file:
//    - Calculates SHA hash
//    - Compares with existing snapshots
//    - If changed:
//      * Copies to project folder
//      * Creates timestamped backup in snapshots/
//    - Logs update or existence
// 3. Updates .sync.txt with project list
// 4. Updates .access.txt with active projects
//
// File structure created:
// {pathSnaps}/
//   .sync.txt                    // Project list with sync flags
//   .access.txt                  // Active projects list
//   {ProjectName}/
//     {ProjectName}.zp           // Latest version
//     snapshots/
//       20251122_1430.ProjectName.zp  // Timestamped backup
//       20251122_1445.ProjectName.zp  // Another backup
//       ...
//
// .sync.txt format:
// ProjectName : true   (sync enabled)
// ProjectName : false  (sync disabled)
//
// Features:
// - Hash-based change detection (no duplicates)
// - Automatic timestamping: yyyyMMdd_HHmm
// - Multiple project support
// - Sync configuration per project
// - Access tracking for active projects
```

---

## SnapCoreDll

### Purpose
Creates snapshots of z3nCore.dll and all ExternalAssemblies, maintaining version archive and updating dependent projects.

### Example
```csharp
using z3nCore.Utilities;

var snapper = new Snapper(project);

// Snapshot core DLL and dependencies
snapper.SnapCoreDll();

// Logs version info:
// "ZP: v7.8.0.0, z3nCore: v1.2.3.4"
```

### Breakdown
```csharp
public void SnapCoreDll()

// Returns: void

// Process:
// 1. Detects versions:
//    - Gets z3nCore.dll version from ExternalAssemblies
//    - Gets ZennoPoster version from process path
//    - Logs both versions
//    - Sets project variables: vZP, vDll
//
// 2. Copies ExternalAssemblies to:
//    - W:\work_hard\zenoposter\CURRENT_JOBS\.snaps\z3nFarm\ExternalAssemblies\
//    - W:\code_hard\.net\z3nCore\ExternalAssemblies\
//
// 3. Archives version:
//    - Creates: W:\code_hard\.net\z3nCore\verions\v{version}\z3nCore.dll
//    - Creates: dependencies.txt with all DLL versions
//
// 4. Updates dependent projects:
//    - _z3nLnch.zp → z3nLauncher.zp
//    - SAFU.zp → SAFU.zp
//    - DbBuilder.zp → DbBuilder.zp
//    - Deletes old versions
//    - Copies from snapshots
//    - Logs: "{n} updated, {m} missing"
//
// dependencies.txt format:
// z3nCore.dll : 1.2.3.4
// Newtonsoft.Json.dll : 13.0.1.0
// OtpNet.dll : 1.9.1.0
// ...

// Hardcoded paths (configure as needed):
// - ExternalAssemblies: {ZP_DIR}\ExternalAssemblies\
// - z3nCore repo: W:\code_hard\.net\z3nCore\ExternalAssemblies\
// - z3nFarm repo: W:\work_hard\zenoposter\CURRENT_JOBS\.snaps\z3nFarm\
// - Version archive: W:\code_hard\.net\z3nCore\verions\v{version}\
// - Project snapshots: W:\work_hard\zenoposter\CURRENT_JOBS\.snaps\

// Notes:
// - Requires specific directory structure
// - Designed for development/deployment workflow
// - Automatically tracks all DLL dependencies
// - Maintains version history
```

---

## Usage Workflow

### Regular Project Snapshots
```csharp
using z3nCore.Utilities;

var snapper = new Snapper(project);

// Daily backup of all projects
snapper.SnapDir(@"W:\backups\daily");

// Only creates new snapshot if file changed (hash-based)
```

### Development Deployment
```csharp
using z3nCore.Utilities;

var snapper = new Snapper(project);

// After updating z3nCore.dll
snapper.SnapCoreDll();
// Result:
// 1. DLL copied to repositories
// 2. Version archived
// 3. Dependent projects updated
// 4. Dependencies documented
```

### Access Control
```csharp
// .sync.txt configuration:
// ProjectA : true   ← Will be added to .access.txt
// ProjectB : false  ← Will NOT be in .access.txt

// .access.txt result:
// ProjectA

// Use case: Only ProjectA gets deployed/synced
```

---
