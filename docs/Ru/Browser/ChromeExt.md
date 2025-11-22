# ChromeExt

Утилиты для управления расширениями браузера Chrome: установка, включение, отключение и удаление.

## Класс: Extension

Современный менеджер расширений с логированием и поддержкой браузеров Chromium и ChromiumFromZB.

### Конструктор (без instance)

```csharp
public Extension(IZennoPosterProjectModel project, bool log = false)
```

**Назначение**: Создает менеджер расширений без экземпляра браузера (для операций, не требующих браузер).

**Пример**:
```csharp
var ext = new Extension(project, log: true);
```

**Детали**:
- `project` - Модель проекта ZennoPoster
- `log` - Включить детальное логирование (по умолчанию: false)

---

### Конструктор (с instance)

```csharp
public Extension(IZennoPosterProjectModel project, Instance instance, bool log = false)
```

**Назначение**: Создает менеджер расширений с экземпляром браузера для полного функционала.

**Пример**:
```csharp
var ext = new Extension(project, instance, log: true);
```

**Детали**:
- `project` - Модель проекта ZennoPoster
- `instance` - Экземпляр браузера
- `log` - Включить детальное логирование (по умолчанию: false)

---

### GetVer()

```csharp
public string GetVer(string extId)
```

**Назначение**: Получает версию установленного расширения из профиля браузера.

**Пример**:
```csharp
var ext = new Extension(project, instance);
string version = ext.GetVer("pbgjpgbpljobkekbhnnmlikbbfhbhmem");
// Возвращает: "1.2.3"
```

**Детали**:
- `extId` - ID расширения Chrome (32-символьный идентификатор)
- Возвращает строку версии из манифеста расширения
- Читает из файла Secure Preferences браузера
- Выбрасывает исключение, если расширение не найдено

---

### InstallFromStore()

```csharp
public bool InstallFromStore(string url, bool log = false)
```

**Назначение**: Устанавливает расширение Chrome напрямую из Chrome Web Store.

**Пример**:
```csharp
var ext = new Extension(project, instance);
bool installed = ext.InstallFromStore("https://chromewebstore.google.com/detail/extension-id");
// Возвращает: true если установлено заново, false если уже установлено
```

**Детали**:
- `url` - URL расширения в Chrome Web Store
- `log` - Включить логирование операции (по умолчанию: false)
- Переходит на страницу магазина
- Нажимает "Add to Chrome", если не установлено
- Включает расширение, если уже установлено, но отключено
- Возвращает true, если выполнена установка, false если уже присутствует

---

### InstallFromCrx()

```csharp
public bool InstallFromCrx(string extId, string fileName, bool log = false)
```

**Назначение**: Устанавливает расширение из локального CRX файла.

**Пример**:
```csharp
var ext = new Extension(project, instance);
bool installed = ext.InstallFromCrx("pbgjpgbpljobkekbhnnmlikbbfhbhmem", "MyExtension.crx");
```

**Детали**:
- `extId` - Ожидаемый ID расширения после установки
- `fileName` - Имя CRX файла (должен быть в папке `ProjectPath\.crx\`)
- `log` - Включить логирование операции (по умолчанию: false)
- Проверяет, установлено ли расширение
- Устанавливает из CRX файла, если не присутствует
- Возвращает true, если установлено, false если уже присутствует
- Выбрасывает FileNotFoundException, если CRX файл не найден

---

### Switch()

```csharp
public bool Switch(string toUse = "", bool log = false)
```

**Назначение**: Включает указанные расширения и отключает все остальные через One-Click Extensions Manager.

**Пример**:
```csharp
var ext = new Extension(project, instance);
ext.Switch("MetaMask,Phantom"); // Включить только MetaMask и Phantom
ext.Switch(""); // Отключить все расширения
```

**Детали**:
- `toUse` - Список имен или ID расширений через запятую для включения
- `log` - Включить логирование операции (по умолчанию: false)
- Устанавливает One-Click Extensions Manager при необходимости
- Открывает popup менеджера расширений
- Включает расширения, соответствующие именам/ID в `toUse`
- Отключает все остальные расширения
- Возвращает true, если выполнено переключение
- Работает только с браузерами Chromium и ChromiumFromZB

---

### Rm()

```csharp
public void Rm(string[] ExtToRemove)
```

**Назначение**: Удаляет (деинсталлирует) указанные расширения из браузера.

**Пример**:
```csharp
var ext = new Extension(project, instance);
ext.Rm(new[] { "ext-id-1", "ext-id-2", "ext-id-3" });
```

**Детали**:
- `ExtToRemove` - Массив ID расширений для удаления
- Деинсталлирует каждое расширение из браузера
- Тихо перехватывает и логирует ошибки удаления
- Без возвращаемого значения

---

## Класс: ChromeExt

Устаревший менеджер расширений с аналогичной функциональностью (сохранен для совместимости).

### Конструктор (без instance)

```csharp
public ChromeExt(IZennoPosterProjectModel project, bool log = false)
```

**Назначение**: Создает устаревший менеджер расширений без экземпляра браузера.

**Пример**:
```csharp
var ext = new ChromeExt(project, log: true);
```

**Детали**:
- `project` - Модель проекта ZennoPoster
- `log` - Включить логирование (по умолчанию: false)

---

### Конструктор (с instance)

```csharp
public ChromeExt(IZennoPosterProjectModel project, Instance instance, bool log = false)
```

**Назначение**: Создает устаревший менеджер расширений с экземпляром браузера.

**Пример**:
```csharp
var ext = new ChromeExt(project, instance);
```

**Детали**:
- `project` - Модель проекта ZennoPoster
- `instance` - Экземпляр браузера
- `log` - Включить логирование (по умолчанию: false)

---

### GetVer()

```csharp
public string GetVer(string extId)
```

**Назначение**: Получает версию установленного расширения (аналогично Extension.GetVer).

**Пример**:
```csharp
var ext = new ChromeExt(project);
string version = ext.GetVer("pbgjpgbpljobkekbhnnmlikbbfhbhmem");
```

**Детали**:
- См. документацию Extension.GetVer()

---

### Install()

```csharp
public bool Install(string extId, string fileName, bool log = false)
```

**Назначение**: Устанавливает расширение из CRX файла (аналогично Extension.InstallFromCrx).

**Пример**:
```csharp
var ext = new ChromeExt(project, instance);
bool installed = ext.Install("ext-id", "Extension.crx");
```

**Детали**:
- См. документацию Extension.InstallFromCrx()

---

### Switch()

```csharp
public bool Switch(string toUse = "", bool log = false)
```

**Назначение**: Включает/отключает расширения (аналогично Extension.Switch).

**Пример**:
```csharp
var ext = new ChromeExt(project, instance);
ext.Switch("MetaMask");
```

**Детали**:
- См. документацию Extension.Switch()
- Работает только с типом браузера Chromium

---

### Rm()

```csharp
public void Rm(string[] ExtToRemove)
```

**Назначение**: Удаляет расширения (аналогично Extension.Rm).

**Пример**:
```csharp
var ext = new ChromeExt(project, instance);
ext.Rm(new[] { "ext-id-1", "ext-id-2" });
```

**Детали**:
- См. документацию Extension.Rm()
