# Документация класса Mexc

## Обзор
Класс `Mexc` предоставляет полную интеграцию с API биржи MEXC для спотовой торговли, выводов, депозитов, переводов и управления аккаунтом.

---

## Конструктор

### `Mexc(IZennoPosterProjectModel project, bool log = false)`

**Назначение:** Инициализирует клиент MEXC API с учетными данными из базы данных.

**Пример:**
```csharp
var mexc = new Mexc(project, log: true);
var balance = mexc.GetSpotBalance();
```

**Разбор:**
```csharp
var mexc = new Mexc(
    project,  // IZennoPosterProjectModel - экземпляр проекта
    true      // bool - включить логирование
);
// Примечание: API ключ, секрет и прокси загружаются из БД (таблица _api, id='mexc')
```

---

## Публичные методы

### `GetSpotBalance(bool log = false, bool toJson = false)`

**Назначение:** Получает все балансы спотового кошелька с ненулевыми суммами.

**Пример:**
```csharp
var mexc = new Mexc(project);
Dictionary<string, string> balances = mexc.GetSpotBalance(log: true);

foreach (var coin in balances)
{
    Console.WriteLine($"{coin.Key}: {coin.Value}");
}
```

**Разбор:**
```csharp
Dictionary<string, string> balances = mexc.GetSpotBalance(
    true,   // bool - включить логирование
    false   // bool - заполнить объект project.Json
);
// Возвращает: Dictionary<string, string> - актив → свободный баланс
// Выбрасывает: Exception - при ошибке API
```

---

### `GetSpotBalance(string coin)`

**Назначение:** Получает баланс для конкретной монеты.

**Пример:**
```csharp
var mexc = new Mexc(project);
string usdtBalance = mexc.GetSpotBalance("USDT");
```

**Разбор:**
```csharp
string balance = mexc.GetSpotBalance(
    "USDT"  // string - символ монеты
);
// Возвращает: string - доступный баланс или "0" если не найдено
```

---

### `Withdraw(string coin, string network, string address, string amount, string memo = "", string remark = "")`

**Назначение:** Выводит криптовалюту на внешний адрес.

**Пример:**
```csharp
var mexc = new Mexc(project);
string withdrawId = mexc.Withdraw(
    "USDT",
    "arbitrum",
    "0x742d35Cc6634C0532925a3b8D45C0532925aAB1",
    "10.5",
    "",           // Опциональное memo
    "Мой вывод"
);
```

**Разбор:**
```csharp
string withdrawalId = mexc.Withdraw(
    "USDT",                 // string - символ монеты
    "arbitrum",             // string - название сети
    "0x742d35Cc...",       // string - адрес получателя
    "10.5",                 // string - сумма вывода
    "",                     // string - опциональное memo/tag
    "Примечание вывода"     // string - опциональное примечание
);
// Возвращает: string - ID вывода
// Выбрасывает: Exception - если вывод не удался
// Примечание: Использует InvariantCulture для форматирования десятичных чисел
```

---

### `GetWithdrawHistory(int limit = 1000)`

**Назначение:** Получает историю выводов.

**Пример:**
```csharp
var mexc = new Mexc(project);
List<string> history = mexc.GetWithdrawHistory(50);
```

**Разбор:**
```csharp
List<string> withdrawals = mexc.GetWithdrawHistory(
    100  // int - максимальное количество записей для получения
);
// Возвращает: List<string> - формат: "id:coin:amount:status:address:network"
```

---

### `GetWithdrawHistory(string searchId)`

**Назначение:** Ищет конкретный вывод по ID.

**Пример:**
```csharp
var mexc = new Mexc(project);
string withdrawal = mexc.GetWithdrawHistory("1234567890");
```

**Разбор:**
```csharp
string matchingWithdrawal = mexc.GetWithdrawHistory(
    "1234567890"  // string - ID вывода для поиска
);
// Возвращает: string - запись вывода или "NoIdFound: {searchId}"
```

---

### `GetDepositHistory(string coin = "", int limit = 1000)`

**Назначение:** Получает историю депозитов.

**Пример:**
```csharp
var mexc = new Mexc(project);

// Все депозиты
List<string> allDeposits = mexc.GetDepositHistory();

// Конкретная монета
List<string> usdtDeposits = mexc.GetDepositHistory("USDT", 50);
```

**Разбор:**
```csharp
List<string> deposits = mexc.GetDepositHistory(
    "USDT",  // string - опциональный фильтр по монете
    100      // int - макс. количество записей
);
// Возвращает: List<string> - формат: "txId:coin:amount:status:address:network"
```

---

### `GetDepositAddress(string coin, string network = "")`

**Назначение:** Получает адрес депозита для конкретной монеты и сети.

**Пример:**
```csharp
var mexc = new Mexc(project);
string address = mexc.GetDepositAddress("USDT", "arbitrum");
// Возвращает: "0x742d35Cc..." или "0x742d35Cc...:memo" если требуется memo
```

**Разбор:**
```csharp
string depositAddress = mexc.GetDepositAddress(
    "USDT",      // string - символ монеты
    "arbitrum"   // string - название сети
);
// Возвращает: string - адрес депозита или "address:memo" если требуется memo
// Выбрасывает: Exception - если адрес не найден
```

---

### `GetCoins()`

**Назначение:** Получает конфигурацию всех монет и заполняет project.Json.

**Пример:**
```csharp
var mexc = new Mexc(project);
mexc.GetCoins();
// Доступ через project.Json
```

**Разбор:**
```csharp
mexc.GetCoins();
// Заполняет project.Json полной конфигурацией монет
// Используйте GetSupportedCoins() для форматированного списка
```

---

### `GetSupportedCoins()`

**Назначение:** Получает список всех поддерживаемых монет и их сетей.

**Пример:**
```csharp
var mexc = new Mexc(project);
List<string> coins = mexc.GetSupportedCoins();
```

**Разбор:**
```csharp
List<string> supportedCoins = mexc.GetSupportedCoins();
// Возвращает: List<string> - формат: "монета:сеть1;сеть2;сеть3"
// Пример: "USDT:ERC20;TRC20;BEP20(BSC);ARBITRUM"
```

---

### `GetPrice<T>(string symbol)`

**Назначение:** Получает текущую рыночную цену для торговой пары.

**Пример:**
```csharp
var mexc = new Mexc(project);
decimal price = mexc.GetPrice<decimal>("BTCUSDT");
string priceStr = mexc.GetPrice<string>("ETHUSDT");
```

**Разбор:**
```csharp
T price = mexc.GetPrice<T>(
    "BTCUSDT"  // string - символ торговой пары
);
// Возвращает: T - цена как decimal или string
// Выбрасывает: Exception - если символ не найден
```

---

### `CancelWithdraw(string withdrawId)`

**Назначение:** Отменяет ожидающий вывод.

**Пример:**
```csharp
var mexc = new Mexc(project);
string result = mexc.CancelWithdraw("1234567890");
```

**Разбор:**
```csharp
string cancelResult = mexc.CancelWithdraw(
    "1234567890"  // string - ID вывода для отмены
);
// Возвращает: string - ID отмененного вывода
// Выбрасывает: Exception - если отмена не удалась
```

---

### `InternalTransfer(string asset, string amount, string fromAccountType = "SPOT", string toAccountType = "FUTURES")`

**Назначение:** Переводит средства между типами аккаунтов (SPOT/FUTURES).

**Пример:**
```csharp
var mexc = new Mexc(project);
string transferId = mexc.InternalTransfer(
    "USDT",
    "100.50",
    "SPOT",
    "FUTURES"
);
```

**Разбор:**
```csharp
string transferId = mexc.InternalTransfer(
    "USDT",     // string - актив для перевода
    "100.50",   // string - сумма
    "SPOT",     // string - тип аккаунта-источника
    "FUTURES"   // string - тип аккаунта-получателя
);
// Возвращает: string - ID перевода
// Выбрасывает: Exception - если перевод не удался
```

---

### `GetTransferHistory(string fromAccountType = "SPOT", string toAccountType = "FUTURES", int size = 10)`

**Назначение:** Получает историю внутренних переводов.

**Пример:**
```csharp
var mexc = new Mexc(project);
List<string> transfers = mexc.GetTransferHistory("SPOT", "FUTURES", 50);
```

**Разбор:**
```csharp
List<string> history = mexc.GetTransferHistory(
    "SPOT",     // string - тип аккаунта-источника
    "FUTURES",  // string - тип аккаунта-получателя
    10          // int - макс. количество записей
);
// Возвращает: List<string> - формат: "tranId:asset:amount:fromType:toType:status"
```

---

## Соответствие сетей

| Входное значение | Код сети MEXC |
|-----------------|---------------|
| arbitrum | ARBITRUM |
| ethereum | ERC20 |
| base | BASE |
| bsc | BEP20(BSC) |
| avalanche | AVAX-C |
| polygon | POLYGON |
| optimism | OP |
| trc20 | TRC20 |
| zksync | ZKSYNC |
| aptos | APTOS |

---

## Примечания

- Учетные данные API загружаются из таблицы БД `_api` с `id = 'mexc'`
- Все запросы используют аутентификацию с подписью HMAC-SHA256
- Использует InvariantCulture для парсинга/форматирования десятичных чисел
- Коды статусов депозита/вывода варьируются по типу
- Все временные метки используют Unix миллисекунды
- User-Agent из профиля проекта
- Ответы парсятся в объекты Newtonsoft.Json
