# NetHttp - HTTP Request Classes

This file contains three public classes for making HTTP requests in ZennoPoster projects.

---

## Class: NetHttpAsync

Asynchronous HTTP request class. Use this class if you can work with async/await.

### Constructor

#### Purpose
Creates a new instance of NetHttpAsync for making asynchronous HTTP requests.

#### Example
```csharp
// Basic usage
var httpClient = new NetHttpAsync(project);

// With logging enabled
var httpClientWithLog = new NetHttpAsync(project, log: true);
```

#### Breakdown
```csharp
public NetHttpAsync(
    IZennoPosterProjectModel project,  // ZennoPoster project instance (required)
    bool log = false)                  // Enable logging (optional, default: false)
```

---

### GetAsync

#### Purpose
Performs an asynchronous HTTP GET request to retrieve data from a URL.

#### Example
```csharp
var httpClient = new NetHttpAsync(project);

// Simple GET request
string response = await httpClient.GetAsync("https://api.example.com/data");

// GET with proxy and headers
var headers = new Dictionary<string, string>
{
    { "Authorization", "Bearer token123" },
    { "Accept", "application/json" }
};
string response = await httpClient.GetAsync(
    url: "https://api.example.com/data",
    proxyString: "user:pass@proxy.com:8080",
    headers: headers,
    parse: true,
    deadline: 30
);
```

#### Breakdown
```csharp
public async Task<string> GetAsync(
    string url,                              // Target URL (required)
    string proxyString = "",                 // Proxy in format "user:pass@host:port" or "+" for instance proxy
    Dictionary<string, string> headers = null, // HTTP headers dictionary
    bool parse = false,                      // Auto-parse JSON response to project.Json
    int deadline = 15,                       // Request timeout in seconds
    bool throwOnFail = false)                // Throw exception on HTTP error instead of returning error message
// Returns: Response body as string, or error message on failure
```

---

### PostAsync

#### Purpose
Performs an asynchronous HTTP POST request to send data to a server.

#### Example
```csharp
var httpClient = new NetHttpAsync(project);

// POST with JSON body
string jsonBody = "{\"username\":\"john\",\"password\":\"secret123\"}";
string response = await httpClient.PostAsync(
    url: "https://api.example.com/login",
    body: jsonBody
);

// POST with headers and proxy
var headers = new Dictionary<string, string>
{
    { "Authorization", "Bearer token123" }
};
string response = await httpClient.PostAsync(
    url: "https://api.example.com/submit",
    body: jsonBody,
    proxyString: "proxy.com:8080",
    headers: headers,
    parse: true,
    deadline: 30,
    throwOnFail: true
);
```

#### Breakdown
```csharp
public async Task<string> PostAsync(
    string url,                              // Target URL (required)
    string body,                             // Request body, usually JSON (required)
    string proxyString = "",                 // Proxy in format "user:pass@host:port"
    Dictionary<string, string> headers = null, // HTTP headers dictionary
    bool parse = false,                      // Auto-parse JSON response to project.Json
    int deadline = 15,                       // Request timeout in seconds
    bool throwOnFail = false)                // Throw exception on HTTP error
// Returns: Response body as string, or error message on failure
// Note: Content-Type is automatically set to "application/json; charset=UTF-8"
```

---

### PutAsync

#### Purpose
Performs an asynchronous HTTP PUT request to update data on a server.

#### Example
```csharp
var httpClient = new NetHttpAsync(project);

// PUT with JSON body
string jsonBody = "{\"name\":\"Updated Name\",\"status\":\"active\"}";
string response = await httpClient.PutAsync(
    url: "https://api.example.com/users/123",
    body: jsonBody
);

// PUT without body (sometimes used for toggles/flags)
string response = await httpClient.PutAsync(
    url: "https://api.example.com/toggle",
    proxyString: "proxy.com:8080"
);
```

#### Breakdown
```csharp
public async Task<string> PutAsync(
    string url,                              // Target URL (required)
    string body = "",                        // Request body, usually JSON (optional)
    string proxyString = "",                 // Proxy in format "user:pass@host:port"
    Dictionary<string, string> headers = null, // HTTP headers dictionary
    bool parse = false)                      // Auto-parse JSON response to project.Json
// Returns: Response body as string, or error message on failure
// Timeout: Fixed at 30 seconds
// Note: Throws exception on HTTP error (non-2xx status codes)
```

---

### DeleteAsync

#### Purpose
Performs an asynchronous HTTP DELETE request to remove data from a server.

#### Example
```csharp
var httpClient = new NetHttpAsync(project);

// Simple DELETE request
string response = await httpClient.DeleteAsync("https://api.example.com/users/123");

// DELETE with headers
var headers = new Dictionary<string, string>
{
    { "Authorization", "Bearer token123" }
};
string response = await httpClient.DeleteAsync(
    url: "https://api.example.com/users/123",
    headers: headers
);
```

#### Breakdown
```csharp
public async Task<string> DeleteAsync(
    string url,                              // Target URL (required)
    string proxyString = "",                 // Proxy in format "user:pass@host:port"
    Dictionary<string, string> headers = null) // HTTP headers dictionary
// Returns: Response body as string, or error message on failure
// Timeout: Fixed at 30 seconds
// Note: Throws exception on HTTP error (non-2xx status codes)
```

---

## Class: NetHttp

Synchronous HTTP request wrapper for ZennoPoster projects that don't support async/await.

**Warning:** This class blocks the thread. Use NetHttpAsync if possible.

### Constructor

#### Purpose
Creates a new instance of NetHttp for making synchronous HTTP requests.

#### Example
```csharp
// Basic usage
var httpClient = new NetHttp(project);

// With logging enabled
var httpClientWithLog = new NetHttp(project, log: true);
```

#### Breakdown
```csharp
public NetHttp(
    IZennoPosterProjectModel project,  // ZennoPoster project instance (required)
    bool log = false)                  // Enable logging (optional, default: false)
```

---

### GET

#### Purpose
Performs a synchronous HTTP GET request. This is a blocking wrapper around GetAsync.

#### Example
```csharp
var httpClient = new NetHttp(project);

// Simple GET request
string response = httpClient.GET("https://api.example.com/data");

// GET with all parameters
var headers = new Dictionary<string, string>
{
    { "Authorization", "Bearer token123" }
};
string response = httpClient.GET(
    url: "https://api.example.com/data",
    proxyString: "user:pass@proxy.com:8080",
    headers: headers,
    parse: true,
    deadline: 30,
    throwOnFail: false
);
```

#### Breakdown
```csharp
public string GET(
    string url,                              // Target URL (required)
    string proxyString = "",                 // Proxy in format "user:pass@host:port" or "+" for instance proxy
    Dictionary<string, string> headers = null, // HTTP headers dictionary
    bool parse = false,                      // Auto-parse JSON response to project.Json
    int deadline = 15,                       // Request timeout in seconds
    bool throwOnFail = false)                // Throw exception on HTTP error
// Returns: Response body as string, or error message on failure
// Warning: Blocks the current thread until request completes
```

---

### POST

#### Purpose
Performs a synchronous HTTP POST request. This is a blocking wrapper around PostAsync.

#### Example
```csharp
var httpClient = new NetHttp(project);

// POST with JSON body
string jsonBody = "{\"email\":\"user@example.com\"}";
string response = httpClient.POST(
    url: "https://api.example.com/subscribe",
    body: jsonBody
);

// POST with all parameters
var headers = new Dictionary<string, string>
{
    { "X-Custom-Header", "value" }
};
string response = httpClient.POST(
    url: "https://api.example.com/data",
    body: jsonBody,
    proxyString: "proxy.com:8080",
    headers: headers,
    parse: true,
    deadline: 30,
    throwOnFail: false
);
```

#### Breakdown
```csharp
public string POST(
    string url,                              // Target URL (required)
    string body,                             // Request body, usually JSON (required)
    string proxyString = "",                 // Proxy in format "user:pass@host:port"
    Dictionary<string, string> headers = null, // HTTP headers dictionary
    bool parse = false,                      // Auto-parse JSON response to project.Json
    int deadline = 15,                       // Request timeout in seconds
    bool throwOnFail = false)                // Throw exception on HTTP error
// Returns: Response body as string, or error message on failure
// Warning: Blocks the current thread until request completes
```

---

### PUT

#### Purpose
Performs a synchronous HTTP PUT request. This is a blocking wrapper around PutAsync.

#### Example
```csharp
var httpClient = new NetHttp(project);

// PUT with JSON body
string jsonBody = "{\"status\":\"completed\"}";
string response = httpClient.PUT(
    url: "https://api.example.com/tasks/456",
    body: jsonBody
);

// PUT with headers
var headers = new Dictionary<string, string>
{
    { "Authorization", "Bearer token123" }
};
string response = httpClient.PUT(
    url: "https://api.example.com/update",
    body: jsonBody,
    headers: headers,
    parse: true
);
```

#### Breakdown
```csharp
public string PUT(
    string url,                              // Target URL (required)
    string body = "",                        // Request body, usually JSON (optional)
    string proxyString = "",                 // Proxy in format "user:pass@host:port"
    Dictionary<string, string> headers = null, // HTTP headers dictionary
    bool parse = false)                      // Auto-parse JSON response to project.Json
// Returns: Response body as string, or error message on failure
// Warning: Blocks the current thread until request completes
```

---

### DELETE

#### Purpose
Performs a synchronous HTTP DELETE request. This is a blocking wrapper around DeleteAsync.

#### Example
```csharp
var httpClient = new NetHttp(project);

// Simple DELETE request
string response = httpClient.DELETE("https://api.example.com/items/789");

// DELETE with headers and proxy
var headers = new Dictionary<string, string>
{
    { "Authorization", "Bearer token123" }
};
string response = httpClient.DELETE(
    url: "https://api.example.com/items/789",
    proxyString: "proxy.com:8080",
    headers: headers
);
```

#### Breakdown
```csharp
public string DELETE(
    string url,                              // Target URL (required)
    string proxyString = "",                 // Proxy in format "user:pass@host:port"
    Dictionary<string, string> headers = null) // HTTP headers dictionary
// Returns: Response body as string, or error message on failure
// Warning: Blocks the current thread until request completes
```

---

## Class: ProjectExtensions

Extension methods for convenient HTTP calls directly from ZennoPoster Project.

### NetGet

#### Purpose
Extension method for making GET requests directly from the project instance.

#### Example
```csharp
// Simple GET request
string response = project.NetGet("https://api.example.com/data");

// GET with all parameters
string[] headers = new string[]
{
    "Authorization: Bearer token123",
    "Accept: application/json"
};
string response = project.NetGet(
    url: "https://api.example.com/data",
    proxyString: "+",  // Use instance proxy
    headers: headers,
    parse: true,
    deadline: 30,
    thrw: false
);
```

#### Breakdown
```csharp
public static string NetGet(
    this IZennoPosterProjectModel project,  // Project instance (implicit)
    string url,                              // Target URL (required)
    string proxyString = "",                 // Proxy in format "user:pass@host:port" or "+" for instance proxy
    string[] headers = null,                 // HTTP headers as string array ("Key: Value" format)
    bool parse = false,                      // Auto-parse JSON response to project.Json
    int deadline = 15,                       // Request timeout in seconds
    bool thrw = false)                       // Throw exception on HTTP error
// Returns: Response body as string, or error message on failure
// Note: Automatically filters out forbidden HTTP/2 headers (authority, method, path, scheme)
```

---

### NetPost

#### Purpose
Extension method for making POST requests directly from the project instance.

#### Example
```csharp
// Simple POST request
string jsonBody = "{\"action\":\"subscribe\"}";
string response = project.NetPost(
    url: "https://api.example.com/action",
    body: jsonBody
);

// POST with all parameters
string[] headers = new string[]
{
    "Authorization: Bearer token123",
    "X-Request-ID: 12345"
};
string response = project.NetPost(
    url: "https://api.example.com/submit",
    body: jsonBody,
    proxyString: "user:pass@proxy.com:8080",
    headers: headers,
    parse: true,
    deadline: 30,
    thrw: false
);
```

#### Breakdown
```csharp
public static string NetPost(
    this IZennoPosterProjectModel project,  // Project instance (implicit)
    string url,                              // Target URL (required)
    string body,                             // Request body, usually JSON (required)
    string proxyString = "",                 // Proxy in format "user:pass@host:port"
    string[] headers = null,                 // HTTP headers as string array ("Key: Value" format)
    bool parse = false,                      // Auto-parse JSON response to project.Json
    int deadline = 15,                       // Request timeout in seconds
    bool thrw = false)                       // Throw exception on HTTP error
// Returns: Response body as string, or error message on failure
// Note: Automatically filters out forbidden HTTP/2 headers (authority, method, path, scheme)
```

---

## Notes

### Proxy Format
- Standard: `"host:port"` or `"user:pass@host:port"`
- Instance proxy: `"+"` (retrieves from project's SQL storage)
- No proxy: `""` (empty string)

### Headers Format
- For NetHttpAsync/NetHttp: `Dictionary<string, string>`
- For ProjectExtensions: `string[]` in format `"Key: Value"`

### Forbidden Headers
The following headers are automatically filtered out:
- `authority`, `method`, `path`, `scheme`
- `host`, `content-length`, `connection`, `upgrade`
- `proxy-connection`, `transfer-encoding`

### Response Cookies
All methods automatically store Set-Cookie headers in `project.Variables["debugCookies"]` for debugging.

### Error Handling
- If `throwOnFail` is `false`: Returns error message as string
- If `throwOnFail` is `true`: Throws HttpRequestException or other exceptions
