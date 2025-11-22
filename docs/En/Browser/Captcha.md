# Captcha

Extension methods for handling Cloudflare challenges and CAPTCHA solving.

## Extension Methods

### CFSolve()

```csharp
public static void CFSolve(this Instance instance)
```

**Purpose**: Solves Cloudflare challenges on the current page.

**Example**:
```csharp
instance.Go("https://example.com");
instance.CFSolve(); // Solves Cloudflare challenge if present
```

**Breakdown**:
- `instance` - Browser instance with Cloudflare challenge
- Automatically detects and solves Cloudflare protection
- No return value
- Waits until challenge is solved

---

### CFToken()

```csharp
public static string CFToken(this Instance instance, int deadline = 60, bool strict = false)
```

**Purpose**: Retrieves the Cloudflare clearance token from cookies after solving the challenge.

**Example**:
```csharp
instance.Go("https://protected-site.com");
string token = instance.CFToken(deadline: 120);
// Returns: "cf_clearance=abc123..."
```

**Breakdown**:
- `instance` - Browser instance
- `deadline` - Maximum wait time in seconds (default: 60)
- `strict` - Use strict validation (default: false)
- Returns Cloudflare clearance token as string
- Throws timeout exception if token not obtained within deadline

---

### CapGuru()

```csharp
public static bool CapGuru(this IZennoPosterProjectModel project)
```

**Purpose**: Integrates with CapMonster/CapGuru for automated CAPTCHA solving.

**Example**:
```csharp
if (project.CapGuru())
{
    project.log("CAPTCHA solved successfully");
}
else
{
    project.log("CAPTCHA solving failed");
}
```

**Breakdown**:
- `project` - ZennoPoster project model
- Returns true if CAPTCHA solved successfully
- Returns false if CAPTCHA solving failed
- Requires CapMonster/CapGuru service configuration
