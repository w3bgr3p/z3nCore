# SolTools

Инструменты для взаимодействия с блокчейном Solana.

---

## GetSolanaBalance

### Назначение
Получает баланс нативного токена SOL для адреса Solana.

### Пример
```csharp
var solTools = new SolTools();
string rpc = "https://api.mainnet-beta.solana.com";
string address = "DYw8jCTfwHNRJhhmFcbXvVDTqWMEVFBX6ZKUmG5CNSKK";
decimal balance = await solTools.GetSolanaBalance(rpc, address);
Console.WriteLine($"SOL Balance: {balance}");
```

### Разбор
```csharp
public async Task<decimal> GetSolanaBalance(
    string rpc,       // URL RPC эндпоинта Solana
    string address    // Адрес кошелька Solana (base58)
)
// Возвращает: Баланс в SOL (конвертированный из lamports, 9 десятичных знаков)
// 1 SOL = 1,000,000,000 lamports
```

---

## GetSplTokenBalance

### Назначение
Получает баланс SPL токена для адреса Solana.

### Пример
```csharp
var solTools = new SolTools();
string rpc = "https://api.mainnet-beta.solana.com";
string walletAddress = "DYw8jCTfwHNRJhhmFcbXvVDTqWMEVFBX6ZKUmG5CNSKK";
string tokenMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v"; // USDC
decimal balance = await solTools.GetSplTokenBalance(rpc, walletAddress, tokenMint);
Console.WriteLine($"Token Balance: {balance}");
```

### Разбор
```csharp
public async Task<decimal> GetSplTokenBalance(
    string rpc,            // URL RPC эндпоинта Solana
    string walletAddress,  // Адрес кошелька
    string tokenMint      // Адрес mint SPL токена
)
// Возвращает: Баланс токена (используя конфигурацию десятичных знаков токена)
// Использует метод RPC getTokenAccountsByOwner
// Возвращает 0 если токен аккаунт не найден
```

---

## SolFeeByTx

### Назначение
Получает комиссию транзакции (в SOL) для завершенной транзакции Solana.

### Пример
```csharp
var solTools = new SolTools();
string txHash = "4xKsZN...";
decimal fee = await solTools.SolFeeByTx(txHash);
Console.WriteLine($"Transaction fee: {fee} SOL");
```

### Разбор
```csharp
public async Task<decimal> SolFeeByTx(
    string transactionHash,              // Подпись/хэш транзакции
    string rpc = null,                   // URL RPC (по умолчанию: mainnet)
    string tokenDecimal = "9"           // Десятичные знаки для конвертации (по умолчанию: 9 для SOL)
)
// Возвращает: Комиссию в SOL (конвертированную из lamports)
// Использует метод RPC getTransaction
// По умолчанию использует Solana mainnet если rpc равен null
```
