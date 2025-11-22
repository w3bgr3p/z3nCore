# X (Twitter) Class Documentation

## Class: X

### Constructor

#### X(IZennoPosterProjectModel project, Instance instance, bool log = false)

**Purpose**: Initializes a new X (Twitter) automation instance with comprehensive authentication and action capabilities.

**Example**:
```csharp
var twitter = new X(project, instance, log: true);
```

**Breakdown**:
```csharp
// Parameters:
// - project: IZennoPosterProjectModel - Project model for database operations
// - instance: Instance - Browser instance for automation
// - log: bool - Enable detailed logging (default: false)
// Returns: X instance
// Note: Automatically loads credentials from "_twitter" table
// Initializes Sleeper for random delays between actions
```

---

## API Methods

### UserByScreenName()

**Purpose**: Extracts user data from X API traffic by screen name.

**Example**:
```csharp
Dictionary<string, string> userData = twitter.UserByScreenName();
string username = userData["user"];
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: Dictionary<string, string> - User data including screen name
// Note: Captures network traffic containing "UserByScreenName" endpoint
// Automatically refreshes page if no traffic found on first attempt
// Throws exception if no matching traffic found after retry
```

### Settings()

**Purpose**: Retrieves account settings data from X API traffic.

**Example**:
```csharp
Dictionary<string, string> settings = twitter.Settings();
string screenName = settings["screen_name"];
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: Dictionary<string, string> - Account settings data
// Note: Captures traffic from "account/settings.json" endpoint
// Refreshes page if needed, throws exception if not found
```

### UserNameFromSetting(bool validate = false)

**Purpose**: Gets current username from account settings, optionally validating against expected login.

**Example**:
```csharp
string username = twitter.UserNameFromSetting(validate: true);
```

**Breakdown**:
```csharp
// Parameters:
// - validate: bool - Verify username matches expected login (default: false)
// Returns: string - Current screen name
// Throws: Exception if validation enabled and wrong account detected
// Note: Sets project variable "status" to "WrongAccount" on mismatch
```

### GenerateJson(string purpouse = "tweet")

**Purpose**: Generates AI-powered tweet or thread content based on random news article.

**Example**:
```csharp
string tweetJson = twitter.GenerateJson("tweet");
project.ToJson(tweetJson);
string tweet = project.Json.statement;
```

**Breakdown**:
```csharp
// Parameters:
// - purpouse: string - Content type ("tweet", "thread", "opinionThread") (default: "tweet")
// Returns: string - JSON formatted content
// Note: Reads random article from .data/news folder
// Uses AI to generate personalized response based on bio
// Tweet format: max 280 characters, single statement
// Thread format: multiple statements for summary and opinion
// OpinionThread: separate summary and opinion statements
```

---

## Authentication Methods

### Load(bool log = false)

**Purpose**: Performs complete X authentication with automatic token/credential handling.

**Example**:
```csharp
string status = twitter.Load();
if (status == "ok") {
    project.SendInfoToLog("Twitter authenticated successfully");
}
```

**Breakdown**:
```csharp
// Parameters:
// - log: bool - Enable logging (default: false)
// Returns: string - Authentication status ("ok", "restricted", "suspended", "emailCapcha", etc.)
// Note: Attempts token authentication first, falls back to credentials
// Automatically extracts and saves new token on successful login
// Clears cookies and retries if mixed account detected
// Returns error status for: restricted, suspended, wrong credentials
```

### LoginWithToken(string token = null)

**Purpose**: Authenticates using auth_token cookie injection.

**Example**:
```csharp
twitter.LoginWithToken();
```

**Breakdown**:
```csharp
// Parameters:
// - token: string - Auth token (uses loaded token if null)
// Returns: void
// Note: Injects token as cookie via JavaScript
// Automatically reloads page after injection
```

### LoginWithCredentials()

**Purpose**: Performs login using username and password with 2FA support.

**Example**:
```csharp
string result = twitter.LoginWithCredentials();
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: string - Login result ("ok" or error message)
// Note: Handles username, password, and OTP inputs
// Detects and returns errors: "NotFound", "WrongPass", "Suspended", "SuspiciousLogin"
// Automatically saves token on successful login
```

### TokenGet()

**Purpose**: Extracts auth_token from browser cookies and saves to database.

**Example**:
```csharp
string token = twitter.TokenGet();
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: string - Extracted auth token
// Note: Parses cookies JSON to find auth_token
// Updates "_twitter" table with token
```

### Auth()

**Purpose**: Handles OAuth authorization flow for third-party apps.

**Example**:
```csharp
twitter.Auth();
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: void
// Throws: Exception for errors (NotFound, Suspended, WrongPass, InvalidRequestToken)
// Note: Handles both OAuth v1 and standard login flows
// Automatically inputs credentials, OTP, and confirms authorization
```

---

## UI Action Methods

### SendSingleTweet(string tweet, string accountToMention = null)

**Purpose**: Posts a single tweet, optionally from another account's profile.

**Example**:
```csharp
twitter.SendSingleTweet("Hello Twitter!", accountToMention: "elonmusk");
```

**Breakdown**:
```csharp
// Parameters:
// - tweet: string - Tweet text content
// - accountToMention: string - Navigate to this profile first (optional)
// Returns: void
// Throws: Exception if toast error message appears
// Note: Uses clipboard (CtrlV) to input text
```

### SendThread(List<string> tweets, string accountToMention = null)

**Purpose**: Posts a Twitter thread with multiple connected tweets.

**Example**:
```csharp
List<string> thread = new List<string> { "Tweet 1", "Tweet 2", "Tweet 3" };
twitter.SendThread(thread);
```

**Breakdown**:
```csharp
// Parameters:
// - tweets: List<string> - List of tweet texts (first is main tweet)
// - accountToMention: string - Navigate to profile first (optional)
// Returns: void
// Note: First item becomes main tweet, rest are added replies
// Falls back to SendSingleTweet if only one tweet provided
// Uses JavaScript interactions for adding thread replies
```

### Follow()

**Purpose**: Follows the account on current profile page.

**Example**:
```csharp
twitter.GoToProfile("elonmusk");
twitter.Follow();
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: void
// Note: Checks if already following before clicking
// Returns immediately if unfollow button detected
```

### RandomLike(string targetAccount = null)

**Purpose**: Likes a random post from specified account or current page.

**Example**:
```csharp
twitter.RandomLike(targetAccount: "elonmusk");
```

**Breakdown**:
```csharp
// Parameters:
// - targetAccount: string - Account whose posts to like (optional)
// Returns: void
// Note: Filters posts to only like from target account
// Scrolls down if no posts found, retries until post found
// Uses random selection from available posts
```

### RandomRetweet(string targetAccount = null)

**Purpose**: Retweets a random post from specified account.

**Example**:
```csharp
twitter.RandomRetweet(targetAccount: "elonmusk");
```

**Breakdown**:
```csharp
// Parameters:
// - targetAccount: string - Account whose posts to retweet (optional)
// Returns: void
// Note: Skips already retweeted posts
// Confirms retweet action in dropdown menu
// Scrolls and retries if no eligible posts found
```

### GetCurrentEmail()

**Purpose**: Retrieves email address associated with X account.

**Example**:
```csharp
string email = twitter.GetCurrentEmail();
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: string - Current email in lowercase
// Note: Navigates to settings/email page
// May require password confirmation
```

---

## Intent Link Methods

### FollowByLink(string screen_name)

**Purpose**: Follows account using X intent link.

**Example**:
```csharp
twitter.FollowByLink("elonmusk");
```

**Breakdown**:
```csharp
// Parameters:
// - screen_name: string - Target account username
// Returns: void
// Note: Opens in new tab, confirms action, closes tab
```

### LikeByLink(string tweet_id)

**Purpose**: Likes tweet using intent link.

**Example**:
```csharp
twitter.LikeByLink("1234567890");
```

**Breakdown**:
```csharp
// Parameters:
// - tweet_id: string - Tweet ID to like
// Returns: void
// Note: Opens in new tab, auto-confirms, closes tab
```

### RetweetByLink(string tweet_id)

**Purpose**: Retweets using intent link.

**Example**:
```csharp
twitter.RetweetByLink("1234567890");
```

**Breakdown**:
```csharp
// Parameters:
// - tweet_id: string - Tweet ID to retweet
// Returns: void
```

### ReplyByLink(string tweet_id, string text)

**Purpose**: Replies to tweet using intent link.

**Example**:
```csharp
twitter.ReplyByLink("1234567890", "Great post!");
```

**Breakdown**:
```csharp
// Parameters:
// - tweet_id: string - Tweet ID to reply to
// - text: string - Reply text content
// Returns: void
// Note: URL-encodes reply text automatically
```

### QuoteByLink(string tweeturl)

**Purpose**: Quote tweets using intent link.

**Example**:
```csharp
twitter.QuoteByLink("https://x.com/user/status/1234567890");
```

**Breakdown**:
```csharp
// Parameters:
// - tweeturl: string - Full tweet URL to quote
// Returns: void
```

---

## Helper Methods

### GoToProfile(string profile = null)

**Purpose**: Navigates to user profile page.

**Example**:
```csharp
twitter.GoToProfile("elonmusk");
```

**Breakdown**:
```csharp
// Parameters:
// - profile: string - Username (uses own profile if null)
// Returns: void
// Note: Skips navigation if already on target profile
```

### CheckCurrent()

**Purpose**: Verifies current logged-in account matches expected username.

**Example**:
```csharp
bool isCorrect = twitter.CheckCurrent();
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: bool - True if correct account, false otherwise
// Note: Clears cookies if wrong account detected
```

### SkipDefaultButtons()

**Purpose**: Dismisses common popup buttons (cookies, migration, tips).

**Example**:
```csharp
twitter.SkipDefaultButtons();
```

**Breakdown**:
```csharp
// Parameters: None
// Returns: void
// Note: Attempts to click: Accept cookies, migration bar, "Got it"
// Uses zero deadline (no wait if not found)
```
