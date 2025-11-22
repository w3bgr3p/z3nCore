# Blockchain

Основной класс для взаимодействия с EVM-совместимыми блокчейнами через Nethereum.

---

## Blockchain (Конструктор)

### Назначение
Инициализирует новый экземпляр Blockchain с учетными данными кошелька и RPC эндпоинтом.

### Пример
```csharp
string privateKey = "0xabc123...";
int chainId = 1; // Ethereum mainnet
string rpc = "https://eth.llamarpc.com";
var blockchain = new Blockchain(privateKey, chainId, rpc);
```

### Разбор
```csharp
public Blockchain(
    string walletKey,    // Приватный ключ для подписи транзакций
    int chainId,         // ID сети (1 для Ethereum, 56 для BSC и т.д.)
    string jsonRpc       // URL RPC эндпоинта
)
// Инициализирует подключение к блокчейну с учетными данными кошелька
```

---

## GetAddressFromPrivateKey

### Назначение
Получает адрес Ethereum из приватного ключа.

### Пример
```csharp
var blockchain = new Blockchain();
string privateKey = "abc123..."; // Без префикса 0x
string address = blockchain.GetAddressFromPrivateKey(privateKey);
Console.WriteLine($"Address: {address}");
```

### Разбор
```csharp
public string GetAddressFromPrivateKey(
    string privateKey    // Приватный ключ (с префиксом 0x или без него)
)
// Возвращает: Адрес Ethereum (0x...)
// Автоматически добавляет префикс 0x если отсутствует
```

---

## GetBalance

### Назначение
Получает баланс нативного токена (ETH, BNB и т.д.) для настроенного кошелька.

### Пример
```csharp
var blockchain = new Blockchain(privateKey, chainId, rpc);
string balance = await blockchain.GetBalance();
Console.WriteLine($"Balance: {balance} ETH");
```

### Разбор
```csharp
public async Task<string> GetBalance()
// Возвращает: Баланс как строку в единицах Ether
// Требует: Инициализированный экземпляр Blockchain с walletKey
```

---

## ReadContract

### Назначение
Вызывает функцию смарт-контракта только для чтения и получает результат.

### Пример
```csharp
var blockchain = new Blockchain(rpc);
string contractAddress = "0x123...";
string abi = "[{...}]"; // ABI контракта в JSON
string result = await blockchain.ReadContract(
    contractAddress,
    "balanceOf",
    abi,
    "0xUserAddress"
);
```

### Разбор
```csharp
public async Task<string> ReadContract(
    string contractAddress,    // Адрес смарт-контракта
    string functionName,       // Имя функции для вызова
    string abi,               // ABI контракта в формате JSON
    params object[] parameters // Параметры функции
)
// Возвращает: Результат функции как строку (отформатирован в зависимости от типа возврата)
// Поддерживает: BigInteger (hex), bool, string, byte[], tuples
```

---

## SendTransaction

### Назначение
Отправляет legacy (Type 0) транзакцию в блокчейн.

### Пример
```csharp
var blockchain = new Blockchain(privateKey, chainId, rpc);
string hash = await blockchain.SendTransaction(
    to: "0x123...",
    amount: 0.1m,  // Может быть decimal, BigInteger, HexBigInteger и т.д.
    data: "0x",
    gasLimit: new BigInteger(21000),
    gasPrice: new BigInteger(20000000000)
);
```

### Разбор
```csharp
public async Task<string> SendTransaction(
    string addressTo,        // Адрес получателя
    object amount,          // Сумма для отправки (поддерживает множество типов)
    string data,            // Данные транзакции (0x для простых переводов)
    BigInteger gasLimit,    // Лимит газа
    BigInteger gasPrice     // Цена газа в wei
)
// Возвращает: Хэш транзакции
// Типы amount: decimal (ETH), BigInteger (wei), HexBigInteger, int, long, double, float, string
```

---

## SendTransactionEIP1559

### Назначение
Отправляет EIP-1559 (Type 2) транзакцию с динамической структурой комиссий.

### Пример
```csharp
var blockchain = new Blockchain(privateKey, chainId, rpc);
string hash = await blockchain.SendTransactionEIP1559(
    to: "0x123...",
    amount: 0.1m,
    data: "0x",
    gasLimit: new BigInteger(21000),
    maxFeePerGas: new BigInteger(30000000000),
    maxPriorityFeePerGas: new BigInteger(2000000000)
);
```

### Разбор
```csharp
public async Task<string> SendTransactionEIP1559(
    string addressTo,                  // Адрес получателя
    object amount,                     // Сумма для отправки (поддерживает множество типов)
    string data,                       // Данные транзакции
    BigInteger gasLimit,               // Лимит газа
    BigInteger maxFeePerGas,          // Максимальная комиссия за газ
    BigInteger maxPriorityFeePerGas   // Приоритетная комиссия (чаевые) за газ
)
// Возвращает: Хэш транзакции
// Использует механизм рынка комиссий EIP-1559 (транзакция Type 2)
```

---

## EstimateGasAsync

### Назначение
Оценивает параметры газа (лимит и цену) для транзакции.

### Пример
```csharp
var blockchain = new Blockchain(privateKey, chainId, rpc);
var web3 = new Web3(rpc);
var (gasLimit, gasPrice, maxFee, priority) = await blockchain.EstimateGasAsync(
    contractAddress: "0x123...",
    encodedData: "0xabc...",
    value: "0",
    txType: 0,
    speedup: 100,
    web3: web3,
    fromAddress: "0xYourAddress"
);
```

### Разбор
```csharp
public async Task<(BigInteger GasLimit, BigInteger GasPrice, BigInteger MaxFeePerGas, BigInteger PriorityFee)> EstimateGasAsync(
    string contractAddress,    // Адрес контракта для взаимодействия
    string encodedData,       // Закодированные данные транзакции
    string value,             // Отправляемое значение в wei
    int txType,              // 0 для legacy, 2 для EIP-1559
    int speedup,             // Процент ускорения (100 = нормально, 110 = на 10% быстрее)
    Web3 web3,               // Экземпляр Web3
    string fromAddress       // Адрес отправителя
)
// Возвращает: Кортеж с параметрами газа
// Выбрасывает: Exception с детальными сообщениями об ошибках RPC
```

---

## GenerateMnemonic

### Назначение
Генерирует новую мнемоническую фразу BIP39.

### Пример
```csharp
string mnemonic = Blockchain.GenerateMnemonic("English", 12);
Console.WriteLine($"Mnemonic: {mnemonic}");
```

### Разбор
```csharp
public static string GenerateMnemonic(
    string wordList = "English",    // Язык: English, Japanese, ChineseSimplified и т.д.
    int wordCount = 12             // Количество слов: 12, 15, 18, 21 или 24
)
// Возвращает: Мнемоническую фразу разделенную пробелами
// Поддерживаемые языки: English, Japanese, Chinese (Simplified/Traditional), Spanish, French, Portuguese, Czech
```

---

## MnemonicToAccountEth

### Назначение
Генерирует несколько Ethereum аккаунтов из мнемонической фразы.

### Пример
```csharp
string mnemonic = "word1 word2 word3...";
var accounts = Blockchain.MnemonicToAccountEth(mnemonic, 5);
foreach (var account in accounts)
{
    Console.WriteLine($"Address: {account.Key}, PrivateKey: {account.Value}");
}
```

### Разбор
```csharp
public static Dictionary<string, string> MnemonicToAccountEth(
    string words,    // Мнемоническая фраза BIP39
    int amount      // Количество аккаунтов для генерации
)
// Возвращает: Dictionary<адрес, приватныйКлюч>
// Использует стандартный путь деривации Ethereum: m/44'/60'/0'/0/i
```

---

## MnemonicToAccountBtc

### Назначение
Генерирует несколько Bitcoin аккаунтов из мнемонической фразы.

### Пример
```csharp
string mnemonic = "word1 word2 word3...";
var accounts = Blockchain.MnemonicToAccountBtc(mnemonic, 5, "Bech32");
foreach (var account in accounts)
{
    Console.WriteLine($"Address: {account.Key}, PrivateKey: {account.Value}");
}
```

### Разбор
```csharp
public static Dictionary<string, string> MnemonicToAccountBtc(
    string mnemonic,                    // Мнемоническая фраза BIP39
    int amount,                         // Количество аккаунтов для генерации
    string walletType = "Bech32"       // Тип адреса
)
// Возвращает: Dictionary<адрес, приватныйКлюч>
// Типы кошельков: "Bech32" (native SegWit), "P2PKH compress", "P2PKH uncompress", "P2SH"
// Использует путь деривации: m/84'/0'/0'/0/i
```

---

## GetEthAccountBalance

### Назначение
Получает баланс ETH для любого адреса (статический утилитарный метод).

### Пример
```csharp
string balance = Blockchain.GetEthAccountBalance(
    "0x123...",
    "https://eth.llamarpc.com"
);
Console.WriteLine($"Balance: {balance} wei");
```

### Разбор
```csharp
public static string GetEthAccountBalance(
    string address,    // Адрес для проверки
    string jsonRpc    // URL RPC эндпоинта
)
// Возвращает: Баланс в wei как строку
// Примечание: Результат в wei, а не в ether
```

---

# Класс Function

Вспомогательный класс для работы с ABI смарт-контрактов.

## GetFuncInputTypes

### Назначение
Извлекает типы входных параметров функции контракта.

### Пример
```csharp
string abi = "[{...}]";
string[] types = Function.GetFuncInputTypes(abi, "transfer");
// Возвращает: ["address", "uint256"]
```

### Разбор
```csharp
public static string[] GetFuncInputTypes(
    string abi,           // ABI контракта в JSON
    string functionName   // Имя функции
)
// Возвращает: Массив строк с типами Solidity
```

---

## GetFuncInputParameters

### Назначение
Получает имена и типы входных параметров в виде словаря.

### Пример
```csharp
var params = Function.GetFuncInputParameters(abi, "transfer");
// Возвращает: {"to": "address", "amount": "uint256"}
```

### Разбор
```csharp
public static Dictionary<string, string> GetFuncInputParameters(
    string abi,           // ABI контракта в JSON
    string functionName   // Имя функции
)
// Возвращает: Dictionary<имяПараметра, тип>
```

---

## GetFuncOutputParameters

### Назначение
Получает имена и типы выходных параметров.

### Пример
```csharp
var outputs = Function.GetFuncOutputParameters(abi, "balanceOf");
// Возвращает: {"": "uint256"}
```

### Разбор
```csharp
public static Dictionary<string, string> GetFuncOutputParameters(
    string abi,           // ABI контракта в JSON
    string functionName   // Имя функции
)
// Возвращает: Dictionary<имяПараметра, тип>
```

---

## GetFuncAddress

### Назначение
Получает селектор функции (первые 4 байта хэша keccak256).

### Пример
```csharp
string selector = Function.GetFuncAddress(abi, "transfer");
// Возвращает: "0xa9059cbb"
```

### Разбор
```csharp
public static string GetFuncAddress(
    string abi,           // ABI контракта в JSON
    string functionName   // Имя функции
)
// Возвращает: Селектор функции (0x + 8 hex символов)
```

---

# Класс Decoder

Декодирует данные смарт-контрактов.

## AbiDataDecode

### Назначение
Декодирует выходные данные транзакции используя ABI.

### Пример
```csharp
var decoded = Decoder.AbiDataDecode(abi, "balanceOf", "0x0000000000000000000000000000000000000000000000000000000000000064");
// Возвращает: {"": "100"}
```

### Разбор
```csharp
public static Dictionary<string, string> AbiDataDecode(
    string abi,           // ABI контракта в JSON
    string functionName,  // Имя функции
    string data          // Hex закодированные данные (с 0x или без)
)
// Возвращает: Dictionary<имяПараметра, декодированноеЗначение>
// Поддерживает: address, uint256, uint8, bool
```

---

# Класс Encoder

Кодирует данные транзакций смарт-контрактов.

## EncodeTransactionData

### Назначение
Кодирует полный вызов функции с параметрами.

### Пример
```csharp
string[] types = {"address", "uint256"};
object[] values = {"0x123...", new BigInteger(100)};
string encoded = Encoder.EncodeTransactionData(abi, "transfer", types, values);
// Возвращает: "0xa9059cbb000000000000000000000000123...0000000000000064"
```

### Разбор
```csharp
public static string EncodeTransactionData(
    string abi,           // ABI контракта в JSON
    string functionName,  // Имя функции для вызова
    string[] types,       // Массив типов параметров
    object[] values      // Массив значений параметров
)
// Возвращает: Закодированные данные транзакции (0x + селектор + закодированные параметры)
```

---

## EncodeParam

### Назначение
Кодирует один параметр.

### Пример
```csharp
string encoded = Encoder.EncodeParam("uint256", new BigInteger(100));
// Возвращает: "0000000000000000000000000000000000000000000000000000000000000064"
```

### Разбор
```csharp
public static string EncodeParam(
    string type,    // Тип Solidity
    object value   // Значение для кодирования
)
// Возвращает: ABI-закодированный параметр (hex, без 0x)
```

---

## EncodeParams

### Назначение
Кодирует несколько параметров.

### Пример
```csharp
string[] types = {"address", "uint256"};
object[] values = {"0x123...", new BigInteger(100)};
string encoded = Encoder.EncodeParams(types, values);
```

### Разбор
```csharp
public static string EncodeParams(
    string[] types,    // Массив типов Solidity
    object[] values   // Массив значений
)
// Возвращает: Объединенные закодированные параметры (hex, без 0x)
```

---

# Класс Converter

Утилита для преобразования значений.

## ValuesToArray

### Назначение
Преобразует динамические параметры в массив объектов.

### Пример
```csharp
object[] values = Converter.ValuesToArray("0x123...", 100, true);
// Может использоваться с EncodeParams
```

### Разбор
```csharp
public static object[] ValuesToArray(
    params dynamic[] inputValues    // Переменное число динамических значений
)
// Возвращает: Массив object[] значений
// Полезно для подготовки параметров к кодированию
```
