# AptosTools

Инструменты для работы с блокчейном Aptos.

---

## GetAptBalance

### Назначение
Получает баланс нативного токена APT для указанного адреса.

### Пример
```csharp
var aptosTools = new AptosTools();
string rpc = "https://fullnode.mainnet.aptoslabs.com/v1";
string address = "0x1234...";
decimal balance = await aptosTools.GetAptBalance(rpc, address, log: true);
Console.WriteLine($"Balance: {balance} APT");
```

### Разбор
```csharp
public async Task<decimal> GetAptBalance(
    string rpc,          // URL RPC эндпоинта (по умолчанию: Aptos mainnet)
    string address,      // Адрес кошелька для проверки баланса
    string proxy = "",   // Опциональный прокси в формате "user:pass:host:port"
    bool log = false     // Включить логирование в консоль
)
// Возвращает: Баланс в APT (конвертированный из octas с 8 десятичными знаками)
// Выбрасывает: HttpRequestException при сетевых ошибках
```

---

## GetAptTokenBalance

### Назначение
Получает баланс конкретного токена (монеты) для указанного адреса на Aptos.

### Пример
```csharp
var aptosTools = new AptosTools();
string rpc = "https://fullnode.mainnet.aptoslabs.com/v1";
string address = "0x1234...";
string coinType = "0x1::aptos_coin::AptosCoin";
decimal balance = await aptosTools.GetAptTokenBalance(coinType, rpc, address, log: true);
Console.WriteLine($"Token balance: {balance}");
```

### Разбор
```csharp
public async Task<decimal> GetAptTokenBalance(
    string coinType,     // Идентификатор типа монеты (например, "0x1::aptos_coin::AptosCoin")
    string rpc,          // URL RPC эндпоинта (по умолчанию: Aptos mainnet)
    string address,      // Адрес кошелька для проверки баланса
    string proxy = "",   // Опциональный прокси в формате "user:pass:host:port"
    bool log = false     // Включить логирование в консоль
)
// Возвращает: Баланс токена (конвертированный с предполагаемыми 6 десятичными знаками)
// Выбрасывает: HttpRequestException при сетевых ошибках
```
