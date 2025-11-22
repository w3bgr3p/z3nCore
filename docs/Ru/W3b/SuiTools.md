# SuiTools

Инструменты для взаимодействия с блокчейном Sui. Примечание: Основной класс внутренний, но доступны публичные методы расширения в W3bTools.

---

## SuiKeyGen.Generate

### Назначение
Генерирует ключи кошелька Sui и адрес из мнемонической фразы.

### Пример
```csharp
string mnemonic = "word1 word2 word3...";
var keys = SuiKeyGen.Generate(mnemonic);
Console.WriteLine($"Private Key (HEX): {SuiKeyGen.ToHex(keys.Priv32)}");
Console.WriteLine($"Private Key (Bech32): {keys.PrivateKeyBech32}");
Console.WriteLine($"Public Key: {SuiKeyGen.ToHex(keys.Pub32)}");
Console.WriteLine($"Address: {keys.Address}");
```

### Разбор
```csharp
public static SuiKeys Generate(
    string mnemonic,                     // Мнемоническая фраза BIP39
    string passphrase = "",              // Опциональная парольная фраза
    string path = "m/44'/784'/0'/0'/0'" // Путь деривации (стандарт Sui)
)
// Возвращает: Объект SuiKeys с:
//   - Priv32: 32-байтный приватный ключ
//   - PrivExpanded64: 64-байтный расширенный приватный ключ
//   - Pub32: 32-байтный публичный ключ (Ed25519)
//   - Address: Адрес Sui (0x...)
//   - PrivateKeyBech32: Приватный ключ в кодировке Bech32 (suiprivkey1...)
```

---

## Методы расширения (W3bTools)

### SuiKey

### Назначение
Извлекает ключевой материал из мнемонической фразы.

### Пример
```csharp
string mnemonic = "word1 word2...";
string privateKeyHex = mnemonic.SuiKey("HEX");
string bech32Key = mnemonic.SuiKey("Bech32");
string publicKey = mnemonic.SuiKey("PubHEX");
string address = mnemonic.SuiKey("Address");
```

### Разбор
```csharp
public static string SuiKey(
    this string mnemonic,    // Мнемоническая фраза
    string keyType = "HEX"  // Тип ключа: "HEX", "Bech32", "PubHEX", "Address"
)
// Возвращает: Ключ в запрошенном формате
```

---

### SuiAddress

### Назначение
Получает адрес Sui из мнемоники, приватного ключа или конвертирует существующий адрес.

### Пример
```csharp
// Из мнемоники
string address1 = mnemonic.SuiAddress();

// Из hex приватного ключа
string hexKey = "abc123...";
string address2 = hexKey.SuiAddress();

// Из Bech32 приватного ключа
string bech32Key = "suiprivkey1...";
string address3 = bech32Key.SuiAddress();
```

### Разбор
```csharp
public static string SuiAddress(
    this string input    // Мнемоника, приватный ключ (hex или bech32), или адрес
)
// Возвращает: Адрес Sui (0x...)
// Автоматически определяет тип входных данных: seed, keySui, keyEvm, addressSui
```

---

### SuiNative

### Назначение
Получает баланс нативного токена SUI для адреса.

### Пример
```csharp
string rpc = "https://fullnode.mainnet.sui.io";
string address = "0x123...";
decimal balance = W3bTools.SuiNative(rpc, address);
Console.WriteLine($"SUI Balance: {balance}");
```

### Разбор
```csharp
public static decimal SuiNative(
    string rpc,       // URL RPC эндпоинта Sui
    string address    // Адрес кошелька Sui
)
// Возвращает: Баланс в SUI (конвертированный из MIST, 9 десятичных знаков)
// 1 SUI = 1,000,000,000 MIST
```

---

### SuiTokenBalance

### Назначение
Получает баланс конкретного типа монеты на Sui.

### Пример
```csharp
string coinType = "0x2::sui::SUI";
decimal balance = W3bTools.SuiTokenBalance(coinType, rpc, address);
```

### Разбор
```csharp
public static decimal SuiTokenBalance(
    string coinType,    // Идентификатор типа монеты
    string rpc,        // URL RPC эндпоинта Sui
    string address     // Адрес кошелька Sui
)
// Возвращает: Баланс токена (предполагается 6 десятичных знаков)
```

---

### SendNativeSui

### Назначение
Отправляет нативные токены SUI на другой адрес (метод расширения для IZennoPosterProjectModel).

### Пример
```csharp
string to = "0x456...";
decimal amount = 0.5m; // 0.5 SUI
string rpc = "https://fullnode.mainnet.sui.io";
string txHash = project.SendNativeSui(to, amount, rpc);
Console.WriteLine($"Transaction: {txHash}");
```

### Разбор
```csharp
public static string SendNativeSui(
    this IZennoPosterProjectModel project,    // Экземпляр проекта
    string to,                                 // Адрес получателя
    decimal amount,                            // Сумма в SUI
    string rpc = null,                        // URL RPC (по умолчанию: mainnet)
    bool debug = false                        // Режим отладки
)
// Возвращает: Digest транзакции (hash)
// Требует: Приватный ключ в базе данных (DbKey("evm"))
// Устанавливает: project.Variables["blockchainHash"] = txHash
// Выбрасывает: Exception при ошибке
```

---

### SuiFaucet

### Назначение
Запрашивает тестовые SUI из faucet (метод расширения для Instance).

### Пример
```csharp
string address = "0x123...";
instance.SuiFaucet(project, address, successRequired: 3, tableToUpdate: "wallets");
```

### Разбор
```csharp
public static void SuiFaucet(
    this Instance instance,                    // Экземпляр браузера
    IZennoPosterProjectModel project,          // Экземпляр проекта
    string address,                            // Адрес Sui для пополнения
    int successRequired = 3,                   // Необходимое количество успешных запросов
    string tableToUpdate = null               // Таблица базы данных для обновления баланса
)
// Запрашивает SUI из testnet faucet
// Пытается до 3 раз
// Обновляет базу данных финальным балансом если указан tableToUpdate
// Автоматически обрабатывает Cloudflare challenges
```
