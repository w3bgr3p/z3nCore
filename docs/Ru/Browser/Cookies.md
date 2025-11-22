# Cookies

Высокопроизводительное управление cookies с экспортом/импортом JSON и Base64 хранением в базе данных.

## Класс: Cookies

### Конструктор

```csharp
public Cookies(IZennoPosterProjectModel project, Instance instance, bool log = false)
```

**Назначение**: Создает менеджер cookies для сохранения, загрузки и управления cookies браузера.

**Пример**:
```csharp
var cookies = new Cookies(project, instance, log: true);
```

**Детали**:
- `project` - Модель проекта ZennoPoster
- `instance` - Экземпляр браузера
- `log` - Включить детальное логирование (по умолчанию: false)

---

### Save()

```csharp
public void Save(string source = null, string jsonPath = null)
```

**Назначение**: Сохраняет cookies в базу данных или файл в оптимизированном формате с Base64 кодированием.

**Пример**:
```csharp
var cookies = new Cookies(project, instance);
cookies.Save("project"); // Сохранить cookies текущего домена в БД
cookies.Save("all"); // Сохранить все cookies в БД
cookies.Save("all", "C:\\cookies.json"); // Сохранить все cookies в БД и JSON файл
```

**Детали**:
- `source` - Режим сохранения: "project" (только текущий домен) или "all" (все домены)
- `jsonPath` - Опциональный путь к файлу для сохранения cookies в JSON
- Сохраняет в таблицу `_instance` с Base64 кодированием для безопасности БД
- Режим "project" фильтрует cookies по текущему домену
- Режим "all" сохраняет полный контейнер cookies
- Выбрасывает исключение при некорректном параметре source

---

### SaveAllFast()

```csharp
public void SaveAllFast(string jsonPath = null)
```

**Назначение**: Высокопроизводительный метод для сохранения всех cookies браузера (все домены).

**Пример**:
```csharp
var cookies = new Cookies(project, instance, log: true);
cookies.SaveAllFast(); // Сохранить только в БД
cookies.SaveAllFast("C:\\backup\\cookies.json"); // Сохранить в БД и файл
```

**Детали**:
- `jsonPath` - Опциональный путь к файлу для сохранения копии в JSON
- Использует нативный экспорт браузера для скорости
- Конвертирует в JSON формат
- Сохраняет в базе данных с Base64 кодированием
- Логирует метрики производительности при включенном логировании
- Автоматически удаляет временные файлы

---

### SaveProjectFast()

```csharp
public void SaveProjectFast()
```

**Назначение**: Высокопроизводительный метод для сохранения cookies только текущего домена.

**Пример**:
```csharp
var cookies = new Cookies(project, instance);
instance.Go("https://example.com");
cookies.SaveProjectFast(); // Сохраняет только cookies example.com
```

**Детали**:
- Без параметров
- Фильтрует cookies по текущему `instance.ActiveTab.MainDomain`
- Быстрее чем Save("project") для операций с конкретным доменом
- Сохраняет в базе данных с Base64 кодированием
- Без возвращаемого значения

---

### CookieFix()

```csharp
public static string CookieFix(string brokenJson)
```

**Назначение**: Исправляет распространенные проблемы форматирования JSON в строках cookies.

**Пример**:
```csharp
string broken = "{\"\"value\":\"abc\" =123}";
string fixed = Cookies.CookieFix(broken);
// Возвращает правильно отформатированный JSON
```

**Детали**:
- `brokenJson` - Некорректная JSON строка cookies
- Удаляет двойные кавычки в поле "value"
- Исправляет проблемы с пробелами вокруг "="
- Нормализует ID cookies до 1
- Возвращает исправленную JSON строку
- Возвращает сообщение об ошибке при неудаче парсинга

---

### Set()

```csharp
public void Set(string cookieSource = null, string jsonPath = null)
```

**Назначение**: Загружает cookies из базы данных или файла в браузер.

**Пример**:
```csharp
var cookies = new Cookies(project, instance);
cookies.Set(); // Загрузить из таблицы _instance (по умолчанию)
cookies.Set("dbProject"); // Загрузить из таблицы проекта
cookies.Set("fromFile", "C:\\cookies.json"); // Загрузить из JSON файла
```

**Детали**:
- `cookieSource` - Источник: "dbMain" (по умолчанию), "dbProject", или "fromFile"
- `jsonPath` - Путь к файлу при использовании источника "fromFile"
- Автоматически декодирует Base64 из базы данных
- "dbMain" читает из таблицы `_instance`
- "dbProject" читает из таблицы проекта
- "fromFile" читает из указанного JSON файла
- Применяет cookies к экземпляру браузера

---

### Get()

```csharp
public string Get(string domainFilter = "")
```

**Назначение**: Экспортирует текущие cookies браузера в JSON строку с опциональной фильтрацией по домену.

**Пример**:
```csharp
var cookies = new Cookies(project, instance, log: true);
string allCookies = cookies.Get(); // Все cookies
string googleCookies = cookies.Get("google.com"); // Только google.com
string currentDomain = cookies.Get("."); // Домен текущей страницы
```

**Детали**:
- `domainFilter` - Фильтр домена: пустая строка (все), "domain.com", или "." (текущий)
- "." автоматически разрешается в `instance.ActiveTab.MainDomain`
- Возвращает JSON массив объектов cookies
- Включает все свойства cookies: name, value, domain, path, expiry и т.д.
- Логирует детальную статистику при включенном логировании
- Возвращает JSON строку с cookies

---

### GetByJs()

```csharp
public string GetByJs(string domainFilter = "", bool log = false)
```

**Назначение**: Получает cookies через JavaScript (легче, но менее полно, чем Get()).

**Пример**:
```csharp
var cookies = new Cookies(project, instance);
string jsCookies = cookies.GetByJs();
project.log(jsCookies);
```

**Детали**:
- `domainFilter` - Фильтр домена (в JS версии пока не реализован)
- `log` - Включить логирование результата
- Использует JavaScript API `document.cookie`
- Быстрее, но получает только HTTP-доступные cookies
- Не может получить HttpOnly cookies
- Возвращает JSON строку с данными cookies

---

### SetByJs()

```csharp
public void SetByJs(string cookiesJson, bool log = false)
```

**Назначение**: Устанавливает cookies через JavaScript для текущего домена.

**Пример**:
```csharp
var cookies = new Cookies(project, instance);
string cookieJson = "[{\"name\":\"token\",\"value\":\"abc123\",\"domain\":\".example.com\"}]";
cookies.SetByJs(cookieJson);
```

**Детали**:
- `cookiesJson` - JSON массив объектов cookies
- `log` - Включить логирование
- Автоматически фильтрует cookies по текущему домену
- Использует JavaScript для установки cookies
- Устанавливает только cookies, совпадающие с текущим доменом или родительским
- Обрабатывает даты истечения cookies
- Дедуплицирует cookies (последнее вхождение побеждает)

---

## Метод расширения: ProjectExtensions.DbCookies()

```csharp
public static string DbCookies(this IZennoPosterProjectModel project)
```

**Назначение**: Извлекает и исправляет cookies из базы данных одним вызовом.

**Пример**:
```csharp
string cookies = project.DbCookies();
// Возвращает очищенный JSON cookies из базы данных
```

**Детали**:
- Метод расширения для project
- Читает cookies из таблицы `_instance`
- Автоматически декодирует Base64
- Применяет CookieFix() для очистки формата
- Возвращает готовый к использованию JSON cookies
- Обрабатывает старый не-Base64 формат для обратной совместимости
