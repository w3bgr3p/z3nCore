

##  BackpackWallet

### Назначение

Класс **BackpackWallet** автоматизирует работу с расширением-кошельком Backpack (Solana/Ethereum) в ZennoPoster: установка и импорт кошелька, разблокировка, подключение, одобрение транзакций, управление сетями и аккаунтами, получение адреса, определение текущей сети, а также интеграцию с базой и аппаратно-зависимым паролем.

### Примеры использования

```csharp
// Импортировать кошелёк и получить активный адрес
var wallet = new BackpackWallet(project, instance, log: true, key: "seed или приватный ключ");
string address = wallet.Launch();

// Разблокировать кошелёк
wallet.Unlock();

// Получить текущий адрес
string addr = wallet.ActiveAddress();

// Одобрить транзакцию/действие
wallet.Approve();

// Подключить кошелёк к сайту
wallet.Connect();

// Переключить сеть на devnet
wallet.DevChain("devnet");

// Добавить новый аккаунт Ethereum по приватному ключу
wallet.Add("Ethereum", "key");

// Получить текущую выбранную сеть ("Solana" или "Ethereum")
string net = wallet.Current();
```


## Описание основных методов

### Launch

```csharp
public string Launch(string fileName = null, bool log = false)
```

- Устанавливает расширение Backpack (если не установлено), импортирует кошелёк по seed/ключу.
- Если расширение уже есть — разблокирует кошелёк.
- Возвращает активный адрес кошелька.


### Import

```csharp
public bool Import(bool log = false)
```

- Импортирует кошелёк по seed/ключу:
    - Определяет тип ключа (Solana, Ethereum, seed).
    - Проходит все этапы интерфейса импорта: выбор сети, способа импорта, ввод seed/ключа, пароля.
    - Для seed-фразы — поочередный ввод каждого слова.
    - Возвращает true при успешном импорте, false если кошелёк уже был импортирован.


### Unlock

```csharp
public void Unlock(bool log = false)
```

- Разблокирует кошелёк по паролю (аппаратно-зависимый HWPass).
- Ожидает появления поля ввода пароля, вводит пароль и кликает "Unlock".
- Повторяет попытку при ошибке.


### ActiveAddress

```csharp
public string ActiveAddress(bool log = false)
```

- Получает текущий активный адрес кошелька из интерфейса Backpack.
- Навигирует на popout-страницу расширения, парсит адрес из DOM.
- Возвращает строку адреса.


### CurrentChain

```csharp
public string CurrentChain(bool log = true)
```

- Определяет текущую выбранную сеть (Solana mainnet/devnet/testnet или Ethereum) по иконке в интерфейсе.
- Возвращает строку: "mainnet", "devnet", "testnet", "ethereum".


### Approve

```csharp
public void Approve(bool log = false)
```

- Одобряет действие/транзакцию в интерфейсе Backpack.
- Если требуется — сначала разблокирует кошелёк по паролю, затем кликает "Approve".


### Connect

```csharp
public void Connect(bool log = false)
```

- Подключает кошелёк к сайту через интерфейс Backpack.
- При необходимости разблокирует кошелёк и кликает "Approve".


### Devmode

```csharp
public void Devmode(bool enable = true)
```

- Включает или выключает Dev Mode в настройках Backpack.
- Переходит в настройки, ищет переключатель Dev Mode, изменяет его состояние.


### DevChain

```csharp
public void DevChain(string reqmode = "devnet")
```

- Переключает сеть Solana на нужную (mainnet/devnet/testnet).
- Включает Dev Mode при необходимости.
- Кликает по иконке нужной сети.


### Add

```csharp
public void Add(string type = "Ethereum", string source = "key")
```

- Добавляет новый аккаунт (Ethereum или Solana) по приватному ключу или seed-фразе.
- Проходит все этапы интерфейса добавления: выбор сети, способа импорта, ввод ключа/seed.


### Switch

```csharp
public void Switch(string type)
```

- Переключает текущую выбранную сеть в интерфейсе кошелька ("Solana" или "Ethereum").


### Current

```csharp
public string Current()
```

- Возвращает название текущей выбранной сети: "Solana", "Ethereum" или "Undefined".


## Вспомогательные детали

- **KeyLoad**: определяет, какой ключ брать (evm, sol, seed) — автоматически подгружает из базы, если не указан явно.
- **HWPass**: аппаратно-зависимый пароль через SAFU.
- **Логирование**: все действия логируются через Logger с эмодзи 🎒.
- **Работа с DOM**: все действия реализованы через методы поиска и клика по элементам (`HeClick`, `HeSet`, `HeGet`).
- **Потокобезопасность**: все циклы и ожидания снабжены таймаутами и контролем времени.


## Особенности

- Автоматизация всех этапов установки, импорта, разблокировки и подключения кошелька Backpack.
- Поддержка работы с несколькими сетями и аккаунтами (Solana/Ethereum).
- Интеграция с базой данных проекта для подстановки ключей.
- Использование аппаратно-зависимого пароля для безопасности.
- Поддержка Dev Mode и переключения сетей.
