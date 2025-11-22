# ListExtensions

Extension methods for working with lists and ZennoPoster project lists.

---

## ListExtensions.RndFromList

### Purpose
Returns a random element from a `List<string>`. Throws an exception if the list is empty.

### Example
```csharp
var fruits = new List<string> { "apple", "banana", "orange" };
var randomFruit = fruits.RndFromList();
// Returns: random element like "banana"
```

### Breakdown
```csharp
public static object RndFromList(this List<string> list)
{
    // list: The source list to select a random element from
    // Throws: ArgumentNullException if the list is empty
    // Returns: A random element from the list as object type

    if (list.Count == 0)
        throw new ArgumentNullException(nameof(list), "List is empty");

    int index = _random.Next(0, list.Count);
    return list[index];
}
```

---

## ProjectExtensions.RndFromList

### Purpose
Returns a random element from a ZennoPoster project list with optional removal of the selected element.

### Example
```csharp
// Get random element without removing it
string proxy = project.RndFromList("ProxyList");

// Get random element and remove it from the list
string account = project.RndFromList("AccountList", remove: true);
```

### Breakdown
```csharp
public static string RndFromList(
    this IZennoPosterProjectModel project,
    string listName,
    bool remove = false)
{
    // project: The ZennoPoster project instance
    // listName: Name of the list in the project
    // remove: If true, removes the selected element from the list (default: false)
    // Throws: ArgumentNullException if the list is empty
    // Returns: Random element from the specified list

    var list = project.Lists[listName];
    if (list.Count == 0)
        throw new ArgumentNullException(nameof(list), "List is empty");

    // If not removing, just return random element
    if (!remove)
        return list[_random.Next(0, list.Count)];

    // If removing, sync to local list, remove, and sync back
    var localList = project.ListSync(listName);
    int index = _random.Next(0, localList.Count);
    var item = localList[index];
    localList.RemoveAt(index);
    project.ListSync(listName, localList);
    return item;
}
```

---

## ProjectExtensions.ListSync (get)

### Purpose
Creates a local copy of a ZennoPoster project list as `List<string>`.

### Example
```csharp
// Create local copy of project list
List<string> localProxies = project.ListSync("ProxyList");

// Modify local copy
localProxies.Add("127.0.0.1:8080");
localProxies.RemoveAt(0);
```

### Breakdown
```csharp
public static List<string> ListSync(
    this IZennoPosterProjectModel project,
    string listName)
{
    // project: The ZennoPoster project instance
    // listName: Name of the list to synchronize
    // Returns: Local copy of the project list as List<string>

    var projectList = project.Lists[listName];
    var localList = new List<string>();

    // Copy all items from project list to local list
    foreach (var item in projectList)
    {
        localList.Add(item);
    }

    return localList;
}
```

---

## ProjectExtensions.ListSync (set)

### Purpose
Synchronizes a local `List<string>` back to a ZennoPoster project list, replacing all existing items.

### Example
```csharp
// Get local copy
List<string> localList = project.ListSync("MyList");

// Modify local copy
localList.Add("new item");
localList.RemoveAt(0);

// Sync changes back to project
project.ListSync("MyList", localList);
```

### Breakdown
```csharp
public static List<string> ListSync(
    this IZennoPosterProjectModel project,
    string listName,
    List<string> localList)
{
    // project: The ZennoPoster project instance
    // listName: Name of the project list to update
    // localList: Local list with new content
    // Returns: The same localList that was passed in

    var projectList = project.Lists[listName];

    // Clear existing project list
    projectList.Clear();

    // Add all items from local list to project list
    foreach (var item in localList)
    {
        projectList.Add(item);
    }

    return localList;
}
```
