# AI Class Documentation

## Overview
The `AI` class provides integration with AI services (Perplexity and AIIO) for generating text, optimizing code, and creating content.

---

## Constructor

### `AI(IZennoPosterProjectModel project, string provider, string model = null, bool log = false)`

**Purpose:** Initializes the AI client with specified provider and model settings.

**Example:**
```csharp
// Initialize with Perplexity provider
var ai = new AI(project, "perplexity", log: true);

// Initialize with specific model
var ai = new AI(project, "aiio", "deepseek-ai/DeepSeek-R1-0528", log: true);
```

**Breakdown:**
```csharp
var ai = new AI(
    project,      // IZennoPosterProjectModel - project instance
    "perplexity", // string - provider name ("perplexity" or "aiio")
    null,         // string - optional: specific model name
    true          // bool - enable logging
);
```

---

## Public Methods

### `Query(string systemContent, string userContent, string aiModel = "rnd", bool log = false, double temperature_ = 0.8, double top_p_ = 0.9, double top_k_ = 0, int presence_penalty_ = 0, int frequency_penalty_ = 1)`

**Purpose:** Sends a query to the AI service and returns the response.

**Example:**
```csharp
var ai = new AI(project, "perplexity");
string response = ai.Query(
    "You are a helpful assistant",
    "Explain blockchain in simple terms"
);
```

**Breakdown:**
```csharp
string result = ai.Query(
    "You are a helpful assistant",  // string - system prompt defining AI behavior
    "Explain blockchain",            // string - user question/request
    "rnd",                          // string - AI model ("rnd" for random selection)
    false,                          // bool - enable logging
    0.8,                            // double - temperature (creativity: 0.0-1.0)
    0.9,                            // double - top_p (nucleus sampling)
    0,                              // double - top_k (token selection diversity)
    0,                              // int - presence_penalty (topic diversity)
    1                               // int - frequency_penalty (repetition control)
);
// Returns: string - AI generated response
// Throws: Exception - if API request fails or response parsing fails
```

---

### `GenerateTweet(string content, string bio = "", bool log = false)`

**Purpose:** Generates a tweet-length social media post based on content and optional bio.

**Example:**
```csharp
var ai = new AI(project, "aiio");
string tweet = ai.GenerateTweet(
    "Create a post about Web3 development",
    "Blockchain developer and crypto enthusiast"
);
```

**Breakdown:**
```csharp
string tweet = ai.GenerateTweet(
    "Create a post about Web3",  // string - content topic for the tweet
    "Blockchain developer",      // string - optional bio to match persona
    true                         // bool - enable logging
);
// Returns: string - generated tweet (max 220 characters)
// Note: Automatically regenerates if tweet exceeds 220 characters
```

---

### `OptimizeCode(string content, bool log = false)`

**Purpose:** Optimizes code using AI, returning only the optimized code without explanations.

**Example:**
```csharp
var ai = new AI(project, "perplexity");
string code = "function test() { var x = 1; var y = 2; return x + y; }";
string optimized = ai.OptimizeCode(code);
```

**Breakdown:**
```csharp
string optimizedCode = ai.OptimizeCode(
    "function test() { ... }",  // string - code to optimize
    true                         // bool - enable logging
);
// Returns: string - optimized code (plain text, no comments)
// Note: Designed for Web3/blockchain code optimization
```

---

### `GoogleAppeal(bool log = false)`

**Purpose:** Generates an appeal message for Google account suspension/ban situations.

**Example:**
```csharp
var ai = new AI(project, "aiio");
string appeal = ai.GoogleAppeal();
// Use the generated appeal to submit to Google support
```

**Breakdown:**
```csharp
string appealMessage = ai.GoogleAppeal(
    false  // bool - enable logging
);
// Returns: string - generated appeal message (~200 characters)
// Note: Creates a humble, non-technical appeal explaining the situation
```

---

## Notes

- The API key is fetched from the database using `project.SqlGet()` based on the provider
- For "rnd" model selection, a random model is chosen from a predefined list
- All methods use JSON for request/response handling
- Responses are logged when log parameter is enabled
