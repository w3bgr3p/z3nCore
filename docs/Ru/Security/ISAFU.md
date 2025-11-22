# ISAFU (Утилита Функций Безопасности и Аутентификации)

Этот модуль предоставляет утилиты безопасности и аутентификации для шифрования/дешифрования конфиденциальных данных в проектах ZennoPoster.

---

## Класс FunctionStorage

Статический класс, предоставляющий потокобезопасное хранилище для функций безопасности.

### Поле Functions

**Назначение**: Хранит делегаты функций безопасности в потокобезопасном параллельном словаре.

**Пример**:
```csharp
// Доступ к сохраненным функциям
if (FunctionStorage.Functions.ContainsKey("SAFU_Encode"))
{
    var encodeFunc = (Func<IZennoPosterProjectModel, string, bool, string>)
        FunctionStorage.Functions["SAFU_Encode"];
}

// Добавить пользовательскую функцию
FunctionStorage.Functions.TryAdd("CustomFunc", myCustomFunction);
```

**Разбор**:
```csharp
public static ConcurrentDictionary<string, object> Functions

// Тип: ConcurrentDictionary<string, object>
// - Ключ: Имя функции (например, "SAFU_Encode", "SAFU_Decode")
// - Значение: Делегат функции (хранится как object, требует приведения типа)

// Примечания:
// - Потокобезопасный для параллельного доступа
// - Используется классом SAFU для хранения функций шифрования/дешифрования
// - Функции могут быть заменены пользовательскими реализациями
```

---

## Интерфейс ISAFU

Публичный интерфейс, определяющий контракт для функций безопасности и аутентификации.

**Назначение**: Определяет методы для шифрования, дешифрования и генерации паролей на основе оборудования.

**Пример**:
```csharp
// Реализовать пользовательский SAFU
public class CustomSAFU : ISAFU
{
    public string Encode(IZennoPosterProjectModel project, string toEncrypt, bool log)
    {
        // Пользовательская логика шифрования
        return encrypted;
    }

    public string EncodeV2(IZennoPosterProjectModel project, string toEncrypt, bool log)
    {
        // Улучшенная логика шифрования
        return encrypted;
    }

    public string Decode(IZennoPosterProjectModel project, string toDecrypt, bool log)
    {
        // Пользовательская логика дешифрования
        return decrypted;
    }

    public string HWPass(IZennoPosterProjectModel project, bool v2)
    {
        // Пароль на основе оборудования
        return password;
    }
}
```

**Разбор**:
```csharp
public interface ISAFU

// Методы:
// - Encode: Шифрует строку
// - EncodeV2: Улучшенная версия шифрования
// - Decode: Дешифрует строку
// - HWPass: Генерирует пароль на основе оборудования

// Примечания:
// - Реализован внутренне классом SimpleSAFU
// - Может быть реализован для пользовательской логики безопасности
// - Все методы принимают IZennoPosterProjectModel для контекста
```

---

## Класс SAFU

Статический класс, предоставляющий безопасное шифрование/дешифрование для конфиденциальных данных проекта.

### Initialize

**Назначение**: Инициализирует систему SAFU с резервными функциями по умолчанию, если пользовательские реализации не зарегистрированы.

**Пример**:
```csharp
// Инициализировать SAFU (обычно вызывается при запуске проекта)
SAFU.Initialize(project);

// После инициализации можно использовать методы Encode/Decode
string encrypted = SAFU.Encode(project, "sensitive data");
```

**Разбор**:
```csharp
public static void Initialize(IZennoPosterProjectModel project)

// Параметры:
// - project: Модель проекта ZennoPoster для логирования

// Примечания:
// - Проверяет, зарегистрированы ли уже функции SAFU в FunctionStorage
// - Если не зарегистрированы, загружает SimpleSAFU как резервный вариант с предупреждением
// - Логирует предупреждение: "⚠️ SAFU fallback: script kiddie security level!"
// - Регистрирует функции: SAFU_Encode, SAFU_Decode, SAFU_HWPass
// - Безопасно вызывать несколько раз (инициализирует только один раз)
```

---

### Encode

**Назначение**: Шифрует строку, используя конфигурацию PIN проекта и зарегистрированную функцию SAFU.

**Пример**:
```csharp
// Зашифровать конфиденциальные данные
project.Variables["cfgPin"].Value = "mySecretPin123";
string plainText = "myPassword123";
string encrypted = SAFU.Encode(project, plainText);

// С включенным логированием
string encryptedWithLog = SAFU.Encode(project, plainText, true);

// Если cfgPin пустой, возвращает исходную строку
project.Variables["cfgPin"].Value = "";
string notEncrypted = SAFU.Encode(project, plainText); // Возвращает plainText
```

**Разбор**:
```csharp
public static string Encode(IZennoPosterProjectModel project, string toEncrypt, bool log = false)

// Параметры:
// - project: Модель проекта ZennoPoster (обращается к переменной cfgPin)
// - toEncrypt: Строка открытого текста для шифрования
// - log: Включить логирование для отладки (по умолчанию: false)

// Возвращает:
// - Зашифрованную строку
// - Возвращает исходную строку, если cfgPin пустой

// Примечания:
// - Использует шифрование AES с MD5-хешированным PIN в качестве ключа
// - Требует установки переменной cfgPin в проекте
// - Если cfgPin пустой, шифрование не выполняется (возвращает вход)
```

---

### EncodeV2

**Назначение**: Улучшенное шифрование с использованием функции V2, если доступна, иначе использует стандартный Encode.

**Пример**:
```csharp
// Зашифровать с помощью V2 (если доступно)
project.Variables["cfgPin"].Value = "mySecretPin123";
string encrypted = SAFU.EncodeV2(project, "sensitive data");

// Если EncodeV2 не зарегистрирован, автоматически использует Encode
// Логирует предупреждение: "EncodeV2 not available, using fallback"
```

**Разбор**:
```csharp
public static string EncodeV2(IZennoPosterProjectModel project, string toEncrypt, bool log = false)

// Параметры:
// - project: Модель проекта ZennoPoster (обращается к переменной cfgPin)
// - toEncrypt: Строка открытого текста для шифрования
// - log: Включить логирование для отладки (по умолчанию: false)

// Возвращает:
// - Зашифрованную строку методом V2
// - Возвращается к стандартному Encode, если V2 недоступен

// Примечания:
// - Проверяет наличие функции SAFU_EncodeV2 в FunctionStorage
// - Если не найдена, логирует предупреждение и использует SAFU_Encode
// - Обеспечивает повышенную безопасность, если зарегистрирована реализация V2
```

---

### Decode

**Назначение**: Дешифрует строку, зашифрованную с помощью SAFU.Encode или SAFU.EncodeV2.

**Пример**:
```csharp
// Расшифровать данные
project.Variables["cfgPin"].Value = "mySecretPin123";
string encrypted = "AB12CD34..."; // зашифрованная строка
string decrypted = SAFU.Decode(project, encrypted);

// С логированием
string decryptedWithLog = SAFU.Decode(project, encrypted, true);

// Если cfgPin пустой, возвращает исходную строку
project.Variables["cfgPin"].Value = "";
string notDecrypted = SAFU.Decode(project, encrypted); // Возвращает encrypted
```

**Разбор**:
```csharp
public static string Decode(IZennoPosterProjectModel project, string toDecrypt, bool log = false)

// Параметры:
// - project: Модель проекта ZennoPoster (обращается к переменной cfgPin)
// - toDecrypt: Зашифрованная строка для дешифрования
// - log: Включить логирование для отладки (по умолчанию: false)

// Возвращает:
// - Расшифрованную строку открытого текста
// - Возвращает исходную строку, если cfgPin пустой

// Исключения:
// - Может бросить исключение, если зашифрованная строка повреждена или ключ неверен
// - SimpleSAFU логирует ошибку и повторно бросает исключение

// Примечания:
// - Необходимо использовать то же значение cfgPin, что и при шифровании
// - Использует дешифрование AES с MD5-хешированным PIN
```

---

### HWPass

**Назначение**: Генерирует пароль на основе оборудования, используя серийный номер материнской платы и данные проекта.

**Пример**:
```csharp
// Сгенерировать пароль на основе оборудования
project.Variables["acc0"].Value = "myAccountID";
string hwPassword = SAFU.HWPass(project);

// Режим V2 (по умолчанию)
string hwPasswordV2 = SAFU.HWPass(project, true);

// Режим V1
string hwPasswordV1 = SAFU.HWPass(project, false);
```

**Разбор**:
```csharp
public static string HWPass(IZennoPosterProjectModel project, bool v2 = true)

// Параметры:
// - project: Модель проекта ZennoPoster (обращается к переменной acc0)
// - v2: Использовать версию 2 генерации пароля (по умолчанию: true)

// Возвращает:
// - Строку пароля на основе оборудования
// - Комбинирует серийный номер материнской платы + значение переменной acc0

// Примечания:
// - Пароль уникален для оборудования (серийный номер материнской платы)
// - Требует доступа WMI для чтения серийного номера Win32_BaseBoard
// - Использует project.Variables["acc0"] для дополнительной энтропии
// - Пароль меняется при изменении оборудования или acc0
// - Реализация SimpleSAFU: конкатенация serial + acc0
```

---

## Соображения безопасности

1. **Безопасность PIN**: Переменная `cfgPin` является мастер-ключом для всего шифрования. Защищайте её тщательно.

2. **Режим AES ECB**: SimpleSAFU использует режим ECB, который менее безопасен, чем CBC/GCM для больших данных. Рассмотрите пользовательскую реализацию для повышенной безопасности.

3. **Хеширование MD5**: MD5 используется для деривации ключа. Хотя приемлем для этого случая, рассмотрите SHA-256 для повышенной безопасности.

4. **Привязка к оборудованию**: HWPass привязывает данные к конкретному оборудованию. Данные не могут быть расшифрованы на других машинах.

5. **Пользовательская реализация**: Замените SimpleSAFU, зарегистрировав пользовательские функции в FunctionStorage перед вызовом SAFU.Initialize().

---
