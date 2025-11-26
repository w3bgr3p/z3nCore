# Класс Disposer

Класс `Disposer` управляет завершением сессии и операциями отчётности для проектов ZennoPoster.

---

## Конструктор

### public Disposer(IZennoPosterProjectModel project, Instance instance, bool log = false)

**Назначение**: Создает новый экземпляр Disposer для управления завершением сессии и отчётностью.

**Пример**:
```csharp
// Создание экземпляра disposer с включённым логированием
var disposer = new Disposer(project, instance, log: true);

// Параметры:
// - project: Экземпляр модели проекта ZennoPoster (обязательный)
// - instance: Текущий объект Instance (обязательный)
// - log: Включить подробное логирование (опционально, по умолчанию: false)

// Конструктор инициализирует внутренние объекты Reporter и Logger
// для обработки отчётов и операций логирования
```

**Детали**:
- **project**: Интерфейс модели проекта ZennoPoster для доступа к переменным и методам проекта
- **instance**: Текущий объект экземпляра для управления браузером и сессией
- **log**: При установке в `true` включает подробное логирование всех операций disposer
- **Исключения**: Выбрасывает `ArgumentNullException`, если project или instance равны null

---

## FinishSession()

### public void FinishSession()

**Назначение**: Завершает текущую сессию, генерируя отчёты, сохраняя cookies и очищая ресурсы.

**Пример**:
```csharp
var disposer = new Disposer(project, instance, log: true);

// Завершение текущей сессии
disposer.FinishSession();

// Этот метод выполняет следующие операции:
// 1. Определяет успешность сессии проверкой переменной 'lastQuery'
// 2. Генерирует отчёты об успехе или ошибке (логи, telegram, база данных)
// 3. Сохраняет cookies браузера при использовании Chromium и установленном acc0
// 4. Логирует финальный статус сессии с затраченным временем
// 5. Очищает глобальные и локальные переменные (acc0)
// 6. Останавливает instance

// Нет возвращаемого значения
// Может выбросить исключения во время очистки, но попытается аварийно остановить instance
```

**Детали**:
- **Возвращаемое значение**: void - нет возвращаемого значения
- **Побочные эффекты**:
  - Генерирует отчёты в лог, Telegram и базу данных
  - Сохраняет cookies по пути, определённому расширением `PathCookies()`
  - Очищает глобальную переменную `acc{acc0}` и локальную переменную `acc0`
  - Останавливает instance
- **Исключения**: Перехватывает и логирует все исключения во время очистки, пытается аварийно остановить instance при необходимости

---

## ErrorReport()

### public string ErrorReport(bool toLog = true, bool toTelegram = false, bool toDb = false, bool screenshot = false)

**Назначение**: Генерирует отчёт об ошибке с опциональным логированием, уведомлением в Telegram, записью в базу данных и захватом скриншота.

**Пример**:
```csharp
var disposer = new Disposer(project, instance);

// Генерация отчёта об ошибке со всеми опциями
string errorMessage = disposer.ErrorReport(
    toLog: true,        // Записать ошибку в лог ZennoPoster
    toTelegram: true,   // Отправить уведомление в Telegram
    toDb: true,         // Записать ошибку в базу данных
    screenshot: true    // Сделать скриншот текущего состояния
);

// Возвращает: Строку с сообщением об ошибке, которое было отправлено
// Пример: "Error: Task failed - connection timeout"

// Применение: Вызывайте этот метод при возникновении ошибки в проекте
// для автоматического логирования и уведомления через настроенные каналы
```

**Детали**:
- **toLog**: При `true` записывает ошибку в лог ZennoPoster (по умолчанию: true)
- **toTelegram**: При `true` отправляет уведомление об ошибке в настроенный Telegram бот (по умолчанию: false)
- **toDb**: При `true` записывает ошибку в базу данных (по умолчанию: false)
- **screenshot**: При `true` делает и включает скриншот в отчёт об ошибке (по умолчанию: false)
- **Возвращаемое значение**: Строка, содержащая сообщение об ошибке, которое было отправлено
- **Исключения**: Внутренняя обработка исключений классом Reporter

---

## SuccessReport()

### public string SuccessReport(bool toLog = true, bool toTelegram = false, bool toDb = false, string customMessage = null)

**Назначение**: Генерирует отчёт об успехе с опциональным логированием, уведомлением в Telegram, записью в базу данных и пользовательским сообщением.

**Пример**:
```csharp
var disposer = new Disposer(project, instance);

// Генерация отчёта об успехе с пользовательским сообщением
string successMessage = disposer.SuccessReport(
    toLog: true,                     // Записать успех в лог ZennoPoster
    toTelegram: true,                // Отправить уведомление в Telegram
    toDb: true,                      // Записать успех в базу данных
    customMessage: "Задача завершена"  // Пользовательское сообщение для включения
);

// Возвращает: Строку с сообщением об успехе, которое было отправлено
// Пример: "Success: Задача завершена - аккаунт успешно обработан"

// Применение: Вызывайте этот метод при успешном завершении проекта
// для логирования результата и отправки уведомлений
```

**Детали**:
- **toLog**: При `true` записывает успех в лог ZennoPoster (по умолчанию: true)
- **toTelegram**: При `true` отправляет уведомление об успехе в настроенный Telegram бот (по умолчанию: false)
- **toDb**: При `true` записывает успех в базу данных (по умолчанию: false)
- **customMessage**: Опциональное пользовательское сообщение для включения в отчёт (по умолчанию: null)
- **Возвращаемое значение**: Строка, содержащая сообщение об успехе, которое было отправлено
- **Исключения**: Внутренняя обработка исключений классом Reporter

---

## Методы расширения

Следующие методы расширения предоставляют удобный доступ к функциональности Disposer без создания экземпляра.

### project.Finish()

### public static void Finish(this IZennoPosterProjectModel project, Instance instance)

**Назначение**: Метод расширения для быстрого завершения сессии без создания экземпляра Disposer.

**Пример**:
```csharp
// Использование метода расширения напрямую на объекте project
project.Finish(instance);

// Эквивалентно:
// new Disposer(project, instance).FinishSession();

// Это удобный метод, который создаёт Disposer внутренне
// и вызывает FinishSession() для очистки и остановки instance

// Нет возвращаемого значения
```

**Детали**:
- **project**: Экземпляр IZennoPosterProjectModel (параметр this)
- **instance**: Объект Instance для завершения
- **Возвращаемое значение**: void
- **Исключения**: Те же, что и в FinishSession()

---

### project.ReportError()

### public static string ReportError(this IZennoPosterProjectModel project, Instance instance, bool toLog = true, bool toTelegram = false, bool toDb = false, bool screenshot = false)

**Назначение**: Метод расширения для отчёта об ошибках без создания экземпляра Disposer.

**Пример**:
```csharp
// Использование метода расширения напрямую на объекте project
string errorMsg = project.ReportError(
    instance,
    toLog: true,
    toTelegram: true,
    toDb: true,
    screenshot: true
);

// Эквивалентно:
// new Disposer(project, instance).ErrorReport(true, true, true, true);

// Возвращает: Строку с сообщением об ошибке
```

**Детали**:
- **project**: Экземпляр IZennoPosterProjectModel (параметр this)
- **instance**: Объект Instance для контекста
- **toLog**, **toTelegram**, **toDb**, **screenshot**: Те же, что и в методе ErrorReport()
- **Возвращаемое значение**: Строка, содержащая сообщение об ошибке
- **Исключения**: Те же, что и в ErrorReport()

---

### project.ReportSuccess()

### public static string ReportSuccess(this IZennoPosterProjectModel project, Instance instance, bool toLog = true, bool toTelegram = false, bool toDb = false, string customMessage = null)

**Назначение**: Метод расширения для отчёта об успехе без создания экземпляра Disposer.

**Пример**:
```csharp
// Использование метода расширения напрямую на объекте project
string successMsg = project.ReportSuccess(
    instance,
    toLog: true,
    toTelegram: true,
    toDb: true,
    customMessage: "Верификация аккаунта завершена"
);

// Эквивалентно:
// new Disposer(project, instance).SuccessReport(true, true, true, "Верификация аккаунта завершена");

// Возвращает: Строку с сообщением об успехе
```

**Детали**:
- **project**: Экземпляр IZennoPosterProjectModel (параметр this)
- **instance**: Объект Instance для контекста
- **toLog**, **toTelegram**, **toDb**: Те же, что и в методе SuccessReport()
- **customMessage**: Опциональное пользовательское сообщение для включения
- **Возвращаемое значение**: Строка, содержащая сообщение об успехе
- **Исключения**: Те же, что и в SuccessReport()
