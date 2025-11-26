# Документация класса DiscordApi

## Обзор
Класс `DiscordApi` предоставляет интеграцию с Discord API для управления ролями сервера используя токены ботов.

---

## Конструктор

### `DiscordApi(IZennoPosterProjectModel project, Instance instance, bool log = false)`

**Назначение:** Инициализирует клиент Discord API.

**Пример:**
```csharp
var discord = new DiscordApi(project, instance, log: true);
bool success = discord.ManageRole(botToken, guildId, "Member", userId, true);
```

**Разбор:**
```csharp
var discord = new DiscordApi(
    project,   // IZennoPosterProjectModel - экземпляр проекта
    instance,  // Instance - экземпляр ZennoPoster
    true       // bool - включить логирование
);
```

---

## Публичные методы

### `ManageRole(string botToken, string guildId, string roleName, string userId, bool assignRole, string callerName = "")`

**Назначение:** Назначает или удаляет роль для пользователя на Discord сервере.

**Пример:**
```csharp
var discord = new DiscordApi(project, instance);

// Назначить роль
bool assigned = discord.ManageRole(
    "Bot_TOKEN_HERE",
    "123456789012345678",     // ID сервера (guild)
    "Verified",                // Название роли
    "987654321098765432",     // ID пользователя
    true                       // Назначить роль (true) или удалить (false)
);

if (assigned)
{
    Console.WriteLine("Роль успешно назначена");
}

// Удалить роль
bool removed = discord.ManageRole(
    botToken,
    guildId,
    "Member",
    userId,
    false  // Удалить роль
);
```

**Разбор:**
```csharp
bool success = discord.ManageRole(
    "Bot_TOKEN...",           // string - токен Discord бота
    "123456789012345678",     // string - ID Discord сервера (guild)
    "Verified",               // string - название роли для назначения/удаления
    "987654321098765432",     // string - ID Discord пользователя
    true,                     // bool - true=назначить, false=удалить
    "MethodName"              // string - имя вызывающего метода (автозаполняется)
);
// Возвращает: bool - true если операция успешна, false в противном случае
// Примечание: Автоматически добавляет задержки в 1 секунду между запросами
```

---

## Рабочий процесс

Метод выполняет следующие шаги:

1. **Получение ролей:** Получает все роли с сервера
2. **Поиск роли:** Ищет указанную роль по имени (без учета регистра)
3. **Проверка роли:** Проверяет существование роли
4. **Назначение/Удаление:** Использует Discord API для назначения или удаления роли
5. **Ожидание:** Добавляет задержку в 1 секунду между каждым API вызовом

---

## Обработка ошибок

Метод обрабатывает несколько сценариев ошибок:

- **Роль не найдена:** Возвращает `false` если указанное имя роли не существует
- **Сбой API:** Возвращает `false` если запрос Discord API не удался
- **Проблемы с правами:** Возвращает `false` если у бота нет прав

Все ошибки логируются с предупреждениями когда логирование включено.

---

## Используемые эндпоинты Discord API

- **GET** `/api/v10/guilds/{guildId}/roles` - Получение ролей сервера
- **PUT** `/api/v10/guilds/{guildId}/members/{userId}/roles/{roleId}` - Назначение роли
- **DELETE** `/api/v10/guilds/{guildId}/members/{userId}/roles/{roleId}` - Удаление роли

---

## Требования

### Права бота

Токен бота должен иметь следующие права:
- **Manage Roles** - Требуется для назначения/удаления ролей
- **View Server Members** - Требуется для доступа к информации о участниках

### Токен бота

Формат: `Bot YOUR_BOT_TOKEN_HERE`

Метод автоматически добавляет префикс "Bot " в заголовке Authorization.

---

## Примечания

- Использует Discord API v10
- Поиск имени роли без учета регистра
- Автоматические задержки в 1 секунду между API вызовами для соблюдения лимитов
- Кодировка UTF-8 для всех запросов
- Возвращает boolean для простой проверки успеха/неудачи
- Логирует все операции когда логирование включено
- Использует класс NetHttp для HTTP операций
