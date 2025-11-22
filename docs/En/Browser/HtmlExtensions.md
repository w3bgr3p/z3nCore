# HtmlExtensions

Extension methods for HtmlElement providing QR code decoding and transaction hash extraction.

## Extension Methods

### DecodeQr()

```csharp
public static string DecodeQr(HtmlElement element)
```

**Purpose**: Decodes QR code from an image element in the browser.

**Example**:
```csharp
HtmlElement qrImage = instance.ActiveTab.FindElementById("qr-code");
string qrContent = qrImage.DecodeQr();
// Returns: "bitcoin:1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa" or "qrIsNull" or "qrError"
```

**Breakdown**:
- `element` - HtmlElement containing QR code image
- Captures 200x200 pixel bitmap from element
- Uses ZXing library for QR code recognition
- Returns decoded QR code text if successful
- Returns "qrIsNull" if QR code is empty or unreadable
- Returns "qrError" if decoding exception occurs

---

### GetTxHash()

```csharp
public static string GetTxHash(HtmlElement element)
```

**Purpose**: Extracts transaction hash from a link element (blockchain transaction URLs).

**Example**:
```csharp
HtmlElement txLink = instance.ActiveTab.FindElementByAttribute("a", "innertext", "View Transaction", "text", 0);
string hash = txLink.GetTxHash();
// Returns: "0x1234abcd5678ef..." (hash from URL)
```

**Breakdown**:
- `element` - HtmlElement with href attribute containing transaction URL
- Extracts last segment from URL path as transaction hash
- Handles URLs like: "https://etherscan.io/tx/0xABC123"
- Returns empty string if URL ends with slash
- Returns full URL if no slashes found
- Throws exception if element has no href attribute or is empty
