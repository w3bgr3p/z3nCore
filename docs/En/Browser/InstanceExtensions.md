# InstanceExtensions

Powerful extension methods for Instance and HtmlElement interaction with automatic retries, flexible element finding, JavaScript execution, and browser utilities.

## Element Finding & Getting

### GetHe()

```csharp
public static HtmlElement GetHe(this Instance instance, object obj, string method = "")
```

**Purpose**: Universal element finder supporting tuples, HtmlElements, and multiple search modes.

**Example**:
```csharp
// By ID (2-tuple)
HtmlElement btn = instance.GetHe(("submit-btn", "id"));

// By name
HtmlElement input = instance.GetHe(("email", "name"));

// By attribute (5-tuple)
HtmlElement div = instance.GetHe(("div", "class", "container", "text", 0));

// Get last element
HtmlElement last = instance.GetHe(("button", "class", "btn", "text", 0), "last");
```

**Breakdown**:
- `obj` - Can be HtmlElement, 2-tuple (value, method), or 5-tuple (tag, attr, pattern, mode, index)
- `method` - Optional: "id", "name", or "last" for getting last matching element
- 2-tuple format: (value, "id") or (value, "name")
- 5-tuple format: (tag, attribute, pattern, mode, position)
- Returns HtmlElement if found
- Throws exception if element not found or is void

---

### HeGet()

```csharp
public static string HeGet(this Instance instance, object obj, string method = "", int deadline = 10, string atr = "innertext", int delay = 1, bool thrw = true, bool thr0w = true, bool waitTillVoid = false)
```

**Purpose**: Waits for element to appear and gets its attribute with retry logic.

**Example**:
```csharp
// Get text when element appears
string text = instance.HeGet(("username", "id"), deadline: 15);

// Get href attribute
string link = instance.HeGet(("a", "class", "link", "text", 0), atr: "href");

// Wait until element disappears
instance.HeGet(("loading", "id"), waitTillVoid: true, deadline: 30);
```

**Breakdown**:
- `obj` - Element selector (see GetHe documentation)
- `method` - Element finding method
- `deadline` - Maximum wait time in seconds (default: 10)
- `atr` - Attribute to get (default: "innertext")
- `delay` - Delay in seconds after element found (default: 1)
- `thrw` - Throw exception on timeout (default: true)
- `thr0w` - Alternative throw parameter (overrides thrw)
- `waitTillVoid` - Wait until element disappears instead of appears
- Returns attribute value as string
- Returns null if timeout and thrw=false
- Throws ElementNotFoundException on timeout if thrw=true

---

### HeCatch()

```csharp
public static string HeCatch(this Instance instance, object obj, string method = "", int deadline = 10, string atr = "innertext", int delay = 1)
```

**Purpose**: Waits for element to NOT appear (opposite of HeGet) - useful for error checking.

**Example**:
```csharp
// Wait for error message to NOT appear (throws if appears)
instance.HeCatch(("div", "class", "error-message", "text", 0), deadline: 5);
// Returns null if element never appeared (success)
```

**Breakdown**:
- `obj` - Element selector to watch for
- `method` - Element finding method
- `deadline` - Maximum wait time in seconds (default: 10)
- `atr` - Attribute to check (default: "innertext")
- `delay` - Delay after check (default: 1)
- Returns null if element never appears (success)
- Throws exception if element appears (error detected)
- Useful for detecting error messages that shouldn't appear

---

## Element Interaction

### HeClick()

```csharp
public static void HeClick(this Instance instance, object obj, string method = "", int deadline = 10, double delay = 1, string comment = "", bool thrw = true, bool thr0w = true, int emu = 0)
```

**Purpose**: Waits for element and clicks it with retry logic and optional mouse emulation.

**Example**:
```csharp
// Simple click
instance.HeClick(("submit-btn", "id"));

// Click with mouse emulation
instance.HeClick(("button", "class", "submit", "text", 0), emu: 1);

// Keep clicking until element disappears
instance.HeClick(("popup-close", "id"), method: "clickOut");
```

**Breakdown**:
- `obj` - Element selector
- `method` - Finding method or "clickOut" (click until element disappears)
- `deadline` - Maximum wait time in seconds (default: 10)
- `delay` - Delay before click in seconds (default: 1)
- `comment` - Description for error messages
- `thrw` - Throw exception on timeout (default: true)
- `thr0w` - Alternative throw parameter
- `emu` - Mouse emulation: 1 (enable), -1 (disable), 0 (no change)
- Retries every 500ms until element found or timeout
- Restores original mouse emulation setting after click

---

### HeSet()

```csharp
public static void HeSet(this Instance instance, object obj, string value, string method = "id", int deadline = 10, double delay = 1, string comment = "", bool thrw = true, bool thr0w = true)
```

**Purpose**: Waits for input element and sets its value with retry logic.

**Example**:
```csharp
// Set input value
instance.HeSet(("email", "id"), "[email protected]");

// Set with longer deadline
instance.HeSet(("password", "name"), "MyP@ssw0rd", deadline: 20);
```

**Breakdown**:
- `obj` - Element selector
- `value` - Text to set in element
- `method` - Finding method (default: "id")
- `deadline` - Maximum wait time in seconds (default: 10)
- `delay` - Delay before setting value (default: 1)
- `comment` - Description for error messages
- `thrw` - Throw exception on timeout (default: true)
- `thr0w` - Alternative throw parameter
- Uses "Full" emulation mode for natural typing
- Retries every 500ms until success or timeout

---

### HeDrop()

```csharp
public static void HeDrop(this Instance instance, object obj, string method = "", int deadline = 10, bool thrw = true)
```

**Purpose**: Removes element from DOM by finding it and calling RemoveChild on parent.

**Example**:
```csharp
// Remove annoying popup
instance.HeDrop(("cookie-banner", "id"));

// Remove without throwing on failure
instance.HeDrop(("ad-overlay", "class"), thrw: false);
```

**Breakdown**:
- `obj` - Element selector
- `method` - Finding method
- `deadline` - Maximum wait time in seconds (default: 10)
- `thrw` - Throw exception on timeout (default: true)
- Finds element's parent and removes child
- Retries every 500ms until success or timeout

---

## JavaScript Methods

### JsClick()

```csharp
public static string JsClick(this Instance instance, string selector, double delayX = 1.0)
```

**Purpose**: Clicks element using JavaScript (works with Shadow DOM and hidden elements).

**Example**:
```csharp
// Click by CSS selector
instance.JsClick("#submit-button");

// Click in shadow DOM
instance.JsClick("my-component >>> .inner-button");
```

**Breakdown**:
- `selector` - CSS selector for element
- `delayX` - Delay multiplier before click (default: 1.0)
- Searches in regular DOM and Shadow DOM
- Scrolls element into view before clicking
- Dispatches proper MouseEvent with bubbling
- Returns "Click successful" on success
- Returns "Error: ..." message on failure

---

### JsSet()

```csharp
public static string JsSet(this Instance instance, string selector, string value, double delayX = 1.0)
```

**Purpose**: Sets input value using JavaScript with proper event triggering.

**Example**:
```csharp
// Set input value
instance.JsSet("#email", "[email protected]");

// Set textarea
instance.JsSet("textarea.description", "My description\nLine 2");
```

**Breakdown**:
- `selector` - CSS selector for input element
- `value` - Text value to set
- `delayX` - Delay multiplier before setting (default: 1.0)
- Scrolls element into view
- Triggers click, focus, focusin events
- Uses execCommand for natural input
- Triggers input and change events
- Returns "Value set successfully" on success
- Returns "Error: ..." message on failure

---

### JsPost()

```csharp
public static string JsPost(this Instance instance, string script, int delay = 0)
```

**Purpose**: Executes arbitrary JavaScript code on the page.

**Example**:
```csharp
// Get page title
string title = instance.JsPost("document.title");

// Scroll page
instance.JsPost("window.scrollTo(0, document.body.scrollHeight)");
```

**Breakdown**:
- `script` - JavaScript code to execute
- `delay` - Delay in seconds before execution (default: 0)
- Automatically converts double quotes to single quotes
- Returns JavaScript execution result as string
- Returns error message on exception

---

## Browser Utilities

### ClearShit()

```csharp
public static void ClearShit(this Instance instance, string domain)
```

**Purpose**: Clears cache and cookies for specified domain and resets browser.

**Example**:
```csharp
instance.ClearShit("google.com");
```

**Breakdown**:
- `domain` - Domain to clear data for
- Closes all tabs
- Clears cache for domain
- Clears cookies for domain
- Navigates to about:blank

---

### CloseExtraTabs()

```csharp
public static void CloseExtraTabs(this Instance instance, bool blank = false, int tabToKeep = 1)
```

**Purpose**: Closes all tabs except the first one (or specified tab index).

**Example**:
```csharp
instance.CloseExtraTabs(); // Keep first tab, close others
instance.CloseExtraTabs(blank: true); // Also navigate to blank page
```

**Breakdown**:
- `blank` - Navigate remaining tab to about:blank (default: false)
- `tabToKeep` - Index of tab to keep (default: 1, first tab)
- Closes all tabs with index >= tabToKeep
- Adds delays between closures for stability

---

### Go()

```csharp
public static void Go(this Instance instance, string url, bool strict = false)
```

**Purpose**: Navigates to URL only if not already there (avoids unnecessary reloads).

**Example**:
```csharp
instance.Go("https://example.com"); // Navigate if different domain
instance.Go("https://example.com/page", strict: true); // Navigate if exact URL differs
```

**Breakdown**:
- `url` - Target URL
- `strict` - true: exact URL match, false: contains check (default: false)
- Only navigates if current URL doesn't match
- Saves time by avoiding unnecessary page loads

---

### F5()

```csharp
public static void F5(this Instance instance)
```

**Purpose**: Reloads current page (force refresh).

**Example**:
```csharp
instance.F5(); // Force reload page
```

**Breakdown**:
- No parameters
- Uses JavaScript location.reload(true)
- Forces full page reload bypassing cache

---

### ScrollDown()

```csharp
public static void ScrollDown(this Instance instance, int y = 420)
```

**Purpose**: Scrolls page down by specified amount.

**Example**:
```csharp
instance.ScrollDown(); // Scroll down 420px
instance.ScrollDown(1000); // Scroll down 1000px
```

**Breakdown**:
- `y` - Pixels to scroll down (default: 420)
- Temporarily enables mouse emulation
- Restores original emulation setting after scroll

---

### CtrlV()

```csharp
public static void CtrlV(this Instance instance, string ToPaste)
```

**Purpose**: Pastes text using Ctrl+V keyboard shortcut (thread-safe clipboard operation).

**Example**:
```csharp
instance.CtrlV("Text to paste");
```

**Breakdown**:
- `ToPaste` - Text to paste
- Thread-safe clipboard manipulation
- Preserves original clipboard content
- Simulates Ctrl+V key press
- Restores clipboard after operation

---

## Cloudflare (Fallback class)

### ClFlv2()

```csharp
public static void ClFlv2(this Instance instance)
```

**Purpose**: Solves Cloudflare v2 challenges (alias for CFSolve).

**Example**:
```csharp
instance.ClFlv2();
```

**Breakdown**:
- Wrapper for CFSolve() method
- See Captcha.CFSolve() documentation

---

### ClFl()

```csharp
public static string ClFl(this Instance instance, int deadline = 60, bool strict = false)
```

**Purpose**: Gets Cloudflare clearance token (alias for CFToken).

**Example**:
```csharp
string token = instance.ClFl(deadline: 120);
```

**Breakdown**:
- Wrapper for CFToken() method
- See Captcha.CFToken() documentation
