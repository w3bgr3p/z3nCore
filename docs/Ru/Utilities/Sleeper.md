# Класс Sleeper

Класс для генерации случайных задержек в указанном диапазоне, полезный для имитации человекоподобного поведения.

---

## Конструктор

### Назначение
Инициализирует экземпляр Sleeper с минимальным и максимальным значениями задержки в миллисекундах.

### Пример
```csharp
using z3nCore.Utilities;

// Создать Sleeper: 1-3 секунды
var sleeper = new Sleeper(min: 1000, max: 3000);

// Создать Sleeper: 500мс - 1.5 секунды
var quickSleep = new Sleeper(500, 1500);

// Создать Sleeper: 5-10 секунд
var longSleep = new Sleeper(5000, 10000);
```

### Детали
```csharp
public Sleeper(
    int min,  // Минимальная задержка в миллисекундах (должна быть >= 0)
    int max)  // Максимальная задержка в миллисекундах (должна быть >= min)

// Выбрасывает:
// - ArgumentException: Если min < 0
// - ArgumentException: Если max < min

// Примечания:
// - Использует случайное зерно на основе GUID для лучшей рандомизации
// - Диапазон задержки включительный: [min, max]
```

---

## Sleep

### Назначение
Приостанавливает выполнение на случайную длительность в настроенном диапазоне, с опциональным множителем.

### Пример
```csharp
using z3nCore.Utilities;

var sleeper = new Sleeper(1000, 3000);  // 1-3 секунды

// Обычная случайная задержка
sleeper.Sleep();
// Приостанавливает на случайную длительность: 1000-3000мс

// Удвоенная задержка
sleeper.Sleep(multiplier: 2.0);
// Приостанавливает на случайную длительность: 2000-6000мс

// Половинная задержка
sleeper.Sleep(multiplier: 0.5);
// Приостанавливает на случайную длительность: 500-1500мс

// Использование в автоматизации
instance.ActiveTab.Navigate("https://example.com");
sleeper.Sleep();  // Случайная человекоподобная задержка
instance.ActiveTab.FillTextBox("#username", "user");
sleeper.Sleep(multiplier: 0.5);  // Более короткая задержка
instance.ActiveTab.FillTextBox("#password", "pass");
```

### Детали
```csharp
public void Sleep(
    double multiplier = 1.0)  // Множитель длительности задержки (по умолчанию: 1.0)

// Возвращает: void

// Как работает:
// 1. Генерирует случайную задержку между min и max
// 2. Умножает задержку на параметр multiplier
// 3. Приостанавливает поток на рассчитанную длительность

// Примеры с Sleeper(1000, 3000):
// - Sleep()           → 1000-3000мс случайно
// - Sleep(2.0)        → 2000-6000мс случайно
// - Sleep(0.5)        → 500-1500мс случайно
// - Sleep(1.5)        → 1500-4500мс случайно

// Варианты использования:
// - multiplier > 1.0: Увеличить задержку (например, ожидание медленной страницы)
// - multiplier < 1.0: Уменьшить задержку (например, быстрая печать)
// - multiplier = 1.0: Обычная случайная задержка
```

---

## Паттерны использования

### Простые случайные задержки
```csharp
var sleeper = new Sleeper(1000, 2000);

// Переход и ожидание
instance.ActiveTab.Navigate(url);
sleeper.Sleep();

// Заполнение формы с задержками
instance.ActiveTab.FillTextBox("#field1", value1);
sleeper.Sleep();
instance.ActiveTab.FillTextBox("#field2", value2);
sleeper.Sleep();
```

### Контекстно-зависимые задержки
```csharp
var sleeper = new Sleeper(800, 1500);

// Быстрое действие
sleeper.Sleep(0.5);

// Обычное действие
sleeper.Sleep();

// Медленное действие (загрузка тяжелой страницы)
sleeper.Sleep(2.0);
```

### Паттерн защиты от обнаружения
```csharp
// Разные диапазоны для разных действий
var readDelay = new Sleeper(2000, 5000);   // Чтение контента
var typeDelay = new Sleeper(100, 300);     // Между нажатиями клавиш
var clickDelay = new Sleeper(500, 1500);   // Между кликами

// Имитация чтения
readDelay.Sleep();

// Посимвольный ввод текста
foreach (char c in text)
{
    instance.ActiveTab.SendKey(c.ToString());
    typeDelay.Sleep();
}

// Клик по кнопке
clickDelay.Sleep();
instance.ActiveTab.Click(button);
```

---
