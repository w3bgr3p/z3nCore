# Документация класса dSql

Универсальный класс для работы с базами данных, поддерживающий SQLite и PostgreSQL с асинхронными операциями и возможностями миграции.

---

## Перегрузки конструктора

### Конструктор (SQLite)
**Назначение**: Создает подключение к базе данных SQLite.

**Пример**:
```csharp
var db = new dSql("/path/to/database.db", null);
db.Dispose();
```

**Разбор**:
```csharp
public dSql(
    string dbPath,   // Путь к файлу базы данных SQLite
    string dbPass    // Пароль (не используется для SQLite, передайте null)
)
// Открывает соединение немедленно
// Соединение готово для запросов после создания
```

---

### Конструктор (PostgreSQL с деталями)
**Назначение**: Создает подключение PostgreSQL с отдельными параметрами.

**Пример**:
```csharp
var db = new dSql("localhost", "5432", "mydb", "postgres", "password");
db.Dispose();
```

**Разбор**:
```csharp
public dSql(
    string hostname,  // Имя хоста сервера
    string port,      // Номер порта
    string database,  // Имя базы данных
    string user,      // Имя пользователя
    string password   // Пароль
)
// Открывает соединение немедленно
// Использует пулинг соединений
```

---

### Конструктор (PostgreSQL со строкой подключения)
**Назначение**: Создает подключение PostgreSQL из строки подключения.

**Пример**:
```csharp
string connStr = "Host=localhost;Port=5432;Database=mydb;Username=postgres;Password=pass";
var db = new dSql(connStr);
db.Dispose();
```

**Разбор**:
```csharp
public dSql(
    string connectionstring  // Полная строка подключения PostgreSQL
)
// Открывает соединение немедленно
```

---

### Конструктор (существующее подключение)
**Назначение**: Оборачивает существующее подключение к базе данных.

**Пример**:
```csharp
IDbConnection existingConn = GetConnection();
var db = new dSql(existingConn);
```

**Разбор**:
```csharp
public dSql(
    IDbConnection connection  // Существующее открытое соединение
)
// Открывает соединение, если оно еще не открыто
// Не освобождает оригинальное соединение
// Выбрасывает: ArgumentNullException если connection равен null
```

---

## Свойства

### ConnectionType
**Назначение**: Получает тип базы данных текущего подключения.

**Пример**:
```csharp
var db = new dSql("/path/to/db.db", null);
if (db.ConnectionType == DatabaseType.SQLite)
{
    // Логика для SQLite
}
```

**Разбор**:
```csharp
public DatabaseType ConnectionType { get; }
// Возвращает: Значение перечисления DatabaseType
//   - DatabaseType.SQLite
//   - DatabaseType.PostgreSQL
//   - DatabaseType.Unknown
```

---

## Методы чтения/записи

### DbReadAsync
**Назначение**: Выполняет SELECT запрос асинхронно и возвращает отформатированные результаты.

**Пример**:
```csharp
var db = new dSql("/path/to/db.db", null);
string result = await db.DbReadAsync("SELECT email, username FROM users");
// Результат: "email1¦username1·email2¦username2"

// Пользовательские разделители
string result = await db.DbReadAsync("SELECT * FROM users", "|", "\n");
```

**Разбор**:
```csharp
public async Task<string> DbReadAsync(
    string sql,                      // SQL SELECT запрос
    string columnSeparator = "|",    // Разделитель между колонками
    string rawSeparator = "\r\n"     // Разделитель между строками
)
// Возвращает: Task<string> с отформатированными результатами
// Пустые ячейки возвращаются как пустые строки
// Выбрасывает: NotSupportedException для неизвестного типа подключения
//         ObjectDisposedException если объект освобожден
```

---

### DbRead
**Назначение**: Выполняет SELECT запрос синхронно (обертка для DbReadAsync).

**Пример**:
```csharp
var db = new dSql("/path/to/db.db", null);
string result = db.DbRead("SELECT email FROM users");
```

**Разбор**:
```csharp
public string DbRead(
    string sql,              // SQL SELECT запрос
    string separator = "|"   // Разделитель колонок
)
// Возвращает: Отформатированные результаты запроса
// Синхронная обертка вокруг DbReadAsync
// Использует \r\n как разделитель строк
```

---

### DbWriteAsync
**Назначение**: Выполняет INSERT, UPDATE, DELETE или DDL команды асинхронно.

**Пример**:
```csharp
var db = new dSql("/path/to/db.db", null);

// Простое обновление
int affected = await db.DbWriteAsync("UPDATE users SET status = 'active'");

// С параметрами
var param = db.CreateParameter("@email", "test@example.com");
int affected = await db.DbWriteAsync("UPDATE users SET email = @email", param);
```

**Разбор**:
```csharp
public async Task<int> DbWriteAsync(
    string sql,                          // SQL команда
    params IDbDataParameter[] parameters // Опциональные параметры
)
// Возвращает: Task<int> с количеством затронутых строк
// Поддерживает параметризованные запросы
// Выбрасывает: Exception с деталями запроса при ошибке
//         NotSupportedException для неизвестного типа подключения
```

---

### DbWrite
**Назначение**: Выполняет команду записи синхронно (обертка для DbWriteAsync).

**Пример**:
```csharp
var db = new dSql("/path/to/db.db", null);
int affected = db.DbWrite("DELETE FROM users WHERE status = 'inactive'");
```

**Разбор**:
```csharp
public int DbWrite(
    string sql,                          // SQL команда
    params IDbDataParameter[] parameters // Опциональные параметры
)
// Возвращает: Количество затронутых строк
// Синхронная обертка вокруг DbWriteAsync
```

---

## Помощники параметров

### CreateParameter
**Назначение**: Создает специфичный для базы данных параметр для безопасных запросов.

**Пример**:
```csharp
var db = new dSql("/path/to/db.db", null);
var param = db.CreateParameter("@email", "test@example.com");
db.DbWrite("UPDATE users SET email = @email WHERE id = 1", param);
```

**Разбор**:
```csharp
public IDbDataParameter CreateParameter(
    string name,   // Имя параметра (с префиксом @)
    object value   // Значение параметра (null становится DBNull.Value)
)
// Возвращает: Объект параметра для конкретной базы данных
//   - SqliteParameter для SQLite
//   - NpgsqlParameter для PostgreSQL
// Выбрасывает: NotSupportedException для неизвестного типа подключения
```

---

### CreateParameters
**Назначение**: Создает несколько параметров из кортежей.

**Пример**:
```csharp
var db = new dSql("/path/to/db.db", null);
var parameters = db.CreateParameters(
    ("@email", "test@example.com"),
    ("@status", "active")
);
db.DbWrite("UPDATE users SET email = @email, status = @status", parameters);
```

**Разбор**:
```csharp
public IDbDataParameter[] CreateParameters(
    params (string name, object value)[] parameters  // Кортежи имя-значение
)
// Возвращает: Массив параметров базы данных
// Удобно для множественных параметров
```

---

## Операции с таблицами

### CopyTableAsync
**Назначение**: Копирует структуру и данные таблицы в новую таблицу.

**Пример**:
```csharp
var db = new dSql("/path/to/db.db", null);

// Копирование в пределах той же базы данных
int rows = await db.CopyTableAsync("old_users", "new_users");

// PostgreSQL со схемами
int rows = await db.CopyTableAsync("public.old_users", "archive.users");
```

**Разбор**:
```csharp
public async Task<int> CopyTableAsync(
    string sourceTable,       // Имя исходной таблицы (опционально schema.table)
    string destinationTable   // Имя целевой таблицы
)
// Возвращает: Task<int> с количеством скопированных строк
// Копирует полную структуру таблицы включая:
//   - Типы колонок
//   - Ограничения NOT NULL
//   - Значения DEFAULT
//   - Ограничения PRIMARY KEY
// Создает целевую таблицу автоматически
// Поддерживает квалификацию схемы для PostgreSQL
// Выбрасывает: ArgumentException для недопустимых имен или форматов таблиц
//         Exception с подробными сообщениями об ошибках
```

---

### MigrateAllTablesAsync (статический)
**Назначение**: Мигрирует все таблицы между базами данных SQLite и PostgreSQL.

**Пример**:
```csharp
var sqliteDb = new dSql("/path/to/sqlite.db", null);
var pgDb = new dSql("Host=localhost;Database=postgres;Username=postgres;Password=pass");

int totalRows = await dSql.MigrateAllTablesAsync(sqliteDb, pgDb);
// Все таблицы мигрированы из SQLite в PostgreSQL

sqliteDb.Dispose();
pgDb.Dispose();
```

**Разбор**:
```csharp
public static async Task<int> MigrateAllTablesAsync(
    dSql sourceDb,       // Подключение к исходной базе данных
    dSql destinationDb   // Подключение к целевой базе данных
)
// Возвращает: Task<int> с общим количеством мигрированных строк
// Мигрирует все пользовательские таблицы (исключая системные)
// Автоматически обрабатывает:
//   - Преобразование типов между базами данных
//   - Определение схемы
//   - Сохранение первичного ключа
//   - Преобразование значений DEFAULT
// Пропускает таблицы, которые уже существуют в назначении
// Продолжает миграцию, даже если отдельные таблицы не удались
// Выбрасывает: ArgumentNullException если любая из баз данных null
//         ArgumentException если источник и назначение одного типа
//         NotSupportedException для неподдерживаемых типов баз данных
```

---

## CRUD операции с Dapper

### Upd (обновление записи)
**Назначение**: Обновляет запись базы данных с интеграцией Dapper.

**Пример**:
```csharp
var db = new dSql("/path/to/db.db", null);

// Обновление по id
await db.Upd("email = 'new@example.com', status = 'active'",
             id: 123,
             tableName: "users");

// С временной меткой last
await db.Upd("status = 'completed'",
             id: 123,
             tableName: "users",
             last: true);

// Пользовательское WHERE
await db.Upd("status = 'processed'",
             id: null,
             tableName: "users",
             where: "status = 'pending'");
```

**Разбор**:
```csharp
public async Task<int> Upd(
    string toUpd,            // Выражение обновления
    object id,               // ID записи
    string tableName = null, // Имя таблицы (обязательно)
    string where = null,     // Пользовательское условие WHERE
    bool last = false        // Добавить колонку временной метки last
)
// Возвращает: Task<int> с количеством затронутых строк
// Автоматически экранирует имена колонок
// Добавляет колонку last с временной меткой UTC, если last=true
// Выбрасывает: Exception если tableName равен null
//         Exception с отформатированным запросом при ошибке
```

---

### Upd (обновление нескольких записей из списка)
**Назначение**: Обновляет несколько записей из списка значений.

**Пример**:
```csharp
var db = new dSql("/path/to/db.db", null);
var values = new List<string> { "value1", "value2", "value3" };
await db.Upd(values, tableName: "users");
```

**Разбор**:
```csharp
public async Task Upd(
    List<string> toWrite,    // Значения для записи
    string tableName = null, // Имя таблицы
    string where = null,     // Пользовательское WHERE
    bool last = false        // Добавить временную метку
)
// Обновляет записи с id = 0, 1, 2, ... последовательно
```

---

### Get
**Назначение**: Извлекает значение из базы данных, используя Dapper.

**Пример**:
```csharp
var db = new dSql("/path/to/db.db", null);
string email = await db.Get("email", "123", "users");

// Пользовательское WHERE
string email = await db.Get("email", null, "users", where: "status = 'active'");
```

**Разбор**:
```csharp
public async Task<string> Get(
    string toGet,            // Имя колонки(колонок)
    string id,               // ID записи
    string tableName = null, // Имя таблицы (обязательно)
    string where = null      // Пользовательское условие WHERE
)
// Возвращает: Task<string> со значением
// Автоматически экранирует имена колонок
// Выбрасывает: Exception если tableName равен null
```

---

### AddRange
**Назначение**: Вставляет диапазон записей ID в таблицу.

**Пример**:
```csharp
var db = new dSql("/path/to/db.db", null);
await db.AddRange(100, "users");  // Вставляет ID 1-100
```

**Разбор**:
```csharp
public async Task AddRange(
    int range,               // Максимальный ID для вставки
    string tableName = null  // Имя таблицы (обязательно)
)
// Вставляет отсутствующие ID от (текущий макс + 1) до range
// Использует ON CONFLICT DO NOTHING для избежания дубликатов
// Выбрасывает: Exception если tableName равен null
```

---

## Управление ресурсами

### Dispose
**Назначение**: Закрывает соединение и освобождает ресурсы.

**Пример**:
```csharp
var db = new dSql("/path/to/db.db", null);
// ... использование базы данных ...
db.Dispose();

// Или используйте оператор using (рекомендуется)
using (var db = new dSql("/path/to/db.db", null))
{
    // Соединение автоматически освобождается
}
```

**Разбор**:
```csharp
public void Dispose()
// Закрывает подключение к базе данных
// Освобождает все ресурсы
// Безопасно вызывать несколько раз
// Реализует паттерн IDisposable
```

---

## Перечисления

### DatabaseType
```csharp
public enum DatabaseType
{
    Unknown,      // Неизвестная или неподдерживаемая база данных
    SQLite,       // База данных SQLite
    PostgreSQL    // База данных PostgreSQL
}
```

---
