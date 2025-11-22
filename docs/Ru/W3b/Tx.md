# Tx

Высокоуровневый класс для выполнения блокчейн транзакций с автоматической оценкой газа и обработкой ошибок.

---

## Tx (Конструктор)

### Назначение
Инициализирует новый обработчик транзакций с поддержкой логирования.

### Пример
```csharp
var tx = new Tx(project, log: true);
```

### Разбор
```csharp
public Tx(
    IZennoPosterProjectModel project,    // Экземпляр проекта
    bool log = false                     // Включить логирование
)
// Инициализирует обработчик транзакций с логгером
```

---

## Read

### Назначение
Вызывает функцию контракта только для чтения (view/pure функции).

### Пример
```csharp
var tx = new Tx(project);
string abi = "[{...}]";
string result = tx.Read(
    contract: "0x123...",
    functionName: "balanceOf",
    abi: abi,
    rpc: "https://eth.llamarpc.com",
    parameters: new object[] { "0xUserAddress" }
);
```

### Разбор
```csharp
public string Read(
    string contract,              // Адрес контракта
    string functionName,          // Имя функции
    string abi,                   // ABI контракта в JSON
    string rpc,                   // URL RPC эндпоинта
    params object[] parameters    // Параметры функции
)
// Возвращает: Результат функции как строку
// Не требует газа или подписи
// Выбрасывает: Exception при ошибках RPC
```

---

## ReadErc20Balance

### Назначение
Читает баланс токена ERC20 (удобный метод).

### Пример
```csharp
var tx = new Tx(project);
BigInteger balance = tx.ReadErc20Balance(
    tokenContract: "0xdac17f958d2ee523a2206206994597c13d831ec7",
    ownerAddress: "0x123...",
    rpc: "https://eth.llamarpc.com"
);
```

### Разбор
```csharp
public BigInteger ReadErc20Balance(
    string tokenContract,    // Адрес контракта токена ERC20
    string ownerAddress,     // Адрес владельца
    string rpc              // URL RPC эндпоинта
)
// Возвращает: Баланс как BigInteger (сырые единицы токена)
// Вызывает функцию balanceOf(address)
```

---

## ReadErc20Allowance

### Назначение
Читает разрешение (allowance) токена ERC20 для spender'а.

### Пример
```csharp
var tx = new Tx(project);
BigInteger allowance = tx.ReadErc20Allowance(
    tokenContract: "0xdac17f958d2ee523a2206206994597c13d831ec7",
    ownerAddress: "0x123...",
    spenderAddress: "0x456...",
    rpc: "https://eth.llamarpc.com"
);
```

### Разбор
```csharp
public BigInteger ReadErc20Allowance(
    string tokenContract,    // Адрес контракта токена ERC20
    string ownerAddress,     // Адрес владельца токена
    string spenderAddress,   // Адрес spender'а
    string rpc              // URL RPC эндпоинта
)
// Возвращает: Allowance как BigInteger
// Вызывает функцию allowance(owner, spender)
```

---

## SendTx

### Назначение
Отправляет транзакцию с автоматической оценкой газа и подписью.

### Пример
```csharp
var tx = new Tx(project, log: true);
string hash = tx.SendTx(
    chainRpc: "https://eth.llamarpc.com",
    contractAddress: "0x123...",
    encodedData: "0xa9059cbb000...",
    value: 0,
    walletKey: null,  // Использует DbKey("evm") если null
    txType: 2,        // 0 = legacy, 2 = EIP-1559
    speedup: 100      // 100 = нормально, 110 = на 10% быстрее
);
```

### Разбор
```csharp
public string SendTx(
    string chainRpc,           // URL RPC эндпоинта
    string contractAddress,    // Адрес контракта или получателя
    string encodedData,        // Закодированные данные транзакции (0x для нативных переводов)
    object value,             // Отправляемое значение (поддерживает множество типов)
    string walletKey,         // Приватный ключ (null = использовать DbKey("evm"))
    int txType = 2,          // 0 = legacy, 2 = EIP-1559
    int speedup = 1,         // Множитель цены газа в процентах
    bool debug = false       // Включить отладочное логирование
)
// Возвращает: Хэш транзакции
// Автоматически оценивает лимит газа и цену газа
// Типы value: string (hex), BigInteger, HexBigInteger, decimal, int, long, double, float
// Выбрасывает: Exception с детальными сообщениями об ошибках
```

---

## Approve

### Назначение
Одобряет allowance токена ERC20 для spender'а.

### Пример
```csharp
var tx = new Tx(project, log: true);

// Одобрить конкретную сумму
string hash1 = tx.Approve(
    contractAddress: "0xTokenContract",
    spender: "0xSpenderAddress",
    amount: "1000000000000000000", // 1 токен с 18 десятичными знаками
    rpc: "https://eth.llamarpc.com"
);

// Одобрить максимум
string hash2 = tx.Approve("0xToken", "0xSpender", "max", rpc);

// Отменить одобрение
string hash3 = tx.Approve("0xToken", "0xSpender", "cancel", rpc);
```

### Разбор
```csharp
public string Approve(
    string contractAddress,    // Адрес контракта токена ERC20
    string spender,           // Адрес spender'а
    string amount,            // Сумма в сырых единицах, или "max"/"cancel"
    string rpc,               // URL RPC эндпоинта
    bool debug = false        // Включить отладочное логирование
)
// Возвращает: Хэш транзакции
// Специальные значения: "max" = uint256.max, "cancel" = 0
// Устанавливает: project.Variables["blockchainHash"]
```

---

## Wrap

### Назначение
Оборачивает нативные токены (ETH -> WETH, BNB -> WBNB и т.д.).

### Пример
```csharp
var tx = new Tx(project, log: true);
string hash = tx.Wrap(
    contract: "0xC02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2", // WETH
    value: 0.1m, // 0.1 ETH
    rpc: "https://eth.llamarpc.com"
);
```

### Разбор
```csharp
public string Wrap(
    string contract,     // Контракт обернутого токена (WETH, WBNB и т.д.)
    decimal value,      // Количество нативных токенов для оборачивания
    string rpc,         // URL RPC эндпоинта
    bool debug = false  // Включить отладочное логирование
)
// Возвращает: Хэш транзакции
// Вызывает функцию deposit() на контракте обернутого токена
// Устанавливает: project.Variables["blockchainHash"]
```

---

## SendNative

### Назначение
Отправляет нативные токены (ETH, BNB, MATIC и т.д.) на адрес.

### Пример
```csharp
var tx = new Tx(project, log: true);
string hash = tx.SendNative(
    to: "0x456...",
    amount: 0.01m,
    rpc: "https://eth.llamarpc.com"
);
```

### Разбор
```csharp
public string SendNative(
    string to,           // Адрес получателя
    decimal amount,      // Сумма в нативных токенах (например, ETH)
    string rpc,         // URL RPC эндпоинта
    bool debug = false  // Включить отладочное логирование
)
// Возвращает: Хэш транзакции
// Простой перевод значения без данных
// Устанавливает: project.Variables["blockchainHash"]
```

---

## SendErc20

### Назначение
Отправляет токены ERC20 на адрес.

### Пример
```csharp
var tx = new Tx(project, log: true);
string hash = tx.SendErc20(
    contract: "0xdac17f958d2ee523a2206206994597c13d831ec7", // USDT
    to: "0x456...",
    amount: 10.5m, // 10.5 токенов (будет конвертировано в сырые единицы)
    rpc: "https://eth.llamarpc.com"
);
```

### Разбор
```csharp
public string SendErc20(
    string contract,     // Адрес контракта токена ERC20
    string to,          // Адрес получателя
    decimal amount,     // Сумма в единицах токена (не в сырых единицах)
    string rpc,         // URL RPC эндпоинта
    bool debug = false  // Включить отладочное логирование
)
// Возвращает: Хэш транзакции
// Автоматически конвертирует сумму в сырые единицы (amount * 10^18)
// Вызывает функцию transfer(address, uint256)
// Устанавливает: project.Variables["blockchainHash"]
```

---

## SendErc721

### Назначение
Отправляет NFT ERC721 на адрес.

### Пример
```csharp
var tx = new Tx(project, log: true);
string hash = tx.SendErc721(
    contract: "0xbc4ca0eda7647a8ab7c2061c2e118a18a936f13d", // BAYC
    to: "0x456...",
    tokenId: new BigInteger(1234),
    rpc: "https://eth.llamarpc.com"
);
```

### Разбор
```csharp
public string SendErc721(
    string contract,      // Адрес контракта ERC721
    string to,           // Адрес получателя
    BigInteger tokenId,  // ID токена NFT
    string rpc,          // URL RPC эндпоинта
    bool debug = false   // Включить отладочное логирование
)
// Возвращает: Хэш транзакции
// Использует safeTransferFrom(from, to, tokenId)
// Адрес from автоматически определяется из ключа кошелька
// Устанавливает: project.Variables["blockchainHash"]
```
