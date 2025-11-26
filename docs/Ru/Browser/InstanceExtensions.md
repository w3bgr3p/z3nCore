# InstanceExtensions

Мощные методы расширения для взаимодействия Instance и HtmlElement с автоматическими повторами, гибким поиском элементов, выполнением JavaScript и утилитами браузера.

## Поиск и получение элементов

### GetHe()

```csharp
public static HtmlElement GetHe(this Instance instance, object obj, string method = "")
```

**Назначение**: Универсальный поиск элементов с поддержкой кортежей, HtmlElements и множества режимов поиска.

**Пример**:
```csharp
// По ID (2-кортеж)
HtmlElement btn = instance.GetHe(("submit-btn", "id"));

// По имени
HtmlElement input = instance.GetHe(("email", "name"));

// По атрибуту (5-кортеж)
HtmlElement div = instance.GetHe(("div", "class", "container", "text", 0));

// Получить последний элемент
HtmlElement last = instance.GetHe(("button", "class", "btn", "text", 0), "last");
```

**Детали**:
- `obj` - Может быть HtmlElement, 2-кортеж (значение, метод), или 5-кортеж (тег, атрибут, паттерн, режим, индекс)
- `method` - Опционально: "id", "name", или "last" для получения последнего совпадающего элемента
- Формат 2-кортежа: (значение, "id") или (значение, "name")
- Формат 5-кортежа: (тег, атрибут, паттерн, режим, позиция)
- Возвращает HtmlElement, если найден
- Выбрасывает исключение, если элемент не найден или void

---

### HeGet()

```csharp
public static string HeGet(this Instance instance, object obj, string method = "", int deadline = 10, string atr = "innertext", int delay = 1, bool thrw = true, bool thr0w = true, bool waitTillVoid = false)
```

**Назначение**: Ожидает появления элемента и получает его атрибут с логикой повторов.

**Пример**:
```csharp
// Получить текст когда элемент появится
string text = instance.HeGet(("username", "id"), deadline: 15);

// Получить атрибут href
string link = instance.HeGet(("a", "class", "link", "text", 0), atr: "href");

// Ждать пока элемент исчезнет
instance.HeGet(("loading", "id"), waitTillVoid: true, deadline: 30);
```

**Детали**:
- `obj` - Селектор элемента (см. документацию GetHe)
- `method` - Метод поиска элемента
- `deadline` - Максимальное время ожидания в секундах (по умолчанию: 10)
- `atr` - Атрибут для получения (по умолчанию: "innertext")
- `delay` - Задержка в секундах после нахождения элемента (по умолчанию: 1)
- `thrw` - Выбрасывать исключение при таймауте (по умолчанию: true)
- `thr0w` - Альтернативный параметр throw (переопределяет thrw)
- `waitTillVoid` - Ждать пока элемент исчезнет вместо появления
- Возвращает значение атрибута как строку
- Возвращает null при таймауте и thrw=false
- Выбрасывает ElementNotFoundException при таймауте и thrw=true

---

### HeCatch()

```csharp
public static string HeCatch(this Instance instance, object obj, string method = "", int deadline = 10, string atr = "innertext", int delay = 1)
```

**Назначение**: Ожидает, что элемент НЕ появится (противоположность HeGet) - полезно для проверки ошибок.

**Пример**:
```csharp
// Ждать что сообщение об ошибке НЕ появится (выбрасывает исключение если появится)
instance.HeCatch(("div", "class", "error-message", "text", 0), deadline: 5);
// Возвращает null если элемент никогда не появился (успех)
```

**Детали**:
- `obj` - Селектор элемента для отслеживания
- `method` - Метод поиска элемента
- `deadline` - Максимальное время ожидания в секундах (по умолчанию: 10)
- `atr` - Атрибут для проверки (по умолчанию: "innertext")
- `delay` - Задержка после проверки (по умолчанию: 1)
- Возвращает null если элемент никогда не появился (успех)
- Выбрасывает исключение если элемент появился (обнаружена ошибка)
- Полезно для обнаружения сообщений об ошибках, которые не должны появляться

---

## Взаимодействие с элементами

### HeClick()

```csharp
public static void HeClick(this Instance instance, object obj, string method = "", int deadline = 10, double delay = 1, string comment = "", bool thrw = true, bool thr0w = true, int emu = 0)
```

**Назначение**: Ожидает элемент и кликает по нему с логикой повторов и опциональной эмуляцией мыши.

**Пример**:
```csharp
// Простой клик
instance.HeClick(("submit-btn", "id"));

// Клик с эмуляцией мыши
instance.HeClick(("button", "class", "submit", "text", 0), emu: 1);

// Кликать пока элемент не исчезнет
instance.HeClick(("popup-close", "id"), method: "clickOut");
```

**Детали**:
- `obj` - Селектор элемента
- `method` - Метод поиска или "clickOut" (кликать пока элемент не исчезнет)
- `deadline` - Максимальное время ожидания в секундах (по умолчанию: 10)
- `delay` - Задержка перед кликом в секундах (по умолчанию: 1)
- `comment` - Описание для сообщений об ошибках
- `thrw` - Выбрасывать исключение при таймауте (по умолчанию: true)
- `thr0w` - Альтернативный параметр throw
- `emu` - Эмуляция мыши: 1 (включить), -1 (выключить), 0 (не менять)
- Повторяет каждые 500мс пока элемент не найден или таймаут
- Восстанавливает исходную настройку эмуляции мыши после клика

---

### HeSet()

```csharp
public static void HeSet(this Instance instance, object obj, string value, string method = "id", int deadline = 10, double delay = 1, string comment = "", bool thrw = true, bool thr0w = true)
```

**Назначение**: Ожидает элемент ввода и устанавливает его значение с логикой повторов.

**Пример**:
```csharp
// Установить значение input
instance.HeSet(("email", "id"), "[email protected]");

// Установить с большим таймаутом
instance.HeSet(("password", "name"), "MyP@ssw0rd", deadline: 20);
```

**Детали**:
- `obj` - Селектор элемента
- `value` - Текст для установки в элемент
- `method` - Метод поиска (по умолчанию: "id")
- `deadline` - Максимальное время ожидания в секундах (по умолчанию: 10)
- `delay` - Задержка перед установкой значения (по умолчанию: 1)
- `comment` - Описание для сообщений об ошибках
- `thrw` - Выбрасывать исключение при таймауте (по умолчанию: true)
- `thr0w` - Альтернативный параметр throw
- Использует режим эмуляции "Full" для естественного набора
- Повторяет каждые 500мс до успеха или таймаута

---

### HeDrop()

```csharp
public static void HeDrop(this Instance instance, object obj, string method = "", int deadline = 10, bool thrw = true)
```

**Назначение**: Удаляет элемент из DOM, находя его и вызывая RemoveChild у родителя.

**Пример**:
```csharp
// Удалить раздражающий popup
instance.HeDrop(("cookie-banner", "id"));

// Удалить без выброса исключения при неудаче
instance.HeDrop(("ad-overlay", "class"), thrw: false);
```

**Детали**:
- `obj` - Селектор элемента
- `method` - Метод поиска
- `deadline` - Максимальное время ожидания в секундах (по умолчанию: 10)
- `thrw` - Выбрасывать исключение при таймауте (по умолчанию: true)
- Находит родителя элемента и удаляет потомка
- Повторяет каждые 500мс до успеха или таймаута

---

## Методы JavaScript

### JsClick()

```csharp
public static string JsClick(this Instance instance, string selector, double delayX = 1.0)
```

**Назначение**: Кликает по элементу используя JavaScript (работает с Shadow DOM и скрытыми элементами).

**Пример**:
```csharp
// Клик по CSS селектору
instance.JsClick("#submit-button");

// Клик в shadow DOM
instance.JsClick("my-component >>> .inner-button");
```

**Детали**:
- `selector` - CSS селектор элемента
- `delayX` - Множитель задержки перед кликом (по умолчанию: 1.0)
- Ищет в обычном DOM и Shadow DOM
- Прокручивает элемент в видимую область перед кликом
- Отправляет корректный MouseEvent с bubbling
- Возвращает "Click successful" при успехе
- Возвращает "Error: ..." сообщение при неудаче

---

### JsSet()

```csharp
public static string JsSet(this Instance instance, string selector, string value, double delayX = 1.0)
```

**Назначение**: Устанавливает значение input через JavaScript с корректной генерацией событий.

**Пример**:
```csharp
// Установить значение input
instance.JsSet("#email", "[email protected]");

// Установить textarea
instance.JsSet("textarea.description", "Моё описание\nСтрока 2");
```

**Детали**:
- `selector` - CSS селектор для элемента input
- `value` - Текстовое значение для установки
- `delayX` - Множитель задержки перед установкой (по умолчанию: 1.0)
- Прокручивает элемент в видимую область
- Генерирует события click, focus, focusin
- Использует execCommand для естественного ввода
- Генерирует события input и change
- Возвращает "Value set successfully" при успехе
- Возвращает "Error: ..." сообщение при неудаче

---

### JsPost()

```csharp
public static string JsPost(this Instance instance, string script, int delay = 0)
```

**Назначение**: Выполняет произвольный JavaScript код на странице.

**Пример**:
```csharp
// Получить заголовок страницы
string title = instance.JsPost("document.title");

// Прокрутить страницу
instance.JsPost("window.scrollTo(0, document.body.scrollHeight)");
```

**Детали**:
- `script` - JavaScript код для выполнения
- `delay` - Задержка в секундах перед выполнением (по умолчанию: 0)
- Автоматически конвертирует двойные кавычки в одинарные
- Возвращает результат выполнения JavaScript как строку
- Возвращает сообщение об ошибке при исключении

---

## Утилиты браузера

### ClearShit()

```csharp
public static void ClearShit(this Instance instance, string domain)
```

**Назначение**: Очищает кэш и cookies для указанного домена и сбрасывает браузер.

**Пример**:
```csharp
instance.ClearShit("google.com");
```

**Детали**:
- `domain` - Домен для очистки данных
- Закрывает все вкладки
- Очищает кэш для домена
- Очищает cookies для домена
- Переходит на about:blank

---

### CloseExtraTabs()

```csharp
public static void CloseExtraTabs(this Instance instance, bool blank = false, int tabToKeep = 1)
```

**Назначение**: Закрывает все вкладки кроме первой (или указанной по индексу).

**Пример**:
```csharp
instance.CloseExtraTabs(); // Оставить первую вкладку, закрыть остальные
instance.CloseExtraTabs(blank: true); // Также перейти на пустую страницу
```

**Детали**:
- `blank` - Перейти на about:blank в оставшейся вкладке (по умолчанию: false)
- `tabToKeep` - Индекс вкладки для сохранения (по умолчанию: 1, первая вкладка)
- Закрывает все вкладки с индексом >= tabToKeep
- Добавляет задержки между закрытиями для стабильности

---

### Go()

```csharp
public static void Go(this Instance instance, string url, bool strict = false)
```

**Назначение**: Переходит на URL только если там еще нет (избегает ненужных перезагрузок).

**Пример**:
```csharp
instance.Go("https://example.com"); // Переход если другой домен
instance.Go("https://example.com/page", strict: true); // Переход если точный URL отличается
```

**Детали**:
- `url` - Целевой URL
- `strict` - true: точное совпадение URL, false: проверка содержания (по умолчанию: false)
- Переходит только если текущий URL не совпадает
- Экономит время, избегая ненужных загрузок страниц

---

### F5()

```csharp
public static void F5(this Instance instance)
```

**Назначение**: Перезагружает текущую страницу (принудительное обновление).

**Пример**:
```csharp
instance.F5(); // Принудительная перезагрузка страницы
```

**Детали**:
- Без параметров
- Использует JavaScript location.reload(true)
- Принудительная полная перезагрузка страницы в обход кэша

---

### ScrollDown()

```csharp
public static void ScrollDown(this Instance instance, int y = 420)
```

**Назначение**: Прокручивает страницу вниз на указанное количество пикселей.

**Пример**:
```csharp
instance.ScrollDown(); // Прокрутка вниз на 420px
instance.ScrollDown(1000); // Прокрутка вниз на 1000px
```

**Детали**:
- `y` - Пиксели для прокрутки вниз (по умолчанию: 420)
- Временно включает эмуляцию мыши
- Восстанавливает исходную настройку эмуляции после прокрутки

---

### CtrlV()

```csharp
public static void CtrlV(this Instance instance, string ToPaste)
```

**Назначение**: Вставляет текст используя комбинацию клавиш Ctrl+V (потокобезопасная операция с буфером обмена).

**Пример**:
```csharp
instance.CtrlV("Текст для вставки");
```

**Детали**:
- `ToPaste` - Текст для вставки
- Потокобезопасная манипуляция буфером обмена
- Сохраняет исходное содержимое буфера обмена
- Симулирует нажатие клавиш Ctrl+V
- Восстанавливает буфер обмена после операции

---

## Cloudflare (класс Fallback)

### ClFlv2()

```csharp
public static void ClFlv2(this Instance instance)
```

**Назначение**: Решает задачи Cloudflare v2 (псевдоним для CFSolve).

**Пример**:
```csharp
instance.ClFlv2();
```

**Детали**:
- Обёртка для метода CFSolve()
- См. документацию Captcha.CFSolve()

---

### ClFl()

```csharp
public static string ClFl(this Instance instance, int deadline = 60, bool strict = false)
```

**Назначение**: Получает токен clearance Cloudflare (псевдоним для CFToken).

**Пример**:
```csharp
string token = instance.ClFl(deadline: 120);
```

**Детали**:
- Обёртка для метода CFToken()
- См. документацию Captcha.CFToken()
