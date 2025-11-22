# Класс BackpackWallet

Класс `BackpackWallet` обеспечивает автоматизацию работы с расширением браузера Backpack Wallet, поддерживающим блокчейны Solana и Ethereum.

---

## Конструктор

### BackpackWallet

**Назначение**: Инициализирует новый экземпляр класса BackpackWallet со ссылками на проект и опциональной конфигурацией.

**Пример**:
```csharp
var backpack = new BackpackWallet(project, instance, log: true);
```

**Разбор**:
```csharp
public BackpackWallet(
    IZennoPosterProjectModel project,                    // Модель проекта ZennoPoster
    Instance instance,                                    // Экземпляр браузера
    bool log = false,                                     // Включить подробное логирование (по умолчанию: false)
    string key = null,                                    // Приватный ключ или сид-фраза (по умолчанию: null, загружается из БД)
    string fileName = "Backpack0.10.94.crx"              // Имя CRX файла расширения (по умолчанию: "Backpack0.10.94.crx")
)
```

---

## Публичные методы

### Launch

**Назначение**: Устанавливает расширение Backpack Wallet, импортирует кошелек или разблокирует его и возвращает активный адрес.

**Пример**:
```csharp
var backpack = new BackpackWallet(project, instance);
string address = backpack.Launch();
project.SendInfoToLog($"Адрес кошелька: {address}");
```

**Разбор**:
```csharp
public string Launch(
    string fileName = null,  // Имя CRX файла (по умолчанию: использует значение из конструктора)
    bool log = false         // Включить логирование (по умолчанию: false)
)
// Возвращает: Активный адрес кошелька (string)
// - Переключается на расширение Backpack
// - Если новая установка: импортирует кошелек используя приватный ключ или сид-фразу
// - Если уже установлен: разблокирует кошелек
// - Возвращает активный адрес
// - Закрывает лишние вкладки
```

---

### Unlock

**Назначение**: Разблокирует Backpack Wallet используя сохраненный пароль.

**Пример**:
```csharp
var backpack = new BackpackWallet(project, instance);
backpack.Unlock();
```

**Разбор**:
```csharp
public void Unlock(
    bool log = false  // Включить логирование (по умолчанию: false)
)
// Возвращает: void
// - Переходит на popup Backpack, если еще не там
// - Ожидает экран разблокировки или разблокированного состояния
// - Если заблокирован: вводит пароль и нажимает кнопку разблокировки
// - Если уже разблокирован: немедленно возвращается
// - Использует паттерн goto для логики конечного автомата
```

---

### Approve

**Назначение**: Подтверждает транзакцию или запрос подключения в Backpack Wallet.

**Пример**:
```csharp
var backpack = new BackpackWallet(project, instance);
backpack.Approve();
```

**Разбор**:
```csharp
public void Approve(
    bool log = false  // Включить логирование (по умолчанию: false)
)
// Возвращает: void
// - Пытается нажать кнопку "Approve" напрямую
// - Если не удается: сначала разблокирует кошелек, затем подтверждает
// - Закрывает лишние вкладки после подтверждения
// - Перехватывает исключения для обработки заблокированного состояния
```

---

### Connect

**Назначение**: Обрабатывает запросы подключения от dApps к Backpack Wallet.

**Пример**:
```csharp
var backpack = new BackpackWallet(project, instance);
backpack.Connect();
```

**Разбор**:
```csharp
public void Connect(
    bool log = false  // Включить логирование (по умолчанию: false)
)
// Возвращает: void
// - Ожидает запрос на подтверждение подключения
// - Разблокирует кошелек при необходимости
// - Нажимает кнопку "Approve" для установления подключения
// - Использует паттерн конечного автомата с goto для надежной обработки
// - Прерывается через 30 секунд, если не обнаружена вкладка кошелька
```

---

### ActiveAddress

**Назначение**: Извлекает текущий активный адрес кошелька из Backpack.

**Пример**:
```csharp
var backpack = new BackpackWallet(project, instance);
string address = backpack.ActiveAddress();
project.SendInfoToLog($"Активный адрес: {address}");
```

**Разбор**:
```csharp
public string ActiveAddress(
    bool log = false  // Включить логирование (по умолчанию: false)
)
// Возвращает: Активный адрес кошелька (string)
// - Переходит на popup Backpack
// - Закрывает лишние вкладки
// - Открывает детали кошелька через навигационные элементы
// - Извлекает адрес со страницы
// - Закрывает вид деталей
// - Возвращает адрес
// - Выбрасывает исключение, если не удается извлечь адрес
```

---

### CurrentChain

**Назначение**: Определяет, к какой сети блокчейна в данный момент подключен Backpack.

**Пример**:
```csharp
var backpack = new BackpackWallet(project, instance);
string chain = backpack.CurrentChain();
project.SendInfoToLog($"Текущая сеть: {chain}");
```

**Разбор**:
```csharp
public string CurrentChain(
    bool log = true  // Включить логирование (по умолчанию: true)
)
// Возвращает: Имя текущей сети: "mainnet", "devnet", "testnet" или "ethereum" (string)
// - Извлекает HTML элемента селектора сети
// - Проверяет наличие специфичных для сети ссылок на изображения
// - Возвращает обнаруженное имя сети
// - Повторяет попытку, пока не будет обнаружена валидная сеть
```

---

### Devmode

**Назначение**: Включает или отключает режим разработчика в Backpack Wallet для доступа к тестовым сетям.

**Пример**:
```csharp
var backpack = new BackpackWallet(project, instance);
backpack.Devmode(enable: true);  // Включить режим разработчика
```

**Разбор**:
```csharp
public void Devmode(
    bool enable = true  // Включить (true) или отключить (false) режим разработчика (по умолчанию: true)
)
// Возвращает: void
// - Переходит на popup Backpack
// - Открывает меню настроек, если еще не открыто
// - Проверяет текущее состояние режима разработчика
// - Переключает чекбокс режима разработчика, если состояние не совпадает с желаемым
// - Проверяет успешность переключения
```

---

### DevChain

**Назначение**: Переключает Backpack Wallet на конкретную сеть разработки Solana (devnet, testnet или mainnet).

**Пример**:
```csharp
var backpack = new BackpackWallet(project, instance);
backpack.DevChain("devnet");  // Переключение на Solana Devnet
```

**Разбор**:
```csharp
public void DevChain(
    string reqmode = "devnet"  // Целевая сеть: "devnet", "testnet" или "mainnet" (по умолчанию: "devnet")
)
// Возвращает: void
// - Сначала переключается на сеть Solana
// - Проверяет текущую сеть
// - Если не на целевой сети: открывает селектор сети
// - Включает режим разработчика, если тестовые сети недоступны
// - Выбирает запрошенную сеть
// - Обрабатывает передачи через мост при необходимости
```

---

### Add

**Назначение**: Добавляет новый кошелек (Solana или Ethereum) в Backpack используя приватный ключ или сид-фразу.

**Пример**:
```csharp
var backpack = new BackpackWallet(project, instance);
backpack.Add(type: "Ethereum", source: "key");
```

**Разбор**:
```csharp
public void Add(
    string type = "Ethereum",  // Тип блокчейна: "Ethereum" или "Solana" (по умолчанию: "Ethereum")
    string source = "key"      // Источник импорта: "key" (приватный ключ) или "phrase" (сид-фраза) (по умолчанию: "key")
)
// Возвращает: void
// - Извлекает соответствующий ключ из базы данных на основе типа
// - Переходит на URL добавления аккаунта
// - Проходит через поток импорта
// - Выбирает тип блокчейна
// - Выбирает метод импорта (ключ или фраза)
// - Вводит ключ или сид-фразу
// - Завершает импорт и закрывает лишние вкладки
```

---

### Switch

**Назначение**: Переключается между кошельками Solana и Ethereum в Backpack.

**Пример**:
```csharp
var backpack = new BackpackWallet(project, instance);
backpack.Switch("Ethereum");  // Переключение на кошелек Ethereum
```

**Разбор**:
```csharp
public void Switch(
    string type  // Тип кошелька для переключения: "Solana" или "Ethereum"
)
// Возвращает: void
// - Переходит на popup Backpack, если еще не там
// - Открывает меню выбора кошелька
// - Считает доступные кошельки
// - Если отсутствует требуемый кошелек: добавляет его автоматически
// - Выбирает целевой кошелек по индексу (0 для Solana, 1 для Ethereum)
```

---

### Current

**Назначение**: Определяет, какой блокчейн (Solana или Ethereum) в данный момент активен в Backpack.

**Пример**:
```csharp
var backpack = new BackpackWallet(project, instance);
string currentChain = backpack.Current();
project.SendInfoToLog($"Текущий блокчейн: {currentChain}");
```

**Разбор**:
```csharp
public string Current()
// Возвращает: Текущий блокчейн: "Solana", "Ethereum" или "Undefined" (string)
// - Переходит на popup Backpack
// - Извлекает HTML элемента селектора сети
// - Проверяет наличие специфичных для блокчейна идентификаторов
// - Возвращает обнаруженное имя блокчейна
// - Логирует результат
```
