# Класс F0rms

Класс для создания интерактивных диалоговых окон Windows Forms для сбора пользовательского ввода во время автоматизации.

---

## Конструктор

### Назначение
Инициализирует класс F0rms с экземпляром проекта для логирования и взаимодействия.

### Пример
```csharp
using z3nCore;

var forms = new F0rms(project);
```

---

## InputBox

### Назначение
Отображает простое диалоговое окно ввода для получения текста от пользователя.

### Пример
```csharp
using z3nCore;

// Простой ввод
string code = F0rms.InputBox("Введите код подтверждения:");

// Диалог с настраиваемым размером
string apiKey = F0rms.InputBox("Введите API ключ:", width: 800, height: 400);

// Использование введенных данных
if (!string.IsNullOrEmpty(code))
{
    project.Variables["code"].Value = code;
}
```

### Детали
```csharp
public static string InputBox(
    string message = "input data please",  // Сообщение-приглашение для пользователя
    int width = 600,                       // Ширина диалога в пикселях
    int height = 600)                      // Высота диалога в пикселях

// Возвращает: Текст, введенный пользователем
// Возвращает: Пустую строку, если пользователь закрыл диалог без ввода

// Особенности:
// - Многострочный ввод текста
// - Кнопка OK для подтверждения
// - Шрифт Cascadia Mono SemiBold
```

---

## GetLinesByKey

### Назначение
Отображает форму для сбора нескольких строк данных с пользовательским именем колонки-ключа, отформатированных для обновления базы данных.

### Пример
```csharp
var forms = new F0rms(project);

// Собрать адреса кошельков
Dictionary<string, string> data = forms.GetLinesByKey("address", "Импорт адресов кошельков");

// Формат результата:
// {
//   "1": "address = '0x123...'",
//   "2": "address = '0x456...'",
//   "3": "address = '0x789...'"
// }

// Использование в обновлении базы данных
foreach (var kvp in data)
{
    project.SendInfoToLog($"Строка {kvp.Key}: {kvp.Value}");
}
```

### Детали
```csharp
public Dictionary<string, string> GetLinesByKey(
    string keycolumn = "input Column Name",     // Имя колонки для данных
    string title = "Input data line per line")  // Заголовок диалога

// Возвращает: Dictionary с числовыми ключами (1, 2, 3...) и отформатированными значениями
//   Ключ: Порядковый номер в виде строки ("1", "2", "3"...)
//   Значение: Отформатировано как "columnName = 'escaped_value'"

// Возвращает: null если пользователь отменил ввод или поля пусты

// Особенности:
// - Автоматически экранирует одинарные кавычки в значениях
// - Пропускает пустые строки
// - Логирует предупреждения для пустых строк
// - Значения обрезаются
```

---

## GetLines

### Назначение
Аналогичен GetLinesByKey, но возвращает List вместо Dictionary, отформатированный для SQL-обновлений.

### Пример
```csharp
var forms = new F0rms(project);

// Собрать email адреса
List<string> emails = forms.GetLines("email", "Импорт списка Email");

// Формат результата:
// [
//   "email = 'user1@example.com'",
//   "email = 'user2@example.com'"
// ]

// Использование для пакетных обновлений
foreach (string emailUpdate in emails)
{
    // Выполнить SQL обновление с emailUpdate
}
```

### Детали
```csharp
public List<string> GetLines(
    string keycolumn = "input Column Name",     // Имя колонки для данных
    string title = "Input data line per line")  // Заголовок диалога

// Возвращает: List отформатированных строк "columnName = 'escaped_value'"
// Возвращает: null если пользователь отменил ввод или поля пусты

// Особенности:
// - Возвращает List<string> вместо Dictionary
// - Такое же форматирование как GetLinesByKey
// - Одинарные кавычки экранируются
```

---

## GetKeyValuePairs

### Назначение
Отображает форму с несколькими полями ввода ключ-значение для сбора структурированных данных.

### Пример
```csharp
var forms = new F0rms(project);

// Простое использование
Dictionary<string, string> config = forms.GetKeyValuePairs(
    quantity: 3,
    title: "Введите конфигурацию"
);

// С плейсхолдерами
List<string> keys = new List<string> { "apiKey", "secretKey", "endpoint" };
List<string> values = new List<string> { "your-api-key", "your-secret", "https://api.example.com" };

Dictionary<string, string> params = forms.GetKeyValuePairs(
    quantity: 3,
    keyPlaceholders: keys,
    valuePlaceholders: values,
    title: "Конфигурация API",
    prepareUpd: true
);

// Результат с prepareUpd=true:
// {
//   "1": "apikey = 'abc123'",
//   "2": "secretkey = 'xyz789'",
//   "3": "endpoint = 'https://api.example.com'"
// }

// Результат с prepareUpd=false:
// {
//   "apikey": "abc123",
//   "secretkey": "xyz789",
//   "endpoint": "https://api.example.com"
// }
```

### Детали
```csharp
public Dictionary<string, string> GetKeyValuePairs(
    int quantity,                          // Количество пар ключ-значение для сбора
    List<string> keyPlaceholders = null,   // Имена ключей по умолчанию
    List<string> valuePlaceholders = null, // Значения-плейсхолдеры, отображаемые серым
    string title = "Input Key-Value Pairs", // Заголовок диалога
    bool prepareUpd = true)                // Форматировать для SQL UPDATE (true) или сырые пары (false)

// Возвращает: Dictionary пар ключ-значение
//   Если prepareUpd=true: Ключи "1","2","3"..., Значения "key = 'value'"
//   Если prepareUpd=false: Ключи - фактические введенные ключи, Значения - фактические введенные значения

// Возвращает: null если пользователь отменил ввод или не введено валидных пар

// Особенности:
// - Динамическая форма с указанным количеством полей
// - Поддержка плейсхолдеров для ключей и значений
// - Ключи преобразуются в нижний регистр
// - Пропускает пустые или содержащие только плейсхолдеры значения
// - Экранирует одинарные кавычки в значениях
```

---

## GetKeyBoolPairs

### Назначение
Отображает форму с чекбоксами для сбора булевых значений для предопределенных ключей.

### Пример
```csharp
var forms = new F0rms(project);

List<string> features = new List<string> { "AutoSave", "DarkMode", "Notifications" };
List<string> descriptions = new List<string> {
    "Включить автосохранение",
    "Использовать темную тему",
    "Показывать уведомления"
};

Dictionary<string, bool> settings = forms.GetKeyBoolPairs(
    quantity: 3,
    keyPlaceholders: features,
    valuePlaceholders: descriptions,
    title: "Настройки функций",
    prepareUpd: false
);

// Результат:
// {
//   "autosave": true,
//   "darkmode": false,
//   "notifications": true
// }

// Применить настройки
foreach (var kvp in settings)
{
    project.SendInfoToLog($"{kvp.Key}: {kvp.Value}");
}
```

### Детали
```csharp
public Dictionary<string, bool> GetKeyBoolPairs(
    int quantity,                           // Количество чекбоксов для отображения
    List<string> keyPlaceholders = null,    // Метки для чекбоксов
    List<string> valuePlaceholders = null,  // Описания чекбоксов
    string title = "Input Key-Bool Pairs",  // Заголовок диалога
    bool prepareUpd = true)                 // Использовать числовые ключи (true) или метки (false)

// Возвращает: Dictionary со строковыми ключами и булевыми значениями
//   Если prepareUpd=true: Ключи "1","2","3"...
//   Если prepareUpd=false: Ключи - метки в нижнем регистре

// Возвращает: null если пользователь отменил ввод или нет валидных пар

// Особенности:
// - Интерфейс с чекбоксами для булевого ввода
// - Все чекбоксы по умолчанию не отмечены (false)
// - Ключи преобразуются в нижний регистр
```

---

## GetKeyValueString

### Назначение
Собирает пары ключ-значение и возвращает их как единую отформатированную строку, подходящую для SQL SET.

### Пример
```csharp
var forms = new F0rms(project);

List<string> keys = new List<string> { "name", "email", "age" };
List<string> values = new List<string> { "John Doe", "john@example.com", "30" };

string updateString = forms.GetKeyValueString(
    quantity: 3,
    keyPlaceholders: keys,
    valuePlaceholders: values,
    title: "Обновить данные пользователя"
);

// Результат: "name='John Doe', email='john@example.com', age='30'"

// Использование в SQL
string sql = $"UPDATE users SET {updateString} WHERE id = 1";
```

### Детали
```csharp
public string GetKeyValueString(
    int quantity,                          // Количество пар ключ-значение
    List<string> keyPlaceholders = null,   // Имена ключей по умолчанию
    List<string> valuePlaceholders = null, // Значения-плейсхолдеры
    string title = "Input Key-Value Pairs") // Заголовок диалога

// Возвращает: Строку в формате "key1='value1', key2='value2', key3='value3'"
// Возвращает: null если пользователь отменил ввод или не введено валидных пар

// Особенности:
// - Идеально для SQL SET конструкций
// - Экранирует одинарные кавычки в значениях
// - Ключи преобразуются в нижний регистр
// - Формат с разделителями-запятыми
```

---

## GetSelectedItem

### Назначение
Отображает выпадающий список для выбора одного элемента из списка опций.

### Пример
```csharp
var forms = new F0rms(project);

List<string> networks = new List<string> { "Mainnet", "Testnet", "Devnet" };

string selected = forms.GetSelectedItem(
    items: networks,
    title: "Выбрать сеть",
    labelText: "Выберите сеть:"
);

// Результат: "Mainnet" (или что выбрал пользователь)

// Использование выбора
if (selected == "Mainnet")
{
    project.Variables["rpcUrl"].Value = "https://mainnet.example.com";
}
```

### Детали
```csharp
public string GetSelectedItem(
    List<string> items,              // Список элементов для выбора
    string title = "Select an Item", // Заголовок диалога
    string labelText = "Select:")    // Текст метки над выпадающим списком

// Возвращает: Выбранный элемент в виде строки
// Возвращает: null если пользователь отменил выбор или список пуст

// Особенности:
// - Интерфейс с выпадающим списком (ComboBox)
// - Первый элемент выбран по умолчанию
// - Стиль DropDownList (только выбор, без ввода)
// - Проверяет, что список не null и не пуст
```

---
