# Документация класса DBuilder

Класс `DBuilder` предоставляет статические утилиты для создания таблиц базы данных и форм импорта данных в проектах ZennoPoster.

---

## Публичные методы

### 1. Columns

**Назначение**: Возвращает предопределенные имена колонок для конкретных схем таблиц.

**Пример**:
```csharp
// Получить имена колонок для таблицы аккаунтов Google
string[] googleColumns = DBuilder.Columns("_google");
// Результат: ["status", "last", "cookies", "login", "password", "otpsecret", "otpbackup", "recoveryemail", "recovery_phone"]
```

**Разбор**:
```csharp
public static string[] Columns(string tableSchem)
{
    // Параметр: tableSchem - название схемы таблицы (например "_google", "_twitter", "_discord")
    // Возвращает: string[] - массив имен колонок для указанной схемы
    // Возвращает пустой массив, если схема не найдена
}
```

---

### 2. GetLines

**Назначение**: Открывает диалоговое окно для ввода нескольких строк текста.

**Пример**:
```csharp
// Запросить у пользователя ввод адресов
List<string> addresses = project.GetLines("Введите адреса кошельков");
if (addresses != null)
{
    // Обработать каждый адрес
    foreach (var addr in addresses)
    {
        project.SendInfoToLog(addr);
    }
}
```

**Разбор**:
```csharp
public static List<string> GetLines(
    this IZennoPosterProjectModel project,  // Метод расширения для проекта
    string title = "Input lines")            // Заголовок диалогового окна
{
    // Возвращает: List<string> - список введенных строк, или null при отмене
    // Каждая строка обрезается и разделяется символом новой строки
}
```

---

### 3. CreateBasicTable

**Назначение**: Создает таблицу базы данных с предопределенной структурой на основе имени таблицы.

**Пример**:
```csharp
// Создать таблицу для аккаунтов Google
project.CreateBasicTable("_google", log: true);

// Создать таблицу настроек
project.CreateBasicTable("_settings", log: false);
```

**Разбор**:
```csharp
public static void CreateBasicTable(
    this IZennoPosterProjectModel project,  // Метод расширения для проекта
    string table,                            // Имя таблицы (например "_google", "_twitter")
    bool log = false)                        // Логировать ли процесс создания
{
    // Создает таблицу с колонкой ID (INTEGER или TEXT PRIMARY KEY в зависимости от типа)
    // Добавляет колонки на основе DBuilder.Columns(table) с типом TEXT DEFAULT ''
    // Ничего не делает, если таблица уже существует
}
```

---

### 4. FormKeyBool

**Назначение**: Открывает диалог для ввода пар ключ-булево_значение (чекбоксы).

**Пример**:
```csharp
// Создать форму с 5 чекбоксами
var keys = new List<string> { "chain1", "chain2", "chain3", "chain4", "chain5" };
var labels = new List<string> { "Включить Ethereum", "Включить BSC", "Включить Polygon", "Включить Arbitrum", "Включить Optimism" };

Dictionary<string, bool> selections = project.FormKeyBool(
    quantity: 5,
    keyPlaceholders: keys,
    valuePlaceholders: labels,
    title: "Выберите сети",
    prepareUpd: false
);

if (selections != null)
{
    foreach (var item in selections)
    {
        project.SendInfoToLog($"{item.Key}: {item.Value}");
    }
}
```

**Разбор**:
```csharp
public static Dictionary<string, bool> FormKeyBool(
    this IZennoPosterProjectModel project,  // Метод расширения для проекта
    int quantity,                            // Количество пар чекбоксов для отображения
    List<string> keyPlaceholders = null,     // Метки ключей (левая сторона)
    List<string> valuePlaceholders = null,   // Метки чекбоксов (правая сторона)
    string title = "Input Key-Bool Pairs",   // Заголовок диалога
    bool prepareUpd = true)                  // Если true, использует числовые ключи (1,2,3...), иначе текст метки
{
    // Возвращает: Dictionary<string, bool> с выбранными значениями, или null при отмене
    // Ключ: либо числовой ID, либо текст метки в зависимости от prepareUpd
    // Значение: состояние чекбокса (true/false)
}
```

---

### 5. FormKeyString

**Назначение**: Открывает диалог для ввода пар ключ-строковое_значение.

**Пример**:
```csharp
// Создать форму для учетных данных API
var keys = new List<string> { "apikey", "secret", "passphrase" };
var placeholders = new List<string> { "Введите API ключ", "Введите секрет", "Опциональная фраза" };

Dictionary<string, string> credentials = project.FormKeyString(
    quantity: 3,
    keyPlaceholders: keys,
    valuePlaceholders: placeholders,
    title: "Настройка API",
    prepareUpd: false
);

if (credentials != null)
{
    string apiKey = credentials["apikey"];
    string secret = credentials["secret"];
}
```

**Разбор**:
```csharp
public static Dictionary<string, string> FormKeyString(
    this IZennoPosterProjectModel project,  // Метод расширения для проекта
    int quantity,                            // Количество пар ключ-значение
    List<string> keyPlaceholders = null,     // Имена ключей по умолчанию
    List<string> valuePlaceholders = null,   // Текст-заполнитель для полей значений
    string title = "Input Key-Value Pairs",  // Заголовок диалога
    bool prepareUpd = true)                  // Если true, форматирует для SQL UPDATE, иначе возвращает сырые пары
{
    // Возвращает: Dictionary<string, string> с введенными значениями, или null при отмене
    // Пустые значения или только заполнители пропускаются
    // Одинарные кавычки в значениях экранируются для безопасности SQL
}
```

---

### 6. FormSocial (Перегрузка 1)

**Назначение**: Открывает диалог для импорта данных социальных аккаунтов с предопределенным сопоставлением полей.

**Пример**:
```csharp
// Импорт аккаунтов Twitter
var fields = new string[] { "", "login", "password", "email", "emailpass", "token", "code2fa" };
var mapping = new Dictionary<string, string>
{
    { "login", "login" },
    { "password", "password" },
    { "email", "email" },
    { "emailpass", "emailpass" },
    { "token", "token" },
    { "CODE2FA", "otpsecret" }
};

string result = project.FormSocial(
    tableName: "_twitter",
    formTitle: "Импорт аккаунтов Twitter",
    availableFields: fields,
    columnMapping: mapping
);

project.SendInfoToLog($"Импортировано {result} аккаунтов");
```

**Разбор**:
```csharp
public static string FormSocial(
    this IZennoPosterProjectModel project,              // Метод расширения
    string tableName,                                    // Целевая таблица БД
    string formTitle,                                    // Заголовок диалога
    string[] availableFields,                            // Доступные варианты полей для выпадающего списка
    Dictionary<string, string> columnMapping,            // Сопоставляет поля формы с колонками БД
    string message = "Select format (one field per box):") // Сообщение с инструкцией
{
    // Возвращает: string - количество импортированных записей, или "0" при отмене/ошибке
    // Пользователь выбирает формат полей через выпадающие списки
    // Данные вводятся по одному аккаунту на строку, разделенные ":"
    // Обновляет записи в БД используя UPDATE WHERE id = номер_строки
}
```

---

### 7. FormSocial (Перегрузка 2)

**Назначение**: Открывает диалог для импорта данных с пользовательским разделителем и автоматическим сопоставлением полей.

**Пример**:
```csharp
// Импорт аккаунтов с пользовательским форматом
var availableFields = new List<string> { "", "login", "password", "email", "token" };

string result = project.FormSocial(
    availableFields: availableFields,
    tableName: "_accounts",
    formTitle: "Импорт аккаунтов",
    message: "Выберите порядок полей"
);

project.SendInfoToLog($"Импортировано {result} записей");
```

**Разбор**:
```csharp
public static string FormSocial(
    this IZennoPosterProjectModel project,      // Метод расширения
    List<string> availableFields,                // Список доступных имен полей
    string tableName,                            // Имя целевой таблицы
    string formTitle,                            // Заголовок диалогового окна
    string message = "Select format (one field per box):") // Инструкция для пользователя
{
    // Возвращает: string - количество импортированных записей
    // Пользователь выбирает символ разделителя (по умолчанию ":")
    // Автоматически добавляет строки в таблицу используя AddRange
    // Парсит данные и обновляет каждую строку значениями полей
}
```

---

### 8. FormSocial (Перегрузка 3)

**Назначение**: Открывает диалог для импорта данных с использованием синтаксиса маски с заполнителями.

**Пример**:
```csharp
// Импорт с использованием формата маски
string result = project.FormSocial(
    tableName: "_twitter",
    formTitle: "Импорт данных Twitter",
    message: "Введите маску формата с синтаксисом {field}"
);

// Пользователь вводит маску типа: {login}:{password}:{email}
// Затем вводит данные типа: user1:pass123:user1@mail.com
```

**Разбор**:
```csharp
public static string FormSocial(
    this IZennoPosterProjectModel project,     // Метод расширения
    string tableName,                           // Имя таблицы базы данных
    string formTitle,                           // Заголовок диалога
    string message = "Enter mask format using {field} syntax:") // Инструкции
{
    // Возвращает: string - количество импортированных записей
    // Использует парсинг маски с заполнителями {имя_поля}
    // Поддерживает гибкие форматы данных
    // Автоматически сопоставляет распарсенные поля с колонками БД
    // Специальная обработка для поля CODE2FA (извлекает значение после '/')
}
```

---

