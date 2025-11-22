# NetHttp - Классы для HTTP-запросов

Этот файл содержит три публичных класса для выполнения HTTP-запросов в проектах ZennoPoster.

---

## Класс: NetHttpAsync

Класс для асинхронных HTTP-запросов. Используйте этот класс, если вы можете работать с async/await.

### Конструктор

#### Назначение
Создает новый экземпляр NetHttpAsync для выполнения асинхронных HTTP-запросов.

#### Пример
```csharp
// Базовое использование
var httpClient = new NetHttpAsync(project);

// С включенным логированием
var httpClientWithLog = new NetHttpAsync(project, log: true);
```

#### Разбор
```csharp
public NetHttpAsync(
    IZennoPosterProjectModel project,  // Экземпляр проекта ZennoPoster (обязательный)
    bool log = false)                  // Включить логирование (опционально, по умолчанию: false)
```

---

### GetAsync

#### Назначение
Выполняет асинхронный HTTP GET-запрос для получения данных с URL.

#### Пример
```csharp
var httpClient = new NetHttpAsync(project);

// Простой GET-запрос
string response = await httpClient.GetAsync("https://api.example.com/data");

// GET с прокси и заголовками
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

#### Разбор
```csharp
public async Task<string> GetAsync(
    string url,                              // Целевой URL (обязательный)
    string proxyString = "",                 // Прокси в формате "user:pass@host:port" или "+" для прокси инстанса
    Dictionary<string, string> headers = null, // Словарь HTTP-заголовков
    bool parse = false,                      // Автоматически парсить JSON-ответ в project.Json
    int deadline = 15,                       // Таймаут запроса в секундах
    bool throwOnFail = false)                // Бросать исключение при HTTP-ошибке вместо возврата сообщения об ошибке
// Возвращает: Тело ответа как строку, или сообщение об ошибке при неудаче
```

---

### PostAsync

#### Назначение
Выполняет асинхронный HTTP POST-запрос для отправки данных на сервер.

#### Пример
```csharp
var httpClient = new NetHttpAsync(project);

// POST с JSON-телом
string jsonBody = "{\"username\":\"john\",\"password\":\"secret123\"}";
string response = await httpClient.PostAsync(
    url: "https://api.example.com/login",
    body: jsonBody
);

// POST с заголовками и прокси
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

#### Разбор
```csharp
public async Task<string> PostAsync(
    string url,                              // Целевой URL (обязательный)
    string body,                             // Тело запроса, обычно JSON (обязательный)
    string proxyString = "",                 // Прокси в формате "user:pass@host:port"
    Dictionary<string, string> headers = null, // Словарь HTTP-заголовков
    bool parse = false,                      // Автоматически парсить JSON-ответ в project.Json
    int deadline = 15,                       // Таймаут запроса в секундах
    bool throwOnFail = false)                // Бросать исключение при HTTP-ошибке
// Возвращает: Тело ответа как строку, или сообщение об ошибке при неудаче
// Примечание: Content-Type автоматически устанавливается в "application/json; charset=UTF-8"
```

---

### PutAsync

#### Назначение
Выполняет асинхронный HTTP PUT-запрос для обновления данных на сервере.

#### Пример
```csharp
var httpClient = new NetHttpAsync(project);

// PUT с JSON-телом
string jsonBody = "{\"name\":\"Updated Name\",\"status\":\"active\"}";
string response = await httpClient.PutAsync(
    url: "https://api.example.com/users/123",
    body: jsonBody
);

// PUT без тела (иногда используется для переключателей/флагов)
string response = await httpClient.PutAsync(
    url: "https://api.example.com/toggle",
    proxyString: "proxy.com:8080"
);
```

#### Разбор
```csharp
public async Task<string> PutAsync(
    string url,                              // Целевой URL (обязательный)
    string body = "",                        // Тело запроса, обычно JSON (опционально)
    string proxyString = "",                 // Прокси в формате "user:pass@host:port"
    Dictionary<string, string> headers = null, // Словарь HTTP-заголовков
    bool parse = false)                      // Автоматически парсить JSON-ответ в project.Json
// Возвращает: Тело ответа как строку, или сообщение об ошибке при неудаче
// Таймаут: Фиксированный на 30 секунд
// Примечание: Бросает исключение при HTTP-ошибке (статус коды не 2xx)
```

---

### DeleteAsync

#### Назначение
Выполняет асинхронный HTTP DELETE-запрос для удаления данных с сервера.

#### Пример
```csharp
var httpClient = new NetHttpAsync(project);

// Простой DELETE-запрос
string response = await httpClient.DeleteAsync("https://api.example.com/users/123");

// DELETE с заголовками
var headers = new Dictionary<string, string>
{
    { "Authorization", "Bearer token123" }
};
string response = await httpClient.DeleteAsync(
    url: "https://api.example.com/users/123",
    headers: headers
);
```

#### Разбор
```csharp
public async Task<string> DeleteAsync(
    string url,                              // Целевой URL (обязательный)
    string proxyString = "",                 // Прокси в формате "user:pass@host:port"
    Dictionary<string, string> headers = null) // Словарь HTTP-заголовков
// Возвращает: Тело ответа как строку, или сообщение об ошибке при неудаче
// Таймаут: Фиксированный на 30 секунд
// Примечание: Бросает исключение при HTTP-ошибке (статус коды не 2xx)
```

---

## Класс: NetHttp

Синхронная обертка для HTTP-запросов для проектов ZennoPoster, которые не поддерживают async/await.

**Предупреждение:** Этот класс блокирует поток. По возможности используйте NetHttpAsync.

### Конструктор

#### Назначение
Создает новый экземпляр NetHttp для выполнения синхронных HTTP-запросов.

#### Пример
```csharp
// Базовое использование
var httpClient = new NetHttp(project);

// С включенным логированием
var httpClientWithLog = new NetHttp(project, log: true);
```

#### Разбор
```csharp
public NetHttp(
    IZennoPosterProjectModel project,  // Экземпляр проекта ZennoPoster (обязательный)
    bool log = false)                  // Включить логирование (опционально, по умолчанию: false)
```

---

### GET

#### Назначение
Выполняет синхронный HTTP GET-запрос. Это блокирующая обертка вокруг GetAsync.

#### Пример
```csharp
var httpClient = new NetHttp(project);

// Простой GET-запрос
string response = httpClient.GET("https://api.example.com/data");

// GET со всеми параметрами
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

#### Разбор
```csharp
public string GET(
    string url,                              // Целевой URL (обязательный)
    string proxyString = "",                 // Прокси в формате "user:pass@host:port" или "+" для прокси инстанса
    Dictionary<string, string> headers = null, // Словарь HTTP-заголовков
    bool parse = false,                      // Автоматически парсить JSON-ответ в project.Json
    int deadline = 15,                       // Таймаут запроса в секундах
    bool throwOnFail = false)                // Бросать исключение при HTTP-ошибке
// Возвращает: Тело ответа как строку, или сообщение об ошибке при неудаче
// Предупреждение: Блокирует текущий поток до завершения запроса
```

---

### POST

#### Назначение
Выполняет синхронный HTTP POST-запрос. Это блокирующая обертка вокруг PostAsync.

#### Пример
```csharp
var httpClient = new NetHttp(project);

// POST с JSON-телом
string jsonBody = "{\"email\":\"user@example.com\"}";
string response = httpClient.POST(
    url: "https://api.example.com/subscribe",
    body: jsonBody
);

// POST со всеми параметрами
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

#### Разбор
```csharp
public string POST(
    string url,                              // Целевой URL (обязательный)
    string body,                             // Тело запроса, обычно JSON (обязательный)
    string proxyString = "",                 // Прокси в формате "user:pass@host:port"
    Dictionary<string, string> headers = null, // Словарь HTTP-заголовков
    bool parse = false,                      // Автоматически парсить JSON-ответ в project.Json
    int deadline = 15,                       // Таймаут запроса в секундах
    bool throwOnFail = false)                // Бросать исключение при HTTP-ошибке
// Возвращает: Тело ответа как строку, или сообщение об ошибке при неудаче
// Предупреждение: Блокирует текущий поток до завершения запроса
```

---

### PUT

#### Назначение
Выполняет синхронный HTTP PUT-запрос. Это блокирующая обертка вокруг PutAsync.

#### Пример
```csharp
var httpClient = new NetHttp(project);

// PUT с JSON-телом
string jsonBody = "{\"status\":\"completed\"}";
string response = httpClient.PUT(
    url: "https://api.example.com/tasks/456",
    body: jsonBody
);

// PUT с заголовками
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

#### Разбор
```csharp
public string PUT(
    string url,                              // Целевой URL (обязательный)
    string body = "",                        // Тело запроса, обычно JSON (опционально)
    string proxyString = "",                 // Прокси в формате "user:pass@host:port"
    Dictionary<string, string> headers = null, // Словарь HTTP-заголовков
    bool parse = false)                      // Автоматически парсить JSON-ответ в project.Json
// Возвращает: Тело ответа как строку, или сообщение об ошибке при неудаче
// Предупреждение: Блокирует текущий поток до завершения запроса
```

---

### DELETE

#### Назначение
Выполняет синхронный HTTP DELETE-запрос. Это блокирующая обертка вокруг DeleteAsync.

#### Пример
```csharp
var httpClient = new NetHttp(project);

// Простой DELETE-запрос
string response = httpClient.DELETE("https://api.example.com/items/789");

// DELETE с заголовками и прокси
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

#### Разбор
```csharp
public string DELETE(
    string url,                              // Целевой URL (обязательный)
    string proxyString = "",                 // Прокси в формате "user:pass@host:port"
    Dictionary<string, string> headers = null) // Словарь HTTP-заголовков
// Возвращает: Тело ответа как строку, или сообщение об ошибке при неудаче
// Предупреждение: Блокирует текущий поток до завершения запроса
```

---

## Класс: ProjectExtensions

Методы расширения для удобного вызова HTTP-запросов напрямую из проекта ZennoPoster.

### NetGet

#### Назначение
Метод расширения для выполнения GET-запросов напрямую из экземпляра проекта.

#### Пример
```csharp
// Простой GET-запрос
string response = project.NetGet("https://api.example.com/data");

// GET со всеми параметрами
string[] headers = new string[]
{
    "Authorization: Bearer token123",
    "Accept: application/json"
};
string response = project.NetGet(
    url: "https://api.example.com/data",
    proxyString: "+",  // Использовать прокси инстанса
    headers: headers,
    parse: true,
    deadline: 30,
    thrw: false
);
```

#### Разбор
```csharp
public static string NetGet(
    this IZennoPosterProjectModel project,  // Экземпляр проекта (неявный)
    string url,                              // Целевой URL (обязательный)
    string proxyString = "",                 // Прокси в формате "user:pass@host:port" или "+" для прокси инстанса
    string[] headers = null,                 // HTTP-заголовки как массив строк (формат "Key: Value")
    bool parse = false,                      // Автоматически парсить JSON-ответ в project.Json
    int deadline = 15,                       // Таймаут запроса в секундах
    bool thrw = false)                       // Бросать исключение при HTTP-ошибке
// Возвращает: Тело ответа как строку, или сообщение об ошибке при неудаче
// Примечание: Автоматически фильтрует запрещенные HTTP/2 заголовки (authority, method, path, scheme)
```

---

### NetPost

#### Назначение
Метод расширения для выполнения POST-запросов напрямую из экземпляра проекта.

#### Пример
```csharp
// Простой POST-запрос
string jsonBody = "{\"action\":\"subscribe\"}";
string response = project.NetPost(
    url: "https://api.example.com/action",
    body: jsonBody
);

// POST со всеми параметрами
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

#### Разбор
```csharp
public static string NetPost(
    this IZennoPosterProjectModel project,  // Экземпляр проекта (неявный)
    string url,                              // Целевой URL (обязательный)
    string body,                             // Тело запроса, обычно JSON (обязательный)
    string proxyString = "",                 // Прокси в формате "user:pass@host:port"
    string[] headers = null,                 // HTTP-заголовки как массив строк (формат "Key: Value")
    bool parse = false,                      // Автоматически парсить JSON-ответ в project.Json
    int deadline = 15,                       // Таймаут запроса в секундах
    bool thrw = false)                       // Бросать исключение при HTTP-ошибке
// Возвращает: Тело ответа как строку, или сообщение об ошибке при неудаче
// Примечание: Автоматически фильтрует запрещенные HTTP/2 заголовки (authority, method, path, scheme)
```

---

## Примечания

### Формат прокси
- Стандартный: `"host:port"` или `"user:pass@host:port"`
- Прокси инстанса: `"+"` (извлекается из SQL-хранилища проекта)
- Без прокси: `""` (пустая строка)

### Формат заголовков
- Для NetHttpAsync/NetHttp: `Dictionary<string, string>`
- Для ProjectExtensions: `string[]` в формате `"Key: Value"`

### Запрещенные заголовки
Следующие заголовки автоматически фильтруются:
- `authority`, `method`, `path`, `scheme`
- `host`, `content-length`, `connection`, `upgrade`
- `proxy-connection`, `transfer-encoding`

### Cookies из ответа
Все методы автоматически сохраняют заголовки Set-Cookie в `project.Variables["debugCookies"]` для отладки.

### Обработка ошибок
- Если `throwOnFail` равен `false`: Возвращает сообщение об ошибке как строку
- Если `throwOnFail` равен `true`: Бросает HttpRequestException или другие исключения
