# Документация класса OkxApi

## Обзор
Класс `OkxApi` предоставляет полную интеграцию с API биржи OKX для торговли, выводов, управления субаккаунтами и операций с активами.

---

## Конструктор

### `OkxApi(IZennoPosterProjectModel project, bool log = false)`

**Назначение:** Инициализирует клиент OKX API с учетными данными из базы данных.

**Пример:**
```csharp
var okx = new OkxApi(project, log: true);
List<string> subs = okx.OKXGetSubAccs();
```

**Разбор:**
```csharp
var okx = new OkxApi(
    project,  // IZennoPosterProjectModel - экземпляр проекта
    true      // bool - включить логирование
);
// Примечание: API ключ, секрет и passphrase загружаются из БД
```

---

## Публичные методы

### `OKXGetSubAccs(string proxy = null, bool log = false)`

**Назначение:** Получает список всех субаккаунтов.

**Пример:**
```csharp
var okx = new OkxApi(project);
List<string> subAccounts = okx.OKXGetSubAccs();

foreach (string subAcct in subAccounts)
{
    Console.WriteLine($"Субаккаунт: {subAcct}");
}
```

**Разбор:**
```csharp
List<string> subAccountsList = okx.OKXGetSubAccs(
    null,   // string - опциональный прокси
    false   // bool - включить логирование
);
// Возвращает: List<string> - названия субаккаунтов
```

---

### `OKXGetSubMax(string accName, string proxy = null, bool log = false)`

**Назначение:** Получает максимальные суммы для вывода для субаккаунта (Торговый аккаунт).

**Пример:**
```csharp
var okx = new OkxApi(project);
List<string> maxBalances = okx.OKXGetSubMax("sub_account_1");

foreach (string balance in maxBalances)
{
    var parts = balance.Split(':');
    Console.WriteLine($"{parts[0]}: {parts[1]}");  // Валюта: Макс. вывод
}
```

**Разбор:**
```csharp
List<string> balances = okx.OKXGetSubMax(
    "sub_account_1",  // string - название субаккаунта
    null,             // string - опциональный прокси
    false             // bool - логирование
);
// Возвращает: List<string> - формат: "валюта:максВывод"
// Пример: ["USDT:1250.50", "BTC:0.025"]
```

---

### `OKXGetSubTrading(string accName, string proxy = null, bool log = false)`

**Назначение:** Получает капитал торгового аккаунта для субаккаунта.

**Пример:**
```csharp
var okx = new OkxApi(project);
List<string> tradingBalances = okx.OKXGetSubTrading("sub_account_1");
```

**Разбор:**
```csharp
List<string> equity = okx.OKXGetSubTrading(
    "sub_account_1",  // string - название субаккаунта
    null,             // string - прокси
    false             // bool - логирование
);
// Возвращает: List<string> - скорректированные значения капитала
```

---

### `OKXGetSubFunding(string accName, string proxy = null, bool log = false)`

**Назначение:** Получает балансы фондового аккаунта для субаккаунта.

**Пример:**
```csharp
var okx = new OkxApi(project);
List<string> fundingBalances = okx.OKXGetSubFunding("sub_account_1");

foreach (string balance in fundingBalances)
{
    var parts = balance.Split(':');
    Console.WriteLine($"{parts[0]}: {parts[1]}");  // Валюта: Доступно
}
```

**Разбор:**
```csharp
List<string> balances = okx.OKXGetSubFunding(
    "sub_account_1",  // string - название субаккаунта
    null,             // string - прокси
    false             // bool - логирование
);
// Возвращает: List<string> - формат: "валюта:доступныйБаланс"
```

---

### `OKXGetSubsBal(string proxy = null, bool log = false)`

**Назначение:** Получает балансы со всех субаккаунтов (фондовых и торговых).

**Пример:**
```csharp
var okx = new OkxApi(project);
List<string> allBalances = okx.OKXGetSubsBal();

foreach (string balance in allBalances)
{
    var parts = balance.Split(':');
    Console.WriteLine($"Суб: {parts[0]}, Валюта: {parts[1]}, Баланс: {parts[2]}");
}
```

**Разбор:**
```csharp
List<string> allSubBalances = okx.OKXGetSubsBal(
    null,   // string - прокси
    false   // bool - логирование
);
// Возвращает: List<string> - формат: "названиеСубаккаунта:валюта:баланс"
// Проходит по всем субаккаунтам, проверяя фондовые и торговые аккаунты
```

---

### `OKXWithdraw(string toAddress, string currency, string chain, decimal amount, decimal fee, string proxy = null, bool log = false)`

**Назначение:** Выводит криптовалюту на внешний адрес.

**Пример:**
```csharp
var okx = new OkxApi(project);
okx.OKXWithdraw(
    "0x742d35Cc6634C0532925a3b8D45C0532925aAB1",
    "USDT",
    "arbitrum",
    10.5m,
    0.1m
);
```

**Разбор:**
```csharp
okx.OKXWithdraw(
    "0x742d35Cc...",   // string - адрес получателя
    "USDT",            // string - символ валюты
    "arbitrum",        // string - название сети
    10.5m,             // decimal - сумма вывода
    0.1m,              // decimal - комиссия вывода
    null,              // string - прокси
    false              // bool - логирование
);
// Примечание: Использует InvariantCulture для форматирования десятичных чисел
// Выбрасывает: Exception - если вывод не удался
```

---

### `OKXCreateSub(string subName, string accountType = "1", string proxy = null, bool log = false)`

**Назначение:** Создает новый субаккаунт.

**Пример:**
```csharp
var okx = new OkxApi(project);
okx.OKXCreateSub("new_sub_account_1", "1");
```

**Разбор:**
```csharp
okx.OKXCreateSub(
    "sub_account_name",  // string - название для нового субаккаунта
    "1",                 // string - тип аккаунта (1=стандартный)
    null,                // string - прокси
    false                // bool - логирование
);
// Выбрасывает: Exception - если создание не удалось
```

---

### `OKXDrainSubs()`

**Назначение:** Переводит все балансы со всех субаккаунтов на основной аккаунт.

**Пример:**
```csharp
var okx = new OkxApi(project);
okx.OKXDrainSubs();
// Все средства субаккаунтов переведены на основной аккаунт
```

**Разбор:**
```csharp
okx.OKXDrainSubs();
// Проходит по всем субаккаунтам
// Переводит фондовые балансы (тип "6")
// Переводит торговые балансы (тип "18")
// Включает задержку 500мс между переводами
// Логирует все операции и неудачи
```

---

### `OKXAddMaxSubs()`

**Назначение:** Создает максимальное количество разрешенных субаккаунтов.

**Пример:**
```csharp
var okx = new OkxApi(project);
okx.OKXAddMaxSubs();
// Создает субаккаунты до достижения лимита
```

**Разбор:**
```csharp
okx.OKXAddMaxSubs();
// Создает субаккаунты с автогенерируемыми названиями: "sub{i}t{timestamp}"
// Продолжает пока не выброшено исключение (достигнут лимит)
// Задержка 1.5 секунды между созданиями
```

---

### `OKXPrice<T>(string pair, string proxy = null, bool log = false)`

**Назначение:** Получает текущую рыночную цену для торговой пары.

**Пример:**
```csharp
var okx = new OkxApi(project);
decimal price = okx.OKXPrice<decimal>("BTC-USDT");
string priceStr = okx.OKXPrice<string>("ETH-USDT");
```

**Разбор:**
```csharp
T price = okx.OKXPrice<T>(
    "BTC-USDT",  // string - торговая пара (используйте разделитель дефис)
    null,        // string - прокси
    false        // bool - логирование
);
// Возвращает: T - цена как decimal или string
// Выбрасывает: Exception - если пара не найдена
```

---

## Соответствие сетей

| Входное значение | Название сети OKX |
|-----------------|-------------------|
| arbitrum | Arbitrum One |
| ethereum | ERC20 |
| base | Base |
| bsc | BSC |
| avalanche | Avalanche C-Chain |
| polygon | Polygon |
| optimism | Optimism |
| trc20 | TRC20 |
| zksync | zkSync Era |
| aptos | Aptos |

---

## Типы аккаунтов

| Тип | Описание |
|-----|----------|
| 6 | Фондовый аккаунт |
| 18 | Торговый аккаунт |
| 1 | Стандартный субаккаунт |

---

## Примечания

- Учетные данные API загружаются из таблицы БД `_api` с `id = 'okx'`
- Использует аутентификацию с подписью HMAC-SHA256 Base64
- Все временные метки используют формат UTC ISO 8601
- Passphrase требуется в дополнение к ключу/секрету
- Использует InvariantCulture для всех десятичных операций
- Переводы субаккаунтов имеют задержки 500-1500мс для соблюдения лимитов частоты запросов
- Все ответы API парсятся в `project.Json`
