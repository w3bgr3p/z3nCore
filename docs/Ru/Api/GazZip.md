# Документация класса GazZip

## Обзор
Класс `GazZip` предоставляет интеграцию с сервисом моста GazZip для межсетевой заправки газом используя EVM кошельки.

---

## Конструктор

### `GazZip(IZennoPosterProjectModel project, string key = null, bool log = false)`

**Назначение:** Инициализирует клиент GazZip для заправки.

**Пример:**
```csharp
var gazzip = new GazZip(project, log: true);
string txHash = gazzip.Refuel("sepolia", 0.001m, "https://rpc.eth.com");
```

**Разбор:**
```csharp
var gazzip = new GazZip(
    project,  // IZennoPosterProjectModel - экземпляр проекта
    null,     // string - опциональный приватный ключ EVM (загружается из БД если null)
    true      // bool - включить логирование
);
```

---

## Публичные методы

### `Refuel(string chainTo, decimal value, string rpc, bool log = false)`

**Назначение:** Заправляет конкретную целевую сеть из исходной сети.

**Пример:**
```csharp
var gazzip = new GazZip(project);
string txHash = gazzip.Refuel(
    "sepolia",                      // Целевая сеть
    0.001m,                         // Сумма в ETH
    "https://rpc.ethereum.org",     // Исходный RPC
    log: true
);
```

**Разбор:**
```csharp
string transactionHash = gazzip.Refuel(
    "sepolia",              // string - название целевой сети или hex ID
    0.001m,                 // decimal - сумма для отправки (в нативном токене)
    "https://rpc.eth...",   // string - URL RPC исходной сети
    true                    // bool - включить логирование
);
// Возвращает: string - хеш транзакции
// Выбрасывает: Exception - если баланс недостаточен или транзакция не удалась
// Примечание: Проверяет баланс перед отправкой (требуется value + 0.00005 ETH комиссия)
```

---

### `Refuel(string chainTo, decimal value, string[] ChainsFrom = null, bool log = false)`

**Назначение:** Автоматически находит сеть с достаточным балансом и заправляет.

**Пример:**
```csharp
var gazzip = new GazZip(project);
string[] sourceChains = { "ethereum", "arbitrum", "optimism", "base" };

string txHash = gazzip.Refuel(
    "sepolia",
    0.001m,
    sourceChains,
    log: true
);
```

**Разбор:**
```csharp
string transactionHash = gazzip.Refuel(
    "sepolia",                           // string - целевая сеть
    0.001m,                              // decimal - сумма
    new[] { "ethereum", "arbitrum" },    // string[] - сети для проверки
    true                                 // bool - логирование
);
// Возвращает: string - хеш транзакции из выбранной исходной сети
// Выбрасывает: Exception - если ни одна сеть не имеет достаточного баланса
// Примечание: Проходит по сетям пока не найдет с балансом > amount
```

---

## Поддерживаемые сети

| Название сети | Hex ID |
|--------------|--------|
| ethereum | 0x0100ff |
| sepolia | 0x010066 |
| soneum | 0x01019e |
| bsc | 0x01000e |
| gravity | 0x0100f0 |
| zero | 0x010169 |
| opbnb | 0x01003a |

Вы также можете предоставить hex ID напрямую, если сеть отсутствует в маппинге.

---

## Детали контракта

- **Адрес контракта:** `0x391E7C679d29bD940d63be94AD22A25d25b5A604`
- **Метод:** Прямой перевод ETH с ID сети в поле data
- **Множители газа:** Базовая комиссия × 2, Приоритетная комиссия × 3
- **Кодировка данных:** Hex ID целевой сети (например, "0x010066")

---

## Поток транзакции

1. **Валидация:** Проверяет, имеет ли исходная сеть достаточный баланс
2. **Кодировка:** Кодирует ID целевой сети как данные транзакции
3. **Выполнение:** Отправляет транзакцию через класс `Tx`
4. **Подтверждение:** Ждет подтверждения транзакции
5. **Возврат:** Возвращает хеш транзакции

---

## Примечания

- Приватный ключ EVM загружается из БД если не предоставлен в конструкторе
- Автоматически проверяет нативный баланс перед отправкой
- Использует `InvariantCulture` для форматирования десятичных чисел
- Хеш транзакции сохраняется в `project.Var("blockchainHash")`
- Ждет подтверждения транзакции используя `W3bTools.WaitTx()`
- Требует минимальный баланс (value + 0.00005 ETH) на исходной сети
- Поддерживает любую EVM-совместимую сеть через RPC URL
