# ListExtensions

Методы расширения для работы со списками и списками проекта ZennoPoster.

---

## ListExtensions.RndFromList

### Назначение
Возвращает случайный элемент из `List<string>`. Выбрасывает исключение, если список пуст.

### Пример
```csharp
var fruits = new List<string> { "apple", "banana", "orange" };
var randomFruit = fruits.RndFromList();
// Вернёт: случайный элемент, например "banana"
```

### Детали реализации
```csharp
public static object RndFromList(this List<string> list)
{
    // list: Исходный список для выбора случайного элемента
    // Исключения: ArgumentNullException, если список пуст
    // Возвращает: Случайный элемент из списка в виде типа object

    if (list.Count == 0)
        throw new ArgumentNullException(nameof(list), "List is empty");

    int index = _random.Next(0, list.Count);
    return list[index];
}
```

---

## ProjectExtensions.RndFromList

### Назначение
Возвращает случайный элемент из списка проекта ZennoPoster с возможностью удаления выбранного элемента.

### Пример
```csharp
// Получить случайный элемент без удаления
string proxy = project.RndFromList("ProxyList");

// Получить случайный элемент и удалить его из списка
string account = project.RndFromList("AccountList", remove: true);
```

### Детали реализации
```csharp
public static string RndFromList(
    this IZennoPosterProjectModel project,
    string listName,
    bool remove = false)
{
    // project: Экземпляр проекта ZennoPoster
    // listName: Имя списка в проекте
    // remove: Если true, удаляет выбранный элемент из списка (по умолчанию: false)
    // Исключения: ArgumentNullException, если список пуст
    // Возвращает: Случайный элемент из указанного списка

    var list = project.Lists[listName];
    if (list.Count == 0)
        throw new ArgumentNullException(nameof(list), "List is empty");

    // Если не удаляем, просто возвращаем случайный элемент
    if (!remove)
        return list[_random.Next(0, list.Count)];

    // Если удаляем, синхронизируем в локальный список, удаляем и синхронизируем обратно
    var localList = project.ListSync(listName);
    int index = _random.Next(0, localList.Count);
    var item = localList[index];
    localList.RemoveAt(index);
    project.ListSync(listName, localList);
    return item;
}
```

---

## ProjectExtensions.ListSync (получение)

### Назначение
Создаёт локальную копию списка проекта ZennoPoster в виде `List<string>`.

### Пример
```csharp
// Создать локальную копию списка проекта
List<string> localProxies = project.ListSync("ProxyList");

// Изменить локальную копию
localProxies.Add("127.0.0.1:8080");
localProxies.RemoveAt(0);
```

### Детали реализации
```csharp
public static List<string> ListSync(
    this IZennoPosterProjectModel project,
    string listName)
{
    // project: Экземпляр проекта ZennoPoster
    // listName: Имя списка для синхронизации
    // Возвращает: Локальную копию списка проекта в виде List<string>

    var projectList = project.Lists[listName];
    var localList = new List<string>();

    // Копируем все элементы из списка проекта в локальный список
    foreach (var item in projectList)
    {
        localList.Add(item);
    }

    return localList;
}
```

---

## ProjectExtensions.ListSync (установка)

### Назначение
Синхронизирует локальный `List<string>` обратно в список проекта ZennoPoster, заменяя все существующие элементы.

### Пример
```csharp
// Получить локальную копию
List<string> localList = project.ListSync("MyList");

// Изменить локальную копию
localList.Add("new item");
localList.RemoveAt(0);

// Синхронизировать изменения обратно в проект
project.ListSync("MyList", localList);
```

### Детали реализации
```csharp
public static List<string> ListSync(
    this IZennoPosterProjectModel project,
    string listName,
    List<string> localList)
{
    // project: Экземпляр проекта ZennoPoster
    // listName: Имя списка проекта для обновления
    // localList: Локальный список с новым содержимым
    // Возвращает: Тот же localList, который был передан

    var projectList = project.Lists[listName];

    // Очищаем существующий список проекта
    projectList.Clear();

    // Добавляем все элементы из локального списка в список проекта
    foreach (var item in localList)
    {
        projectList.Add(item);
    }

    return localList;
}
```
