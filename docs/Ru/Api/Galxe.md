# Документация класса Galxe

## Обзор
Класс `Galxe` предоставляет интеграцию с платформой Galxe (ранее Project Galaxy) для парсинга квест-задач и получения информации о пользователе используя GraphQL API.

---

## Конструктор

### `Galxe(IZennoPosterProjectModel project, Instance instance, bool log = false)`

**Назначение:** Инициализирует клиент Galxe API.

**Пример:**
```csharp
var galxe = new Galxe(project, instance, log: true);
List<HtmlElement> tasks = galxe.ParseTasks("tasksUnComplete");
```

**Разбор:**
```csharp
var galxe = new Galxe(
    project,   // IZennoPosterProjectModel - экземпляр проекта
    instance,  // Instance - экземпляр ZennoPoster для доступа к DOM
    true       // bool - включить логирование
);
```

---

## Публичные методы

### `ParseTasks(string type = "tasksUnComplete", bool log = false)`

**Назначение:** Парсит и категоризирует квест-задачи с текущей страницы.

**Пример:**
```csharp
var galxe = new Galxe(project, instance);

// Получить невыполненные задачи
List<HtmlElement> uncompletedTasks = galxe.ParseTasks("tasksUnComplete");

// Получить выполненные задачи
List<HtmlElement> completedTasks = galxe.ParseTasks("tasksComplete");

// Получить невыполненные требования
List<HtmlElement> requirements = galxe.ParseTasks("reqUnComplete");

foreach (HtmlElement task in uncompletedTasks)
{
    string taskInfo = task.InnerText.Replace("\n", " ");
    Console.WriteLine($"Задача: {taskInfo}");
}
```

**Разбор:**
```csharp
List<HtmlElement> tasks = galxe.ParseTasks(
    "tasksUnComplete",  // string - тип задач для получения
    false               // bool - включить дополнительное логирование
);
// Возвращает: List<HtmlElement> - HTML элементы соответствующие указанному типу
// Типы: tasksComplete, tasksUnComplete, reqComplete, reqUnComplete,
//       refComplete, refUnComplete
```

---

### Типы задач

| Тип | Описание |
|-----|----------|
| `tasksComplete` | Выполненные задачи "Get" |
| `tasksUnComplete` | Невыполненные задачи "Get" |
| `reqComplete` | Выполненные требования |
| `reqUnComplete` | Невыполненные требования |
| `refComplete` | Выполненные реферальные задачи |
| `refUnComplete` | Невыполненные реферальные задачи |

---

### `BasicUserInfo(string token, string address)`

**Назначение:** Получает полную информацию о пользователе из Galxe API.

**Пример:**
```csharp
var galxe = new Galxe(project, instance);
string userInfo = galxe.BasicUserInfo(
    "Bearer eyJhbGciOiJIUzI1NiIs...",
    "0x742d35Cc6634C0532925a3b8D45C0532925aAB1"
);

project.Json.FromString(userInfo);
string username = project.Json.data.addressInfo.username;
string level = project.Json.data.addressInfo.userLevel.level.name;
```

**Разбор:**
```csharp
string userData = galxe.BasicUserInfo(
    "Bearer token...",  // string - токен авторизации
    "0x742d35Cc..."     // string - адрес EVM кошелька
);
// Возвращает: string - JSON ответ с данными пользователя
// Ответ включает: username, level, exp, gold, социальные аккаунты,
//                 адреса кошельков, статистику участия
// Выбрасывает: Exception - если токен пуст или запрос API не удался
```

---

### Структура ответа User Info

```json
{
  "data": {
    "addressInfo": {
      "id": "user_id",
      "username": "username",
      "address": "0x...",
      "userLevel": {
        "level": {"name": "Explorer", "logo": "..."},
        "exp": 1500,
        "gold": 250
      },
      "twitterUserName": "twitter_handle",
      "discordUserName": "discord#1234",
      "participatedCampaigns": {"totalCount": 42}
    }
  }
}
```

---

### `GetLoyaltyPoints(string alias, string address)`

**Назначение:** Получает очки лояльности и ранг для конкретного пространства.

**Пример:**
```csharp
var galxe = new Galxe(project, instance);
string loyaltyData = galxe.GetLoyaltyPoints(
    "arbitrum",
    "0x742d35Cc6634C0532925a3b8D45C0532925aAB1"
);

project.Json.FromString(loyaltyData);
string points = project.Json.data.space.addressLoyaltyPoints.points;
string rank = project.Json.data.space.addressLoyaltyPoints.rank;

Console.WriteLine($"Очки: {points}, Ранг: {rank}");
```

**Разбор:**
```csharp
string loyaltyInfo = galxe.GetLoyaltyPoints(
    "arbitrum",      // string - алиас пространства (например, "arbitrum", "polygon")
    "0x742d35Cc..."  // string - адрес кошелька пользователя (в нижнем регистре)
);
// Возвращает: string - JSON ответ с очками лояльности и рангом
// Примечание: Адрес автоматически конвертируется в нижний регистр
```

---

## GraphQL Запросы

Класс использует эти GraphQL операции:

### BasicUserInfo Query
- Получает: Профиль пользователя, уровень, exp, социальные аккаунты, адреса кошельков
- Требует: Токен авторизации
- Эндпоинт: `https://graphigo.prd.galaxy.eco/query`

### SpaceAccessQuery
- Получает: Очки лояльности и ранг для пространства
- Публичный запрос (без авторизации)
- Эндпоинт: `https://graphigo.prd.galaxy.eco/query`

---

## Логика парсинга задач

Метод `ParseTasks` идентифицирует задачи используя структуру DOM:

1. **Поиск секций:** Находит основные контейнеры секций с классом `mb-20`
2. **Идентификация категорий:** Определяет, является ли секция Requirements/Tasks/Referral
3. **Проверка выполнения:** Использует SVG путь для идентификации выполненных задач
4. **Категоризация:** Сортирует в списки выполненных/невыполненных
5. **Возврат:** Возвращает запрошенную категорию

**Индикатор выполнения:**
```csharp
// SVG путь для выполненных задач
string dDone = "M10 19a9 9 0 1 0 0-18 9 9 0 0 0 0 18m3.924-10.576...";
```

---

## Примечания

- GraphQL API эндпоинт: `https://graphigo.prd.galaxy.eco/query`
- Формат заголовка авторизации: `Authorization: {token}` (уже включает "Bearer")
- Все GraphQL запросы используют POST метод
- Ответы автоматически парсятся в `project.Json`
- Парсинг задач требует загрузки страницы в instance
- Параметр address чувствителен к регистру для некоторых запросов (используйте нижний регистр)
- User-Agent: "Galaxy/v1"
- Поддерживает множество типов кошельков: EVM, Solana, Aptos, Starknet, Bitcoin, Sui, XRPL, TON
