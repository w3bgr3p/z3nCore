# GitHub Class Documentation

## Overview
The `GitHub` class provides comprehensive GitHub API integration for managing repositories, collaborators, and repository settings for both personal accounts and organizations.

---

## Constructors

### `GitHub(string token, string username, string organization = null)`

**Purpose:** Initializes GitHub API client with authentication.

**Example:**
```csharp
// Personal account
var github = new GitHub("ghp_token...", "username");

// Organization
var github = new GitHub("ghp_token...", "username", "my-org");
```

**Breakdown:**
```csharp
var github = new GitHub(
    "ghp_token...",    // string - GitHub personal access token
    "username",        // string - GitHub username
    "organization"     // string - optional organization name
);
// Note: HttpClient automatically configured with GitHub API base URL
```

---

## Public Methods

### `GetRepositoryInfo(string repoName)`

**Purpose:** Retrieves detailed information about a repository.

**Example:**
```csharp
var github = new GitHub(token, username);
string repoInfo = github.GetRepositoryInfo("my-repo");
// Returns JSON with repo details: stars, forks, language, etc.
```

**Breakdown:**
```csharp
string repositoryInfo = github.GetRepositoryInfo(
    "my-repo"  // string - repository name
);
// Returns: string - JSON response with repository metadata
// Error format: "Error: {message}"
```

---

### `GetCollaborators(string repoName)`

**Purpose:** Lists all collaborators for a repository.

**Example:**
```csharp
var github = new GitHub(token, username);
string collaborators = github.GetCollaborators("my-repo");
// Returns JSON array of collaborators with permissions
```

**Breakdown:**
```csharp
string collaboratorsList = github.GetCollaborators(
    "my-repo"  // string - repository name
);
// Returns: string - JSON array of collaborator objects
// Each object includes: login, permissions, role_name
```

---

### `CreateRepository(string repoName)`

**Purpose:** Creates a new private repository.

**Example:**
```csharp
var github = new GitHub(token, username);
string result = github.CreateRepository("new-project");

// For organization
var github = new GitHub(token, username, "my-org");
string result = github.CreateRepository("org-project");
```

**Breakdown:**
```csharp
string createResult = github.CreateRepository(
    "new-repo"  // string - name for new repository
);
// Returns: string - JSON response with created repo details
// Creates as: Private repository by default
// Location: Personal account or organization (based on constructor)
```

---

### `ChangeVisibility(string repoName, bool makePrivate)`

**Purpose:** Changes repository visibility between public and private.

**Example:**
```csharp
var github = new GitHub(token, username);

// Make private
string result = github.ChangeVisibility("my-repo", true);

// Make public
string result = github.ChangeVisibility("my-repo", false);
```

**Breakdown:**
```csharp
string changeResult = github.ChangeVisibility(
    "my-repo",  // string - repository name
    true        // bool - true=private, false=public
);
// Returns: string - JSON response with updated repo settings
// Note: Requires appropriate permissions
```

---

### `AddCollaborator(string repoName, string collaboratorUsername, string permission = "pull")`

**Purpose:** Adds a collaborator to a repository with specific permissions.

**Example:**
```csharp
var github = new GitHub(token, username);

// Add with read-only access
github.AddCollaborator("my-repo", "johndoe", "pull");

// Add with write access
github.AddCollaborator("my-repo", "janedoe", "push");

// Add with admin access
github.AddCollaborator("my-repo", "admin-user", "admin");
```

**Breakdown:**
```csharp
string addResult = github.AddCollaborator(
    "my-repo",     // string - repository name
    "username",    // string - collaborator's GitHub username
    "pull"         // string - permission level
);
// Returns: string - JSON response
// Permissions: "pull" (read), "push" (write), "admin", "maintain", "triage"
```

---

### `RemoveCollaborator(string repoName, string collaboratorUsername)`

**Purpose:** Removes a collaborator from a repository.

**Example:**
```csharp
var github = new GitHub(token, username);
string result = github.RemoveCollaborator("my-repo", "johndoe");
```

**Breakdown:**
```csharp
string removeResult = github.RemoveCollaborator(
    "my-repo",     // string - repository name
    "username"     // string - collaborator to remove
);
// Returns: string - JSON response confirming removal
```

---

### `ChangeCollaboratorPermission(string repoName, string collaboratorUsername, string permission = "pull")`

**Purpose:** Updates an existing collaborator's permission level.

**Example:**
```csharp
var github = new GitHub(token, username);

// Upgrade to admin
string result = github.ChangeCollaboratorPermission(
    "my-repo",
    "johndoe",
    "admin"
);
```

**Breakdown:**
```csharp
string changeResult = github.ChangeCollaboratorPermission(
    "my-repo",     // string - repository name
    "username",    // string - collaborator's username
    "push"         // string - new permission level
);
// Returns: "Success: Permission updated" or JSON response
// Valid permissions: "pull", "push", "admin", "maintain", "triage"
// Returns error if invalid permission provided
```

---

## Permission Levels

| Permission | Access Level | Description |
|-----------|-------------|-------------|
| pull | Read | Can read and clone |
| triage | Triage | Can manage issues/PRs |
| push | Write | Can push changes |
| maintain | Maintain | Can manage repo (no admin) |
| admin | Admin | Full repository access |

---

## API Endpoints Used

All endpoints relative to `https://api.github.com/`:

- `GET repos/{owner}/{repo}` - Repository info
- `GET repos/{owner}/{repo}/collaborators` - List collaborators
- `POST user/repos` - Create repo (personal)
- `POST orgs/{org}/repos` - Create repo (organization)
- `PATCH repos/{owner}/{repo}` - Update repo settings
- `PUT repos/{owner}/{repo}/collaborators/{username}` - Add/update collaborator
- `DELETE repos/{owner}/{repo}/collaborators/{username}` - Remove collaborator

---

## Authentication

All requests include:
```
Authorization: token {your_github_token}
User-Agent: GitHubManagerApp
```

---

## Error Handling

All methods return error messages in format:
```
"Error: {error_message}"
```

Check response for "Error:" prefix to detect failures.

---

## Notes

- Token must have appropriate scopes (`repo`, `admin:org` for orgs)
- HttpClient automatically handles base URL and headers
- All operations use synchronous `.Result` calls
- User-Agent header required by GitHub API
- Organization name in constructor routes create operations to organization
- Default repository visibility: Private
- All HTTP methods use JSON content type
