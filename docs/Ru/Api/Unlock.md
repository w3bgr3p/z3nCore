# Документация класса UnlockApi

## Обзор
Класс `UnlockApi` предоставляет интеграцию со смарт-контрактами Unlock Protocol для чтения данных NFT членства, временных меток истечения и информации о держателях.

---

## Конструктор

### `UnlockApi(IZennoPosterProjectModel project, bool log = false)`

**Назначение:** Инициализирует клиент Unlock Protocol API с RPC сети Optimism.

**Пример:**
```csharp
var unlock = new UnlockApi(project, log: true);
string expiration = unlock.keyExpirationTimestampFor(
    "0x1234567890123456789012345678901234567890",
    1
);
```

**Разбор:**
```csharp
var unlock = new UnlockApi(
    project,  // IZennoPosterProjectModel - экземпляр проекта
    true      // bool - включить логирование
);
// Примечание: Использует RPC Optimism по умолчанию
```

---

## Публичные методы

### `keyExpirationTimestampFor(string addressTo, int tokenId, bool decode = true)`

**Назначение:** Получает временную метку истечения для ключа NFT членства.

**Пример:**
```csharp
var unlock = new UnlockApi(project);
string expirationTimestamp = unlock.keyExpirationTimestampFor(
    "0x1234567890123456789012345678901234567890",  // Контракт замка
    1,                                               // ID токена
    true                                             // Декодировать результат
);
Console.WriteLine($"Истекает в: {expirationTimestamp}");
```

**Разбор:**
```csharp
string expiration = unlock.keyExpirationTimestampFor(
    "0x1234567...",  // string - адрес контракта замка Unlock Protocol
    1,               // int - ID NFT токена
    true             // bool - декодировать hex результат в читаемый формат
);
// Возвращает: string - Unix timestamp (декодированный) или hex (сырой)
// Выбрасывает: Exception - если вызов контракта не удался
```

---

### `ownerOf(string addressTo, int tokenId, bool decode = true)`

**Назначение:** Получает адрес владельца конкретного NFT токена.

**Пример:**
```csharp
var unlock = new UnlockApi(project);
string owner = unlock.ownerOf(
    "0x1234567890123456789012345678901234567890",
    1,
    true
);
Console.WriteLine($"Владелец: {owner}");
```

**Разбор:**
```csharp
string ownerAddress = unlock.ownerOf(
    "0x1234567...",  // string - адрес контракта замка
    1,               // int - ID токена
    true             // bool - декодировать результат
);
// Возвращает: string - Ethereum адрес владельца
// Выбрасывает: Exception - если вызов контракта не удался
```

---

### `Decode(string toDecode, string function)`

**Назначение:** Декодирует ABI-кодированные hex данные из ответов контракта.

**Пример:**
```csharp
var unlock = new UnlockApi(project);
string decoded = unlock.Decode(
    "0x000000000000000000000000000000000000000000000000000000006789abcd",
    "keyExpirationTimestampFor"
);
```

**Разбор:**
```csharp
string decodedValue = unlock.Decode(
    "0x6789abcd...",              // string - hex данные для декодирования
    "keyExpirationTimestampFor"   // string - название функции для соответствия ABI
);
// Возвращает: string - декодированное значение
// Автоматически дополняет hex до 64 символов при необходимости
// Использует внутренний ABI для декодирования
```

---

### `Holders(string contract)`

**Назначение:** Получает всех держателей NFT и их временные метки истечения из контракта замка.

**Пример:**
```csharp
var unlock = new UnlockApi(project);
Dictionary<string, string> holders = unlock.Holders(
    "0x1234567890123456789012345678901234567890"
);

foreach (var holder in holders)
{
    Console.WriteLine($"Адрес: {holder.Key}, Истекает: {holder.Value}");
}
```

**Разбор:**
```csharp
Dictionary<string, string> allHolders = unlock.Holders(
    "0x1234567..."  // string - адрес контракта замка
);
// Возвращает: Dictionary<string, string> - адрес владельца → временная метка истечения
// Проходит по ID токенов начиная с 1
// Останавливается при встрече нулевого адреса (0x000...000)
// Все адреса и временные метки возвращаются в нижнем регистре
```

---

## ABI методы контракта

Класс использует следующие ABI методы Unlock Protocol:

### keyExpirationTimestampFor
- **Вход:** uint256 tokenId
- **Выход:** uint256 временная метка истечения
- **Назначение:** Получить когда истекает ключ

### ownerOf
- **Вход:** uint256 tokenId
- **Выход:** address владелец
- **Назначение:** Получить владельца конкретного токена

---

## Поток взаимодействия с контрактом

1. **Вызов контракта:** Использует `Blockchain.ReadContract()` для запроса смарт-контракта
2. **Получение результата:** Получает hex-кодированный результат
3. **Декодирование (Опционально):** Декодирует hex в человекочитаемый формат используя ABI
4. **Возврат:** Возвращает обработанные данные

---

## Формат данных

### Сырой (decode=false)
```
0x000000000000000000000000000000000000000000000000000000006789abcd
```

### Декодированный (decode=true)
```
1736793293  // Unix timestamp
0x742d35Cc6634C0532925a3b8D45C0532925aAB1  // Адрес
```

---

## Примечания

- Блокчейн по умолчанию: Optimism (через `Rpc.Get("optimism")`)
- Все чтения контракта являются view/pure функциями (без затрат газа)
- Использует класс Blockchain для Web3 взаимодействий
- ABI жестко закодирован для контрактов Unlock Protocol v11+
- Итерация токенов в `Holders()` начинается с ID 1
- Нулевой адрес (0x000...000) указывает на несуществующий токен
- Все возвращаемые адреса в нижнем регистре
- Временные метки в Unix epoch секундах
- Автоматическое дополнение hex для коротких ответов
- Логирует ошибки в лог проекта с деталями внутреннего исключения
