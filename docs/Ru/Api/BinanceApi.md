# Документация класса BinanceApi

## Обзор
Класс `BinanceApi` предоставляет интеграцию с API биржи Binance для вывода криптовалюты и управления балансом.

---

## Конструктор

### `BinanceApi(IZennoPosterProjectModel project, bool log = false)`

**Назначение:** Инициализирует клиент Binance API с учетными данными из базы данных.

**Пример:**
```csharp
var binance = new BinanceApi(project, log: true);
var balance = binance.GetUserAsset("USDT");
```

**Разбор:**
```csharp
var binance = new BinanceApi(
    project,  // IZennoPosterProjectModel - экземпляр проекта
    true      // bool - включить логирование
);
// Примечание: API ключ, секрет и прокси загружаются из БД (таблица _api)
```

---

## Публичные методы

### `Withdraw(string coin, string network, string address, string amount)`

**Назначение:** Выводит криптовалюту на внешний адрес.

**Пример:**
```csharp
var binance = new BinanceApi(project);
string result = binance.Withdraw(
    "USDT",
    "arbitrum",
    "0x742d35Cc6634C0532925a3b8D45C0...AB1",
    "10.5"
);
```

**Разбор:**
```csharp
string withdrawalResult = binance.Withdraw(
    "USDT",                          // string - символ монеты
    "arbitrum",                      // string - сеть (arbitrum/ethereum/base/bsc/и т.д.)
    "0x742d35Cc6634C0532...",       // string - адрес получателя
    "10.5"                          // string - сумма для вывода
);
// Возвращает: string - ответ API с деталями вывода
// Примечание: Названия сетей автоматически преобразуются (например, "arbitrum" → "ARBITRUM")
// Выбрасывает: Exception - если вывод не удался
```

---

### `GetUserAsset()`

**Назначение:** Получает все активы пользователя с ненулевым балансом.

**Пример:**
```csharp
var binance = new BinanceApi(project);
Dictionary<string, string> balances = binance.GetUserAsset();

foreach (var asset in balances)
{
    Console.WriteLine($"{asset.Key}: {asset.Value}");
}
// Вывод: USDT: 1250.50
//        BTC: 0.025
```

**Разбор:**
```csharp
Dictionary<string, string> allBalances = binance.GetUserAsset();
// Возвращает: Dictionary<string, string> - ключ: символ актива, значение: свободный баланс
// Пример: {"USDT": "1250.50", "BTC": "0.025", "ETH": "1.5"}
```

---

### `GetUserAsset(string coin)`

**Назначение:** Получает баланс для конкретной монеты.

**Пример:**
```csharp
var binance = new BinanceApi(project);
string usdtBalance = binance.GetUserAsset("USDT");
Console.WriteLine($"USDT Баланс: {usdtBalance}");
```

**Разбор:**
```csharp
string balance = binance.GetUserAsset(
    "USDT"  // string - символ монеты для проверки
);
// Возвращает: string - свободный баланс для указанной монеты
// Пример: "1250.50"
```

---

### `GetWithdrawHistory()`

**Назначение:** Получает полную историю выводов.

**Пример:**
```csharp
var binance = new BinanceApi(project);
List<string> history = binance.GetWithdrawHistory();

foreach (string withdrawal in history)
{
    Console.WriteLine(withdrawal);
}
// Вывод: 123456:10.5:USDT:6
//        789012:0.001:BTC:1
```

**Разбор:**
```csharp
List<string> withdrawalHistory = binance.GetWithdrawHistory();
// Возвращает: List<string> - каждая запись в формате: "id:amount:coin:status"
// Коды статусов: 0=EmailSent, 1=Cancelled, 2=AwaitingApproval, 3=Rejected,
//                4=Processing, 5=Failure, 6=Completed
```

---

### `GetWithdrawHistory(string searchId)`

**Назначение:** Ищет конкретный вывод по ID или любому поисковому термину.

**Пример:**
```csharp
var binance = new BinanceApi(project);
string withdrawal = binance.GetWithdrawHistory("123456");
Console.WriteLine(withdrawal);
// Вывод: 123456:10.5:USDT:6
```

**Разбор:**
```csharp
string matchingWithdrawal = binance.GetWithdrawHistory(
    "123456"  // string - ID вывода или поисковый термин
);
// Возвращает: string - найденный вывод в формате "id:amount:coin:status"
// Возвращает: "NoIdFound: {searchId}" - если совпадений не найдено
```

---

## Соответствие сетей

Класс автоматически преобразует общие названия сетей в коды Binance:

| Входное значение | Код Binance |
|-----------------|-------------|
| arbitrum | ARBITRUM |
| ethereum | ETH |
| base | BASE |
| bsc | BSC |
| avalanche | AVAXC |
| polygon | MATIC |
| optimism | OPTIMISM |
| trc20 | TRC20 |
| zksync | ZkSync |
| aptos | APT |

---

## Примечания

- Учетные данные API (ключ, секрет, прокси) загружаются из таблицы БД `_api` с `id = 'binance'`
- Все запросы используют аутентификацию с подписью HMAC-SHA256
- Использует HTTP методы ZennoPoster с поддержкой контейнера cookie
- Логирование выводит детали вывода и информацию о балансе
- Коды статусов вывода: 0=EmailSent, 1=Cancelled, 2=AwaitingApproval, 3=Rejected, 4=Processing, 5=Failure, 6=Completed
