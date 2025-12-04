# SocialReport - Визуализация статуса социальных сетей

## 📋 Описание

Класс `SocialReport` создаёт визуальную HTML таблицу для отображения статуса социальных сетей аккаунтов. Использует те же стили, что и классы `Accountant` и `DailyReport`.

## 🎨 Особенности визуализации

### Хитмапы (квадраты)

Для каждого аккаунта создаётся 4 маленьких квадрата (2x2 grid):

```
┌─────────────┐
│  [#1]       │
│  ┌───┬───┐  │
│  │ T │ G │  │  T = Twitter (голубой #1DA1F2)
│  ├───┼───┤  │  G = GitHub (белый #FFFFFF)
│  │ D │ TG│  │  D = Discord (фиолетовый #5865F2)
│  └───┴───┘  │  TG = Telegram (синий #0088CC)
└─────────────┘
```

### Цветовая схема

| Соц. сеть | Цвет     | Hex код  |
|-----------|----------|----------|
| Twitter   | Голубой  | #1DA1F2  |
| GitHub    | Белый    | #FFFFFF  |
| Discord   | Фиолетовый| #5865F2 |
| Telegram  | Синий    | #0088CC  |

### Индикация статуса

- ✅ **Есть данные + status = "ok"** → Яркий цвет (opacity: 1.0)
- ⚠️ **Есть данные + status ≠ "ok"** → Тусклый цвет (opacity: 0.3, тень)
- ❌ **Нет данных** → Прозрачный (только граница)

### Интерактивность

- **Hover** → всплывающая подсказка с информацией:
  ```
  account #5
  twitter
  @username
  Status: ok
  ```

- **Click** → копирование информации в буфер обмена

## 📊 Форматы данных

### Старый формат (ваш текущий)

```csharp
List<Dictionary<string, string[]>>

// Пример:
var dataList = new List<Dictionary<string, string[]>>
{
    new Dictionary<string, string[]>
    {
        { "twitter", new[] { "ok", "@user1" } },
        { "github", new[] { "ok", "user1" } },
        { "discord", new[] { "banned", "user1#1234" } },
        { "telegram", new[] { "user1_tg" } }  // без статуса
    }
};
```

**Недостатки:**
- ❌ Нет типобезопасности
- ❌ Можно ошибиться в индексе массива
- ❌ Сложно понять структуру
- ❌ Нет IntelliSense
- ❌ Telegram без статуса = разные длины массивов

### Новый формат (рекомендуется) ✨

```csharp
List<AccountSocialData>

// Пример:
var accounts = new List<AccountSocialData>
{
    new AccountSocialData(1)
    {
        Twitter = new SocialStatus { Status = "ok", Login = "@user1" },
        GitHub = new SocialStatus { Status = "ok", Login = "user1" },
        Discord = new SocialStatus { Status = "banned", Login = "user1#1234" },
        Telegram = new SocialStatus { Status = "ok", Login = "user1_tg" }
    }
};
```

**Преимущества:**
- ✅ Типобезопасность
- ✅ IntelliSense подсказки
- ✅ Читаемый код
- ✅ Единая структура для всех соцсетей
- ✅ Проверки `IsActive` и `IsOk`

## 🚀 Использование

### Вариант 1: Старый формат (совместимость)

```csharp
// Ваш текущий код:
var acc0 = project.Int("rangeStart") - 1;
var dataList = new List<Dictionary<string, string[]>>();

while (acc0 < project.Int("rangeEnd"))
{
    acc0++;
    project.Var("acc0", acc0);

    var twitter = project.DbGetColumns("status, login", "_twitter", log: true);
    var github = project.DbGetColumns("status, login", "_github", log: true);
    var discord = project.DbGetColumns("status, username", "_discord", log: true);
    var telegram = project.DbGetColumns("username", "_telegram", log: true);

    var data = new Dictionary<string, string[]>();
    if (twitter.ContainsKey("login"))
        data.Add("twitter", new[] { twitter["status"], twitter["login"] });
    if (github.ContainsKey("login"))
        data.Add("github", new[] { github["status"], github["login"] });
    if (discord.ContainsKey("username"))
        data.Add("discord", new[] { discord["status"], discord["username"] });
    if (telegram.ContainsKey("username"))
        data.Add("telegram", new[] { telegram["username"] });

    dataList.Add(data);
}

// Генерация отчета:
project.GenerateSocialReportFromOldFormat(dataList, call: true);
```

### Вариант 2: Новый формат (рекомендуется)

```csharp
var accounts = new List<AccountSocialData>();
var acc0 = project.Int("rangeStart") - 1;

while (acc0 < project.Int("rangeEnd"))
{
    acc0++;
    project.Var("acc0", acc0);

    var account = new AccountSocialData(acc0);

    // Twitter
    var twitter = project.DbGetColumns("status, login", "_twitter", log: true);
    if (twitter.ContainsKey("login"))
    {
        account.Twitter = new SocialStatus
        {
            Status = twitter["status"],
            Login = twitter["login"]
        };
    }

    // GitHub
    var github = project.DbGetColumns("status, login", "_github", log: true);
    if (github.ContainsKey("login"))
    {
        account.GitHub = new SocialStatus
        {
            Status = github["status"],
            Login = github["login"]
        };
    }

    // Discord
    var discord = project.DbGetColumns("status, username", "_discord", log: true);
    if (discord.ContainsKey("username"))
    {
        account.Discord = new SocialStatus
        {
            Status = discord["status"],
            Login = discord["username"]
        };
    }

    // Telegram
    var telegram = project.DbGetColumns("username", "_telegram", log: true);
    if (telegram.ContainsKey("username"))
    {
        account.Telegram = new SocialStatus
        {
            Status = "ok",
            Login = telegram["username"]
        };
    }

    accounts.Add(account);
}

// Генерация отчета:
project.GenerateSocialReport(accounts, call: true);
```

### Вариант 3: Конвертация форматов

```csharp
// Если у вас уже есть данные в старом формате
var oldData = GetOldFormatData(); // ваш метод

// Конвертируем в новый формат
var newData = SocialReport.ConvertFromOldFormat(oldData);

// Теперь можем работать с новым форматом
foreach (var account in newData)
{
    if (account.Twitter?.IsActive == true && !account.Twitter.IsOk)
    {
        project.SendWarningToLog($"Acc {account.AccountId}: Twitter problem - {account.Twitter.Status}");
    }
}

// Генерируем отчет
project.GenerateSocialReport(newData, call: true);
```

## 📈 Результат

После выполнения создаётся файл `.data/socialReport.html` с:

1. **Header** - заголовок с датой и временем
2. **Summary Cards** - статистика по каждой соцсети
3. **HeatMap** - визуальная таблица с квадратами для каждого аккаунта
4. **Интерактивные элементы** - hover и click для деталей

## 🔧 API

### Классы

```csharp
// Данные одного аккаунта
public class AccountSocialData
{
    public int AccountId { get; set; }
    public SocialStatus Twitter { get; set; }
    public SocialStatus GitHub { get; set; }
    public SocialStatus Discord { get; set; }
    public SocialStatus Telegram { get; set; }
}

// Статус одной соцсети
public class SocialStatus
{
    public string Status { get; set; }  // "ok", "banned", etc.
    public string Login { get; set; }
    public bool IsActive { get; }       // есть ли логин
    public bool IsOk { get; }           // Status == "ok"
}
```

### Extension методы

```csharp
// Из нового формата
project.GenerateSocialReport(
    List<AccountSocialData> accounts,
    bool call = false  // открыть файл после создания
);

// Из старого формата
project.GenerateSocialReportFromOldFormat(
    List<Dictionary<string, string[]>> dataList,
    bool call = false
);
```

### Прямое использование

```csharp
var report = new SocialReport(project, log: true);

// Новый формат
report.ShowSocialTable(accounts, call: true);

// Старый формат
report.ShowSocialTableFromOldFormat(dataList, call: true);

// Конвертация
var newData = SocialReport.ConvertFromOldFormat(oldData);
```

## 💡 Рекомендации

1. **Переходите на новый формат** - он безопаснее и удобнее
2. **Используйте проверки** - `IsActive` и `IsOk` вместо прямых сравнений
3. **Логируйте проблемы** - если `!IsOk`, значит есть проблема с аккаунтом
4. **Статус для всех** - даже для Telegram добавляйте status = "ok"

## 📝 Примеры в коде

Смотрите файл `Examples/SocialReportExample.cs` для полных примеров использования.
