# Класс RssNewsParser

Класс для парсинга RSS-лент из криптовалютных новостных источников и сохранения статей.

---

## Конструктор

### Назначение
Инициализирует парсер RSS-новостей с настройкой логирования.

### Пример
```csharp
using z3nCore.Utilities;

// Создать парсер с включенным логированием
var parser = new RssNewsParser(project, log: true);

// Создать парсер без детального логирования
var parserQuiet = new RssNewsParser(project, log: false);
```

### Детали
```csharp
public RssNewsParser(
    IZennoPosterProjectModel project,  // Экземпляр проекта для логирования
    bool log = false)                  // Включить детальное логирование (по умолчанию: false)

// Встроенные RSS-источники:
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

### Назначение
Асинхронно парсит сегодняшние новости из всех настроенных RSS-лент и сохраняет их в виде отдельных текстовых и JSON-файлов.

### Пример
```csharp
using z3nCore.Utilities;
using System.Threading.Tasks;

var parser = new RssNewsParser(project, log: true);

// Парсинг новостей асинхронно
await parser.ParseAndSaveNewsAsync();

// Новостные файлы сохранены в: {project.Path}/.data/news/
// Созданные файлы: 1.txt, 1.json, 2.txt, 2.json, и т.д.
```

### Детали
```csharp
public async Task ParseAndSaveNewsAsync()

// Возвращает: Task (async void)

// Процесс:
// 1. Очищает предыдущие файлы новостей из директории .data/news
// 2. Загружает RSS-ленты из всех источников
// 3. Фильтрует только сегодняшние статьи
// 4. Загружает полный текст статьи по каждой ссылке
// 5. Сортирует статьи по дате публикации (новейшие первые)
// 6. Сохраняет каждую статью как:
//    - TXT-файл: {номер}.txt с отформатированным текстом
//    - JSON-файл: {номер}.json со структурированными данными
//
// Расположение вывода: {ProjectPath}/.data/news/
//
// Формат TXT-файла:
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
// Структура JSON:
// {
//   "Title": "Заголовок статьи",
//   "Link": "https://...",
//   "FullText": "Полный текст статьи...",
//   "Description": "Краткое содержание статьи",
//   "PubDate": "2025-11-22T10:30:00",
//   "Source": "Decrypt"
// }

// Особенности:
// - Автоматическое удаление HTML
// - Удаление CSS, SVG, JavaScript кода
// - Фильтрация навигации и текста подвала
// - Удаление дублирующихся заголовков
// - Минимальная длина параграфа: 40 символов
// - Задержка 1 секунда между загрузками статей
// - Комплексная обработка ошибок для каждого источника
```

---

## ParseAndSaveNewsSync

### Назначение
Синхронная версия ParseAndSaveNewsAsync для использования в неасинхронных контекстах.

### Пример
```csharp
using z3nCore.Utilities;

var parser = new RssNewsParser(project, log: true);

// Парсинг новостей синхронно
parser.ParseAndSaveNewsSync();

// Проверка результатов
var newsDir = System.IO.Path.Combine(project.Path, ".data", "news");
var files = System.IO.Directory.GetFiles(newsDir);
project.SendInfoToLog($"Сохранено {files.Length / 2} статей");
```

### Детали
```csharp
public void ParseAndSaveNewsSync()

// Возвращает: void

// Примечания:
// - Блокирует выполнение до завершения парсинга и сохранения всех новостей
// - Та же функциональность что и ParseAndSaveNewsAsync
// - Внутри использует GetAwaiter().GetResult()
// - Подходит для синхронных действий ZennoPoster
```

---

## Класс RssNewsItem

### Назначение
Модель данных, представляющая одну новостную статью.

### Свойства
```csharp
public class RssNewsItem
{
    public string Title { get; set; }       // Заголовок статьи
    public string Link { get; set; }        // URL статьи
    public string FullText { get; set; }    // Полный текст статьи
    public string Description { get; set; } // Краткое содержание из RSS
    public DateTime PubDate { get; set; }   // Дата/время публикации
    public string Source { get; set; }      // Название источника (например, "Decrypt")
}
```

---
