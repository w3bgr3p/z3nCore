# RssNewsParser Class

Class for parsing RSS feeds from cryptocurrency news sources and saving articles.

---

## Constructor

### Purpose
Initializes the RSS news parser with logging configuration.

### Example
```csharp
using z3nCore.Utilities;

// Create parser with logging enabled
var parser = new RssNewsParser(project, log: true);

// Create parser without detailed logging
var parserQuiet = new RssNewsParser(project, log: false);
```

### Breakdown
```csharp
public RssNewsParser(
    IZennoPosterProjectModel project,  // Project instance for logging
    bool log = false)                  // Enable detailed logging (default: false)

// Built-in RSS sources:
// - Decrypt: https://decrypt.co/feed
// - Bitcoin Magazine: https://bitcoinmagazine.com/.rss/full/
// - CryptoSlate: https://cryptoslate.com/feed/
// - BeInCrypto: https://beincrypto.com/feed/
// - U.Today: https://u.today/rss
// - Bitcoinist: https://bitcoinist.com/feed/
// - NewsBTC: https://www.newsbtc.com/feed/
// - Blockworks: https://blockworks.co/feed
// - CoinJournal: https://coinjournal.net/feed/
// - AMBCrypto: https://ambcrypto.com/feed/
```

---

## ParseAndSaveNewsAsync

### Purpose
Asynchronously parses today's news from all configured RSS feeds and saves them as separate text and JSON files.

### Example
```csharp
using z3nCore.Utilities;
using System.Threading.Tasks;

var parser = new RssNewsParser(project, log: true);

// Parse news asynchronously
await parser.ParseAndSaveNewsAsync();

// News files saved to: {project.Path}/.data/news/
// Files created: 1.txt, 1.json, 2.txt, 2.json, etc.
```

### Breakdown
```csharp
public async Task ParseAndSaveNewsAsync()

// Returns: Task (async void)

// Process:
// 1. Clears previous news files from .data/news directory
// 2. Fetches RSS feeds from all sources
// 3. Filters for today's articles only
// 4. Downloads full article text from each link
// 5. Sorts articles by publication date (newest first)
// 6. Saves each article as:
//    - TXT file: {number}.txt with formatted text
//    - JSON file: {number}.json with structured data
//
// Output location: {ProjectPath}/.data/news/
//
// TXT file format:
// Источник: {Source}
// Заголовок: {Title}
// Дата: {PubDate}
// Ссылка: {Link}
//
// === ОПИСАНИЕ ===
// {Description}
//
// === ПОЛНЫЙ ТЕКСТ ===
// {FullText}
//
// JSON structure:
// {
//   "Title": "Article title",
//   "Link": "https://...",
//   "FullText": "Full article text...",
//   "Description": "Article summary",
//   "PubDate": "2025-11-22T10:30:00",
//   "Source": "Decrypt"
// }

// Features:
// - Automatic HTML stripping
// - Removes CSS, SVG, JavaScript code
// - Filters navigation and footer text
// - Removes duplicate headers
// - Minimum paragraph length: 40 characters
// - 1-second delay between article fetches
// - Comprehensive error handling per source
```

---

## ParseAndSaveNewsSync

### Purpose
Synchronous version of ParseAndSaveNewsAsync for use in non-async contexts.

### Example
```csharp
using z3nCore.Utilities;

var parser = new RssNewsParser(project, log: true);

// Parse news synchronously
parser.ParseAndSaveNewsSync();

// Check results
var newsDir = System.IO.Path.Combine(project.Path, ".data", "news");
var files = System.IO.Directory.GetFiles(newsDir);
project.SendInfoToLog($"Saved {files.Length / 2} articles");
```

### Breakdown
```csharp
public void ParseAndSaveNewsSync()

// Returns: void

// Notes:
// - Blocks until all news is parsed and saved
// - Same functionality as ParseAndSaveNewsAsync
// - Uses GetAwaiter().GetResult() internally
// - Suitable for synchronous ZennoPoster actions
```

---

## RssNewsItem Class

### Purpose
Data model representing a single news article.

### Properties
```csharp
public class RssNewsItem
{
    public string Title { get; set; }       // Article headline
    public string Link { get; set; }        // Article URL
    public string FullText { get; set; }    // Complete article text
    public string Description { get; set; } // Brief summary from RSS
    public DateTime PubDate { get; set; }   // Publication date/time
    public string Source { get; set; }      // Source name (e.g., "Decrypt")
}
```

---
