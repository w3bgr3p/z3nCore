# DiscordApi Class Documentation

## Overview
The `DiscordApi` class provides integration with Discord API for managing server roles using bot tokens.

---

## Constructor

### `DiscordApi(IZennoPosterProjectModel project, Instance instance, bool log = false)`

**Purpose:** Initializes the Discord API client.

**Example:**
```csharp
var discord = new DiscordApi(project, instance, log: true);
bool success = discord.ManageRole(botToken, guildId, "Member", userId, true);
```

**Breakdown:**
```csharp
var discord = new DiscordApi(
    project,   // IZennoPosterProjectModel - project instance
    instance,  // Instance - ZennoPoster instance
    true       // bool - enable logging
);
```

---

## Public Methods

### `ManageRole(string botToken, string guildId, string roleName, string userId, bool assignRole, string callerName = "")`

**Purpose:** Assigns or removes a role for a user in a Discord server.

**Example:**
```csharp
var discord = new DiscordApi(project, instance);

// Assign role
bool assigned = discord.ManageRole(
    "Bot_TOKEN_HERE",
    "123456789012345678",     // Server (guild) ID
    "Verified",                // Role name
    "987654321098765432",     // User ID
    true                       // Assign role (true) or remove (false)
);

if (assigned)
{
    Console.WriteLine("Role assigned successfully");
}

// Remove role
bool removed = discord.ManageRole(
    botToken,
    guildId,
    "Member",
    userId,
    false  // Remove role
);
```

**Breakdown:**
```csharp
bool success = discord.ManageRole(
    "Bot_TOKEN...",           // string - Discord bot token
    "123456789012345678",     // string - Discord server (guild) ID
    "Verified",               // string - role name to assign/remove
    "987654321098765432",     // string - Discord user ID
    true,                     // bool - true=assign, false=remove
    "MethodName"              // string - caller method name (auto-filled)
);
// Returns: bool - true if operation succeeded, false otherwise
// Note: Automatically adds 1-second delays between requests
```

---

## Workflow

The method performs the following steps:

1. **Fetch Roles:** Gets all roles from the server
2. **Find Role:** Searches for the specified role by name (case-insensitive)
3. **Validate Role:** Checks if role exists
4. **Assign/Remove:** Uses Discord API to assign or remove the role
5. **Wait:** Adds 1-second delay between each API call

---

## Error Handling

The method handles several error scenarios:

- **Role not found:** Returns `false` if the specified role name doesn't exist
- **API failure:** Returns `false` if Discord API request fails
- **Permission issues:** Returns `false` if bot lacks permissions

All errors are logged with warnings when logging is enabled.

---

## Discord API Endpoints Used

- **GET** `/api/v10/guilds/{guildId}/roles` - Fetch server roles
- **PUT** `/api/v10/guilds/{guildId}/members/{userId}/roles/{roleId}` - Assign role
- **DELETE** `/api/v10/guilds/{guildId}/members/{userId}/roles/{roleId}` - Remove role

---

## Requirements

### Bot Permissions

The bot token must have the following permissions:
- **Manage Roles** - Required to assign/remove roles
- **View Server Members** - Required to access member information

### Bot Token

Format: `Bot YOUR_BOT_TOKEN_HERE`

The method automatically adds "Bot " prefix in the Authorization header.

---

## Notes

- Uses Discord API v10
- Case-insensitive role name matching
- Automatic 1-second delays between API calls to respect rate limits
- UTF-8 encoding for all requests
- Returns boolean for easy success/failure checking
- Logs all operations when logging is enabled
- Uses NetHttp class for HTTP operations
