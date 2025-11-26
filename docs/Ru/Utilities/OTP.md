# Класс OTP

Статический класс для генерации одноразовых паролей (OTP) с использованием алгоритма TOTP.

---

## Offline

### Назначение
Генерирует TOTP-код из секретного ключа, с опциональным ожиданием свежего кода, если оставшееся время мало.

### Пример
```csharp
using z3nCore;

// Генерация OTP из секретного ключа
string secret = "JBSWY3DPEHPK3PXP";
string code = OTP.Offline(secret);
// Результат: "123456" (6-значный код)

// Ожидание свежего кода, если осталось меньше 10 секунд
string freshCode = OTP.Offline(secret, waitIfTimeLess: 10);

// Использование в автоматизации
project.SendInfoToLog($"OTP код: {code}");
instance.ActiveTab.FillTextBox("input[name='otp']", code);
```

### Детали
```csharp
public static string Offline(
    string keyString,           // Секретный ключ в кодировке Base32
    int waitIfTimeLess = 5)    // Ожидать новый код, если осталось секунд < этого значения

// Возвращает: 6-значный TOTP-код в виде строки

// Выбрасывает:
// - Exception: Если keyString null или пуст

// Как работает:
// 1. Декодирует секретный ключ из Base32
// 2. Генерирует текущий TOTP-код
// 3. Проверяет оставшиеся секунды в текущем временном окне
// 4. Если оставшиеся секунды <= waitIfTimeLess:
//    - Ожидает следующего временного окна
//    - Генерирует свежий код
// 5. Возвращает код

// Примечания:
// - Использует 30-секундные временные окна (стандарт TOTP)
// - Требуется кодировка Base32 для секретного ключа
// - Код обновляется каждые 30 секунд
// - waitIfTimeLess предотвращает использование кодов на грани истечения
```

---

## FirstMail

### Назначение
Получает OTP-код из почтового сервиса FirstMail.

### Пример
```csharp
using z3nCore;

// Получение OTP из email
string email = "user@firstmail.ltd";
string code = OTP.FirstMail(project, email);

// Использование кода
if (!string.IsNullOrEmpty(code))
{
    project.SendInfoToLog($"Получен OTP: {code}");
    instance.ActiveTab.FillTextBox("#otp-input", code);
}
```

### Детали
```csharp
public static string FirstMail(
    IZennoPosterProjectModel project,  // Экземпляр проекта для логирования
    string email)                      // Email адрес для проверки

// Возвращает: OTP-код, извлеченный из email
// Возвращает: null или пустую строку, если OTP не найден

// Выбрасывает:
// - Exception: Если параметр email null или пуст

// Примечания:
// - Требует реализации класса FirstMail
// - Email должен быть доступен через сервис FirstMail
// - Автоматически извлекает OTP из содержимого email
```

---
