# Документация класса FirstMail

## Обзор
Класс `FirstMail` предоставляет интеграцию с API сервисом FirstMail для управления временной почтой, включая чтение писем, извлечение OTP и поиск ссылок верификации.

---

## Конструкторы

### `FirstMail(IZennoPosterProjectModel project, bool log = false)`

**Назначение:** Инициализирует клиент FirstMail с учетными данными из базы данных.

**Пример:**
```csharp
var mail = new FirstMail(project, log: true);
string message = mail.GetOne("test@example.com");
```

**Разбор:**
```csharp
var mail = new FirstMail(
    project,  // IZennoPosterProjectModel - экземпляр проекта
    true      // bool - включить логирование
);
// Примечание: API ключ, логин и пароль загружаются из БД (таблица _api)
```

---

### `FirstMail(IZennoPosterProjectModel project, string mail, string password, bool log = false)`

**Назначение:** Инициализирует клиент FirstMail с конкретными учетными данными email.

**Пример:**
```csharp
var mail = new FirstMail(
    project,
    "test@firstmail.ltd",
    "password123",
    log: true
);
```

**Разбор:**
```csharp
var mail = new FirstMail(
    project,            // IZennoPosterProjectModel - экземпляр проекта
    "test@first.com",   // string - email адрес
    "password",         // string - пароль email
    true                // bool - включить логирование
);
// Примечание: Учетные данные автоматически кодируются в URI
```

---

## Публичные методы

### `Delete(string email, bool seen = false)`

**Назначение:** Удаляет письма из почтового ящика.

**Пример:**
```csharp
var mail = new FirstMail(project);
string result = mail.Delete("test@firstmail.ltd", seen: true);
```

**Разбор:**
```csharp
string deleteResult = mail.Delete(
    "test@firstmail.ltd",  // string - email адрес
    true                    // bool - удалить только просмотренные/прочитанные письма
);
// Возвращает: string - ответ API (JSON)
// Примечание: Если seen=false, удаляет все письма
```

---

### `GetOne(string email)`

**Назначение:** Получает самое последнее письмо.

**Пример:**
```csharp
var mail = new FirstMail(project);
string result = mail.GetOne("test@firstmail.ltd");
project.Json.FromString(result);

string sender = project.Json.from;
string subject = project.Json.subject;
string text = project.Json.text;
```

**Разбор:**
```csharp
string latestEmail = mail.GetOne(
    "test@firstmail.ltd"  // string - email адрес
);
// Возвращает: string - JSON ответ с данными письма
// Поля ответа: from, to, subject, text, html, date
```

---

### `GetAll(string email)`

**Назначение:** Получает все сообщения из почтового ящика.

**Пример:**
```csharp
var mail = new FirstMail(project);
string allEmails = mail.GetAll("test@firstmail.ltd");
project.Json.FromString(allEmails);

foreach (var email in project.Json)
{
    Console.WriteLine($"Тема: {email.subject}");
}
```

**Разбор:**
```csharp
string allMessages = mail.GetAll(
    "test@firstmail.ltd"  // string - email адрес
);
// Возвращает: string - JSON массив всех email сообщений
```

---

### `GetOTP(string email)`

**Назначение:** Извлекает 6-значный OTP код из последнего письма.

**Пример:**
```csharp
var mail = new FirstMail(project);
string otp = mail.GetOTP("test@firstmail.ltd");
Console.WriteLine($"OTP Код: {otp}");  // Вывод: "123456"
```

**Разбор:**
```csharp
string otpCode = mail.GetOTP(
    "test@firstmail.ltd"  // string - email адрес
);
// Возвращает: string - 6-значный OTP код
// Ищет в порядке: subject → text → html
// Выбрасывает: Exception - если письмо не найдено или OTP не найден
```

---

### `GetLink(string email)`

**Назначение:** Извлекает первую HTTP/HTTPS ссылку из последнего письма.

**Пример:**
```csharp
var mail = new FirstMail(project);
string verificationLink = mail.GetLink("test@firstmail.ltd");
Console.WriteLine($"Ссылка: {verificationLink}");
// Вывод: https://example.com/verify?token=abc123
```

**Разбор:**
```csharp
string extractedLink = mail.GetLink(
    "test@firstmail.ltd"  // string - email адрес
);
// Возвращает: string - первый найденный валидный HTTP/HTTPS URL
// Выбрасывает: Exception - если email не совпадает или ссылка не найдена
```

---

## Методы расширения

### `Otp(this IZennoPosterProjectModel project, string source)`

**Назначение:** Метод расширения для извлечения OTP из email или оффлайн источника.

**Пример:**
```csharp
// Из email
string otp = project.Otp("test@firstmail.ltd");

// Из текста (оффлайн извлечение)
string otp = project.Otp("Ваш код 123456");
```

**Разбор:**
```csharp
string otpCode = project.Otp(
    "test@example.com"  // string - email или текст содержащий OTP
);
// Возвращает: string - 6-значный OTP код
// Примечание: Использует FirstMail если source содержит @, иначе оффлайн извлечение
```

---

## API Эндпоинты

Класс использует эти FirstMail API эндпоинты:

| Метод | Эндпоинт | Назначение |
|-------|----------|------------|
| DELETE | /v1/mail/delete | Удалить письма |
| GET | /v1/get/messages | Получить все сообщения |
| GET | /v1/mail/one | Получить последнее сообщение |

---

## Структура ответа Email

```json
{
  "from": "sender@example.com",
  "to": ["recipient@firstmail.ltd"],
  "subject": "Код верификации",
  "text": "Ваш код 123456",
  "html": "<p>Ваш код 123456</p>",
  "date": "2025-01-15T10:30:00Z"
}
```

---

## Примечания

- API ключ хранится в таблице БД `_api` с `id = 'firstmail'`
- Все учетные данные автоматически кодируются в URI
- Поддерживает конфигурацию прокси из базы данных
- Извлечение OTP использует regex шаблон `\b\d{6}\b`
- Извлечение ссылок поддерживает как http://, так и https://
- Автоматически заполняет `project.Json` для удобного доступа к данным
- Заголовки включают API ключ в формате `X-API-KEY`
