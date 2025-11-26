# Galxe Class Documentation

## Overview
The `Galxe` class provides integration with Galxe (formerly Project Galaxy) platform for parsing quest tasks and retrieving user information using GraphQL API.

---

## Constructor

### `Galxe(IZennoPosterProjectModel project, Instance instance, bool log = false)`

**Purpose:** Initializes the Galxe API client.

**Example:**
```csharp
var galxe = new Galxe(project, instance, log: true);
List<HtmlElement> tasks = galxe.ParseTasks("tasksUnComplete");
```

**Breakdown:**
```csharp
var galxe = new Galxe(
    project,   // IZennoPosterProjectModel - project instance
    instance,  // Instance - ZennoPoster instance for DOM access
    true       // bool - enable logging
);
```

---

## Public Methods

### `ParseTasks(string type = "tasksUnComplete", bool log = false)`

**Purpose:** Parses and categorizes quest tasks from the current page.

**Example:**
```csharp
var galxe = new Galxe(project, instance);

// Get uncompleted tasks
List<HtmlElement> uncompletedTasks = galxe.ParseTasks("tasksUnComplete");

// Get completed tasks
List<HtmlElement> completedTasks = galxe.ParseTasks("tasksComplete");

// Get uncompleted requirements
List<HtmlElement> requirements = galxe.ParseTasks("reqUnComplete");

foreach (HtmlElement task in uncompletedTasks)
{
    string taskInfo = task.InnerText.Replace("\n", " ");
    Console.WriteLine($"Task: {taskInfo}");
}
```

**Breakdown:**
```csharp
List<HtmlElement> tasks = galxe.ParseTasks(
    "tasksUnComplete",  // string - task type to retrieve
    false               // bool - enable additional logging
);
// Returns: List<HtmlElement> - HTML elements matching the specified type
// Types: tasksComplete, tasksUnComplete, reqComplete, reqUnComplete,
//        refComplete, refUnComplete
```

---

### Task Types

| Type | Description |
|------|-------------|
| `tasksComplete` | Completed "Get" tasks |
| `tasksUnComplete` | Uncompleted "Get" tasks |
| `reqComplete` | Completed requirements |
| `reqUnComplete` | Uncompleted requirements |
| `refComplete` | Completed referral tasks |
| `refUnComplete` | Uncompleted referral tasks |

---

### `BasicUserInfo(string token, string address)`

**Purpose:** Retrieves comprehensive user information from Galxe API.

**Example:**
```csharp
var galxe = new Galxe(project, instance);
string userInfo = galxe.BasicUserInfo(
    "Bearer eyJhbGciOiJIUzI1NiIs...",
    "0x742d35Cc6634C0532925a3b8D45C0532925aAB1"
);

project.Json.FromString(userInfo);
string username = project.Json.data.addressInfo.username;
string level = project.Json.data.addressInfo.userLevel.level.name;
```

**Breakdown:**
```csharp
string userData = galxe.BasicUserInfo(
    "Bearer token...",  // string - authorization token
    "0x742d35Cc..."     // string - EVM wallet address
);
// Returns: string - JSON response with user data
// Response includes: username, level, exp, gold, social accounts,
//                    wallet addresses, participation stats
// Throws: Exception - if token is empty or API request fails
```

---

### User Info Response Structure

```json
{
  "data": {
    "addressInfo": {
      "id": "user_id",
      "username": "username",
      "address": "0x...",
      "userLevel": {
        "level": {"name": "Explorer", "logo": "..."},
        "exp": 1500,
        "gold": 250
      },
      "twitterUserName": "twitter_handle",
      "discordUserName": "discord#1234",
      "participatedCampaigns": {"totalCount": 42}
    }
  }
}
```

---

### `GetLoyaltyPoints(string alias, string address)`

**Purpose:** Retrieves loyalty points and rank for a specific space.

**Example:**
```csharp
var galxe = new Galxe(project, instance);
string loyaltyData = galxe.GetLoyaltyPoints(
    "arbitrum",
    "0x742d35Cc6634C0532925a3b8D45C0532925aAB1"
);

project.Json.FromString(loyaltyData);
string points = project.Json.data.space.addressLoyaltyPoints.points;
string rank = project.Json.data.space.addressLoyaltyPoints.rank;

Console.WriteLine($"Points: {points}, Rank: {rank}");
```

**Breakdown:**
```csharp
string loyaltyInfo = galxe.GetLoyaltyPoints(
    "arbitrum",      // string - space alias (e.g., "arbitrum", "polygon")
    "0x742d35Cc..."  // string - user wallet address (lowercase)
);
// Returns: string - JSON response with loyalty points and rank
// Note: Address is automatically converted to lowercase
```

---

## GraphQL Queries

The class uses these GraphQL operations:

### BasicUserInfo Query
- Retrieves: User profile, level, exp, social accounts, wallet addresses
- Requires: Authorization token
- Endpoint: `https://graphigo.prd.galaxy.eco/query`

### SpaceAccessQuery
- Retrieves: Loyalty points and rank for a space
- Public query (no auth required)
- Endpoint: `https://graphigo.prd.galaxy.eco/query`

---

## Task Parsing Logic

The `ParseTasks` method identifies tasks using DOM structure:

1. **Find Sections:** Locates main section containers with class `mb-20`
2. **Identify Categories:** Determines if section is Requirements/Tasks/Referral
3. **Check Completion:** Uses SVG path to identify completed tasks
4. **Categorize:** Sorts into completed/uncompleted lists
5. **Return:** Returns requested category

**Completion Indicator:**
```csharp
// SVG path for completed tasks
string dDone = "M10 19a9 9 0 1 0 0-18 9 9 0 0 0 0 18m3.924-10.576...";
```

---

## Notes

- GraphQL API endpoint: `https://graphigo.prd.galaxy.eco/query`
- Authorization header format: `Authorization: {token}` (already includes "Bearer")
- All GraphQL queries use POST method
- Responses are automatically parsed to `project.Json`
- Task parsing requires page to be loaded in instance
- Address parameter is case-sensitive for some queries (use lowercase)
- User-Agent: "Galaxy/v1"
- Supports multiple wallet types: EVM, Solana, Aptos, Starknet, Bitcoin, Sui, XRPL, TON
