# Документация класса DMail

## Обзор
Класс `DMail` предоставляет интеграцию с децентрализованным почтовым сервисом DMail для чтения, управления и аутентификации с Web3 почтовыми аккаунтами.

---

## Конструктор

### `DMail(IZennoPosterProjectModel project, string key = null, bool log = false)`

**Назначение:** Инициализирует клиент DMail с Web3 аутентификацией.

**Пример:**
```csharp
var dmail = new DMail(project, log: true);
dynamic mails = dmail.GetAll();
```

**Разбор:**
```csharp
var dmail = new DMail(
    project,  // IZennoPosterProjectModel - экземпляр проекта
    null,     // string - опциональный приватный ключ EVM (загружается из БД если null)
    true      // bool - включить логирование
);
// Примечание: Автоматически аутентифицируется используя подпись EVM кошелька
```

---

## Публичные методы

### `CheckAuth()`

**Назначение:** Проверяет и устанавливает аутентификацию с сервисом DMail.

**Пример:**
```csharp
var dmail = new DMail(project);
dmail.CheckAuth();
// Токены аутентификации теперь доступны
```

**Разбор:**
```csharp
dmail.CheckAuth();
// Проверяет наличие токенов аутентификации в переменных проекта
// Если не найдены, выполняет аутентификацию используя EVM кошелек
// Устанавливает заголовки с токенами dm-encstring и dm-pid
```

---

### `GetAll()`

**Назначение:** Получает все письма из inbox с их содержимым.

**Пример:**
```csharp
var dmail = new DMail(project);
dynamic allMails = dmail.GetAll();

foreach (var mail in allMails)
{
    Console.WriteLine($"От: {mail.dm_salias}");
    Console.WriteLine($"Тема: {mail.content.subject}");
}
```

**Разбор:**
```csharp
dynamic mailList = dmail.GetAll();
// Возвращает: dynamic - массив объектов писем с полным содержимым
// Каждое письмо содержит: dm_salias (отправитель), dm_date, dm_scid, dm_smid, content (subject, html)
// Размер страницы по умолчанию: 20 писем
```

---

### `ReadMsg(int index = 0, dynamic mail = null, bool markAsRead = true, bool trash = true)`

**Назначение:** Читает конкретное письмо и опционально отмечает как прочитанное или перемещает в корзину.

**Пример:**
```csharp
var dmail = new DMail(project);
var mailList = dmail.GetAll();
Dictionary<string, string> message = dmail.ReadMsg(
    0,           // Прочитать первое письмо
    mailList,    // Список писем
    true,        // Отметить как прочитанное
    false        // Не перемещать в корзину
);

Console.WriteLine($"Отправитель: {message["sender"]}");
Console.WriteLine($"Тема: {message["subj"]}");
Console.WriteLine($"Тело: {message["html"]}");
```

**Разбор:**
```csharp
Dictionary<string, string> email = dmail.ReadMsg(
    0,        // int - индекс письма в списке (начиная с 0)
    null,     // dynamic - список писем (автоматически загружается если null)
    true,     // bool - отметить как прочитанное после чтения
    false     // bool - переместить в корзину после чтения
);
// Возвращает: Dictionary с ключами: sender, date, subj, html, dm_scid, dm_smid
```

---

### `GetUnread(bool parse = false, string key = null)`

**Назначение:** Получает количество непрочитанных писем или конкретную статистику почты.

**Пример:**
```csharp
var dmail = new DMail(project);
string unreadCount = dmail.GetUnread(key: "mail_unread_count");
Console.WriteLine($"Непрочитанных писем: {unreadCount}");

// Или получить полный JSON
string fullStats = dmail.GetUnread();
```

**Разбор:**
```csharp
string unreadInfo = dmail.GetUnread(
    false,               // bool - парсить в project.Json
    "mail_unread_count"  // string - конкретный ключ для извлечения
);
// Возвращает: string - количество непрочитанных или полный JSON ответ
// Доступные ключи: mail_unread_count, message_unread_count, not_read_count, used_total_size
```

---

### `Trash(int index = 0, string dm_scid = null, string dm_smid = null)`

**Назначение:** Перемещает письмо в корзину.

**Пример:**
```csharp
var dmail = new DMail(project);
dmail.Trash(0);  // Переместить первое письмо в корзину

// Или переместить конкретное письмо по ID
dmail.Trash(dm_scid: "scid123", dm_smid: "smid456");
```

**Разбор:**
```csharp
dmail.Trash(
    0,           // int - индекс письма
    "scid123",   // string - опциональный конкретный dm_scid
    "smid456"    // string - опциональный конкретный dm_smid
);
// Перемещает письмо из inbox в папку корзины
// Если ID не предоставлены, получает из ReadMsg по указанному индексу
```

---

### `MarkAsRead(int index = 0, string dm_scid = null, string dm_smid = null)`

**Назначение:** Отмечает письмо как прочитанное.

**Пример:**
```csharp
var dmail = new DMail(project);
dmail.MarkAsRead(0);  // Отметить первое письмо как прочитанное

// Или отметить конкретное письмо
dmail.MarkAsRead(dm_scid: "scid123", dm_smid: "smid456");
```

**Разбор:**
```csharp
dmail.MarkAsRead(
    0,           // int - индекс письма
    "scid123",   // string - опциональный конкретный dm_scid
    "smid456"    // string - опциональный конкретный dm_smid
);
// Устанавливает флаг dm_is_read в 1 для указанного письма
```

---

## Поток аутентификации

Класс использует Web3 аутентификацию со следующим процессом:

1. **Получение Nonce:** Запрашивает уникальный nonce из DMail API
2. **Подпись сообщения:** Подписывает сообщение EVM кошельком включая:
   - Имя приложения: "dmail"
   - Адрес кошелька
   - Nonce
   - Текущую временную метку
3. **Проверка подписи:** Отправляет подпись в DMail для верификации
4. **Получение токенов:** Получает токены аутентификации (encstring и pid)
5. **Установка заголовков:** Сохраняет токены в заголовках для последующих запросов

---

## Структура письма

Каждый объект письма содержит:

```csharp
{
    "sender": "user@dmail.ai",              // Email отправителя
    "date": "2025-01-15T10:30:00Z",        // Дата письма
    "subj": "Тема письма",                  // Тема
    "html": "<p>Тело письма</p>",          // HTML содержимое
    "dm_scid": "conversation_id",           // ID разговора
    "dm_smid": "message_id"                 // ID сообщения
}
```

---

## Примечания

- Использует Ethereum подпись сообщений для аутентификации (EIP-191)
- Токены аутентификации сохраняются в переменных проекта для повторного использования
- Поддерживает автоматическую повторную аутентификацию если токены истекли
- Все запросы используют класс NetHttp для HTTP операций
- Приватный ключ загружается из базы данных если не предоставлен в конструкторе
- Интегрируется с бэкендом Internet Computer Protocol (ICP)
