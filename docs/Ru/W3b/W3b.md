# W3b

Утилитарные классы и методы расширения для проверки цен криптовалют и операций Web3.

---

# CoinGecko API

## PriceById

### Назначение
Получает текущую цену криптовалюты в USD по ID CoinGecko.

### Пример
```csharp
decimal ethPrice = CoinGecco.PriceById("ethereum");
decimal btcPrice = CoinGecco.PriceById("bitcoin");
decimal solPrice = CoinGecco.PriceById("solana");
Console.WriteLine($"ETH: ${ethPrice}");
```

### Разбор
```csharp
public static decimal PriceById(
    string CGid = "ethereum",           // ID монеты в CoinGecko
    [CallerMemberName] string callerName = ""  // Автоматически заполняется именем вызывающего для ошибок
)
// Возвращает: Текущую цену в USD как decimal
// Выбрасывает: Exception с контекстом вызывающего при ошибках
// Примечание: Использует бесплатный API CoinGecko
```

---

## PriceByTiker

### Назначение
Получает цену криптовалюты по символу тикера (ETH, BNB, SOL).

### Пример
```csharp
decimal ethPrice = CoinGecco.PriceByTiker("ETH");
decimal bnbPrice = CoinGecco.PriceByTiker("BNB");
decimal solPrice = CoinGecco.PriceByTiker("SOL");
```

### Разбор
```csharp
public static decimal PriceByTiker(
    string tiker,                              // Символ тикера (ETH, BNB, SOL)
    [CallerMemberName] string callerName = ""  // Автоматически заполняется имя вызывающего
)
// Возвращает: Текущую цену в USD
// Поддерживаемые тикеры: ETH, BNB, SOL
// Выбрасывает: Exception для неподдерживаемых тикеров
```

---

# KuCoin API

## KuPrice

### Назначение
Получает цену криптовалюты с биржи KuCoin.

### Пример
```csharp
decimal ethPrice = KuCoin.KuPrice("ETH");
decimal btcPrice = KuCoin.KuPrice("BTC");
```

### Разбор
```csharp
public static decimal KuPrice(
    string tiker = "ETH",                      // Символ тикера
    [CallerMemberName] string callerName = ""  // Автоматически заполняется имя вызывающего
)
// Возвращает: Текущую цену в USD из orderbook KuCoin
// Использует публичный API KuCoin (пара symbol-USDT)
```

---

# DexScreener API

## DSPrice

### Назначение
Получает цену токена из DexScreener (для DEX токенов).

### Пример
```csharp
string solMint = "So11111111111111111111111111111111111111112"; // SOL
decimal price = W3bTools.DSPrice(solMint, "solana");
```

### Разбор
```csharp
public static decimal DSPrice(
    string contract = "So11111111111111111111111111111111111111112",  // Адрес контракта токена
    string chain = "solana",                                         // Название сети
    [CallerMemberName] string callerName = ""                       // Автоматически заполняется имя вызывающего
)
// Возвращает: Цену токена в нативной валюте сети
// Полезно для DEX токенов, не листящихся на CEX
```

---

# Методы расширения W3bTools

## CGPrice

### Назначение
Получает цену CoinGecko (статический метод).

### Пример
```csharp
decimal price = W3bTools.CGPrice("ethereum");
```

### Разбор
```csharp
public static decimal CGPrice(
    string CGid = "ethereum",                  // ID CoinGecko
    [CallerMemberName] string callerName = ""  // Автоматически заполняется имя вызывающего
)
// Возвращает: Цену в USD из CoinGecko
// То же самое что CoinGecco.PriceById
```

---

## OKXPrice

### Назначение
Получает цену криптовалюты с биржи OKX (расширение проекта).

### Пример
```csharp
decimal ethPrice = project.OKXPrice("ETH");
decimal btcPrice = project.OKXPrice("BTC");
```

### Разбор
```csharp
public static decimal OKXPrice(
    this IZennoPosterProjectModel project,    // Экземпляр проекта
    string tiker                               // Символ тикера
)
// Возвращает: Цену в USD из OKX
// Использует интеграцию OkxApi
// Тикер автоматически конвертируется в верхний регистр
```

---

## UsdToToken

### Назначение
Конвертирует сумму USD в количество токенов используя цену в реальном времени.

### Пример
```csharp
// Конвертировать $100 в ETH используя цену KuCoin
decimal ethAmount = project.UsdToToken(100, "ETH", "KuCoin");

// Конвертировать $50 в BTC используя цену OKX
decimal btcAmount = project.UsdToToken(50, "BTC", "OKX");

// Конвертировать $200 в SOL используя CoinGecko
decimal solAmount = project.UsdToToken(200, "SOL", "CoinGecco");
```

### Разбор
```csharp
public static decimal UsdToToken(
    this IZennoPosterProjectModel project,    // Экземпляр проекта
    decimal usdAmount,                         // Сумма USD для конвертации
    string tiker,                              // Тикер токена
    string apiProvider = "KuCoin"             // Провайдер цен: "KuCoin", "OKX", "CoinGecco"
)
// Возвращает: Количество токенов (usdAmount / price)
// Поддерживает несколько провайдеров цен
// Выбрасывает: ArgumentException для неизвестного провайдера
```

---

# Статические вспомогательные методы (W3bTools)

## EvmNative

### Назначение
Получает баланс нативного EVM токена (статический метод).

### Пример
```csharp
decimal balance = W3bTools.EvmNative(
    rpc: "https://eth.llamarpc.com",
    address: "0x123..."
);
```

### Разбор
```csharp
public static decimal EvmNative(
    string rpc,       // URL RPC эндпоинта
    string address    // Адрес кошелька
)
// Возвращает: Баланс в нативных токенах (ETH, BNB и т.д.)
// Конвертирует из wei в ether (18 десятичных знаков)
```

---

## ERC20

### Назначение
Получает баланс токена ERC20 (статический метод).

### Пример
```csharp
string usdtContract = "0xdac17f958d2ee523a2206206994597c13d831ec7";
decimal balance = W3bTools.ERC20(
    tokenContract: usdtContract,
    rpc: "https://eth.llamarpc.com",
    address: "0x123...",
    tokenDecimal: "18"
);
```

### Разбор
```csharp
public static decimal ERC20(
    string tokenContract,     // Адрес контракта ERC20
    string rpc,              // URL RPC эндпоинта
    string address,          // Адрес кошелька
    string tokenDecimal = "18"  // Десятичные знаки токена (по умолчанию: 18)
)
// Возвращает: Баланс токена в decimal формате
// Автоматически конвертирует из сырых единиц
```

---

## ERC721

### Назначение
Получает количество NFT ERC721 (статический метод).

### Пример
```csharp
decimal nftCount = W3bTools.ERC721(
    tokenContract: "0xbc4ca0eda7647a8ab7c2061c2e118a18a936f13d",
    rpc: "https://eth.llamarpc.com",
    address: "0x123..."
);
```

### Разбор
```csharp
public static decimal ERC721(
    string tokenContract,    // Адрес контракта ERC721
    string rpc,             // URL RPC эндпоинта
    string address          // Адрес кошелька
)
// Возвращает: Количество принадлежащих NFT
```

---

## ERC1155

### Назначение
Получает баланс токена ERC1155 (статический метод).

### Пример
```csharp
decimal balance = W3bTools.ERC1155(
    tokenContract: "0x123...",
    tokenId: "1",
    rpc: "https://eth.llamarpc.com",
    address: "0x456..."
);
```

### Разбор
```csharp
public static decimal ERC1155(
    string tokenContract,    // Адрес контракта ERC1155
    string tokenId,         // ID токена
    string rpc,             // URL RPC эндпоинта
    string address          // Адрес кошелька
)
// Возвращает: Баланс токена для конкретного ID
```

---

## Nonce

### Назначение
Получает nonce транзакции для адреса (статический метод).

### Пример
```csharp
int nonce = W3bTools.Nonce(
    rpc: "https://eth.llamarpc.com",
    address: "0x123..."
);
```

### Разбор
```csharp
public static int Nonce(
    string rpc,       // URL RPC эндпоинта
    string address    // Адрес кошелька
)
// Возвращает: Текущий счетчик транзакций (nonce)
```

---

## ChainId

### Назначение
Получает ID сети от RPC эндпоинта (статический метод).

### Пример
```csharp
int chainId = W3bTools.ChainId("https://eth.llamarpc.com");
// Возвращает: 1 (Ethereum mainnet)
```

### Разбор
```csharp
public static int ChainId(
    string rpc    // URL RPC эндпоинта
)
// Возвращает: ID сети как integer
```

---

## GasPrice

### Назначение
Получает текущую цену газа (статический метод).

### Пример
```csharp
decimal gasPrice = W3bTools.GasPrice("https://eth.llamarpc.com");
```

### Разбор
```csharp
public static decimal GasPrice(
    string rpc    // URL RPC эндпоинта
)
// Возвращает: Текущую цену газа в wei
```

---

## WaitTx

### Назначение
Ожидает подтверждения транзакции (статический метод).

### Пример
```csharp
bool success = W3bTools.WaitTx(
    rpc: "https://eth.llamarpc.com",
    hash: "0xabc123...",
    deadline: 120,
    log: true,
    extended: true  // Использовать расширенное логирование
);
```

### Разбор
```csharp
public static bool WaitTx(
    string rpc,          // URL RPC эндпоинта
    string hash,         // Хэш транзакции
    int deadline = 60,   // Таймаут в секундах
    string proxy = "",   // Опциональный прокси
    bool log = false,    // Включить логирование
    bool extended = false // Использовать расширенное логирование (показывает pending состояние)
)
// Возвращает: true если транзакция успешна, false если провалена
// Выбрасывает: Exception при таймауте
```

---

## SolNative

### Назначение
Получает нативный баланс Solana (статический метод).

### Пример
```csharp
decimal balance = W3bTools.SolNative(
    address: "DYw8jCTfwHNRJhhmFcbXvVDTqWMEVFBX6ZKUmG5CNSKK",
    rpc: "https://api.mainnet-beta.solana.com"
);
```

### Разбор
```csharp
public static decimal SolNative(
    string address,                                 // Адрес Solana
    string rpc = "https://api.mainnet-beta.solana.com"  // URL RPC
)
// Возвращает: Баланс SOL (конвертированный из lamports)
```

---

## SPL

### Назначение
Получает баланс SPL токена на Solana (статический метод).

### Пример
```csharp
string usdcMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v";
decimal balance = W3bTools.SPL(
    tokenMint: usdcMint,
    walletAddress: "DYw8jCTfwHNRJhhmFcbXvVDTqWMEVFBX6ZKUmG5CNSKK",
    rpc: "https://api.mainnet-beta.solana.com"
);
```

### Разбор
```csharp
public static decimal SPL(
    string tokenMint,     // Адрес mint SPL токена
    string walletAddress, // Адрес кошелька
    string rpc = "https://api.mainnet-beta.solana.com"  // URL RPC
)
// Возвращает: Баланс SPL токена
```

---

## SolTxFee

### Назначение
Получает комиссию транзакции Solana (статический метод).

### Пример
```csharp
decimal fee = W3bTools.SolTxFee(
    transactionHash: "4xKsZN...",
    rpc: null,  // Использует mainnet по умолчанию
    tokenDecimal: "9"
);
```

### Разбор
```csharp
public static decimal SolTxFee(
    string transactionHash,        // Подпись транзакции
    string rpc = null,            // URL RPC (по умолчанию: mainnet)
    string tokenDecimal = "9"     // Десятичные знаки (9 для SOL)
)
// Возвращает: Комиссию транзакции в SOL
```
