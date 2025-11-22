# Документация класса Guild

## Класс: Guild

### Конструктор

#### Guild(IZennoPosterProjectModel project, Instance instance, bool log = false)

**Назначение**: Инициализирует новый экземпляр автоматизации Guild для парсинга данных платформы Guild.xyz.

**Пример**:
```csharp
var guild = new Guild(project, instance, log: true);
```

**Разбор**:
```csharp
// Параметры:
// - project: IZennoPosterProjectModel - Модель проекта для операций с базой данных
// - instance: Instance - Экземпляр браузера для автоматизации
// - log: bool - Включить логирование (по умолчанию: false)
// Возвращает: Экземпляр Guild
// Примечание: Используется для извлечения требований ролей и статуса подключений с Guild.xyz
```

---

## Методы парсинга данных

### ParseRoles(string tablename, bool append = true)

**Назначение**: Парсит роли Guild.xyz, извлекая выполненные и невыполненные роли с их требованиями.

**Пример**:
```csharp
guild.ParseRoles("myproject", append: true);
string doneRoles = project.Variables["guildDone"].Value;
string undoneRoles = project.Variables["guildUndone"].Value;
```

**Разбор**:
```csharp
// Параметры:
// - tablename: string - Имя таблицы базы данных для сохранения результатов
// - append: bool - Добавить к существующим данным вместо замены (по умолчанию: true)
// Возвращает: void
// Примечание: Создает колонки "guild_done" и "guild_undone" в указанной таблице
// Сохраняет выполненные роли с общим количеством в guild_done
// Сохраняет невыполненные роли со списками задач или статусом переподключения в guild_undone
// Сохраняет данные в формате JSON в переменные проекта и базу данных
```

### ParseConnections()

**Назначение**: Извлекает статус подключения социальных платформ из профиля Guild.xyz.

**Пример**:
```csharp
Dictionary<string, string> connections = guild.ParseConnections();
foreach (var conn in connections) {
    project.SendInfoToLog($"{conn.Key}: {conn.Value}");
}
```

**Разбор**:
```csharp
// Параметры: Отсутствуют
// Возвращает: Dictionary<string, string> - Имя платформы как ключ, статус/данные подключения как значение
// Примечание: Идентифицирует платформы по иконкам SVG (discord, twitter, github, email, telegram, farcaster, world, google)
// Возвращает "none" для неподключенных платформ
// Возвращает данные подключения для подключенных платформ
```

---

## Вспомогательные методы

### Svg(string d)

**Назначение**: Определяет тип социальной платформы по данным пути SVG.

**Пример**:
```csharp
string svgPath = "M108,136a16,16,0,1,1-16-16A16,16,0,0,1,108,136Zm56-16a16...";
string platform = guild.Svg(svgPath);
// Возвращает: "discord"
```

**Разбор**:
```csharp
// Параметры:
// - d: string - Строка данных пути SVG
// Возвращает: string - Имя платформы ("discord", "twitter", "github", "google", "email", "telegram", "farcaster", "world") или пустую строку
// Примечание: Сопоставляет путь SVG с известными иконками платформ
```

### Svg(HtmlElement he)

**Назначение**: Определяет тип социальной платформы из SVG содержимого HtmlElement.

**Пример**:
```csharp
HtmlElement element = instance.GetHe(("svg", "class", "icon", "regexp", 0));
string platform = guild.Svg(element);
```

**Разбор**:
```csharp
// Параметры:
// - he: HtmlElement - HTML элемент, содержащий SVG
// Возвращает: string - Имя платформы или пустую строку
// Примечание: Извлекает InnerHtml из элемента и передает в метод Svg(string d)
```

### MainButton()

**Назначение**: Получает элемент основной кнопки действия из интерфейса Guild.xyz.

**Пример**:
```csharp
HtmlElement button = guild.MainButton();
instance.HeClick(button);
```

**Разбор**:
```csharp
// Параметры: Отсутствуют
// Возвращает: HtmlElement - Элемент основной кнопки
// Примечание: Находит кнопку по сложному шаблону атрибута класса
// Используется для основных действий на страницах Guild.xyz
```
