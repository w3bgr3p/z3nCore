# Traffic

Modern HTTP traffic monitoring and analysis with automatic caching and convenient data extraction.

## Class: Traffic

### Constructor

```csharp
public Traffic(IZennoPosterProjectModel project, Instance instance, bool log = false)
```

**Purpose**: Creates traffic monitor with automatic caching for analyzing HTTP requests and responses.

**Example**:
```csharp
var traffic = new Traffic(project, instance, log: true);
```

**Breakdown**:
- `project` - ZennoPoster project model
- `instance` - Browser instance to monitor
- `log` - Enable detailed logging (default: false)
- Automatically enables `instance.UseTrafficMonitoring = true`

---

## Finding Traffic Elements

### FindTrafficElement()

```csharp
public TrafficElement FindTrafficElement(string url, bool strict = false, int timeoutSeconds = 15, int retryDelaySeconds = 1, bool reload = false)
```

**Purpose**: Finds first HTTP traffic element matching URL with automatic waiting and retries.

**Example**:
```csharp
var traffic = new Traffic(project, instance);

// Find by URL substring
var element = traffic.FindTrafficElement("api/user");

// Find exact URL
var element = traffic.FindTrafficElement("https://api.example.com/v1/data", strict: true);

// Reload page and search
var element = traffic.FindTrafficElement("api/token", reload: true, timeoutSeconds: 30);
```

**Breakdown**:
- `url` - URL or substring to search for
- `strict` - true: exact match, false: contains substring (default: false)
- `timeoutSeconds` - Maximum wait time (default: 15)
- `retryDelaySeconds` - Delay between retries (default: 1)
- `reload` - Reload page before searching (default: false)
- Returns TrafficElement with full request/response data
- Throws TimeoutException if not found within timeout

---

### FindAllTrafficElements()

```csharp
public List<TrafficElement> FindAllTrafficElements(string url, bool strict = false)
```

**Purpose**: Finds all HTTP traffic elements matching URL (no waiting, uses current cache).

**Example**:
```csharp
var traffic = new Traffic(project, instance);
var elements = traffic.FindAllTrafficElements("api/analytics");

foreach (var element in elements)
{
    project.log($"Request to: {element.Url}");
}
```

**Breakdown**:
- `url` - URL or substring to search for
- `strict` - true: exact match, false: contains substring (default: false)
- Returns list of all matching TrafficElement objects
- Works with current cache (no waiting)
- Returns empty list if no matches found

---

### GetAllTraffic()

```csharp
public List<TrafficElement> GetAllTraffic()
```

**Purpose**: Gets complete HTTP traffic snapshot (all requests and responses).

**Example**:
```csharp
var traffic = new Traffic(project, instance);
var allRequests = traffic.GetAllTraffic();

project.log($"Total HTTP requests: {allRequests.Count}");
```

**Breakdown**:
- No parameters
- Returns list of all TrafficElement objects in cache
- Automatically refreshes cache if expired
- Excludes OPTIONS requests

---

## Quick Data Extraction

### GetResponseBody()

```csharp
public string GetResponseBody(string url, bool strict = false, int timeoutSeconds = 15)
```

**Purpose**: Quick method to get response body from HTTP traffic.

**Example**:
```csharp
var traffic = new Traffic(project, instance);
string json = traffic.GetResponseBody("api/user/profile");
project.Json.FromString(json);
```

**Breakdown**:
- `url` - URL to search for
- `strict` - Exact URL match (default: false)
- `timeoutSeconds` - Maximum wait time (default: 15)
- Returns response body as string
- Waits for traffic element if not immediately found
- Throws TimeoutException if not found

---

### GetRequestBody()

```csharp
public string GetRequestBody(string url, bool strict = false, int timeoutSeconds = 15)
```

**Purpose**: Quick method to get request body from HTTP traffic.

**Example**:
```csharp
var traffic = new Traffic(project, instance);
string requestData = traffic.GetRequestBody("api/submit");
project.log($"Posted data: {requestData}");
```

**Breakdown**:
- `url` - URL to search for
- `strict` - Exact URL match (default: false)
- `timeoutSeconds` - Maximum wait time (default: 15)
- Returns request body as string
- Useful for seeing what data was sent to server

---

### GetRequestHeader()

```csharp
public string GetRequestHeader(string url, string headerName, bool strict = false, int timeoutSeconds = 15)
```

**Purpose**: Gets specific header from HTTP request.

**Example**:
```csharp
var traffic = new Traffic(project, instance);
string auth = traffic.GetRequestHeader("api/protected", "Authorization");
// Returns: "Bearer eyJhbGc..."

string userAgent = traffic.GetRequestHeader("api/data", "user-agent");
```

**Breakdown**:
- `url` - URL to search for
- `headerName` - Header name to extract (case-insensitive)
- `strict` - Exact URL match (default: false)
- `timeoutSeconds` - Maximum wait time (default: 15)
- Returns header value as string
- Returns null if header not found

---

### GetResponseHeader()

```csharp
public string GetResponseHeader(string url, string headerName, bool strict = false, int timeoutSeconds = 15)
```

**Purpose**: Gets specific header from HTTP response.

**Example**:
```csharp
var traffic = new Traffic(project, instance);
string contentType = traffic.GetResponseHeader("api/data", "content-type");
// Returns: "application/json; charset=utf-8"
```

**Breakdown**:
- `url` - URL to search for
- `headerName` - Header name to extract (case-insensitive)
- `strict` - Exact URL match (default: false)
- `timeoutSeconds` - Maximum wait time (default: 15)
- Returns header value as string
- Returns null if header not found

---

### GetAllRequestHeaders()

```csharp
public Dictionary<string, string> GetAllRequestHeaders(string url, bool strict = false, int timeoutSeconds = 15)
```

**Purpose**: Gets all headers from HTTP request as dictionary.

**Example**:
```csharp
var traffic = new Traffic(project, instance);
var headers = traffic.GetAllRequestHeaders("api/protected");

foreach (var header in headers)
{
    project.log($"{header.Key}: {header.Value}");
}
```

**Breakdown**:
- `url` - URL to search for
- `strict` - Exact URL match (default: false)
- `timeoutSeconds` - Maximum wait time (default: 15)
- Returns Dictionary<string, string> with all headers
- Header names are lowercase
- Empty dictionary if no headers

---

### GetAllResponseHeaders()

```csharp
public Dictionary<string, string> GetAllResponseHeaders(string url, bool strict = false, int timeoutSeconds = 15)
```

**Purpose**: Gets all headers from HTTP response as dictionary.

**Example**:
```csharp
var traffic = new Traffic(project, instance);
var headers = traffic.GetAllResponseHeaders("api/data");

if (headers.ContainsKey("x-rate-limit-remaining"))
{
    int remaining = int.Parse(headers["x-rate-limit-remaining"]);
    project.log($"API calls remaining: {remaining}");
}
```

**Breakdown**:
- `url` - URL to search for
- `strict` - Exact URL match (default: false)
- `timeoutSeconds` - Maximum wait time (default: 15)
- Returns Dictionary<string, string> with all headers
- Header names are lowercase
- Empty dictionary if no headers

---

## Page Actions

### ReloadPage()

```csharp
public Traffic ReloadPage(int delaySeconds = 1)
```

**Purpose**: Reloads page and refreshes traffic cache (chainable).

**Example**:
```csharp
var traffic = new Traffic(project, instance);
traffic.ReloadPage().FindTrafficElement("api/fresh-data");
```

**Breakdown**:
- `delaySeconds` - Delay after reload (default: 1)
- Waits for page to finish loading
- Automatically refreshes traffic cache
- Returns this for method chaining

---

### RefreshTrafficCache()

```csharp
public Traffic RefreshTrafficCache()
```

**Purpose**: Manually refreshes traffic cache (usually automatic).

**Example**:
```csharp
var traffic = new Traffic(project, instance);
traffic.RefreshTrafficCache();
```

**Breakdown**:
- Forces immediate cache update
- Usually not needed (automatic refresh every 2 seconds)
- Returns this for method chaining

---

## Nested Class: TrafficElement

Represents single HTTP request/response pair with convenient data access.

### Properties

```csharp
public string Method { get; }          // HTTP method: GET, POST, etc.
public string Url { get; }             // Full request URL
public string StatusCode { get; }      // HTTP status code: 200, 404, etc.
public string RequestHeaders { get; }  // All request headers
public string RequestCookies { get; }  // Request cookies
public string RequestBody { get; }     // Request payload
public string ResponseHeaders { get; } // All response headers
public string ResponseCookies { get; } // Response cookies
public string ResponseBody { get; }    // Response content
public string ResponseContentType { get; } // Content-Type header
```

**Example**:
```csharp
var element = traffic.FindTrafficElement("api/user");
project.log($"Method: {element.Method}");
project.log($"Status: {element.StatusCode}");
project.log($"Response: {element.ResponseBody}");
```

---

### ParseResponseBodyAsJson()

```csharp
public TrafficElement ParseResponseBodyAsJson()
```

**Purpose**: Parses response body as JSON into project.Json (chainable).

**Example**:
```csharp
var element = traffic.FindTrafficElement("api/user")
                     .ParseResponseBodyAsJson();

string userId = project.Json.SelectToken("$.data.id").ToString();
```

**Breakdown**:
- Parses ResponseBody JSON into project.Json
- Chainable (returns this)
- Throws exception if JSON invalid

---

### ParseRequestBodyAsJson()

```csharp
public TrafficElement ParseRequestBodyAsJson()
```

**Purpose**: Parses request body as JSON into project.Json (chainable).

**Example**:
```csharp
var element = traffic.FindTrafficElement("api/submit")
                     .ParseRequestBodyAsJson();

string username = project.Json.SelectToken("$.username").ToString();
```

**Breakdown**:
- Parses RequestBody JSON into project.Json
- Chainable (returns this)
- Useful for inspecting what was sent to API

---

### GetRequestHeader()

```csharp
public string GetRequestHeader(string headerName)
```

**Purpose**: Gets specific request header from element.

**Example**:
```csharp
var element = traffic.FindTrafficElement("api/data");
string auth = element.GetRequestHeader("Authorization");
```

**Breakdown**:
- `headerName` - Header name (case-insensitive)
- Returns header value or null

---

### GetResponseHeader()

```csharp
public string GetResponseHeader(string headerName)
```

**Purpose**: Gets specific response header from element.

**Example**:
```csharp
var element = traffic.FindTrafficElement("api/data");
string contentType = element.GetResponseHeader("content-type");
```

**Breakdown**:
- `headerName` - Header name (case-insensitive)
- Returns header value or null

---

## Extension Methods

### SaveRequestHeadersToVariable()

```csharp
public static void SaveRequestHeadersToVariable(this IZennoPosterProjectModel project, Instance instance, string url, bool strict = false, bool log = false)
```

**Purpose**: Extracts request headers and saves to project variable "headers".

**Example**:
```csharp
project.SaveRequestHeadersToVariable(instance, "api/protected", log: true);
string headers = project.Var("headers");
```

**Breakdown**:
- Finds traffic for URL
- Removes HTTP/2 pseudo-headers (starting with ":")
- Saves clean headers to variable "headers"
- Logs result if log=true

---

### CollectRequestHeaders()

```csharp
public static void CollectRequestHeaders(this IZennoPosterProjectModel project, Instance instance, string url, bool strict = false, bool saveToVariable = true, bool saveToDatabase = true, bool log = false)
```

**Purpose**: Collects request headers and saves to variable and/or database.

**Example**:
```csharp
// Save to both variable and database
project.CollectRequestHeaders(instance, "api/auth");

// Save only to variable
project.CollectRequestHeaders(instance, "api/data", saveToDatabase: false);
```

**Breakdown**:
- `url` - URL to find in traffic
- `strict` - Exact URL match (default: false)
- `saveToVariable` - Save to "headers" variable (default: true)
- `saveToDatabase` - Save to database (default: true)
- `log` - Enable logging (default: false)
- Removes HTTP/2 pseudo-headers
- Saves clean headers text
