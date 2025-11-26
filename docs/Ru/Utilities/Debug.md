# Класс Debugger

Статический утилитный класс для отладки и системной диагностики.

---

## AssemblyVer

### Назначение
Получает информацию о версии и публичном ключе загруженной сборки по имени.

### Пример
```csharp
using z3nCore.Utilities;

// Получить информацию о версии сборки ZennoLab
string info = Debugger.AssemblyVer("ZennoLab.InterfacesLibrary");
// Результат: "ZennoLab.InterfacesLibrary 7.8.0.0, PublicKeyToken: AB-CD-EF-..."

// Проверить, загружена ли сборка
string result = Debugger.AssemblyVer("MyCustomLib");
// Результат: "MyCustomLib not loaded" (если не загружена)
```

### Детали
```csharp
public static string AssemblyVer(
    string dllName)  // Имя сборки (без расширения .dll)

// Возвращает: Строку версии в формате "Имя Версия, PublicKeyToken: XXX"
// Возвращает: "dllName not loaded" если сборка не найдена в текущем AppDomain

// Примеры вывода:
// - "ZennoLab.InterfacesLibrary 7.8.0.0, PublicKeyToken: AB-CD-EF-12"
// - "MyLib not loaded"
```

---

## ZennoProcesses

### Назначение
Получает информацию обо всех запущенных процессах ZennoPoster и ZennoBox, включая использование памяти и время работы.

### Пример
```csharp
using z3nCore.Utilities;

// Получить все процессы Zenno
List<string[]> processes = Debugger.ZennoProcesses();

// Вывести информацию о процессах
foreach (var proc in processes)
{
    string name = proc[0];        // Имя процесса
    string memoryMB = proc[1];    // Использование памяти в МБ
    string runtimeMin = proc[2];  // Время работы в минутах

    project.SendInfoToLog($"{name}: {memoryMB}МБ, работает {runtimeMin} мин");
}

// Проверить, есть ли запущенные процессы
if (processes.Count == 0)
{
    project.SendInfoToLog("Процессы Zenno не найдены");
}
```

### Детали
```csharp
public static List<string[]> ZennoProcesses()

// Возвращает: Список массивов строк, каждый содержит:
//   [0] - Имя процесса ("ZennoPoster" или "zbe1")
//   [1] - Использование памяти в МБ (WorkingSet64)
//   [2] - Время работы в минутах (с момента запуска процесса)

// Возвращает: Пустой список, если процессы ZennoPoster/ZennoBox не найдены

// Пример результата:
// [
//   ["ZennoPoster", "1024", "45"],
//   ["zbe1", "512", "30"]
// ]
```

---
