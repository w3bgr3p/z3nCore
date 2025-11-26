# HtmlExtensions

Методы расширения для HtmlElement, предоставляющие декодирование QR кодов и извлечение хэшей транзакций.

## Методы расширения

### DecodeQr()

```csharp
public static string DecodeQr(HtmlElement element)
```

**Назначение**: Декодирует QR код из изображения элемента в браузере.

**Пример**:
```csharp
HtmlElement qrImage = instance.ActiveTab.FindElementById("qr-code");
string qrContent = qrImage.DecodeQr();
// Возвращает: "bitcoin:1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa" или "qrIsNull" или "qrError"
```

**Детали**:
- `element` - HtmlElement, содержащий изображение QR кода
- Захватывает bitmap 200x200 пикселей из элемента
- Использует библиотеку ZXing для распознавания QR кода
- Возвращает декодированный текст QR кода при успехе
- Возвращает "qrIsNull", если QR код пуст или нечитаем
- Возвращает "qrError" при исключении декодирования

---

### GetTxHash()

```csharp
public static string GetTxHash(HtmlElement element)
```

**Назначение**: Извлекает хэш транзакции из элемента ссылки (URL транзакций блокчейна).

**Пример**:
```csharp
HtmlElement txLink = instance.ActiveTab.FindElementByAttribute("a", "innertext", "View Transaction", "text", 0);
string hash = txLink.GetTxHash();
// Возвращает: "0x1234abcd5678ef..." (хэш из URL)
```

**Детали**:
- `element` - HtmlElement с атрибутом href, содержащим URL транзакции
- Извлекает последний сегмент из пути URL как хэш транзакции
- Обрабатывает URL вида: "https://etherscan.io/tx/0xABC123"
- Возвращает пустую строку, если URL заканчивается на слэш
- Возвращает полный URL, если слэши не найдены
- Выбрасывает исключение, если элемент не имеет атрибута href или пуст
