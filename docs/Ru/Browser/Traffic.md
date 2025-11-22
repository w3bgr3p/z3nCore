# Traffic

Современный мониторинг и анализ HTTP трафика с автоматическим кешированием и удобным извлечением данных.

## Класс: Traffic

### Конструктор

```csharp
public Traffic(IZennoPosterProjectModel project, Instance instance, bool log = false)
```

**Назначение**: Создает монитор трафика с автоматическим кешированием для анализа HTTP запросов и ответов.

**Пример**:
```csharp
var traffic = new Traffic(project, instance, log: true);
```

**Детали**:
- `project` - Модель проекта ZennoPoster
- `instance` - Экземпляр браузера для мониторинга
- `log` - Включить детальное логирование (по умолчанию: false)
- Автоматически устанавливает `instance.UseTrafficMonitoring = true`

---

## Поиск элементов трафика

### FindTrafficElement()

```csharp
public TrafficElement FindTrafficElement(string url, bool strict = false, int timeoutSeconds = 15, int retryDelaySeconds = 1, bool reload = false)
```

**Назначение**: Находит первый элемент HTTP трафика по URL с автоматическим ожиданием и повторами.

**Пример**:
```csharp
var traffic = new Traffic(project, instance);

// Поиск по подстроке URL
var element = traffic.FindTrafficElement("api/user");

// Поиск по точному URL
var element = traffic.FindTrafficElement("https://api.example.com/v1/data", strict: true);

// Перезагрузить страницу и искать
var element = traffic.FindTrafficElement("api/token", reload: true, timeoutSeconds: 30);
```

**Детали**:
- `url` - URL или подстрока для поиска
- `strict` - true: точное совпадение, false: содержит подстроку (по умолчанию: false)
- `timeoutSeconds` - Максимальное время ожидания (по умолчанию: 15)
- `retryDelaySeconds` - Задержка между повторами (по умолчанию: 1)
- `reload` - Перезагрузить страницу перед поиском (по умолчанию: false)
- Возвращает TrafficElement с полными данными запроса/ответа
- Выбрасывает TimeoutException, если не найдено в течение таймаута

---

### FindAllTrafficElements()

```csharp
public List<TrafficElement> FindAllTrafficElements(string url, bool strict = false)
```

**Назначение**: Находит все элементы HTTP трафика по URL (без ожидания, использует текущий кеш).

**Пример**:
```csharp
var traffic = new Traffic(project, instance);
var elements = traffic.FindAllTrafficElements("api/analytics");

foreach (var element in elements)
{
    project.log($"Запрос к: {element.Url}");
}
```

**Детали**:
- `url` - URL или подстрока для поиска
- `strict` - true: точное совпадение, false: содержит подстроку (по умолчанию: false)
- Возвращает список всех подходящих объектов TrafficElement
- Работает с текущим кешем (без ожидания)
- Возвращает пустой список, если совпадений не найдено

---

### GetAllTraffic()

```csharp
public List<TrafficElement> GetAllTraffic()
```

**Назначение**: Получает полный снимок HTTP трафика (все запросы и ответы).

**Пример**:
```csharp
var traffic = new Traffic(project, instance);
var allRequests = traffic.GetAllTraffic();

project.log($"Всего HTTP запросов: {allRequests.Count}");
```

**Детали**:
- Без параметров
- Возвращает список всех объектов TrafficElement в кеше
- Автоматически обновляет кеш, если он устарел
- Исключает OPTIONS запросы

---

## Быстрое извлечение данных

### GetResponseBody()

```csharp
public string GetResponseBody(string url, bool strict = false, int timeoutSeconds = 15)
```

**Назначение**: Быстрый метод для получения тела ответа из HTTP трафика.

**Пример**:
```csharp
var traffic = new Traffic(project, instance);
string json = traffic.GetResponseBody("api/user/profile");
project.Json.FromString(json);
```

**Детали**:
- `url` - URL для поиска
- `strict` - Точное совпадение URL (по умолчанию: false)
- `timeoutSeconds` - Максимальное время ожидания (по умолчанию: 15)
- Возвращает тело ответа как строку
- Ожидает элемент трафика, если не найден сразу
- Выбрасывает TimeoutException, если не найдено

---

### GetRequestBody()

```csharp
public string GetRequestBody(string url, bool strict = false, int timeoutSeconds = 15)
```

**Назначение**: Быстрый метод для получения тела запроса из HTTP трафика.

**Пример**:
```csharp
var traffic = new Traffic(project, instance);
string requestData = traffic.GetRequestBody("api/submit");
project.log($"Отправленные данные: {requestData}");
```

**Детали**:
- `url` - URL для поиска
- `strict` - Точное совпадение URL (по умолчанию: false)
- `timeoutSeconds` - Максимальное время ожидания (по умолчанию: 15)
- Возвращает тело запроса как строку
- Полезно для просмотра данных, отправленных на сервер

---

### GetRequestHeader()

```csharp
public string GetRequestHeader(string url, string headerName, bool strict = false, int timeoutSeconds = 15)
```

**Назначение**: Получает конкретный заголовок из HTTP запроса.

**Пример**:
```csharp
var traffic = new Traffic(project, instance);
string auth = traffic.GetRequestHeader("api/protected", "Authorization");
// Возвращает: "Bearer eyJhbGc..."

string userAgent = traffic.GetRequestHeader("api/data", "user-agent");
```

**Детали**:
- `url` - URL для поиска
- `headerName` - Имя заголовка для извлечения (регистронезависимо)
- `strict` - Точное совпадение URL (по умолчанию: false)
- `timeoutSeconds` - Максимальное время ожидания (по умолчанию: 15)
- Возвращает значение заголовка как строку
- Возвращает null, если заголовок не найден

---

### GetResponseHeader()

```csharp
public string GetResponseHeader(string url, string headerName, bool strict = false, int timeoutSeconds = 15)
```

**Назначение**: Получает конкретный заголовок из HTTP ответа.

**Пример**:
```csharp
var traffic = new Traffic(project, instance);
string contentType = traffic.GetResponseHeader("api/data", "content-type");
// Возвращает: "application/json; charset=utf-8"
```

**Детали**:
- `url` - URL для поиска
- `headerName` - Имя заголовка для извлечения (регистронезависимо)
- `strict` - Точное совпадение URL (по умолчанию: false)
- `timeoutSeconds` - Максимальное время ожидания (по умолчанию: 15)
- Возвращает значение заголовка как строку
- Возвращает null, если заголовок не найден

---

### GetAllRequestHeaders()

```csharp
public Dictionary<string, string> GetAllRequestHeaders(string url, bool strict = false, int timeoutSeconds = 15)
```

**Назначение**: Получает все заголовки из HTTP запроса в виде словаря.

**Пример**:
```csharp
var traffic = new Traffic(project, instance);
var headers = traffic.GetAllRequestHeaders("api/protected");

foreach (var header in headers)
{
    project.log($"{header.Key}: {header.Value}");
}
```

**Детали**:
- `url` - URL для поиска
- `strict` - Точное совпадение URL (по умолчанию: false)
- `timeoutSeconds` - Максимальное время ожидания (по умолчанию: 15)
- Возвращает Dictionary<string, string> со всеми заголовками
- Имена заголовков в нижнем регистре
- Пустой словарь, если заголовков нет

---

### GetAllResponseHeaders()

```csharp
public Dictionary<string, string> GetAllResponseHeaders(string url, bool strict = false, int timeoutSeconds = 15)
```

**Назначение**: Получает все заголовки из HTTP ответа в виде словаря.

**Пример**:
```csharp
var traffic = new Traffic(project, instance);
var headers = traffic.GetAllResponseHeaders("api/data");

if (headers.ContainsKey("x-rate-limit-remaining"))
{
    int remaining = int.Parse(headers["x-rate-limit-remaining"]);
    project.log($"Осталось вызовов API: {remaining}");
}
```

**Детали**:
- `url` - URL для поиска
- `strict` - Точное совпадение URL (по умолчанию: false)
- `timeoutSeconds` - Максимальное время ожидания (по умолчанию: 15)
- Возвращает Dictionary<string, string> со всеми заголовками
- Имена заголовков в нижнем регистре
- Пустой словарь, если заголовков нет

---

## Действия со страницей

### ReloadPage()

```csharp
public Traffic ReloadPage(int delaySeconds = 1)
```

**Назначение**: Перезагружает страницу и обновляет кеш трафика (цепной вызов).

**Пример**:
```csharp
var traffic = new Traffic(project, instance);
traffic.ReloadPage().FindTrafficElement("api/fresh-data");
```

**Детали**:
- `delaySeconds` - Задержка после перезагрузки (по умолчанию: 1)
- Ожидает завершения загрузки страницы
- Автоматически обновляет кеш трафика
- Возвращает this для цепных вызовов

---

### RefreshTrafficCache()

```csharp
public Traffic RefreshTrafficCache()
```

**Назначение**: Вручную обновляет кеш трафика (обычно автоматически).

**Пример**:
```csharp
var traffic = new Traffic(project, instance);
traffic.RefreshTrafficCache();
```

**Детали**:
- Принудительно обновляет кеш немедленно
- Обычно не требуется (автоматическое обновление каждые 2 секунды)
- Возвращает this для цепных вызовов

---

## Вложенный класс: TrafficElement

Представляет одну пару HTTP запрос/ответ с удобным доступом к данным.

### Свойства

```csharp
public string Method { get; }          // HTTP метод: GET, POST и т.д.
public string Url { get; }             // Полный URL запроса
public string StatusCode { get; }      // HTTP код статуса: 200, 404 и т.д.
public string RequestHeaders { get; }  // Все заголовки запроса
public string RequestCookies { get; }  // Cookies запроса
public string RequestBody { get; }     // Тело запроса
public string ResponseHeaders { get; } // Все заголовки ответа
public string ResponseCookies { get; } // Cookies ответа
public string ResponseBody { get; }    // Содержимое ответа
public string ResponseContentType { get; } // Заголовок Content-Type
```

**Пример**:
```csharp
var element = traffic.FindTrafficElement("api/user");
project.log($"Метод: {element.Method}");
project.log($"Статус: {element.StatusCode}");
project.log($"Ответ: {element.ResponseBody}");
```

---

### ParseResponseBodyAsJson()

```csharp
public TrafficElement ParseResponseBodyAsJson()
```

**Назначение**: Парсит тело ответа как JSON в project.Json (цепной вызов).

**Пример**:
```csharp
var element = traffic.FindTrafficElement("api/user")
                     .ParseResponseBodyAsJson();

string userId = project.Json.SelectToken("$.data.id").ToString();
```

**Детали**:
- Парсит ResponseBody JSON в project.Json
- Цепной вызов (возвращает this)
- Выбрасывает исключение, если JSON некорректен

---

### ParseRequestBodyAsJson()

```csharp
public TrafficElement ParseRequestBodyAsJson()
```

**Назначение**: Парсит тело запроса как JSON в project.Json (цепной вызов).

**Пример**:
```csharp
var element = traffic.FindTrafficElement("api/submit")
                     .ParseRequestBodyAsJson();

string username = project.Json.SelectToken("$.username").ToString();
```

**Детали**:
- Парсит RequestBody JSON в project.Json
- Цепной вызов (возвращает this)
- Полезно для проверки данных, отправленных в API

---

### GetRequestHeader()

```csharp
public string GetRequestHeader(string headerName)
```

**Назначение**: Получает конкретный заголовок запроса из элемента.

**Пример**:
```csharp
var element = traffic.FindTrafficElement("api/data");
string auth = element.GetRequestHeader("Authorization");
```

**Детали**:
- `headerName` - Имя заголовка (регистронезависимо)
- Возвращает значение заголовка или null

---

### GetResponseHeader()

```csharp
public string GetResponseHeader(string headerName)
```

**Назначение**: Получает конкретный заголовок ответа из элемента.

**Пример**:
```csharp
var element = traffic.FindTrafficElement("api/data");
string contentType = element.GetResponseHeader("content-type");
```

**Детали**:
- `headerName` - Имя заголовка (регистронезависимо)
- Возвращает значение заголовка или null

---

## Методы расширения

### SaveRequestHeadersToVariable()

```csharp
public static void SaveRequestHeadersToVariable(this IZennoPosterProjectModel project, Instance instance, string url, bool strict = false, bool log = false)
```

**Назначение**: Извлекает заголовки запроса и сохраняет в переменную проекта "headers".

**Пример**:
```csharp
project.SaveRequestHeadersToVariable(instance, "api/protected", log: true);
string headers = project.Var("headers");
```

**Детали**:
- Находит трафик для URL
- Удаляет псевдо-заголовки HTTP/2 (начинающиеся с ":")
- Сохраняет очищенные заголовки в переменную "headers"
- Логирует результат, если log=true

---

### CollectRequestHeaders()

```csharp
public static void CollectRequestHeaders(this IZennoPosterProjectModel project, Instance instance, string url, bool strict = false, bool saveToVariable = true, bool saveToDatabase = true, bool log = false)
```

**Назначение**: Собирает заголовки запроса и сохраняет в переменную и/или базу данных.

**Пример**:
```csharp
// Сохранить и в переменную, и в базу данных
project.CollectRequestHeaders(instance, "api/auth");

// Сохранить только в переменную
project.CollectRequestHeaders(instance, "api/data", saveToDatabase: false);
```

**Детали**:
- `url` - URL для поиска в трафике
- `strict` - Точное совпадение URL (по умолчанию: false)
- `saveToVariable` - Сохранить в переменную "headers" (по умолчанию: true)
- `saveToDatabase` - Сохранить в базу данных (по умолчанию: true)
- `log` - Включить логирование (по умолчанию: false)
- Удаляет псевдо-заголовки HTTP/2
- Сохраняет очищенный текст заголовков
