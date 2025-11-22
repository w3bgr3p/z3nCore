# AntiCaptcha Class Documentation

## Overview
The `AntiCaptcha` class provides integration with the Anti-Captcha service for solving image-based captchas using their API.

---

## Constructor

### `AntiCaptcha(string apiKey)`

**Purpose:** Initializes the Anti-Captcha client with API credentials.

**Example:**
```csharp
var solver = new AntiCaptcha("your-api-key-here");
string result = solver.SolveCaptcha("path/to/captcha.png");
solver.Dispose();
```

**Breakdown:**
```csharp
var solver = new AntiCaptcha(
    "your-api-key"  // string - Anti-Captcha API key
);
// Note: Implements IDisposable - use 'using' statement or call Dispose()
```

---

## Public Methods

### `SolveCaptcha(string imagePath, int numeric = 0, int minLength = 0, int maxLength = 0, bool phrase = false, bool caseSensitive = true, bool math = false)`

**Purpose:** Solves a captcha from an image file path.

**Example:**
```csharp
using (var solver = new AntiCaptcha(apiKey))
{
    string result = solver.SolveCaptcha(
        "C:/captchas/image.png",
        numeric: 1,      // Numbers only
        minLength: 6,
        maxLength: 6
    );
    Console.WriteLine($"Captcha solved: {result}");
}
```

**Breakdown:**
```csharp
string captchaText = solver.SolveCaptcha(
    "path/to/image.png",  // string - full path to captcha image file
    0,                     // int - 0=any, 1=numbers only, 2=letters only
    4,                     // int - minimum text length (0=no limit)
    8,                     // int - maximum text length (0=no limit)
    false,                 // bool - is text contains multiple words
    true,                  // bool - is case sensitive
    false                  // bool - is math calculation required
);
// Returns: string - solved captcha text
// Throws: Exception - if API error or task creation/retrieval fails
// Note: Waits up to 3 minutes for solution (60 attempts × 3s delay)
```

---

### `SolveCaptchaFromBase64(string base64Image, int numeric = 0, int minLength = 0, int maxLength = 0, bool phrase = false, bool caseSensitive = true, bool math = false)`

**Purpose:** Solves a captcha from a base64-encoded image string (synchronous).

**Example:**
```csharp
using (var solver = new AntiCaptcha(apiKey))
{
    string base64 = Convert.ToBase64String(File.ReadAllBytes("captcha.png"));
    string result = solver.SolveCaptchaFromBase64(
        base64,
        numeric: 1,
        minLength: 6
    );
}
```

**Breakdown:**
```csharp
string captchaText = solver.SolveCaptchaFromBase64(
    "iVBORw0KGgoAAAANS...",  // string - base64 encoded image data
    1,                        // int - 0=any, 1=numbers only, 2=letters only
    0,                        // int - minimum length
    0,                        // int - maximum length
    false,                    // bool - multiple words
    true,                     // bool - case sensitive
    false                     // bool - math calculation
);
// Returns: string - solved captcha text
// Throws: Exception - if API error or timeout
```

---

### `SolveCaptchaFromBase64Async(string base64Image, int numeric = 0, int minLength = 0, int maxLength = 0, bool phrase = false, bool caseSensitive = true, bool math = false)`

**Purpose:** Solves a captcha from a base64-encoded image string (asynchronous).

**Example:**
```csharp
using (var solver = new AntiCaptcha(apiKey))
{
    string base64 = Convert.ToBase64String(imageBytes);
    string result = await solver.SolveCaptchaFromBase64Async(
        base64,
        numeric: 0,
        caseSensitive: false
    );
}
```

**Breakdown:**
```csharp
string captchaText = await solver.SolveCaptchaFromBase64Async(
    base64String,    // string - base64 encoded image
    0,               // int - character type filter
    0,               // int - min length
    0,               // int - max length
    false,           // bool - phrase mode
    true,            // bool - case sensitive
    false            // bool - math mode
);
// Returns: Task<string> - solved captcha text
// Throws: Exception - if task creation or result retrieval fails
```

---

### `SolveCaptchaAsync(string imagePath, int numeric = 0, int minLength = 0, int maxLength = 0, bool phrase = false, bool caseSensitive = true, bool math = false)`

**Purpose:** Solves a captcha from an image file path (asynchronous).

**Example:**
```csharp
using (var solver = new AntiCaptcha(apiKey))
{
    string result = await solver.SolveCaptchaAsync(
        "captcha.png",
        numeric: 1
    );
}
```

**Breakdown:**
```csharp
string captchaText = await solver.SolveCaptchaAsync(
    "path/to/captcha.png",  // string - image file path
    1,                       // int - numeric mode
    6,                       // int - min length
    6,                       // int - max length
    false,                   // bool - phrase mode
    true,                    // bool - case sensitive
    false                    // bool - math mode
);
// Returns: Task<string> - solved captcha text
// Note: Reads file, converts to base64, then solves
```

---

### `Dispose()`

**Purpose:** Releases resources used by the HttpClient.

**Example:**
```csharp
var solver = new AntiCaptcha(apiKey);
try
{
    string result = solver.SolveCaptcha("captcha.png");
}
finally
{
    solver.Dispose();
}

// Better: use 'using' statement
using (var solver = new AntiCaptcha(apiKey))
{
    string result = solver.SolveCaptcha("captcha.png");
}
```

**Breakdown:**
```csharp
solver.Dispose();
// Releases HttpClient resources
// Automatically called when using 'using' statement
```

---

## Extension Methods

The file also includes extension methods in the `CaptchaExtensions` class:

### `SolveHeWithAntiCaptcha(this HtmlElement he, IZennoPosterProjectModel project)`

**Purpose:** Solves a captcha from an HtmlElement by converting it to bitmap.

**Example:**
```csharp
var captchaElement = instance.ActiveTab.FindElementByTag("img", 0);
string solution = captchaElement.SolveHeWithAntiCaptcha(project);
```

---

### `SolveCaptchaFromUrl(IZennoPosterProjectModel project, string url, string proxy = "+")`

**Purpose:** Solves a captcha by fetching it from a URL (supports SVG).

**Example:**
```csharp
string solution = CaptchaExtensions.SolveCaptchaFromUrl(
    project,
    "https://example.com/captcha",
    proxy: "+"
);
```

---

## Notes

- API key is stored in database (_api table) with id='anticaptcha'
- Timeout for solving: 180 seconds (60 attempts × 3s)
- Supports all standard image formats through base64 encoding
- HttpClient timeout set to 5 minutes
- All methods use Anti-Captcha's ImageToTextTask type
