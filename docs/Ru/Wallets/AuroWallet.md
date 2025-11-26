# Класс AuroWallet

Класс `AuroWallet` обеспечивает автоматизацию работы с расширением браузера Auro Wallet, которое используется для управления учетными записями Mina Protocol.

---

## Конструктор

### AuroWallet

**Назначение**: Инициализирует новый экземпляр класса AuroWallet с необходимыми ссылками на проект и экземпляр браузера.

**Пример**:
```csharp
var auroWallet = new AuroWallet(project, instance, _showLog: true);
```

**Разбор**:
```csharp
public AuroWallet(
    IZennoPosterProjectModel project,  // Модель проекта ZennoPoster для доступа к функциональности проекта
    Instance instance,                  // Экземпляр браузера для взаимодействия
    bool _showLog = false              // Показывать ли подробные логи (по умолчанию: false)
)
```

---

## Публичные методы

### Launch

**Назначение**: Устанавливает или восстанавливает расширение Auro Wallet, импортирует сид-фразу или разблокирует кошелек и возвращает адрес Mina.

**Пример**:
```csharp
var auroWallet = new AuroWallet(project, instance);
string minaAddress = auroWallet.Launch();
project.SendInfoToLog($"Адрес кошелька: {minaAddress}");
```

**Разбор**:
```csharp
public string Launch()
// Возвращает: Адрес кошелька Mina (string)
// - Устанавливает расширение Auro Wallet из CRX файла, если оно отсутствует
// - Если новая установка: восстанавливает кошелек используя сид-фразу из базы данных
// - Если уже установлен: разблокирует кошелек используя сохраненный пароль
// - Переходит на страницу получения для извлечения адреса кошелька
// - Обновляет базу данных адресом
// - Закрывает лишние вкладки
// - Возвращает адрес Mina
```

---

### SwitchChain

**Назначение**: Переключает Auro Wallet на другую сеть Mina (Testnet, Devnet или Mainnet).

**Пример**:
```csharp
var auroWallet = new AuroWallet(project, instance);
auroWallet.Launch();
auroWallet.SwitchChain("Testnet");  // Переключение на Testnet
```

**Разбор**:
```csharp
public void SwitchChain(
    string chain = "Testnet"  // Целевая сеть: "Testnet", "Devnet" или "Mainnet" (по умолчанию: "Testnet")
)
// Возвращает: void
// - Активирует расширение Auro Wallet
// - Проверяет, находится ли уже в целевой сети
// - Если нет, открывает селектор сети
// - Выбирает соответствующую сеть на основе параметра chain
// - Раскрывает дополнительные сети при необходимости (например, для Testnet)
```

---

### Unlock

**Назначение**: Разблокирует Auro Wallet используя сохраненный пароль.

**Пример**:
```csharp
var auroWallet = new AuroWallet(project, instance);
auroWallet.Unlock();
```

**Разбор**:
```csharp
public void Unlock()
// Возвращает: void
// - Активирует расширение Auro Wallet
// - Извлекает пароль устройства из безопасного хранилища
// - Проверяет, разблокирован ли уже кошелек
// - Если заблокирован: вводит пароль и нажимает кнопку разблокировки
// - Если уже разблокирован: немедленно возвращается
```
