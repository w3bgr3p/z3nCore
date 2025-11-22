# ZB (ZennoBrowser)

Утилиты для работы с профилями ZennoBrowser и выполнения операций, специфичных для ZennoBrowser.

## Методы расширения

### ZBids()

```csharp
public static Dictionary<string, string> ZBids(this IZennoPosterProjectModel project)
```

**Назначение**: Извлекает сопоставление ID профилей ZennoBrowser с именами аккаунтов из базы данных ZennoBrowser.

**Пример**:
```csharp
var zbProfiles = project.ZBids();

foreach (var profile in zbProfiles)
{
    project.log($"ID профиля: {profile.Key}, Аккаунт: {profile.Value}");
}

// Проверить существование конкретного аккаунта
if (zbProfiles.ContainsValue("myAccount123"))
{
    project.log("Профиль найден!");
}
```

**Детали**:
- Метод расширения для project
- Читает из ProfileManagement.db ZennoBrowser
- Расположение БД: `%LocalAppData%\ZennoLab\ZP8\.zp8\ProfileManagement.db`
- Возвращает Dictionary<string, string> с сопоставлением profile_id => account_name
- Автоматически пропускает шаблонные профили
- Потокобезопасно с блокировкой базы данных
- Временно переключается в режим SQLite и восстанавливает исходные настройки
- Выбрасывает FileNotFoundException, если база данных не найдена

---

### ZB()

```csharp
public static bool ZB(this IZennoPosterProjectModel project, string toDo)
```

**Назначение**: Выполняет операции, специфичные для ZennoBrowser, запуская внутренний проект ZB.zp.

**Пример**:
```csharp
// Выполнить операцию ZennoBrowser
bool success = project.ZB("createProfile");

if (success)
{
    project.log("Операция ZennoBrowser выполнена успешно");
}
else
{
    project.log("Операция ZennoBrowser не удалась");
}
```

**Детали**:
- `toDo` - Операция для выполнения (передается в проект ZB.zp)
- Выполняет проект `ProjectPath\.internal\ZB.zp`
- Автоматически сопоставляет необходимые переменные между проектами:
  - acc0, cfgLog, cfgPin
  - DBmode, DBpstgrPass, DBpstgrUser, DBsqltPath
  - instancePort, lastQuery, cookies
  - projectScript, varSessionId, toDo
- Устанавливает переменную "toDo" с именем операции
- Возвращает true, если выполнение ZB.zp успешно
- Возвращает false, если выполнение ZB.zp не удалось
- Требует наличия файла проекта ZB.zp в папке `.internal`
- Передает переменные двунаправленно (в ZB.zp и обратно)
- Ожидает завершения ZB.zp перед продолжением
