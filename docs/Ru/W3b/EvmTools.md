# EvmTools

Инструменты для взаимодействия с EVM-совместимыми блокчейнами (Ethereum, BSC, Polygon и т.д.).

---

## WaitTx

### Назначение
Ожидает подтверждения транзакции в блокчейне.

### Пример
```csharp
var evmTools = new EvmTools();
string rpc = "https://eth.llamarpc.com";
string hash = "0xabc123...";
bool success = await evmTools.WaitTx(rpc, hash, deadline: 120, log: true);
if (success) Console.WriteLine("Transaction succeeded!");
```

### Разбор
```csharp
public async Task<bool> WaitTx(
    string rpc,          // URL RPC эндпоинта
    string hash,         // Хэш транзакции
    int deadline = 60,   // Таймаут в секундах
    string proxy = "",   // Опциональный прокси "user:pass:host:port"
    bool log = false     // Включить логирование в консоль
)
// Возвращает: true если транзакция успешна (status = 1), false если провалена
// Выбрасывает: Exception при таймауте
// Опрашивает каждые 2-3 секунды до подтверждения или таймаута
```

---

## WaitTxExtended

### Назначение
Ожидает транзакцию с детальным логированием (показывает pending состояние, информацию о газе, nonce).

### Пример
```csharp
var evmTools = new EvmTools();
bool success = await evmTools.WaitTxExtended(rpc, hash, deadline: 180, log: true);
```

### Разбор
```csharp
public async Task<bool> WaitTxExtended(
    string rpc,          // URL RPC эндпоинта
    string hash,         // Хэш транзакции
    int deadline = 60,   // Таймаут в секундах
    string proxy = "",   // Опциональный прокси
    bool log = false     // Включить детальное логирование
)
// Возвращает: true если успешна, false если провалена
// Дополнительное логирование: показывает pending статус, gasLimit, gasPrice, nonce, value
// Полезно для отладки застрявших транзакций
```

---

## Native

### Назначение
Получает баланс нативного токена (ETH, BNB, MATIC и т.д.) для адреса.

### Пример
```csharp
var evmTools = new EvmTools();
string hexBalance = await evmTools.Native(rpc, "0x123...");
BigInteger weiBalance = BigInteger.Parse(hexBalance, NumberStyles.AllowHexSpecifier);
```

### Разбор
```csharp
public async Task<string> Native(
    string rpc,       // URL RPC эндпоинта
    string address    // Адрес кошелька (0x...)
)
// Возвращает: Баланс в hex формате (без префикса 0x)
// Для конвертации в decimal: Используйте BigInteger.Parse с AllowHexSpecifier
```

---

## Erc20

### Назначение
Получает баланс токена ERC20 для адреса.

### Пример
```csharp
var evmTools = new EvmTools();
string tokenContract = "0xdac17f958d2ee523a2206206994597c13d831ec7"; // USDT
string hexBalance = await evmTools.Erc20(tokenContract, rpc, "0x123...");
```

### Разбор
```csharp
public async Task<string> Erc20(
    string tokenContract,    // Адрес контракта токена ERC20
    string rpc,             // URL RPC эндпоинта
    string address          // Адрес кошелька
)
// Возвращает: Баланс в hex формате (сырые единицы токена)
// Вызывает функцию balanceOf(address) на контракте токена
```

---

## Erc721

### Назначение
Получает количество NFT ERC721, принадлежащих адресу.

### Пример
```csharp
var evmTools = new EvmTools();
string nftContract = "0xbc4ca0eda7647a8ab7c2061c2e118a18a936f13d"; // BAYC
string hexCount = await evmTools.Erc721(nftContract, rpc, "0x123...");
```

### Разбор
```csharp
public async Task<string> Erc721(
    string tokenContract,    // Адрес контракта NFT ERC721
    string rpc,             // URL RPC эндпоинта
    string address          // Адрес кошелька
)
// Возвращает: Количество принадлежащих NFT (hex формат)
// Вызывает balanceOf(address) для ERC721
```

---

## Erc1155

### Назначение
Получает баланс токена ERC1155 для конкретного ID токена.

### Пример
```csharp
var evmTools = new EvmTools();
string hexBalance = await evmTools.Erc1155(
    tokenContract: "0x123...",
    tokenId: "1",
    rpc: rpc,
    address: "0x456..."
);
```

### Разбор
```csharp
public async Task<string> Erc1155(
    string tokenContract,    // Адрес контракта ERC1155
    string tokenId,         // ID токена (decimal формат)
    string rpc,             // URL RPC эндпоинта
    string address          // Адрес кошелька
)
// Возвращает: Баланс в hex формате
// Вызывает balanceOf(address, tokenId)
```

---

## Nonce

### Назначение
Получает счетчик транзакций (nonce) для адреса.

### Пример
```csharp
var evmTools = new EvmTools();
string nonceHex = await evmTools.Nonce(rpc, "0x123...", log: true);
int nonce = Convert.ToInt32(nonceHex, 16);
```

### Разбор
```csharp
public async Task<string> Nonce(
    string rpc,          // URL RPC эндпоинта
    string address,      // Адрес кошелька
    string proxy = "",   // Опциональный прокси
    bool log = false     // Включить логирование
)
// Возвращает: Nonce в hex формате (без префикса 0x)
// Использует eth_getTransactionCount с параметром "latest"
```

---

## ChainId

### Назначение
Получает ID сети от RPC эндпоинта.

### Пример
```csharp
var evmTools = new EvmTools();
string chainIdHex = await evmTools.ChainId(rpc);
int chainId = Convert.ToInt32(chainIdHex.Replace("0x", ""), 16);
// 1 = Ethereum, 56 = BSC, 137 = Polygon и т.д.
```

### Разбор
```csharp
public async Task<string> ChainId(
    string rpc,          // URL RPC эндпоинта
    string proxy = "",   // Опциональный прокси
    bool log = false     // Включить логирование
)
// Возвращает: ID сети в hex формате (с префиксом 0x)
// Использует метод eth_chainId
```

---

## GasPrice

### Назначение
Получает текущую цену газа из сети.

### Пример
```csharp
var evmTools = new EvmTools();
string gasPriceHex = await evmTools.GasPrice(rpc);
BigInteger gasPrice = BigInteger.Parse(gasPriceHex, NumberStyles.AllowHexSpecifier);
```

### Разбор
```csharp
public async Task<string> GasPrice(
    string rpc,          // URL RPC эндпоинта
    string proxy = "",   // Опциональный прокси
    bool log = false     // Включить логирование
)
// Возвращает: Цену газа в hex формате (wei, без префикса 0x)
// Использует метод eth_gasPrice
```
