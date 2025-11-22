# Класс Converter

Статический класс для преобразования данных между различными форматами (hex, base64, bech32, bytes, text).

---

## ConvertFormat

### Назначение
Преобразует данные из одного формата в другой. Поддерживает форматы hex, base64, bech32, bytes и text с автоматической валидацией и обработкой ошибок.

### Пример
```csharp
using z3nCore.Utilities;

// Преобразование hex в base64
string hex = "0x48656c6c6f";
string base64 = Converer.ConvertFormat(project, hex, "hex", "base64", log: true);
// Результат: "SGVsbG8="

// Преобразование текста в hex
string text = "Hello";
string hexResult = Converer.ConvertFormat(project, text, "text", "hex");
// Результат: "0x48656c6c6f"

// Преобразование bech32 адреса в hex
string bech32Addr = "init1qqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqqq";
string hexAddr = Converer.ConvertFormat(project, bech32Addr, "bech32", "hex");
```

### Детали
```csharp
public static string ConvertFormat(
    IZennoPosterProjectModel project,  // Экземпляр проекта для логирования
    string toProcess,                  // Входные данные для преобразования
    string input,                      // Входной формат: "hex", "base64", "bech32", "bytes", "text"
    string output,                     // Выходной формат: "hex", "base64", "bech32", "bytes", "text"
    bool log = false)                  // Включить логирование (по умолчанию: false)

// Возвращает: Преобразованную строку в указанном выходном формате
// Возвращает: null при ошибке преобразования

// Исключения обрабатываются внутри метода:
// - ArgumentException: Неподдерживаемый формат или невалидные входные данные
// - Exception: Общие ошибки преобразования (логируются в проект)

// Примечания:
// - Hex-строки могут начинаться с "0x" (опционально)
// - Формат Bech32 требует ровно 32 байта и префикс "init"
// - Все ошибки логируются в проект, при неудаче возвращается null
```

---
