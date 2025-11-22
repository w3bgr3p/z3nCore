# Документация класса PostgresDB

Класс для прямых операций с базой данных PostgreSQL с использованием драйвера Npgsql.

---

## Публичные методы

### Конструктор
**Назначение**: Создает новое подключение к базе данных PostgreSQL.

**Пример**:
```csharp
// Базовое подключение
var db = new PostgresDB("localhost", "mydb", "postgres", "password");

// С указанием порта
var db = new PostgresDB("localhost:5433", "mydb", "postgres", "password");
```

**Разбор**:
```csharp
public PostgresDB(
    string host,        // Адрес хоста с опциональным портом (например, "localhost:5432")
    string database,    // Имя базы данных
    string user,        // Имя пользователя
    string password     // Пароль
)
// Создает строку подключения, но не открывает соединение
// Вызовите метод Open() для установления соединения
// Поддерживает формат host:port для параметра host
```

---

### Open
**Назначение**: Открывает подключение к базе данных.

**Пример**:
```csharp
var db = new PostgresDB("localhost", "mydb", "postgres", "password");
db.Open();
// Соединение готово для запросов
```

**Разбор**:
```csharp
public void Open()
// Открывает подключение PostgreSQL
// Выбрасывает: Exception с сообщением "DB connection failed" при ошибке
// Освобождает соединение при неудачном открытии
```

---

### DbRead
**Назначение**: Выполняет SELECT запрос и возвращает отформатированные результаты.

**Пример**:
```csharp
db.Open();
string result = db.DbRead("SELECT email, username FROM users");
// Формат результата: "email1|username1\r\nemail2|username2"

// Пользовательский разделитель
string result = db.DbRead("SELECT * FROM users", separator: ",");
```

**Разбор**:
```csharp
public string DbRead(
    string sql,              // SQL SELECT запрос
    string separator = "|"   // Разделитель колонок в результатах
)
// Возвращает: Отформатированную строку со строками, разделенными \r\n
// Каждая строка имеет колонки, разделенные параметром separator
// Выбрасывает: InvalidOperationException если соединение не открыто
```

---

### DbWrite
**Назначение**: Выполняет INSERT, UPDATE, DELETE или другие не-запросные SQL команды.

**Пример**:
```csharp
db.Open();

// Простое обновление
int affected = db.DbWrite("UPDATE users SET status = 'active' WHERE id = 1");

// С параметрами (безопаснее против SQL injection)
var param = new NpgsqlParameter("@email", "test@example.com");
int affected = db.DbWrite("UPDATE users SET email = @email WHERE id = 1", param);
```

**Разбор**:
```csharp
public int DbWrite(
    string sql,                        // SQL команда (INSERT, UPDATE, DELETE и т.д.)
    params NpgsqlParameter[] parameters // Опциональные параметры для параметризованных запросов
)
// Возвращает: Количество затронутых строк
// Поддерживает параметризованные запросы для безопасности
// Выбрасывает: Exception с сообщением об ошибке SQL и запросом
// InvalidOperationException если соединение не открыто
```

---

### Raw (Статический)
**Назначение**: Выполняет запрос с автоматическим управлением подключением.

**Пример**:
```csharp
// Не нужно управлять жизненным циклом подключения
string result = PostgresDB.Raw(
    "SELECT email FROM users WHERE id = 1",
    host: "localhost:5432",
    dbName: "mydb",
    dbUser: "postgres",
    dbPswd: "password"
);

// Соединение автоматически открывается и освобождается
```

**Разбор**:
```csharp
public static string Raw(
    string query,                    // SQL запрос для выполнения
    bool throwOnEx = false,          // Выбрасывать исключения при true
    string host = "localhost:5432",  // Хост с опциональным портом
    string dbName = "postgres",      // Имя базы данных
    string dbUser = "postgres",      // Имя пользователя
    string dbPswd = ""               // Пароль (обязателен)
)
// Возвращает: Результат запроса в виде строки
//   - SELECT запросы: отформатированный результат
//   - Другие запросы: количество затронутых строк
// Автоматически открывает соединение, выполняет запрос и освобождает
// Возвращает сообщение об ошибке вместо выброса (если только throwOnEx=true)
// Выбрасывает: Exception если пароль null или пустой
```

---

### DbQueryPostgre (Статический)
**Назначение**: Выполняет запрос, используя переменные проекта ZennoPoster для подключения.

**Пример**:
```csharp
// Использует переменные проекта для деталей подключения
string result = PostgresDB.DbQueryPostgre(
    project,
    "SELECT * FROM users",
    throwOnEx: false
);
```

**Разбор**:
```csharp
public static string DbQueryPostgre(
    IZennoPosterProjectModel project,  // Экземпляр проекта ZennoPoster
    string query,                      // SQL запрос для выполнения
    bool throwOnEx = false,            // Выбрасывать исключения при true
    string host = "localhost:5432",    // Хост с опциональным портом
    string dbName = "postgres",        // Имя базы данных (по умолчанию: переменная DBpstgrName)
    string dbUser = "postgres",        // Имя пользователя (по умолчанию: переменная DBpstgrUser)
    string dbPswd = ""                 // Пароль (по умолчанию: переменная DBpstgrPass)
)
// Возвращает: Результат запроса в виде строки
// Использует переменные проекта, когда параметры null:
//   - DBpstgrName для имени базы данных
//   - DBpstgrUser для имени пользователя
//   - DBpstgrPass для пароля
// Логирует предупреждения при ошибке
// Возвращает пустую строку при ошибке, если только throwOnEx=true
// Выбрасывает: Exception если пароль null или пустой
```

---

### MkTablePostgre (Статический)
**Назначение**: Создает или обновляет таблицу PostgreSQL с автоматическим управлением колонками.

**Пример**:
```csharp
var structure = new Dictionary<string, string> {
    {"id", "INTEGER PRIMARY KEY"},
    {"email", "TEXT DEFAULT ''"},
    {"acc0", "INTEGER"}
};

PostgresDB.MkTablePostgre(
    project,
    structure,
    tableName: "users",
    strictMode: false,
    insertData: true
);
```

**Разбор**:
```csharp
public static void MkTablePostgre(
    IZennoPosterProjectModel project,       // Экземпляр проекта ZennoPoster
    Dictionary<string, string> tableStructure, // Определения колонок
    string tableName = "",                   // Имя таблицы (по умолчанию: переменная projectTable)
    bool strictMode = false,                 // Удалить колонки, не входящие в структуру
    bool insertData = true,                  // Вставить начальный диапазон данных
    string host = null,                      // Адрес хоста
    string dbName = "postgres",              // Имя базы данных
    string dbUser = "postgres",              // Имя пользователя
    string dbPswd = "",                      // Пароль (по умолчанию: переменная DBpstgrPass)
    string schemaName = "projects",          // Имя схемы
    bool log = false                         // Включить логирование
)
// Создает таблицу, если она не существует
// Добавляет отсутствующие колонки
// Удаляет лишние колонки, если strictMode=true
// Вставляет диапазон значений acc0, если колонка acc0 существует и insertData=true
// Автоматически преобразует AUTOINCREMENT в SERIAL
// Использует переменную rangeEnd для диапазона данных
// Выбрасывает: Exception если пароль null
```

---

### Dispose
**Назначение**: Закрывает и освобождает подключение к базе данных.

**Пример**:
```csharp
var db = new PostgresDB("localhost", "mydb", "postgres", "password");
db.Open();
// ... выполнение операций ...
db.Dispose();

// Или используйте оператор using (рекомендуется)
using (var db = new PostgresDB("localhost", "mydb", "postgres", "password"))
{
    db.Open();
    // Соединение автоматически освобождается
}
```

**Разбор**:
```csharp
public void Dispose()
// Закрывает подключение к базе данных
// Освобождает все ресурсы
// Безопасно вызывать несколько раз
// Автоматически вызывается при использовании оператора 'using'
```

---

## Приватные методы (для справки)

### CheckAndCreateTable
Создает таблицу, если она не существует в указанной схеме.

### ManageColumns
Добавляет новые колонки и опционально удаляет колонки, не входящие в структуру (strictMode).

### InsertInitialData
Вставляет диапазон значений acc0 от текущего максимума до rangeEnd.

---
