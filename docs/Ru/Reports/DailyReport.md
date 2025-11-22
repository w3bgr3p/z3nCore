# Классы DailyReport

Предоставляет инструменты для генерации комплексных HTML-отчетов с тепловыми картами и статистикой для отслеживания выполнения проектов.

---

## Класс ProjectData

### `ProjectData.CollectData(IZennoPosterProjectModel project, string tableName)` [static]

**Назначение**
Собирает и парсит данные выполнения из таблицы проекта, извлекая статус завершения, временные метки и детали выполнения для всех обработанных аккаунтов.

**Пример**
```csharp
// Собрать данные из конкретной таблицы проекта
var projectData = DailyReport.ProjectData.CollectData(project, "__myProject");

// Получить доступ к собранным данным
foreach (var account in projectData.All.Keys)
{
    var data = projectData.All[account];
    string status = data[0];      // "+" или "-"
    string timestamp = data[1];    // ISO временная метка
    string execTime = data[2];     // Время выполнения в секундах
    string report = data[3];       // Подробный отчет
}
```

**Разбор**
```csharp
// Параметры:
// - project: Экземпляр IZennoPosterProjectModel для операций с базой данных
// - tableName: Имя таблицы проекта для сбора данных
var projectData = DailyReport.ProjectData.CollectData(project, "__projectName");

// Возвращает: Экземпляр ProjectData с:
//   - ProjectName: Очищенное имя таблицы (без префикса "__")
//   - All: Dictionary<string, string[]> где:
//       Ключ: ID аккаунта
//       Значение: string[] с 4 элементами:
//         [0] = Статус завершения ("+" успех, "-" ошибка)
//         [1] = UTC временная метка (формат ISO 8601)
//         [2] = Время выполнения в секундах
//         [3] = Полный текст отчета (многострочный)
// Источник данных: Запрашивает колонки "id, last" где last LIKE '+ %' OR '- %'
// Исключения: Нет (некорректные записи пропускаются)
```

---

## Класс HtmlEncoder

### `HtmlEncoder.HtmlEncode(string text)` [static]

**Назначение**
Кодирует текст для безопасного вывода в HTML путем экранирования специальных символов.

**Пример**
```csharp
// Закодировать текст с HTML специальными символами
string rawText = "<div>Аккаунт & 'Данные'</div>";
string encoded = DailyReport.HtmlEncoder.HtmlEncode(rawText);
// Результат: "&lt;div&gt;Аккаунт &amp; &#39;Данные&#39;&lt;/div&gt;"
```

**Разбор**
```csharp
// Параметр:
// - text: Строка для кодирования в HTML вывод
string encoded = DailyReport.HtmlEncoder.HtmlEncode("<script>alert('XSS')</script>");

// Возвращает: HTML-безопасную закодированную строку
// Замены символов:
//   & → &amp;
//   < → &lt;
//   > → &gt;
//   " → &quot;
//   ' → &#39;
// Возвращает оригинальную строку, если null или пустая
// Исключения: Нет
```

---

### `HtmlEncoder.HtmlAttributeEncode(string text)` [static]

**Назначение**
Кодирует текст для безопасного использования в HTML атрибутах (data-*, title и т.д.).

**Пример**
```csharp
// Закодировать текст для HTML атрибута
string tooltipData = "Аккаунт: acc1 | Баланс: 5.23";
string encoded = DailyReport.HtmlEncoder.HtmlAttributeEncode(tooltipData);

// Использовать в HTML атрибуте
string html = $"<div data-tooltip='{encoded}'>Ячейка</div>";
```

**Разбор**
```csharp
// Параметр:
// - text: Строка для кодирования в HTML атрибут
string encoded = DailyReport.HtmlEncoder.HtmlAttributeEncode("значение=\"тест\"");

// Возвращает: Безопасную для атрибутов закодированную строку
// Замены символов:
//   & → &amp;
//   " → &quot;
//   ' → &#39;
//   < → &lt;
//   > → &gt;
// Возвращает оригинальную строку, если null или пустая
// Исключения: Нет
```

---

## Класс FarmReportGenerator

### `FarmReportGenerator.GenerateHtmlReport(List<ProjectData> projects, string userId = null)` [static]

**Назначение**
Генерирует комплексный HTML ежедневный отчет с тепловыми картами, статистикой и сводками выполнения для нескольких проектов.

**Пример**
```csharp
// Собрать данные из нескольких проектов
var projects = new List<DailyReport.ProjectData>();
projects.Add(DailyReport.ProjectData.CollectData(project, "__project1"));
projects.Add(DailyReport.ProjectData.CollectData(project, "__project2"));

// Сгенерировать HTML отчет
string htmlContent = DailyReport.FarmReportGenerator.GenerateHtmlReport(
    projects,
    userId: "admin"
);

// Сохранить в файл
File.WriteAllText("report.html", htmlContent, Encoding.UTF8);
```

**Разбор**
```csharp
// Параметры:
// - projects: Список экземпляров ProjectData для включения в отчет
// - userId: Опциональный идентификатор пользователя для отображения в заголовке (по умолчанию: null)
string html = DailyReport.FarmReportGenerator.GenerateHtmlReport(
    projects: projectsList,
    userId: "john_doe"
);

// Возвращает: Полный HTML документ как строку
// Отчет включает:
//   - Карточки сводки: Всего попыток, успешных, неудачных (с процентами)
//   - Интерактивная тепловая карта для каждого проекта с:
//       * Цветными ячейками по дате завершения (сегодня, вчера, 2 дня, 3+ дней)
//       * Индикаторы Успеха (зеленые тона) vs Ошибки (красные тона)
//       * Всплывающие подсказки при наведении с деталями аккаунта, метками времени, временем выполнения
//       * Функция копирования по клику
//   - Статистика проекта: Мин/Макс/Средн время выполнения, процент успеха
//   - Боковая панель ZennoProcesses (базовая версия без деталей PID)
//   - Секция неактивных проектов (проекты без данных выполнения)
// Цветовое кодирование тепловой карты:
//   - Сегодня: Яркие цвета (зеленый/красный)
//   - Вчера: Средние тона
//   - 2 дня назад: Темные тона
//   - 3+ дней: Очень темные тона
//   - Не обработано: Прозрачный
// Исключения: Нет (пустые списки обрабатываются корректно)
```

---

### `FarmReportGenerator.GenerateHtmlReportWithPid(List<ProjectData> projects, string userId = null)` [static]

**Назначение**
Генерирует расширенный HTML ежедневный отчет с дополнительным отслеживанием ID процессов и информацией о привязке аккаунтов.

**Пример**
```csharp
// Собрать данные из нескольких проектов
var projects = new List<DailyReport.ProjectData>();
projects.Add(DailyReport.ProjectData.CollectData(project, "__swapper"));
projects.Add(DailyReport.ProjectData.CollectData(project, "__bridge"));

// Сгенерировать расширенный отчет с отслеживанием PID
string htmlContent = DailyReport.FarmReportGenerator.GenerateHtmlReportWithPid(
    projects,
    userId: "operator_1"
);

// Сохранить и открыть
File.WriteAllText("dailyReport.html", htmlContent, Encoding.UTF8);
Process.Start("dailyReport.html");
```

**Разбор**
```csharp
// Параметры:
// - projects: Список экземпляров ProjectData для включения в отчет
// - userId: Опциональный идентификатор пользователя для заголовка отчета (по умолчанию: null)
string html = DailyReport.FarmReportGenerator.GenerateHtmlReportWithPid(
    projects: projectDataList,
    userId: "admin"
);

// Возвращает: Полный HTML документ как строку
// Дополнительные возможности по сравнению с GenerateHtmlReport:
//   - Боковая панель ZennoProcesses с расширенными деталями:
//       * ID процесса (PID)
//       * Использование памяти в МБ
//       * Возраст процесса в минутах
//       * Привязка имени проекта
//       * Привязка аккаунта (acc#, unbinded, или unknown)
//   - Те же тепловая карта и статистика, что и в стандартном отчете
// Источник данных: Использует ProcAcc.PidReport() для отслеживания процессов
// Формат отображения: "acc123 [projectName]" или "unbinded"
// Исключения: Нет (корректно обрабатывает сбои ProcAcc.PidReport())
```
