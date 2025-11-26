# Документация класса Bitget

## Обзор
Класс `Bitget` предоставляет полную интеграцию с API биржи Bitget для торговли, выводов, депозитов, переводов и управления аккаунтом.

---

## Конструктор

### `Bitget(IZennoPosterProjectModel project, bool log = false)`

**Назначение:** Инициализирует клиент Bitget API с учетными данными из базы данных.

**Пример:**
```csharp
var bitget = new Bitget(project, log: true);
var balance = bitget.GetSpotBalance();
```

**Разбор:**
```csharp
var bitget = new Bitget(
    project,  // IZennoPosterProjectModel - экземпляр проекта
    true      // bool - включить логирование
);
// Примечание: API ключ, секрет, passphrase и прокси загружаются из БД
```

---

## Публичные методы

### `GetSpotBalance(bool log = false, bool toJson = false)`

**Назначение:** Получает все балансы спотового кошелька с ненулевыми суммами.

**Пример:**
```csharp
var bitget = new Bitget(project);
Dictionary<string, string> balances = bitget.GetSpotBalance(log: true);

foreach (var coin in balances)
{
    Console.WriteLine($"{coin.Key}: {coin.Value}");
}
```

**Разбор:**
```csharp
Dictionary<string, string> balances = bitget.GetSpotBalance(
    true,   // bool - включить логирование
    false   // bool - заполнить объект project.Json
);
// Возвращает: Dictionary<string, string> - название монеты → доступный баланс
// Пример: {"USDT": "1250.50", "BTC": "0.025"}
```

---

### `GetSpotBalance(string coin)`

**Назначение:** Получает баланс для конкретной монеты.

**Пример:**
```csharp
var bitget = new Bitget(project);
string usdtBalance = bitget.GetSpotBalance("USDT");
```

**Разбор:**
```csharp
string balance = bitget.GetSpotBalance(
    "USDT"  // string - символ монеты
);
// Возвращает: string - доступный баланс или "0" если не найдено
```

---

### `Withdraw(string coin, string chain, string address, string amount, string tag = "", string remark = "", string clientOid = "")`

**Назначение:** Выводит криптовалюту на внешний адрес.

**Пример:**
```csharp
var bitget = new Bitget(project);
string orderId = bitget.Withdraw(
    "USDT",
    "arbitrum",
    "0x742d35Cc6634C0532925a3b8D45C0...AB1",
    "10.5"
);
```

**Разбор:**
```csharp
string withdrawalOrderId = bitget.Withdraw(
    "USDT",                    // string - символ монеты
    "arbitrum",                // string - название сети
    "0x742d35Cc...",          // string - адрес получателя
    "10.5",                    // string - сумма вывода
    "",                        // string - опциональное memo/tag
    "",                        // string - опциональное примечание
    ""                         // string - опциональный client order ID
);
// Возвращает: string - ID заказа на вывод
// Выбрасывает: Exception - если вывод не удался
// Примечание: Использует InvariantCulture для форматирования десятичных чисел
```

---

### `GetWithdrawHistory(int limit = 100)`

**Назначение:** Получает историю выводов.

**Пример:**
```csharp
var bitget = new Bitget(project);
List<string> history = bitget.GetWithdrawHistory(50);

foreach (string withdrawal in history)
{
    var parts = withdrawal.Split(':');
    Console.WriteLine($"Заказ: {parts[0]}, Монета: {parts[1]}, Сумма: {parts[2]}");
}
```

**Разбор:**
```csharp
List<string> withdrawals = bitget.GetWithdrawHistory(
    100  // int - максимальное количество записей для получения
);
// Возвращает: List<string> - формат: "orderId:coin:amount:status:address:chain"
```

---

### `GetWithdrawHistory(string searchId)`

**Назначение:** Ищет конкретный вывод по ID заказа.

**Пример:**
```csharp
var bitget = new Bitget(project);
string withdrawal = bitget.GetWithdrawHistory("1234567890");
```

**Разбор:**
```csharp
string matchingWithdrawal = bitget.GetWithdrawHistory(
    "1234567890"  // string - ID заказа для поиска
);
// Возвращает: string - запись вывода или "NoIdFound: {searchId}"
```

---

### `GetSupportedCoins()`

**Назначение:** Получает список всех поддерживаемых монет и их доступных сетей.

**Пример:**
```csharp
var bitget = new Bitget(project);
List<string> coins = bitget.GetSupportedCoins();

foreach (string coin in coins)
{
    var parts = coin.Split(':');
    Console.WriteLine($"{parts[0]}: {parts[1]}");
}
```

**Разбор:**
```csharp
List<string> supportedCoins = bitget.GetSupportedCoins();
// Возвращает: List<string> - формат: "coinName:chain1;chain2;chain3"
// Пример: "USDT:ERC20;TRC20;BSC"
```

---

### `GetPrice<T>(string symbol)`

**Назначение:** Получает текущую рыночную цену для торговой пары.

**Пример:**
```csharp
var bitget = new Bitget(project);
decimal priceDecimal = bitget.GetPrice<decimal>("BTCUSDT");
string priceString = bitget.GetPrice<string>("ETHUSDT");
```

**Разбор:**
```csharp
T price = bitget.GetPrice<T>(
    "BTCUSDT"  // string - символ торговой пары
);
// Возвращает: T - цена как decimal или string тип
// Пример: 45000.50 (decimal) или "45000.50" (string)
// Выбрасывает: Exception - при ошибке API
```

---

### `GetSubAccountsAssets()`

**Назначение:** Получает активы со всех субаккаунтов с ненулевыми балансами.

**Пример:**
```csharp
var bitget = new Bitget(project);
List<string> subAssets = bitget.GetSubAccountsAssets();

foreach (string asset in subAssets)
{
    var parts = asset.Split(':');
    Console.WriteLine($"Пользователь: {parts[0]}, Монета: {parts[1]}, Доступно: {parts[2]}");
}
```

**Разбор:**
```csharp
List<string> assets = bitget.GetSubAccountsAssets();
// Возвращает: List<string> - формат: "userId:coinName:available:frozen:locked"
```

---

### `GetAccountInfo()`

**Назначение:** Получает информацию об основном аккаунте, включая ID пользователя и права.

**Пример:**
```csharp
var bitget = new Bitget(project);
Dictionary<string, object> info = bitget.GetAccountInfo();

string userId = info["userId"].ToString();
string authorities = info["authorities"].ToString();
```

**Разбор:**
```csharp
Dictionary<string, object> accountInfo = bitget.GetAccountInfo();
// Возвращает: Dictionary с ключами: userId, inviterId, parentId,
//             isTrader, isSpotTrader, authorities
```

---

### `SubTransfer(string fromUserId, string toUserId, string coin, string amount, string fromType = "spot", string toType = "spot", string clientOid = null)`

**Назначение:** Переводит средства между субаккаунтами или из суб-аккаунта в основной.

**Пример:**
```csharp
var bitget = new Bitget(project);
bitget.SubTransfer(
    "sub_user_123",
    "main_user_456",
    "USDT",
    "100.50"
);
```

**Разбор:**
```csharp
string result = bitget.SubTransfer(
    "123456",     // string - ID пользователя-источника
    "789012",     // string - ID пользователя-получателя
    "USDT",       // string - монета для перевода
    "100.50",     // string - сумма
    "spot",       // string - тип аккаунта-источника
    "spot",       // string - тип аккаунта-получателя
    null          // string - опциональный client order ID
);
// Возвращает: "Success" при успешном переводе
// Выбрасывает: Exception - если перевод не удался
```

---

### `InternalTransfer(string coin, string amount, string fromType = "spot", string toType = "spot", string clientOid = null)`

**Назначение:** Переводит средства внутри одного аккаунта между разными типами аккаунтов.

**Пример:**
```csharp
var bitget = new Bitget(project);
string transferId = bitget.InternalTransfer(
    "USDT",
    "50.25",
    "spot",
    "futures"
);
```

**Разбор:**
```csharp
string transferId = bitget.InternalTransfer(
    "USDT",      // string - монета для перевода
    "50.25",     // string - сумма
    "spot",      // string - тип аккаунта-источника
    "futures",   // string - тип аккаунта-получателя
    null         // string - опциональный client order ID
);
// Возвращает: string - ID перевода
// Выбрасывает: Exception - если перевод не удался
```

---

### `DrainSubAccounts()`

**Назначение:** Автоматически переводит все активы со всех субаккаунтов на основной аккаунт.

**Пример:**
```csharp
var bitget = new Bitget(project);
bitget.DrainSubAccounts();
// Все средства субаккаунтов будут переведены на основной аккаунт
```

**Разбор:**
```csharp
bitget.DrainSubAccounts();
// Проходит по всем субаккаунтам и переводит все положительные балансы
// Использует задержку в 1 секунду между переводами для избежания лимитов
// Логирует количество переводов и любые неудачи
```

---

### `GetDepositAddress(string coin, string chain)`

**Назначение:** Получает адрес для депозита конкретной монеты и сети.

**Пример:**
```csharp
var bitget = new Bitget(project);
string address = bitget.GetDepositAddress("USDT", "arbitrum");
// Возвращает: "0x742d35Cc..." или "0x742d35Cc...:memo123" если требуется tag/memo
```

**Разбор:**
```csharp
string depositAddress = bitget.GetDepositAddress(
    "USDT",      // string - символ монеты
    "arbitrum"   // string - название сети
);
// Возвращает: string - адрес депозита или "address:tag" если требуется memo
// Выбрасывает: Exception - при ошибке получения адреса
```

---

### `GetTransferHistory(string coinId, string fromType, string after, string before, int limit = 100)`

**Назначение:** Получает историю внутренних переводов между типами аккаунтов.

**Пример:**
```csharp
var bitget = new Bitget(project);
List<string> transfers = bitget.GetTransferHistory(
    "USDT",
    "spot",
    "0",
    "9999999999999",
    50
);
```

**Разбор:**
```csharp
List<string> history = bitget.GetTransferHistory(
    "USDT",        // string - ID монеты
    "spot",        // string - тип аккаунта-источника
    "0",           // string - начальная временная метка
    "99999999",    // string - конечная временная метка
    100            // int - макс. количество записей
);
// Возвращает: List<string> - формат: "transferId:coin:amount:fromType:toType:status:time"
```

---

## Соответствие сетей

| Входное значение | Название сети Bitget |
|-----------------|----------------------|
| arbitrum | Arbitrum One |
| ethereum | ERC20 |
| base | Base |
| bsc | BEP20 |
| avalanche | AVAX-C |
| polygon | Polygon |
| optimism | Optimism |
| trc20 | TRC20 |
| zksync | zkSync Era |
| aptos | Aptos |

---

## Примечания

- Учетные данные API загружаются из таблицы БД `_api` с `id = 'bitget'`
- Все запросы используют аутентификацию с подписью HMAC-SHA256 Base64
- Использует InvariantCulture для парсинга десятичных чисел во избежание проблем с локалью
- Включает задержки в 1 секунду между операциями с субаккаунтами для соблюдения лимитов частоты запросов
- Все временные метки используют формат Unix в миллисекундах
