# BrowserScan

Утилиты для проверки отпечатков браузера и настройки часового пояса через browserscan.net.

## Класс: BrowserScan

### Конструктор

```csharp
public BrowserScan(IZennoPosterProjectModel project, Instance instance, bool log = false)
```

**Назначение**: Создает новый экземпляр BrowserScan для анализа отпечатков браузера.

**Пример**:
```csharp
var scanner = new BrowserScan(project, instance, log: true);
```

**Детали**:
- `project` - Модель проекта ZennoPoster
- `instance` - Экземпляр браузера для анализа
- `log` - Включить логирование (по умолчанию: false)

---

### ParseStats()

```csharp
public void ParseStats()
```

**Назначение**: Переходит на browserscan.net, парсит статистику отпечатков браузера и сохраняет их в базу данных.

**Пример**:
```csharp
var scanner = new BrowserScan(project, instance);
scanner.ParseStats(); // Сохраняет WebGL, Audio, Fonts и т.д. в таблицу _browserscan
```

**Детали**:
- Загружает browserscan.net и ждет завершения сканирования
- Извлекает данные оборудования: WebGL, WebGLReport, Audio, ClientRects, WebGPUReport
- Извлекает программные данные: Fonts, TimeZoneBasedonIP, TimeFromIP
- Сохраняет все данные в таблицу базы данных `_browserscan`
- Без параметров
- Без возвращаемого значения
- Может выбросить исключение таймаута, если загрузка страницы превышает 60 секунд

---

### GetScore()

```csharp
public string GetScore()
```

**Назначение**: Получает оценку отпечатка браузера с browserscan.net с деталями проблем, если оценка не 100%.

**Пример**:
```csharp
var scanner = new BrowserScan(project, instance);
string score = scanner.GetScore();
// Возвращает: "[100%] " или "[85%] WebGL: Suspicious; Audio: Mismatched"
```

**Детали**:
- Загружает browserscan.net и считывает оценку
- Возвращает форматированную строку: `[score%] problems`
- Если оценка 100%, возвращает `[100%] ` (без проблем)
- Если оценка < 100%, включает обнаруженные проблемы
- Возвращает строку с процентом оценки и описанием проблем

---

### FixTime()

```csharp
public void FixTime()
```

**Назначение**: Автоматически исправляет часовой пояс браузера в соответствии с геолокацией IP согласно browserscan.net.

**Пример**:
```csharp
var scanner = new BrowserScan(project, instance, log: true);
scanner.FixTime(); // Устанавливает часовой пояс в соответствии с локацией IP
```

**Детали**:
- Считывает данные часового пояса с browserscan.net
- Извлекает смещение GMT и имя часового пояса IANA
- Устанавливает часовой пояс браузера через `instance.SetTimezone()`
- Устанавливает часовой пояс IANA через `instance.SetIanaTimezone()`
- Без параметров
- Без возвращаемого значения

---

## Метод расширения: ProjectExtensions.FixTime()

```csharp
public static void FixTime(this IZennoPosterProjectModel project, Instance instance, bool log = false)
```

**Назначение**: Быстрый метод расширения для исправления часового пояса без создания экземпляра BrowserScan.

**Пример**:
```csharp
project.FixTime(instance, log: true);
```

**Детали**:
- `project` - Модель проекта ZennoPoster
- `instance` - Экземпляр браузера для исправления
- `log` - Включить логирование (по умолчанию: false)
- Переходит на browserscan.net
- Автоматически настраивает часовой пояс в соответствии с IP
- Перехватывает и логирует исключения без их выброса
