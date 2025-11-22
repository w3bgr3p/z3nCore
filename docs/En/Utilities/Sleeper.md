# Sleeper Class

Class for generating randomized delays within a specified range, useful for simulating human-like behavior.

---

## Constructor

### Purpose
Initializes a Sleeper instance with minimum and maximum delay values in milliseconds.

### Example
```csharp
using z3nCore.Utilities;

// Create sleeper: 1-3 seconds
var sleeper = new Sleeper(min: 1000, max: 3000);

// Create sleeper: 500ms - 1.5 seconds
var quickSleep = new Sleeper(500, 1500);

// Create sleeper: 5-10 seconds
var longSleep = new Sleeper(5000, 10000);
```

### Breakdown
```csharp
public Sleeper(
    int min,  // Minimum delay in milliseconds (must be >= 0)
    int max)  // Maximum delay in milliseconds (must be >= min)

// Throws:
// - ArgumentException: If min < 0
// - ArgumentException: If max < min

// Notes:
// - Uses GUID-based random seed for better randomization
// - Delay range is inclusive: [min, max]
```

---

## Sleep

### Purpose
Pauses execution for a random duration within the configured range, with optional multiplier.

### Example
```csharp
using z3nCore.Utilities;

var sleeper = new Sleeper(1000, 3000);  // 1-3 seconds

// Normal random delay
sleeper.Sleep();
// Pauses for random duration: 1000-3000ms

// Double the delay
sleeper.Sleep(multiplier: 2.0);
// Pauses for random duration: 2000-6000ms

// Half the delay
sleeper.Sleep(multiplier: 0.5);
// Pauses for random duration: 500-1500ms

// Use in automation
instance.ActiveTab.Navigate("https://example.com");
sleeper.Sleep();  // Random human-like delay
instance.ActiveTab.FillTextBox("#username", "user");
sleeper.Sleep(multiplier: 0.5);  // Shorter delay
instance.ActiveTab.FillTextBox("#password", "pass");
```

### Breakdown
```csharp
public void Sleep(
    double multiplier = 1.0)  // Multiplier for delay duration (default: 1.0)

// Returns: void

// How it works:
// 1. Generates random delay between min and max
// 2. Multiplies delay by multiplier parameter
// 3. Pauses thread for calculated duration

// Examples with Sleeper(1000, 3000):
// - Sleep()           → 1000-3000ms random
// - Sleep(2.0)        → 2000-6000ms random
// - Sleep(0.5)        → 500-1500ms random
// - Sleep(1.5)        → 1500-4500ms random

// Use cases:
// - multiplier > 1.0: Increase delay (e.g., waiting for slow page)
// - multiplier < 1.0: Decrease delay (e.g., fast typing)
// - multiplier = 1.0: Normal random delay
```

---

## Usage Patterns

### Simple Random Delays
```csharp
var sleeper = new Sleeper(1000, 2000);

// Navigate and wait
instance.ActiveTab.Navigate(url);
sleeper.Sleep();

// Fill form with delays
instance.ActiveTab.FillTextBox("#field1", value1);
sleeper.Sleep();
instance.ActiveTab.FillTextBox("#field2", value2);
sleeper.Sleep();
```

### Context-Aware Delays
```csharp
var sleeper = new Sleeper(800, 1500);

// Quick action
sleeper.Sleep(0.5);

// Normal action
sleeper.Sleep();

// Slow action (loading heavy page)
sleeper.Sleep(2.0);
```

### Anti-Detection Pattern
```csharp
// Different ranges for different actions
var readDelay = new Sleeper(2000, 5000);   // Reading content
var typeDelay = new Sleeper(100, 300);     // Between keystrokes
var clickDelay = new Sleeper(500, 1500);   // Between clicks

// Simulate reading
readDelay.Sleep();

// Type text character by character
foreach (char c in text)
{
    instance.ActiveTab.SendKey(c.ToString());
    typeDelay.Sleep();
}

// Click button
clickDelay.Sleep();
instance.ActiveTab.Click(button);
```

---
