# CosmosTools

Инструменты для работы с кошельками и адресами Cosmos SDK.

---

## KeyFromSeed

### Назначение
Получает приватный ключ из мнемонической фразы используя путь деривации Cosmos SDK.

### Пример
```csharp
var cosmosTools = new CosmosTools();
string mnemonic = "word1 word2 word3...";
string privateKey = cosmosTools.KeyFromSeed(mnemonic);
Console.WriteLine($"Private Key: {privateKey}");
```

### Разбор
```csharp
public string KeyFromSeed(
    string mnemonic    // Мнемоническая фраза BIP39 (12-24 слова)
)
// Возвращает: Приватный ключ в hex формате (lowercase, без 0x)
// Использует путь деривации: m/44'/118'/0'/0/0 (стандарт Cosmos SDK)
```

---

## AddressFromSeed

### Назначение
Получает адрес кошелька Bech32 из мнемонической фразы.

### Пример
```csharp
var cosmosTools = new CosmosTools();
string mnemonic = "word1 word2 word3...";
string address = cosmosTools.AddressFromSeed(mnemonic, "cosmos");
Console.WriteLine($"Cosmos Address: {address}");
// Для других сетей: AddressFromSeed(mnemonic, "osmo") для Osmosis
```

### Разбор
```csharp
public string AddressFromSeed(
    string mnemonic,           // Мнемоническая фраза BIP39
    string chain = "cosmos"    // Префикс сети (cosmos, osmo, juno и т.д.)
)
// Возвращает: Адрес в кодировке Bech32 (например, cosmos1abc...)
// Сеть по умолчанию: "cosmos"
```

---

## AddressFromKey

### Назначение
Получает адрес кошелька Bech32 из приватного ключа.

### Пример
```csharp
var cosmosTools = new CosmosTools();
string privateKey = "abc123def456..."; // Hex формат
string address = cosmosTools.AddressFromKey(privateKey, "cosmos");
Console.WriteLine($"Address: {address}");
```

### Разбор
```csharp
public string AddressFromKey(
    string privateKey,         // Приватный ключ в hex формате
    string chain = "cosmos"    // Префикс сети
)
// Возвращает: Адрес в кодировке Bech32
// Процесс: privateKey -> pubKey -> SHA256 -> RIPEMD160 -> Bech32 encode
```

---

## AccFromSeed

### Назначение
Получает и приватный ключ, и адрес из мнемонической фразы.

### Пример
```csharp
var cosmosTools = new CosmosTools();
string mnemonic = "word1 word2 word3...";
string[] account = cosmosTools.AccFromSeed(mnemonic, "osmo");
string privateKey = account[0];
string address = account[1];
Console.WriteLine($"PrivateKey: {privateKey}");
Console.WriteLine($"Address: {address}");
```

### Разбор
```csharp
public string[] AccFromSeed(
    string mnemonic,           // Мнемоническая фраза BIP39
    string chain = "cosmos"    // Префикс сети
)
// Возвращает: Массив string[2] [приватныйКлюч, адрес]
// Объединяет функциональность KeyFromSeed и AddressFromSeed
```
