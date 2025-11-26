# Класс Reporter

Обрабатывает создание, форматирование и доставку отчетов об ошибках и успешном выполнении через различные каналы (логи, Telegram, база данных).

---

## Конструктор

### `Reporter(IZennoPosterProjectModel project, Instance instance)`

**Назначение**
Инициализирует новый экземпляр Reporter с контекстом проекта и экземпляра браузера.

**Пример**
```csharp
// Создать экземпляр Reporter
var reporter = new Reporter(project, instance);
```

**Разбор**
```csharp
// Параметры:
// - project: Экземпляр IZennoPosterProjectModel для операций проекта
// - instance: Экземпляр браузера для захвата скриншотов и отслеживания URL
var reporter = new Reporter(project, instance);

// Возвращает: Экземпляр класса Reporter
// Исключения: ArgumentNullException если project или instance равен null
```

---

## Публичные методы

### `ReportError(bool toLog = true, bool toTelegram = false, bool toDb = true, bool screenshot = false)`

**Назначение**
Создает и отправляет форматированный отчет об ошибке в указанные назначения (лог, Telegram, база данных) с опциональным захватом скриншота.

**Пример**
```csharp
var reporter = new Reporter(project, instance);

// Отправить отчет об ошибке только в лог и базу данных
reporter.ReportError(toLog: true, toDb: true);

// Отправить отчет об ошибке во все каналы со скриншотом
reporter.ReportError(toLog: true, toTelegram: true, toDb: true, screenshot: true);

// Отправить отчет об ошибке только в Telegram
reporter.ReportError(toLog: false, toTelegram: true, toDb: false);
```

**Разбор**
```csharp
// Параметры:
// - toLog: Отправить отчет об ошибке в лог проекта (по умолчанию: true)
// - toTelegram: Отправить отчет об ошибке в Telegram (по умолчанию: false)
// - toDb: Обновить базу данных информацией об ошибке (по умолчанию: true)
// - screenshot: Захватить и сохранить скриншот с водяным знаком (по умолчанию: false)
string errorReport = reporter.ReportError(
    toLog: true,        // Записать в лог проекта
    toTelegram: true,   // Отправить в Telegram бот
    toDb: true,         // Обновить статус аккаунта в БД
    screenshot: true    // Сохранить скриншот в папку .failed
);

// Возвращает: string содержащий форматированный отчет для лога
// Побочные эффекты:
// - Логирует ошибку оранжевым цветом в лог проекта (если toLog=true)
// - Отправляет сообщение в Telegram с Markdown форматированием (если toTelegram=true)
// - Обновляет базу данных: status='dropped', last='{детали ошибки}' (если toDb=true)
// - Сохраняет скриншот в: {project.Path}/.failed/{projectName}/[timestamp].jpg (если screenshot=true)
// Данные об ошибке включают:
//   - ID аккаунта (переменная acc0)
//   - ID действия и комментарий
//   - Тип исключения и сообщение
//   - Трассировка стека
//   - Сообщение внутреннего исключения
//   - Текущий URL
// Исключения: Нет (предупреждения логируются, если данные об ошибке недоступны)
```

---

### `ReportSuccess(bool toLog = true, bool toTelegram = false, bool toDb = true, string customMessage = null)`

**Назначение**
Создает и отправляет форматированный отчет об успешном выполнении в указанные назначения с опциональным пользовательским сообщением.

**Пример**
```csharp
var reporter = new Reporter(project, instance);

// Отправить отчет об успехе в лог и базу данных
reporter.ReportSuccess(toLog: true, toDb: true);

// Отправить отчет об успехе с пользовательским сообщением во все каналы
reporter.ReportSuccess(
    toLog: true,
    toTelegram: true,
    toDb: true,
    customMessage: "Транзакция успешно завершена"
);

// Отправить отчет об успехе в Telegram с пользовательскими деталями
reporter.ReportSuccess(
    toLog: false,
    toTelegram: true,
    customMessage: "Баланс обновлен: 5.23 ETH"
);
```

**Разбор**
```csharp
// Параметры:
// - toLog: Отправить отчет об успехе в лог проекта (по умолчанию: true)
// - toTelegram: Отправить отчет об успехе в Telegram (по умолчанию: false)
// - toDb: Обновить базу данных информацией об успехе (по умолчанию: true)
// - customMessage: Опциональное пользовательское сообщение для включения в отчет (по умолчанию: null)
string successReport = reporter.ReportSuccess(
    toLog: true,                              // Записать в лог проекта
    toTelegram: true,                         // Отправить в Telegram
    toDb: true,                               // Обновить статус в БД
    customMessage: "Обмен выполнен: 0.5 ETH"  // Пользовательская информация
);

// Возвращает: string содержащий форматированный отчет для лога
// Побочные эффекты:
// - Логирует успех голубым цветом в лог проекта (если toLog=true)
// - Отправляет сообщение в Telegram с эмодзи успеха и Markdown (если toTelegram=true)
// - Обновляет базу данных: status='idle', last='{детали успеха}' (если toDb=true)
// Данные об успехе включают:
//   - Имя скрипта (только имя файла)
//   - ID аккаунта (переменная acc0)
//   - Последний выполненный запрос (переменная lastQuery)
//   - Затраченное время выполнения в секундах
//   - Пользовательское сообщение (если предоставлено)
//   - UTC временная метка
// Исключения: Нет
```
