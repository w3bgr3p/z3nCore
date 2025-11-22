# Класс SuietWallet

Класс `SuietWallet` обеспечивает автоматизацию работы с расширением браузера Suiet Wallet, разработанным для блокчейна Sui.

---

## Конструктор

### SuietWallet

**Назначение**: Инициализирует новый экземпляр класса SuietWallet для управления кошельками Sui.

**Пример**:
```csharp
var suiet = new SuietWallet(project, instance, log: true);
```

**Разбор**:
```csharp
public SuietWallet(
    IZennoPosterProjectModel project,                              // Модель проекта ZennoPoster
    Instance instance,                                              // Экземпляр браузера
    bool log = false,                                               // Включить подробное логирование (по умолчанию: false)
    string key = null,                                              // Приватный ключ или сид-фраза (опционально)
    string fileName = "Suiet-Sui-Wallet-Chrome.crx"                // Имя CRX файла расширения (по умолчанию: "Suiet-Sui-Wallet-Chrome.crx")
)
```

---

## Публичные методы

### Launch

**Назначение**: Устанавливает расширение Suiet Wallet, импортирует кошелек или разблокирует его и возвращает адрес Sui.

**Пример**:
```csharp
var suiet = new SuietWallet(project, instance);
string address = suiet.Launch(source: "key");
project.SendInfoToLog($"Адрес Sui: {address}");
```

**Разбор**:
```csharp
public string Launch(
    string source = null  // Источник ключа: "key" (приватный ключ) или "seed" (сид-фраза) (по умолчанию: "key")
)
// Возвращает: Адрес кошелька Sui (string)
// - Определяет тип ключа и конвертирует сид-фразу в Sui-совместимый ключ при необходимости
// - Отключает полную эмуляцию мыши для более быстрого взаимодействия
// - Переключается на расширение Suiet
// - Если новая установка: импортирует кошелек используя определенный ключ
// - Если уже установлен: разблокирует кошелек
// - Извлекает активный адрес кошелька
// - Закрывает лишние вкладки
// - Восстанавливает настройку эмуляции мыши
// - Возвращает адрес Sui
```

---

### Sign

**Назначение**: Подписывает транзакцию или сообщение в Suiet Wallet путем нажатия основной кнопки.

**Пример**:
```csharp
var suiet = new SuietWallet(project, instance);
suiet.Sign(deadline: 15, delay: 5);
```

**Разбор**:
```csharp
public void Sign(
    int deadline = 10,  // Максимальное время ожидания кнопки (по умолчанию: 10 секунд)
    int delay = 3       // Задержка после нажатия (по умолчанию: 3 секунды)
)
// Возвращает: void
// - Ожидает появления основной кнопки (до deadline секунд)
// - Нажимает основную кнопку (обычно "Approve" или "Sign")
// - Ожидает указанную задержку после нажатия
// - Использует CSS селектор класса для поиска кнопки
```

---

### Unlock

**Назначение**: Разблокирует Suiet Wallet используя сохраненный пароль.

**Пример**:
```csharp
var suiet = new SuietWallet(project, instance);
suiet.Unlock();
```

**Разбор**:
```csharp
public void Unlock()
// Возвращает: void
// - Переходит на popup Suiet
// - Извлекает пароль устройства из безопасного хранилища
// - Вводит пароль в поле пароля (первый элемент input:password)
// - Нажимает кнопку "Unlock"
```

---

### SwitchChain

**Назначение**: Переключает Suiet Wallet на другую сеть Sui (Mainnet, Testnet или Devnet).

**Пример**:
```csharp
var suiet = new SuietWallet(project, instance);
suiet.SwitchChain("Testnet");  // Переключение на Sui Testnet
```

**Разбор**:
```csharp
public void SwitchChain(
    string mode = "Mainnet"  // Целевая сеть: "Mainnet", "Testnet" или "Devnet" (по умолчанию: "Mainnet")
)
// Возвращает: void
// - Определяет индекс сети (0 для Mainnet, 1 для Testnet, 2 для Devnet)
// - Переходит на страницу настроек сетей
// - Нажимает на контейнер выбора сети по вычисленному индексу
// - Нажимает "Save" для применения изменения сети
```

---

### ActiveAddress

**Назначение**: Извлекает текущий активный адрес кошелька Sui.

**Пример**:
```csharp
var suiet = new SuietWallet(project, instance);
string address = suiet.ActiveAddress();
project.SendInfoToLog($"Активный адрес Sui: {address}");
```

**Разбор**:
```csharp
public string ActiveAddress()
// Возвращает: Активный адрес кошелька Sui (string)
// - Находит элемент платежной ссылки, содержащий адрес кошелька
// - Извлекает атрибут href
// - Удаляет префикс "https://pay.suiet.app/?wallet_address="
// - Возвращает чистый адрес кошелька
```
