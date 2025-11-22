# StringExtensions

Методы расширения для работы со строками, криптографией, парсингом и преобразованием данных.

---

## Криптографические методы

### NormalizeAddress

#### Назначение
Обеспечивает наличие префикса "0x" в Ethereum-адресе.

#### Пример
```csharp
string address = "1234567890abcdef";
string normalized = address.NormalizeAddress();
// Вернёт: "0x1234567890abcdef"
```

#### Детали реализации
```csharp
public static string NormalizeAddress(this string address)
{
    // address: Ethereum-адрес для нормализации
    // Возвращает: Адрес с префиксом "0x", или исходную строку если null/empty

    if (string.IsNullOrEmpty(address))
        return address;

    if (!address.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        return "0x" + address;

    return address;
}
```

---

### GetTxHash

#### Назначение
Извлекает хеш транзакции из URL блокчейн-эксплорера.

#### Пример
```csharp
string url = "https://etherscan.io/tx/0xabc123...";
string hash = url.GetTxHash();
// Вернёт: "0xabc123..."
```

#### Детали реализации
```csharp
public static string GetTxHash(this string link)
{
    // link: URL, содержащий хеш транзакции
    // Исключения: Exception, если ссылка null или пуста
    // Возвращает: Хеш транзакции, извлечённый из URL

    if (!string.IsNullOrEmpty(link))
    {
        int lastSlashIndex = link.LastIndexOf('/');
        if (lastSlashIndex == -1)
            hash = link;
        else if (lastSlashIndex == link.Length - 1)
            hash = string.Empty;
        else
            hash = link.Substring(lastSlashIndex + 1);
    }
    else
        throw new Exception("empty Element");

    return hash;
}
```

---

### ToAdrEvm / ToEvmAddress

#### Назначение
Преобразует приватный ключ или сид-фразу в EVM (Ethereum) адрес.

#### Пример
```csharp
string privateKey = "0x1234...";
string address = privateKey.ToEvmAddress();

string seed = "word1 word2 word3 ... word12";
string addressFromSeed = seed.ToEvmAddress();
```

#### Детали реализации
```csharp
public static string ToEvmAddress(this string key)
{
    // key: Приватный ключ (64 hex символа) или сид-фраза (12/24 слова)
    // Возвращает: EVM-адрес, полученный из ключа

    string keyType = key.DetectKeyType();
    var blockchain = new Blockchain();

    // Если сид-фраза, сначала получаем приватный ключ
    if (keyType == "seed")
    {
        var mnemonicObj = new Mnemonic(key);
        var hdRoot = mnemonicObj.DeriveExtKey();
        var derivationPath = new NBitcoin.KeyPath("m/44'/60'/0'/0/0");
        key = hdRoot.Derive(derivationPath).PrivateKey.ToHex();
    }

    return blockchain.GetAddressFromPrivateKey(key);
}
```

---

### ToSepc256k1

#### Назначение
Получает приватный ключ secp256k1 из сид-фразы используя определённый путь деривации.

#### Пример
```csharp
string seed = "word1 word2 ... word12";
string privateKey = seed.ToSepc256k1();       // Путь: m/44'/60'/0'/0/0
string privateKey2 = seed.ToSepc256k1(path: 1); // Путь: m/44'/60'/0'/0/1
```

#### Детали реализации
```csharp
public static string ToSepc256k1(this string seed, int path = 0)
{
    // seed: BIP39 сид-фраза
    // path: Индекс пути деривации (по умолчанию: 0)
    // Возвращает: Приватный ключ в шестнадцатеричном формате

    var blockchain = new Blockchain();
    var mnemonicObj = new Mnemonic(seed);
    var hdRoot = mnemonicObj.DeriveExtKey();
    var derivationPath = new NBitcoin.KeyPath($"m/44'/60'/0'/0/{path}");
    var key = hdRoot.Derive(derivationPath).PrivateKey.ToHex();
    return key;
}
```

---

### ToEvmPrivateKey

#### Назначение
Генерирует детерминированный приватный ключ из любой строки используя SHA256 хеш.

#### Пример
```csharp
string password = "mySecretPassword123";
string privateKey = password.ToEvmPrivateKey();
// Вернёт: SHA256 хеш входных данных в виде hex-строки
```

#### Детали реализации
```csharp
public static string ToEvmPrivateKey(this string input)
{
    // input: Любая строка для преобразования
    // Исключения: ArgumentException, если input null или пустой
    // Возвращает: SHA256 хеш входных данных в шестнадцатеричном виде

    if (string.IsNullOrEmpty(input))
        throw new ArgumentException("Input string cannot be null or empty.");

    byte[] inputBytes = Encoding.UTF8.GetBytes(input);

    using (SHA256 sha256 = SHA256.Create())
    {
        byte[] hashBytes = sha256.ComputeHash(inputBytes);
        StringBuilder hex = new StringBuilder(hashBytes.Length * 2);
        foreach (byte b in hashBytes)
        {
            hex.AppendFormat("{0:x2}", b);
        }
        return hex.ToString();
    }
}
```

---

### TxToString

#### Назначение
Парсит JSON транзакции и извлекает ключевые поля в виде массива строк.

#### Пример
```csharp
string txJson = "{\"gas\":\"0x5208\",\"value\":\"0x0\",\"from\":\"0x...\",\"to\":\"0x...\",\"data\":\"0x\"}";
string[] txData = txJson.TxToString();
// Вернёт: [gas, value, sender, data, recipient, gwei]
```

#### Детали реализации
```csharp
public static string[] TxToString(this string txJson)
{
    // txJson: Данные транзакции в формате JSON
    // Возвращает: Массив [gas, value, sender, data, recipient, gwei]

    dynamic txData = JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(txJson);

    string gas = $"{txData.gas}";
    string value = $"{txData.value}";
    string sender = $"{txData.from}";
    string recipient = $"{txData.to}";
    string data = $"{txData.data}";

    // Преобразуем gas из hex в Gwei
    BigInteger gasWei = BigInteger.Parse("0" + gas.TrimStart('0', 'x'), NumberStyles.AllowHexSpecifier);
    decimal gasGwei = (decimal)gasWei / 1000000000m;
    string gwei = gasGwei.ToString().Replace(',', '.');

    return new string[] { gas, value, sender, data, recipient, gwei };
}
```

---

### ChkAddress

#### Назначение
Проверяет, соответствует ли сокращённый адрес (например, "0x1234…5678") полному адресу.

#### Пример
```csharp
string shortAddr = "0x1234…7890";
string fullAddr = "0x123456789abcdef67890";
bool matches = shortAddr.ChkAddress(fullAddr);
// Вернёт: true, если префикс и суффикс совпадают
```

#### Детали реализации
```csharp
public static bool ChkAddress(this string shortAddress, string fullAddress)
{
    // shortAddress: Сокращённый адрес с разделителем "…"
    // fullAddress: Полный адрес для проверки
    // Возвращает: true, если префикс и суффикс совпадают, иначе false

    if (string.IsNullOrEmpty(shortAddress) || string.IsNullOrEmpty(fullAddress))
        return false;

    if (!shortAddress.Contains("…") || shortAddress.Count(c => c == '…') != 1)
        return false;

    var parts = shortAddress.Split('…');
    string prefix = parts[0];
    string suffix = parts[1];

    bool prefixMatch = fullAddress.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    bool suffixMatch = fullAddress.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);

    return prefixMatch && suffixMatch;
}
```

---

### StringToHex

#### Назначение
Преобразует десятичное число в виде строки в шестнадцатеричный формат с опциональной конвертацией единиц (Gwei, ETH).

#### Пример
```csharp
"100".StringToHex();           // Вернёт: "0x64"
"5".StringToHex("gwei");       // Вернёт: "0x12A05F200" (5 Gwei в Wei)
"1".StringToHex("eth");        // Вернёт: "0xDE0B6B3A7640000" (1 ETH в Wei)
```

#### Детали реализации
```csharp
public static string StringToHex(this string value, string convert = "")
{
    // value: Десятичное число в виде строки
    // convert: Опциональная единица конвертации ("gwei", "eth" или пусто)
    // Возвращает: Шестнадцатеричное представление с префиксом "0x"

    if (string.IsNullOrEmpty(value)) return "0x0";

    value = value?.Trim();
    if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal number))
        return "0x0";

    BigInteger result;
    switch (convert.ToLower())
    {
        case "gwei":
            result = (BigInteger)(number * 1000000000m);
            break;
        case "eth":
            result = (BigInteger)(number * 1000000000000000000m);
            break;
        default:
            result = (BigInteger)number;
            break;
    }

    string hex = result.ToString("X").TrimStart('0');
    return string.IsNullOrEmpty(hex) ? "0x0" : "0x" + hex;
}
```

---

### HexToString

#### Назначение
Преобразует шестнадцатеричное значение в десятичную строку с опциональной конвертацией единиц.

#### Пример
```csharp
"0x64".HexToString();          // Вернёт: "100"
"0x12A05F200".HexToString("gwei");  // Вернёт: "5"
"0xDE0B6B3A7640000".HexToString("eth");  // Вернёт: "1"
```

#### Детали реализации
```csharp
public static string HexToString(this string hexValue, string convert = "")
{
    // hexValue: Шестнадцатеричное значение (с или без "0x")
    // convert: Опциональная единица конвертации ("gwei", "eth" или пусто)
    // Возвращает: Десятичное значение в виде строки

    hexValue = hexValue?.Replace("0x", "").Trim();
    if (string.IsNullOrEmpty(hexValue)) return "0";

    BigInteger number = BigInteger.Parse("0" + hexValue, NumberStyles.AllowHexSpecifier);

    switch (convert.ToLower())
    {
        case "gwei":
            decimal gweiValue = (decimal)number / 1000000000m;
            return gweiValue.ToString("0.#########", CultureInfo.InvariantCulture);
        case "eth":
            decimal ethValue = (decimal)number / 1000000000000000000m;
            return ethValue.ToString("0.##################", CultureInfo.InvariantCulture);
        default:
            return number.ToString();
    }
}
```

---

### KeyType

#### Назначение
Определяет тип криптовалютного приватного ключа или сид-фразы.

#### Пример
```csharp
"word1 word2 ... word12".KeyType();  // Вернёт: "seed"
"0x1234...64chars".KeyType();        // Вернёт: "keyEvm"
"base58string87chars".KeyType();     // Вернёт: "keySol"
"suiprivkey1...".KeyType();          // Вернёт: "keySui"
```

#### Детали реализации
```csharp
public static string KeyType(this string input)
{
    // input: Ключ или сид-фраза для анализа
    // Исключения: Exception, если input null/пустой
    // Возвращает: "seed", "keyEvm", "keySol", "keySui" или "undefined"

    if (string.IsNullOrWhiteSpace(input))
        throw new Exception($"input isNullOrEmpty");

    input = input.Trim();

    // Проверка на Bech32 (приватный ключ Sui)
    if (input.StartsWith("suiprivkey1"))
        return "keySui";

    string cleanInput = input.StartsWith("0x") ? input.Substring(2) : input;

    // EVM приватный ключ (64 hex символа)
    if (Regex.IsMatch(cleanInput, @"^[0-9a-fA-F]{64}$"))
        return "keyEvm";

    // Solana приватный ключ (Base58, 87-88 символов)
    if (Regex.IsMatch(input, @"^[123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz]{87,88}$"))
        return "keySol";

    // Мнемоника (12 или 24 слова)
    var words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    if (words.Length == 12 || words.Length == 24)
        return "seed";

    return "undefined";
}
```

---

## Методы для работы с SVG

### SaveSvgStringToImage

#### Назначение
Преобразует SVG-строку в PNG-изображение и сохраняет в файл.

#### Пример
```csharp
string svgContent = "<svg>...</svg>";
svgContent.SaveSvgStringToImage("C:\\path\\to\\output.png");
```

#### Детали реализации
```csharp
public static void SaveSvgStringToImage(this string svgContent, string pathToScreen)
{
    // svgContent: SVG-содержимое в виде строки
    // pathToScreen: Путь к файлу, куда будет сохранено PNG-изображение
    // Возвращает: void

    var svgDocument = SvgDocument.FromSvg<SvgDocument>(svgContent);
    using (var bitmap = svgDocument.Draw())
    {
        bitmap.Save(pathToScreen);
    }
}
```

---

### SvgToBase64

#### Назначение
Преобразует SVG-строку в Base64-кодированное PNG-изображение.

#### Пример
```csharp
string svgContent = "<svg>...</svg>";
string base64Image = svgContent.SvgToBase64();
// Вернёт: Base64-строку PNG-изображения
```

#### Детали реализации
```csharp
public static string SvgToBase64(this string svgContent)
{
    // svgContent: SVG-содержимое в виде строки
    // Возвращает: Base64-кодированное PNG-изображение

    var svgDocument = SvgDocument.FromSvg<SvgDocument>(svgContent);
    using (var bitmap = svgDocument.Draw())
    using (var ms = new System.IO.MemoryStream())
    {
        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return Convert.ToBase64String(ms.ToArray());
    }
}
```

---

## Методы обработки текста

### Regx

#### Назначение
Извлекает текст используя regex-паттерн через макрос ZennoLab.

#### Пример
```csharp
string text = "Order #12345 confirmed";
string orderNumber = text.Regx(@"Order #(\d+)");
// Вернёт: "12345"
```

#### Детали реализации
```csharp
public static string Regx(this string input, string pattern)
{
    // input: Исходный текст
    // pattern: Regex-паттерн
    // Возвращает: Первую найденную группу или null

    var result = ZennoLab.Macros.TextProcessing.Regex(input, pattern, "0")[0].FirstOrDefault();
    return result;
}
```

---

### EscapeMarkdown

#### Назначение
Экранирует специальные символы Markdown в тексте.

#### Пример
```csharp
string text = "Price: $100 (50% off!)";
string escaped = text.EscapeMarkdown();
// Вернёт: "Price: \\$100 \\(50\\% off\\!\\)"
```

#### Детали реализации
```csharp
public static string EscapeMarkdown(this string text)
{
    // text: Текст, содержащий специальные символы Markdown
    // Возвращает: Текст с экранированными символами Markdown

    string[] specialChars = new[] { "_", "*", "[", "]", "(", ")", "~", "`", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!" };

    foreach (var ch in specialChars)
    {
        text = text.Replace(ch, "\\" + ch);
    }

    return text;
}
```

---

### JsonToDic

#### Назначение
Преобразует вложенный JSON в словарь с ключами, разделёнными подчёркиванием.

#### Пример
```csharp
string json = "{\"user\":{\"name\":\"John\",\"age\":30}}";
var dict = json.JsonToDic();
// Вернёт: {"user_name": "John", "user_age": "30"}
```

#### Детали реализации
```csharp
public static Dictionary<string, string> JsonToDic(this string json)
{
    // json: JSON-строка для "выравнивания"
    // Возвращает: Словарь с "выравненными" ключами через подчёркивание

    var result = new Dictionary<string, string>();
    var jObject = JObject.Parse(json);
    FlattenJson(jObject, "", result);
    return result;

    // Рекурсивная функция для "выравнивания" вложенных объектов и массивов
    void FlattenJson(JToken token, string prefix, Dictionary<string, string> dict)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                foreach (var property in token.Children<JProperty>())
                {
                    var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}_{property.Name}";
                    FlattenJson(property.Value, key, dict);
                }
                break;
            case JTokenType.Array:
                var index = 0;
                foreach (var item in token.Children())
                {
                    FlattenJson(item, $"{prefix}_{index}", dict);
                    index++;
                }
                break;
            default:
                dict[prefix] = token.ToString();
                break;
        }
    }
}
```

---

### ParseCreds

#### Назначение
Парсит строку с разделителями в словарь на основе шаблона формата.

#### Пример
```csharp
string data = "john@mail.com:password123:extra";
string format = "{email}:{password}:{note}";
var parsed = data.ParseCreds(format);
// Вернёт: {"email": "john@mail.com", "password": "password123", "note": "extra"}
```

#### Детали реализации
```csharp
public static Dictionary<string, string> ParseCreds(this string data, string format, char devider = ':')
{
    // data: Строка с данными, разделёнными символами
    // format: Шаблон с плейсхолдерами в фигурных скобках, например "{email}:{password}"
    // devider: Символ-разделитель (по умолчанию: ':')
    // Возвращает: Словарь, сопоставляющий имена плейсхолдеров со значениями

    var parsedData = new Dictionary<string, string>();

    if (string.IsNullOrWhiteSpace(format) || string.IsNullOrWhiteSpace(data))
        return parsedData;

    string[] formatParts = format.Split(devider);
    string[] dataParts = data.Split(devider);

    for (int i = 0; i < formatParts.Length && i < dataParts.Length; i++)
    {
        string key = formatParts[i].Trim('{', '}').Trim();
        if (!string.IsNullOrEmpty(key))
            parsedData[key] = dataParts[i].Trim();
    }
    return parsedData;
}
```

---

### ParseByMask

#### Назначение
Парсит строку используя шаблон-маску с плейсхолдерами.

#### Пример
```csharp
string input = "User: John, Age: 30";
string mask = "User: {name}, Age: {age}";
var result = input.ParseByMask(mask);
// Вернёт: {"name": "John", "age": "30"}
```

#### Детали реализации
```csharp
public static Dictionary<string, string> ParseByMask(this string input, string mask)
{
    // input: Строка для парсинга
    // mask: Шаблон с маркерами {placeholder}
    // Возвращает: Словарь с извлечёнными значениями, пустой если нет совпадений

    input = input?.Trim();
    mask = mask?.Trim();

    if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(mask))
        return new Dictionary<string, string>();

    // Извлекаем имена переменных из маски
    var variableNames = new List<string>();
    var regex = new Regex(@"\{([^{}]+)\}");
    foreach (Match mtch in regex.Matches(mask))
    {
        variableNames.Add(mtch.Groups[1].Value);
    }

    if (variableNames.Count == 0)
        return new Dictionary<string, string>();

    // Преобразуем маску в regex-паттерн
    string pattern = Regex.Escape(mask);
    foreach (var varName in variableNames)
    {
        string escapedVar = Regex.Escape("{" + varName + "}");
        pattern = pattern.Replace(escapedVar, "(.*?)");
    }
    pattern += "$";

    // Находим совпадения и извлекаем значения
    var match = Regex.Match(input, pattern);
    if (!match.Success)
        return new Dictionary<string, string>();

    var result = new Dictionary<string, string>();
    for (int i = 0; i < variableNames.Count; i++)
    {
        result[variableNames[i]] = match.Groups[i + 1].Value;
    }
    return result;
}
```

---

### Range

#### Назначение
Преобразует нотацию диапазона в массив строк.

#### Пример
```csharp
"1,2,5".Range();      // Вернёт: ["1", "2", "5"]
"1-5".Range();        // Вернёт: ["1", "2", "3", "4", "5"]
"10".Range();         // Вернёт: ["1", "2", ..., "10"]
```

#### Детали реализации
```csharp
public static string[] Range(this string accRange)
{
    // accRange: Диапазон в формате "1,2,5" или "1-5" или "10"
    // Исключения: Exception, если входные данные null или пусты
    // Возвращает: Массив строковых чисел

    if (string.IsNullOrEmpty(accRange))
        throw new Exception("range cannot be empty");

    // Список через запятую
    if (accRange.Contains(","))
        return accRange.Split(',');

    // Нотация диапазона (например, "1-5")
    else if (accRange.Contains("-"))
    {
        var rangeParts = accRange.Split('-').Select(int.Parse).ToArray();
        int rangeS = rangeParts[0];
        int rangeE = rangeParts[1];
        accRange = string.Join(",", Enumerable.Range(rangeS, rangeE - rangeS + 1));
        return accRange.Split(',');
    }

    // Одно число (например, "10" означает "1-10")
    else
    {
        int rangeS = 1;
        int rangeE = int.Parse(accRange);
        accRange = string.Join(",", Enumerable.Range(rangeS, rangeE - rangeS + 1));
        return accRange.Split(',');
    }
}
```

---

### GetLink

#### Назначение
Извлекает HTTP/HTTPS URL из текста.

#### Пример
```csharp
string message = "Visit https://example.com for more info";
string url = message.GetLink();
// Вернёт: "https://example.com"
```

#### Детали реализации
```csharp
public static string GetLink(this string text)
{
    // text: Текст, содержащий URL
    // Исключения: Exception, если валидный URL не найден
    // Возвращает: Извлечённый URL

    int startIndex = text.IndexOf("https://");
    if (startIndex == -1) startIndex = text.IndexOf("http://");
    if (startIndex == -1)
        throw new Exception($"No Link found in message {text}");

    string potentialLink = text.Substring(startIndex);

    // Находим конец URL (пробел, перевод строки, кавычка и т.д.)
    int endIndex = potentialLink.IndexOfAny(new[] { ' ', '\n', '\r', '\t', '"' });
    if (endIndex != -1)
        potentialLink = potentialLink.Substring(0, endIndex);

    return Uri.TryCreate(potentialLink, UriKind.Absolute, out _)
        ? potentialLink
        : throw new Exception($"No Link found in message {text}");
}
```

---

### GetOTP

#### Назначение
Извлекает 6-значный OTP-код из текста.

#### Пример
```csharp
string message = "Your verification code is 123456";
string otp = message.GetOTP();
// Вернёт: "123456"
```

#### Детали реализации
```csharp
public static string GetOTP(this string text)
{
    // text: Текст, содержащий 6-значный OTP
    // Исключения: Exception, если OTP не найден
    // Возвращает: 6-значный OTP-код

    Match match = Regex.Match(text, @"\b\d{6}\b");
    if (match.Success)
        return match.Value;
    else
        throw new Exception($"Fmail: OTP not found in [{text}]");
}
```

---

### CleanFilePath

#### Назначение
Удаляет недопустимые символы из строки для безопасного использования в качестве имени файла.

#### Пример
```csharp
string dirty = "file<name>:test?.txt";
string clean = dirty.CleanFilePath();
// Вернёт: "filenametest.txt"
```

#### Детали реализации
```csharp
public static string CleanFilePath(this string text)
{
    // text: Строка, потенциально содержащая недопустимые символы имени файла
    // Возвращает: Строку с удалёнными недопустимыми символами

    if (string.IsNullOrEmpty(text))
        return text;

    char[] invalidChars = Path.GetInvalidFileNameChars();

    string cleaned = text;
    foreach (char c in invalidChars)
    {
        cleaned = cleaned.Replace(c.ToString(), "");
    }
    return cleaned;
}
```

---

### GetFileNameFromUrl

#### Назначение
Извлекает имя файла из URL или HTML-атрибута.

#### Пример
```csharp
string url = "https://example.com/path/image.jpg?param=value";
string name = StringExtensions.GetFileNameFromUrl(url);
// Вернёт: "image"

string nameWithExt = StringExtensions.GetFileNameFromUrl(url, withExtension: true);
// Вернёт: "image.jpg"
```

#### Детали реализации
```csharp
public static string GetFileNameFromUrl(string input, bool withExtension = false)
{
    // input: URL или строка HTML-атрибута
    // withExtension: Включить расширение файла в результат (по умолчанию: false)
    // Возвращает: Имя файла, извлечённое из URL

    try
    {
        // Пытаемся найти URL в строке
        var urlMatch = Regex.Match(input, @"(?:src|href)=[""']?([^""'\s>]+)", RegexOptions.IgnoreCase);
        var url = urlMatch.Success ? urlMatch.Groups[1].Value : input;

        // Извлекаем имя файла (последний сегмент)
        var fileMatch = Regex.Match(url, @"([^/\\?#]+)(?:\?[^/]*)?$");
        if (fileMatch.Success)
        {
            var fileName = fileMatch.Groups[1].Value;

            if (withExtension)
                return fileName;

            // Удаляем расширение
            return Regex.Replace(fileName, @"\.[^.]+$", "");
        }

        return input;
    }
    catch
    {
        return input;
    }
}
```

---

### ConvertUrl

#### Назначение
Парсит параметры URL-запроса в форматированную строку или извлекает параметры Ethereum-сети.

#### Пример
```csharp
string url = "https://example.com?name=John&age=30";
string result = url.ConvertUrl();
// Вернёт:
// name: John
// age: 30

string oneline = url.ConvertUrl(oneline: true);
// Вернёт: "name: John | age: 30 | "
```

#### Детали реализации
```csharp
public static string ConvertUrl(this string url, bool oneline = false)
{
    // url: URL с параметрами запроса
    // oneline: Форматировать вывод одной строкой (по умолчанию: false)
    // Возвращает: Форматированную строку параметров или сообщение об ошибке

    if (string.IsNullOrEmpty(url))
        return "Error: URL is empty or null";

    string queryString = url.Contains("?") ? url.Substring(url.IndexOf('?') + 1) : string.Empty;
    if (string.IsNullOrEmpty(queryString))
        return "Error: No query parameters found in URL";

    // Обработка фрагментов хеша
    if (queryString.Contains("#"))
    {
        int hashIndex = queryString.IndexOf('#');
        int nextQueryIndex = queryString.IndexOf('?', hashIndex);
        if (nextQueryIndex != -1)
            queryString = queryString.Substring(nextQueryIndex + 1);
        else
            queryString = queryString.Substring(0, hashIndex);
    }

    // Парсим параметры
    var parameters = new NameValueCollection();
    string[] queryParts = queryString.Split('&');
    foreach (string part in queryParts)
    {
        if (string.IsNullOrEmpty(part)) continue;
        string[] keyValue = part.Split(new[] { '=' }, 2);
        if (keyValue.Length == 2)
        {
            string key = Uri.UnescapeDataString(keyValue[0]);
            string value = Uri.UnescapeDataString(keyValue[1]);
            parameters.Add(key, value);
        }
    }

    // Специальная обработка параметров Ethereum-сети
    string chainParam = parameters["addEthereumChainParameter"];
    if (!string.IsNullOrEmpty(chainParam))
    {
        try
        {
            var json = JObject.Parse(chainParam);
            string jsonResult = JsonConvert.SerializeObject(json, oneline ? Formatting.None : Formatting.Indented);
            return oneline ? jsonResult.Replace('\n', ' ').Replace('\r', ' ') : jsonResult;
        }
        catch (JsonException) { }
    }

    // Форматируем вывод
    StringBuilder result = new StringBuilder();
    foreach (string key in parameters.AllKeys)
    {
        if (oneline)
            result.Append($"{key}: {parameters[key]} | ");
        else
            result.AppendLine($"{key}: {parameters[key]}");
    }

    string finalResult = result.ToString();
    finalResult = finalResult.Length > 0 ? finalResult : "Error: No valid parameters found";
    return oneline ? finalResult.Replace('\n', ' ').Replace('\r', ' ') : finalResult;
}
```

---

### NewPassword

#### Назначение
Генерирует случайный безопасный пароль со смешанными типами символов.

#### Пример
```csharp
string password = StringExtensions.NewPassword(12);
// Вернёт: например, "aB3$xY9@pLmK"
```

#### Детали реализации
```csharp
public static string NewPassword(int length)
{
    // length: Длина пароля (минимум 8 символов)
    // Исключения: ArgumentException, если length < 8
    // Возвращает: Случайный пароль с символами нижнего/верхнего регистра, цифрами и спецсимволами

    if (length < 8)
        throw new ArgumentException("Length must be at least 8 characters.");

    string lowercase = "abcdefghijklmnopqrstuvwxyz";
    string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    string numbers = "0123456789";
    string special = "!@#$%^&*()";
    string allChars = lowercase + uppercase + numbers + special;
    Random random = new Random();
    StringBuilder password = new StringBuilder();

    // Обеспечиваем хотя бы один символ из каждого набора
    password.Append(lowercase[random.Next(lowercase.Length)]);
    password.Append(uppercase[random.Next(uppercase.Length)]);
    password.Append(numbers[random.Next(numbers.Length)]);
    password.Append(special[random.Next(special.Length)]);

    // Заполняем оставшуюся длину
    for (int i = 4; i < length; i++)
    {
        password.Append(allChars[random.Next(allChars.Length)]);
    }

    // Перемешиваем пароль
    for (int i = 0; i < password.Length; i++)
    {
        int randomIndex = random.Next(password.Length);
        char temp = password[i];
        password[i] = password[randomIndex];
        password[randomIndex] = temp;
    }

    return password.ToString();
}
```

---

## ProjectExtensions.ToJson

### Назначение
Загружает JSON-строку в объект JSON проекта ZennoPoster с резервным вариантом для индексированного формата.

### Пример
```csharp
// Простой JSON
project.ToJson("{\"status\":\"ok\"}");

// Индексированный JSON (несколько пронумерованных объектов)
string multiJson = "1:{\"user\":\"john\"}\n2:{\"user\":\"jane\"}";
project.ToJson(multiJson, objIndex: 2);  // Загружает второй объект
```

### Детали реализации
```csharp
public static void ToJson(
    this IZennoPosterProjectModel project,
    string json,
    bool thrw = false,
    int objIndex = 1)
{
    // project: Экземпляр проекта ZennoPoster
    // json: JSON-строка или индексированный JSON формат
    // thrw: Выбросить исключение при неудаче (по умолчанию: false)
    // objIndex: Индекс объекта для загрузки из индексированного формата (по умолчанию: 1)
    // Возвращает: void

    try
    {
        // Пытаемся напрямую распарсить JSON
        project.Json.FromString(json);
        return;
    }
    catch (Exception ex)
    {
        project.SendWarningToLog(ex.Message);
    }

    try
    {
        // Пытаемся использовать индексированный формат (например, "1:{json}\n2:{json}")
        string[] lines = json.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        string jsonData = "";

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith($"{objIndex}:"))
            {
                jsonData = lines[i].Substring(2);
                break;
            }
        }

        if (jsonData == "")
            throw new Exception($"Не найдены данные с индексом {objIndex}");

        project.Json.FromString(jsonData);
        return;
    }
    catch (Exception ex)
    {
        project.SendWarningToLog(ex.Message);
        if (thrw) throw;
    }
}
```
