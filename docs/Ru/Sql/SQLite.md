# Документация класса SQLite

Статический класс, предоставляющий операции с базой данных SQLite для проектов ZennoPoster с использованием ODBC подключения.

---

## Публичные методы

### lSQL
**Назначение**: Выполняет SQL запросы к базе данных SQLite.

**Пример**:
```csharp
// SELECT запрос
string result = project.lSQL("SELECT email FROM users WHERE id = 1");

// UPDATE запрос
project.lSQL("UPDATE users SET status = 'active' WHERE id = 1");

// С включенным логированием
project.lSQL("SELECT * FROM users", log: true);

// Игнорировать ошибки
project.lSQL("INSERT INTO users (id) VALUES (1)", ignoreErrors: true);
```

**Разбор**:
```csharp
public static string lSQL(
    IZennoPosterProjectModel project,  // Экземпляр проекта ZennoPoster
    string query,                      // SQL запрос для выполнения
    bool log = false,                  // Включить логирование с цветным выводом
    bool ignoreErrors = false          // Вернуть пустую строку при ошибке вместо выброса исключения
)
// Возвращает: Строку результата запроса
//   - SELECT: строки разделены \r\n, колонки разделены |
//   - Другие: количество затронутых строк
// Использует ODBC подключение с путем к базе данных из переменной DBsqltPath
// Логирование показывает разные цвета для SELECT (серый) и модификаций (по умолчанию)
// Показывает специальный цвет, когда не затронуто строк
// Выбрасывает: Exception при ошибке (если только ignoreErrors=true)
```

---

### lSQLMakeTable
**Назначение**: Создает или обновляет таблицу SQLite с автоматическим управлением колонками.

**Пример**:
```csharp
// Создать таблицу со структурой
var structure = new Dictionary<string, string> {
    {"id", "INTEGER PRIMARY KEY AUTOINCREMENT"},
    {"email", "TEXT DEFAULT ''"},
    {"status", "TEXT DEFAULT ''"},
    {"acc0", "INTEGER"}
};

project.lSQLMakeTable(structure, "users");

// Строгий режим - удаляет колонки, не входящие в структуру
project.lSQLMakeTable(structure, "users", strictMode: true);

// Использовать переменную projectTable
project.lSQLMakeTable(structure);
```

**Разбор**:
```csharp
public static void lSQLMakeTable(
    IZennoPosterProjectModel project,         // Экземпляр проекта ZennoPoster
    Dictionary<string, string> tableStructure, // Имя колонки -> определение SQL типа
    string tableName = "",                     // Имя таблицы (по умолчанию: переменная projectTable)
    bool strictMode = false                    // Удалить колонки, не входящие в структуру
)
// Создает таблицу, если она не существует
// Добавляет отсутствующие колонки, если таблица существует
// Удаляет лишние колонки, если strictMode=true
// Автоматически заполняет колонку acc0 диапазоном (от 1 до переменной rangeEnd)
// Использует INSERT OR IGNORE для избежания дублирования ID
```

---

## Примечания

- Использует встроенное ODBC подключение ZennoPoster
- Путь к базе данных должен быть установлен в переменной проекта DBsqltPath
- Разделитель колонок: `|`
- Разделитель строк: `\r\n`
- Логирование включает сжатие запроса (удаляет лишние пробелы)
- Поддерживает как создание таблиц, так и обновление схемы

---
