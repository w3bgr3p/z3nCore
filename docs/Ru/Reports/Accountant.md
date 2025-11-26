# Класс Accountant

Предоставляет инструменты для генерации и отображения балансовых отчетов в формате HTML.

---

## Конструктор

### `Accountant(IZennoPosterProjectModel project, bool log = false)`

**Назначение**
Инициализирует новый экземпляр класса Accountant с контекстом проекта и опциональным логированием.

**Пример**
```csharp
// Создать экземпляр Accountant с включенным логированием
var accountant = new Accountant(project, log: true);
```

**Разбор**
```csharp
// Параметры:
// - project: Экземпляр IZennoPosterProjectModel для операций проекта
// - log: Опциональный boolean для включения/отключения логирования (по умолчанию: false)
var accountant = new Accountant(project, log: true);

// Возвращает: Экземпляр класса Accountant
// Исключения: ArgumentNullException если project равен null
```

---

## Публичные методы

### `ShowBalanceTable(string chains = null, bool single = false, bool call = false)`

**Назначение**
Генерирует и отображает HTML-таблицу балансов для указанных блокчейн-сетей. Поддерживает как одноколоночный, так и многоколоночный макеты в зависимости от объема данных.

**Пример**
```csharp
var accountant = new Accountant(project);

// Показать таблицу балансов для конкретных сетей
accountant.ShowBalanceTable(chains: "eth,bsc,polygon", call: true);

// Показать балансы для всех сетей без открытия
accountant.ShowBalanceTable();
```

**Разбор**
```csharp
// Параметры:
// - chains: Список имен сетей через запятую (null = все сети из таблицы "_native")
// - single: Принудительный одноколоночный макет даже для больших наборов данных (по умолчанию: false)
// - call: Открыть HTML-файл в браузере после генерации (по умолчанию: false)
accountant.ShowBalanceTable(chains: "eth,bsc", single: false, call: true);

// Возвращает: void
// Побочные эффекты:
// - Генерирует HTML-файл по пути: {project.Path}/.data/balanceReport.html
// - Записывает прогресс в лог проекта
// Исключения: Нет (предупреждения логируются, если данные не найдены)
```

---

### `ShowBalanceTableHeatmap(string chains = null, bool call = false)`

**Назначение**
Генерирует визуальное представление балансов аккаунтов в виде тепловой карты с цветовой индикацией балансов по блокчейн-сетям.

**Пример**
```csharp
var accountant = new Accountant(project);

// Сгенерировать тепловую карту для всех сетей
accountant.ShowBalanceTableHeatmap(call: true);

// Сгенерировать тепловую карту для конкретных сетей
accountant.ShowBalanceTableHeatmap(chains: "eth,bsc,arbitrum", call: false);
```

**Разбор**
```csharp
// Параметры:
// - chains: Имена сетей через запятую (null = все сети кроме 'id')
// - call: Открыть HTML-файл в браузере после генерации (по умолчанию: false)
accountant.ShowBalanceTableHeatmap(chains: "eth,polygon", call: true);

// Возвращает: void
// Побочные эффекты:
// - Генерирует HTML-файл по пути: {project.Path}/.data/balanceHeatmap.html
// - Логирует количество аккаунтов и информацию о сетях
// Цветовое кодирование:
//   - Синий (≥0.1): Самый высокий баланс
//   - Зеленый (≥0.01): Высокий баланс
//   - Желто-зеленый (≥0.001): Средний баланс
//   - Хаки (≥0.0001): Низкий баланс
//   - Лососевый (≥0.00001): Очень низкий баланс
//   - Красный (>0): Минимальный баланс
//   - Прозрачный (0): Пустой
// Исключения: Нет (предупреждения логируются, если данные не найдены)
```

---

### `ShowBalanceTableFromList(List<string> data, bool call = false)`

**Назначение**
Генерирует простую двухколоночную таблицу балансов из списка пар аккаунт:баланс.

**Пример**
```csharp
var accountant = new Accountant(project);

// Подготовить данные как List<string>
var balanceData = new List<string>
{
    "account1: 0.5",
    "account2: 1.23",
    "account3: 0.001"
};

// Сгенерировать таблицу балансов из списка
accountant.ShowBalanceTableFromList(balanceData, call: true);
```

**Разбор**
```csharp
// Параметры:
// - data: Список строк в формате "имяАккаунта: значениеБаланса"
// - call: Открыть HTML-файл в браузере после генерации (по умолчанию: false)
var data = new List<string> { "acc1: 0.5", "acc2: 1.2" };
accountant.ShowBalanceTableFromList(data, call: true);

// Возвращает: void
// Побочные эффекты:
// - Генерирует HTML-файл по пути: {project.Path}/.data/balanceListReport.html
// - Включает пагинацию для больших наборов данных (50 строк на страницу)
// - Отображает общую сумму и статистику
// Формат входных данных: Каждая строка должна быть "имя: значение" (разделено двоеточием)
// Исключения: Нет (некорректные записи пропускаются)
```
