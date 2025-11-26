# Класс Snapper

Класс для создания снимков и резервных копий проектов ZennoPoster и файлов DLL ядра.

---

## Конструктор

### Назначение
Инициализирует класс Snapper с экземпляром проекта.

### Пример
```csharp
using z3nCore.Utilities;

var snapper = new Snapper(project);
```

---

## SnapDir

### Назначение
Создает снимки всех файлов проектов в текущей директории, отслеживая изменения через хеши файлов и ведя историю версий.

### Пример
```csharp
using z3nCore.Utilities;

var snapper = new Snapper(project);

// Создать снимок с путем по умолчанию (из переменной проекта)
snapper.SnapDir();

// Создать снимок с пользовательским путем
snapper.SnapDir(pathSnaps: @"W:\backups\projects");
```

### Детали
```csharp
public void SnapDir(
    string pathSnaps = null)  // Путь к директории снимков (null = использовать переменную проекта "snapsDir")

// Возвращает: void

// Процесс:
// 1. Получает все файлы в директории проекта (*.zp, и т.д.)
// 2. Для каждого файла:
//    - Вычисляет SHA хеш
//    - Сравнивает с существующими снимками
//    - Если изменен:
//      * Копирует в папку проекта
//      * Создает резервную копию с временной меткой в snapshots/
//    - Логирует обновление или существование
// 3. Обновляет .sync.txt со списком проектов
// 4. Обновляет .access.txt с активными проектами
//
// Создаваемая структура файлов:
// {pathSnaps}/
//   .sync.txt                    // Список проектов с флагами синхронизации
//   .access.txt                  // Список активных проектов
//   {ProjectName}/
//     {ProjectName}.zp           // Последняя версия
//     snapshots/
//       20251122_1430.ProjectName.zp  // Резервная копия с временной меткой
//       20251122_1445.ProjectName.zp  // Другая резервная копия
//       ...
//
// Формат .sync.txt:
// ProjectName : true   (синхронизация включена)
// ProjectName : false  (синхронизация отключена)
//
// Особенности:
// - Обнаружение изменений на основе хешей (без дубликатов)
// - Автоматические временные метки: yyyyMMdd_HHmm
// - Поддержка нескольких проектов
// - Конфигурация синхронизации для каждого проекта
// - Отслеживание доступа для активных проектов
```

---

## SnapCoreDll

### Назначение
Создает снимки z3nCore.dll и всех ExternalAssemblies, ведет архив версий и обновляет зависимые проекты.

### Пример
```csharp
using z3nCore.Utilities;

var snapper = new Snapper(project);

// Снимок основной DLL и зависимостей
snapper.SnapCoreDll();

// Логирует информацию о версиях:
// "ZP: v7.8.0.0, z3nCore: v1.2.3.4"
```

### Детали
```csharp
public void SnapCoreDll()

// Возвращает: void

// Процесс:
// 1. Определяет версии:
//    - Получает версию z3nCore.dll из ExternalAssemblies
//    - Получает версию ZennoPoster из пути процесса
//    - Логирует обе версии
//    - Устанавливает переменные проекта: vZP, vDll
//
// 2. Копирует ExternalAssemblies в:
//    - W:\work_hard\zenoposter\CURRENT_JOBS\.snaps\z3nFarm\ExternalAssemblies\
//    - W:\code_hard\.net\z3nCore\ExternalAssemblies\
//
// 3. Архивирует версию:
//    - Создает: W:\code_hard\.net\z3nCore\verions\v{version}\z3nCore.dll
//    - Создает: dependencies.txt со всеми версиями DLL
//
// 4. Обновляет зависимые проекты:
//    - _z3nLnch.zp → z3nLauncher.zp
//    - SAFU.zp → SAFU.zp
//    - DbBuilder.zp → DbBuilder.zp
//    - Удаляет старые версии
//    - Копирует из снимков
//    - Логирует: "{n} обновлено, {m} отсутствует"
//
// Формат dependencies.txt:
// z3nCore.dll : 1.2.3.4
// Newtonsoft.Json.dll : 13.0.1.0
// OtpNet.dll : 1.9.1.0
// ...

// Жестко заданные пути (настройте по необходимости):
// - ExternalAssemblies: {ZP_DIR}\ExternalAssemblies\
// - Репозиторий z3nCore: W:\code_hard\.net\z3nCore\ExternalAssemblies\
// - Репозиторий z3nFarm: W:\work_hard\zenoposter\CURRENT_JOBS\.snaps\z3nFarm\
// - Архив версий: W:\code_hard\.net\z3nCore\verions\v{version}\
// - Снимки проектов: W:\work_hard\zenoposter\CURRENT_JOBS\.snaps\

// Примечания:
// - Требует определенную структуру директорий
// - Разработан для рабочего процесса разработки/развертывания
// - Автоматически отслеживает все зависимости DLL
// - Ведет историю версий
```

---

## Рабочий процесс использования

### Регулярные снимки проектов
```csharp
using z3nCore.Utilities;

var snapper = new Snapper(project);

// Ежедневная резервная копия всех проектов
snapper.SnapDir(@"W:\backups\daily");

// Создает новый снимок только если файл изменен (на основе хеша)
```

### Развертывание в разработке
```csharp
using z3nCore.Utilities;

var snapper = new Snapper(project);

// После обновления z3nCore.dll
snapper.SnapCoreDll();
// Результат:
// 1. DLL скопирована в репозитории
// 2. Версия заархивирована
// 3. Зависимые проекты обновлены
// 4. Зависимости задокументированы
```

### Контроль доступа
```csharp
// Конфигурация .sync.txt:
// ProjectA : true   ← Будет добавлен в .access.txt
// ProjectB : false  ← НЕ будет в .access.txt

// Результат .access.txt:
// ProjectA

// Вариант использования: Только ProjectA будет развернут/синхронизирован
```

---
