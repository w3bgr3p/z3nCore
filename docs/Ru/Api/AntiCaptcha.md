# Документация класса AntiCaptcha

## Обзор
Класс `AntiCaptcha` предоставляет интеграцию с сервисом Anti-Captcha для решения текстовых капч с помощью их API.

---

## Конструктор

### `AntiCaptcha(string apiKey)`

**Назначение:** Инициализирует клиент Anti-Captcha с учетными данными API.

**Пример:**
```csharp
var solver = new AntiCaptcha("ваш-api-ключ");
string result = solver.SolveCaptcha("путь/к/captcha.png");
solver.Dispose();
```

**Разбор:**
```csharp
var solver = new AntiCaptcha(
    "your-api-key"  // string - API ключ Anti-Captcha
);
// Примечание: Реализует IDisposable - используйте 'using' или вызовите Dispose()
```

---

## Публичные методы

### `SolveCaptcha(string imagePath, int numeric = 0, int minLength = 0, int maxLength = 0, bool phrase = false, bool caseSensitive = true, bool math = false)`

**Назначение:** Решает капчу из файла изображения.

**Пример:**
```csharp
using (var solver = new AntiCaptcha(apiKey))
{
    string result = solver.SolveCaptcha(
        "C:/captchas/image.png",
        numeric: 1,      // Только цифры
        minLength: 6,
        maxLength: 6
    );
    Console.WriteLine($"Капча решена: {result}");
}
```

**Разбор:**
```csharp
string captchaText = solver.SolveCaptcha(
    "путь/к/image.png",   // string - полный путь к файлу капчи
    0,                     // int - 0=любые, 1=только цифры, 2=только буквы
    4,                     // int - минимальная длина текста (0=без ограничений)
    8,                     // int - максимальная длина текста (0=без ограничений)
    false,                 // bool - текст содержит несколько слов
    true,                  // bool - учитывать регистр
    false                  // bool - требуется математическое вычисление
);
// Возвращает: string - решенный текст капчи
// Выбрасывает: Exception - при ошибке API или сбое создания/получения задачи
// Примечание: Ожидает решения до 3 минут (60 попыток × 3с задержка)
```

---

### `SolveCaptchaFromBase64(string base64Image, int numeric = 0, int minLength = 0, int maxLength = 0, bool phrase = false, bool caseSensitive = true, bool math = false)`

**Назначение:** Решает капчу из строки изображения в base64 (синхронно).

**Пример:**
```csharp
using (var solver = new AntiCaptcha(apiKey))
{
    string base64 = Convert.ToBase64String(File.ReadAllBytes("captcha.png"));
    string result = solver.SolveCaptchaFromBase64(
        base64,
        numeric: 1,
        minLength: 6
    );
}
```

**Разбор:**
```csharp
string captchaText = solver.SolveCaptchaFromBase64(
    "iVBORw0KGgoAAAANS...",  // string - изображение в кодировке base64
    1,                        // int - 0=любые, 1=только цифры, 2=только буквы
    0,                        // int - минимальная длина
    0,                        // int - максимальная длина
    false,                    // bool - несколько слов
    true,                     // bool - учитывать регистр
    false                     // bool - математическое вычисление
);
// Возвращает: string - решенный текст капчи
// Выбрасывает: Exception - при ошибке API или таймауте
```

---

### `SolveCaptchaFromBase64Async(string base64Image, int numeric = 0, int minLength = 0, int maxLength = 0, bool phrase = false, bool caseSensitive = true, bool math = false)`

**Назначение:** Решает капчу из строки изображения в base64 (асинхронно).

**Пример:**
```csharp
using (var solver = new AntiCaptcha(apiKey))
{
    string base64 = Convert.ToBase64String(imageBytes);
    string result = await solver.SolveCaptchaFromBase64Async(
        base64,
        numeric: 0,
        caseSensitive: false
    );
}
```

**Разбор:**
```csharp
string captchaText = await solver.SolveCaptchaFromBase64Async(
    base64String,    // string - изображение в base64
    0,               // int - фильтр типа символов
    0,               // int - мин. длина
    0,               // int - макс. длина
    false,           // bool - режим фраз
    true,            // bool - учитывать регистр
    false            // bool - математический режим
);
// Возвращает: Task<string> - решенный текст капчи
// Выбрасывает: Exception - при сбое создания задачи или получения результата
```

---

### `SolveCaptchaAsync(string imagePath, int numeric = 0, int minLength = 0, int maxLength = 0, bool phrase = false, bool caseSensitive = true, bool math = false)`

**Назначение:** Решает капчу из файла изображения (асинхронно).

**Пример:**
```csharp
using (var solver = new AntiCaptcha(apiKey))
{
    string result = await solver.SolveCaptchaAsync(
        "captcha.png",
        numeric: 1
    );
}
```

**Разбор:**
```csharp
string captchaText = await solver.SolveCaptchaAsync(
    "путь/к/captcha.png",   // string - путь к файлу изображения
    1,                       // int - числовой режим
    6,                       // int - мин. длина
    6,                       // int - макс. длина
    false,                   // bool - режим фраз
    true,                    // bool - учитывать регистр
    false                    // bool - математический режим
);
// Возвращает: Task<string> - решенный текст капчи
// Примечание: Читает файл, конвертирует в base64, затем решает
```

---

### `Dispose()`

**Назначение:** Освобождает ресурсы, используемые HttpClient.

**Пример:**
```csharp
var solver = new AntiCaptcha(apiKey);
try
{
    string result = solver.SolveCaptcha("captcha.png");
}
finally
{
    solver.Dispose();
}

// Лучше: используйте 'using'
using (var solver = new AntiCaptcha(apiKey))
{
    string result = solver.SolveCaptcha("captcha.png");
}
```

**Разбор:**
```csharp
solver.Dispose();
// Освобождает ресурсы HttpClient
// Автоматически вызывается при использовании 'using'
```

---

## Методы расширения

Файл также включает методы расширения в классе `CaptchaExtensions`:

### `SolveHeWithAntiCaptcha(this HtmlElement he, IZennoPosterProjectModel project)`

**Назначение:** Решает капчу из HtmlElement, конвертируя ее в bitmap.

**Пример:**
```csharp
var captchaElement = instance.ActiveTab.FindElementByTag("img", 0);
string solution = captchaElement.SolveHeWithAntiCaptcha(project);
```

---

### `SolveCaptchaFromUrl(IZennoPosterProjectModel project, string url, string proxy = "+")`

**Назначение:** Решает капчу, загружая ее по URL (поддерживает SVG).

**Пример:**
```csharp
string solution = CaptchaExtensions.SolveCaptchaFromUrl(
    project,
    "https://example.com/captcha",
    proxy: "+"
);
```

---

## Примечания

- API ключ хранится в базе данных (таблица _api) с id='anticaptcha'
- Таймаут решения: 180 секунд (60 попыток × 3с)
- Поддерживает все стандартные форматы изображений через кодировку base64
- Таймаут HttpClient установлен на 5 минут
- Все методы используют тип задачи ImageToTextTask сервиса Anti-Captcha
