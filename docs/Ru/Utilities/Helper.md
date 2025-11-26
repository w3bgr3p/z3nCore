# Класс Helper

Статический утилитный класс, предоставляющий методы расширения для отладки проекта и поиска в XML-документации.

---

## Help

### Назначение
Выполняет поиск по XML-файлам документации ZennoPoster для методов, свойств или классов и отображает детальную информацию в читаемом виде.

### Пример
```csharp
using z3nCore.Utilities;

// Поиск конкретного метода
project.Help("SendInfoToLog");

// Поиск с диалогом ввода (если параметр null)
project.Help();  // Показывает диалог ввода

// Поиск методов браузера
project.Help("Navigate");

// Поиск методов экземпляра
project.Help("FindElement");
```

### Детали
```csharp
public static void Help(
    this IZennoPosterProjectModel project,  // Экземпляр проекта (метод расширения)
    string toSearch = null)                 // Поисковый запрос (null - запросить ввод)

// Возвращает: void (отображает результаты в диалоговом окне)

// Выбрасывает:
// - ArgumentException: Если поисковый запрос пуст

// Особенности:
// - Поиск в 4 XML-файлах:
//   * ZennoLab.CommandCenter.xml
//   * ZennoLab.InterfacesLibrary.xml
//   * ZennoLab.Macros.xml
//   * ZennoLab.Emulation.xml
//
// - Отображает для каждого совпадения:
//   * Имя члена
//   * Краткое описание (Summary)
//   * Параметры с описаниями
//   * Описание возвращаемого значения
//   * Примечания (Remarks)
//   * Примеры кода
//   * Требования
//   * Связанные методы (See Also)
//   * Информация о перегрузках
//   * Исключения
//
// - Результаты показаны в текстовом редакторе с возможностью копирования
// - Использует шрифт Cascadia Mono для читабельности
// - Окно поддерживает Ctrl+A (выделить всё), Ctrl+C (копировать), Esc (закрыть)
```

### Пример вывода
```
=== M:ZennoLab.InterfacesLibrary.ProjectModel.IZennoPosterProjectModel.SendInfoToLog ===
Summary: Отправляет информационное сообщение в лог проекта
Parameter [message]: Текст сообщения для логирования
Parameter [showDialog]: Показывать ли сообщение в диалоге (по умолчанию: false)
Returns: void
Example 1: project.SendInfoToLog("Процесс успешно завершен");

--------------------------------------------------
```

---
