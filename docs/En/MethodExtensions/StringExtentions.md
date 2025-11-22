# StringExtensions

Extension methods for string manipulation, cryptography, parsing, and data conversion.

---

## CRYPTO Methods

### NormalizeAddress

#### Purpose
Ensures an Ethereum address starts with "0x" prefix.

#### Example
```csharp
string address = "1234567890abcdef";
string normalized = address.NormalizeAddress();
// Returns: "0x1234567890abcdef"
```

#### Breakdown
```csharp
public static string NormalizeAddress(this string address)
{
    // address: The Ethereum address to normalize
    // Returns: Address with "0x" prefix, or original if null/empty

    if (string.IsNullOrEmpty(address))
        return address;

    if (!address.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        return "0x" + address;

    return address;
}
```

---

### GetTxHash

#### Purpose
Extracts transaction hash from a blockchain explorer URL.

#### Example
```csharp
string url = "https://etherscan.io/tx/0xabc123...";
string hash = url.GetTxHash();
// Returns: "0xabc123..."
```

#### Breakdown
```csharp
public static string GetTxHash(this string link)
{
    // link: URL containing transaction hash
    // Throws: Exception if link is null or empty
    // Returns: Transaction hash extracted from URL

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

#### Purpose
Converts a private key or seed phrase to an EVM (Ethereum) address.

#### Example
```csharp
string privateKey = "0x1234...";
string address = privateKey.ToEvmAddress();

string seed = "word1 word2 word3 ... word12";
string addressFromSeed = seed.ToEvmAddress();
```

#### Breakdown
```csharp
public static string ToEvmAddress(this string key)
{
    // key: Private key (64 hex chars) or seed phrase (12/24 words)
    // Returns: EVM address derived from the key

    string keyType = key.DetectKeyType();
    var blockchain = new Blockchain();

    // If seed phrase, derive private key first
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

#### Purpose
Derives a secp256k1 private key from a seed phrase using a specific derivation path.

#### Example
```csharp
string seed = "word1 word2 ... word12";
string privateKey = seed.ToSepc256k1();       // Path: m/44'/60'/0'/0/0
string privateKey2 = seed.ToSepc256k1(path: 1); // Path: m/44'/60'/0'/0/1
```

#### Breakdown
```csharp
public static string ToSepc256k1(this string seed, int path = 0)
{
    // seed: BIP39 seed phrase
    // path: Derivation path index (default: 0)
    // Returns: Private key in hexadecimal format

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

#### Purpose
Generates a deterministic private key from any string using SHA256 hash.

#### Example
```csharp
string password = "mySecretPassword123";
string privateKey = password.ToEvmPrivateKey();
// Returns: SHA256 hash of the input as hex string
```

#### Breakdown
```csharp
public static string ToEvmPrivateKey(this string input)
{
    // input: Any string to convert
    // Throws: ArgumentException if input is null or empty
    // Returns: SHA256 hash of input as hexadecimal string

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

#### Purpose
Parses transaction JSON and extracts key fields as string array.

#### Example
```csharp
string txJson = "{\"gas\":\"0x5208\",\"value\":\"0x0\",\"from\":\"0x...\",\"to\":\"0x...\",\"data\":\"0x\"}";
string[] txData = txJson.TxToString();
// Returns: [gas, value, sender, data, recipient, gwei]
```

#### Breakdown
```csharp
public static string[] TxToString(this string txJson)
{
    // txJson: Transaction data in JSON format
    // Returns: Array [gas, value, sender, data, recipient, gwei]

    dynamic txData = JsonConvert.DeserializeObject<System.Dynamic.ExpandoObject>(txJson);

    string gas = $"{txData.gas}";
    string value = $"{txData.value}";
    string sender = $"{txData.from}";
    string recipient = $"{txData.to}";
    string data = $"{txData.data}";

    // Convert gas from hex to Gwei
    BigInteger gasWei = BigInteger.Parse("0" + gas.TrimStart('0', 'x'), NumberStyles.AllowHexSpecifier);
    decimal gasGwei = (decimal)gasWei / 1000000000m;
    string gwei = gasGwei.ToString().Replace(',', '.');

    return new string[] { gas, value, sender, data, recipient, gwei };
}
```

---

### ChkAddress

#### Purpose
Checks if a shortened address (e.g., "0x1234…5678") matches a full address.

#### Example
```csharp
string shortAddr = "0x1234…7890";
string fullAddr = "0x123456789abcdef67890";
bool matches = shortAddr.ChkAddress(fullAddr);
// Returns: true if prefix and suffix match
```

#### Breakdown
```csharp
public static bool ChkAddress(this string shortAddress, string fullAddress)
{
    // shortAddress: Shortened address with "…" separator
    // fullAddress: Full address to check against
    // Returns: true if prefix and suffix match, false otherwise

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

#### Purpose
Converts a decimal number string to hexadecimal format with optional unit conversion (Gwei, ETH).

#### Example
```csharp
"100".StringToHex();           // Returns: "0x64"
"5".StringToHex("gwei");       // Returns: "0x12A05F200" (5 Gwei in Wei)
"1".StringToHex("eth");        // Returns: "0xDE0B6B3A7640000" (1 ETH in Wei)
```

#### Breakdown
```csharp
public static string StringToHex(this string value, string convert = "")
{
    // value: Decimal number as string
    // convert: Optional conversion unit ("gwei", "eth", or empty)
    // Returns: Hexadecimal representation with "0x" prefix

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

#### Purpose
Converts hexadecimal value to decimal string with optional unit conversion.

#### Example
```csharp
"0x64".HexToString();          // Returns: "100"
"0x12A05F200".HexToString("gwei");  // Returns: "5"
"0xDE0B6B3A7640000".HexToString("eth");  // Returns: "1"
```

#### Breakdown
```csharp
public static string HexToString(this string hexValue, string convert = "")
{
    // hexValue: Hexadecimal value (with or without "0x")
    // convert: Optional conversion unit ("gwei", "eth", or empty)
    // Returns: Decimal value as string

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

#### Purpose
Detects the type of cryptocurrency private key or seed phrase.

#### Example
```csharp
"word1 word2 ... word12".KeyType();  // Returns: "seed"
"0x1234...64chars".KeyType();        // Returns: "keyEvm"
"base58string87chars".KeyType();     // Returns: "keySol"
"suiprivkey1...".KeyType();          // Returns: "keySui"
```

#### Breakdown
```csharp
public static string KeyType(this string input)
{
    // input: Key or seed phrase to analyze
    // Throws: Exception if input is null/empty
    // Returns: "seed", "keyEvm", "keySol", "keySui", or "undefined"

    if (string.IsNullOrWhiteSpace(input))
        throw new Exception($"input isNullOrEmpty");

    input = input.Trim();

    // Check for Bech32 (Sui private key)
    if (input.StartsWith("suiprivkey1"))
        return "keySui";

    string cleanInput = input.StartsWith("0x") ? input.Substring(2) : input;

    // EVM private key (64 hex characters)
    if (Regex.IsMatch(cleanInput, @"^[0-9a-fA-F]{64}$"))
        return "keyEvm";

    // Solana private key (Base58, 87-88 characters)
    if (Regex.IsMatch(input, @"^[123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz]{87,88}$"))
        return "keySol";

    // Mnemonic (12 or 24 words)
    var words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    if (words.Length == 12 || words.Length == 24)
        return "seed";

    return "undefined";
}
```

---

## SVG Methods

### SaveSvgStringToImage

#### Purpose
Converts SVG string to PNG image and saves it to a file.

#### Example
```csharp
string svgContent = "<svg>...</svg>";
svgContent.SaveSvgStringToImage("C:\\path\\to\\output.png");
```

#### Breakdown
```csharp
public static void SaveSvgStringToImage(this string svgContent, string pathToScreen)
{
    // svgContent: SVG content as string
    // pathToScreen: File path where PNG image will be saved
    // Returns: void

    var svgDocument = SvgDocument.FromSvg<SvgDocument>(svgContent);
    using (var bitmap = svgDocument.Draw())
    {
        bitmap.Save(pathToScreen);
    }
}
```

---

### SvgToBase64

#### Purpose
Converts SVG string to Base64-encoded PNG image.

#### Example
```csharp
string svgContent = "<svg>...</svg>";
string base64Image = svgContent.SvgToBase64();
// Returns: Base64 string of PNG image
```

#### Breakdown
```csharp
public static string SvgToBase64(this string svgContent)
{
    // svgContent: SVG content as string
    // Returns: Base64-encoded PNG image

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

## Text Processing Methods

### Regx

#### Purpose
Extracts text using regex pattern via ZennoLab macro.

#### Example
```csharp
string text = "Order #12345 confirmed";
string orderNumber = text.Regx(@"Order #(\d+)");
// Returns: "12345"
```

#### Breakdown
```csharp
public static string Regx(this string input, string pattern)
{
    // input: Source text
    // pattern: Regex pattern
    // Returns: First matched group or null

    var result = ZennoLab.Macros.TextProcessing.Regex(input, pattern, "0")[0].FirstOrDefault();
    return result;
}
```

---

### EscapeMarkdown

#### Purpose
Escapes special Markdown characters in text.

#### Example
```csharp
string text = "Price: $100 (50% off!)";
string escaped = text.EscapeMarkdown();
// Returns: "Price: \\$100 \\(50\\% off\\!\\)"
```

#### Breakdown
```csharp
public static string EscapeMarkdown(this string text)
{
    // text: Text containing Markdown special characters
    // Returns: Text with escaped Markdown characters

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

#### Purpose
Flattens nested JSON into a dictionary with underscore-separated keys.

#### Example
```csharp
string json = "{\"user\":{\"name\":\"John\",\"age\":30}}";
var dict = json.JsonToDic();
// Returns: {"user_name": "John", "user_age": "30"}
```

#### Breakdown
```csharp
public static Dictionary<string, string> JsonToDic(this string json)
{
    // json: JSON string to flatten
    // Returns: Dictionary with flattened keys using underscore notation

    var result = new Dictionary<string, string>();
    var jObject = JObject.Parse(json);
    FlattenJson(jObject, "", result);
    return result;

    // Recursive function to flatten nested objects and arrays
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

#### Purpose
Parses delimited credential string into a dictionary based on format template.

#### Example
```csharp
string data = "john@mail.com:password123:extra";
string format = "{email}:{password}:{note}";
var parsed = data.ParseCreds(format);
// Returns: {"email": "john@mail.com", "password": "password123", "note": "extra"}
```

#### Breakdown
```csharp
public static Dictionary<string, string> ParseCreds(this string data, string format, char devider = ':')
{
    // data: Delimited credential string
    // format: Template with placeholders in braces, e.g., "{email}:{password}"
    // devider: Delimiter character (default: ':')
    // Returns: Dictionary mapping placeholder names to values

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

#### Purpose
Parses string using a template mask with placeholders.

#### Example
```csharp
string input = "User: John, Age: 30";
string mask = "User: {name}, Age: {age}";
var result = input.ParseByMask(mask);
// Returns: {"name": "John", "age": "30"}
```

#### Breakdown
```csharp
public static Dictionary<string, string> ParseByMask(this string input, string mask)
{
    // input: String to parse
    // mask: Template with {placeholder} markers
    // Returns: Dictionary with extracted values, empty if no match

    input = input?.Trim();
    mask = mask?.Trim();

    if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(mask))
        return new Dictionary<string, string>();

    // Extract variable names from mask
    var variableNames = new List<string>();
    var regex = new Regex(@"\{([^{}]+)\}");
    foreach (Match mtch in regex.Matches(mask))
    {
        variableNames.Add(mtch.Groups[1].Value);
    }

    if (variableNames.Count == 0)
        return new Dictionary<string, string>();

    // Convert mask to regex pattern
    string pattern = Regex.Escape(mask);
    foreach (var varName in variableNames)
    {
        string escapedVar = Regex.Escape("{" + varName + "}");
        pattern = pattern.Replace(escapedVar, "(.*?)");
    }
    pattern += "$";

    // Match and extract values
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

#### Purpose
Converts range notation into array of strings.

#### Example
```csharp
"1,2,5".Range();      // Returns: ["1", "2", "5"]
"1-5".Range();        // Returns: ["1", "2", "3", "4", "5"]
"10".Range();         // Returns: ["1", "2", ..., "10"]
```

#### Breakdown
```csharp
public static string[] Range(this string accRange)
{
    // accRange: Range in format "1,2,5" or "1-5" or "10"
    // Throws: Exception if input is null or empty
    // Returns: Array of string numbers

    if (string.IsNullOrEmpty(accRange))
        throw new Exception("range cannot be empty");

    // Comma-separated list
    if (accRange.Contains(","))
        return accRange.Split(',');

    // Range notation (e.g., "1-5")
    else if (accRange.Contains("-"))
    {
        var rangeParts = accRange.Split('-').Select(int.Parse).ToArray();
        int rangeS = rangeParts[0];
        int rangeE = rangeParts[1];
        accRange = string.Join(",", Enumerable.Range(rangeS, rangeE - rangeS + 1));
        return accRange.Split(',');
    }

    // Single number (e.g., "10" means "1-10")
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

#### Purpose
Extracts HTTP/HTTPS URL from text.

#### Example
```csharp
string message = "Visit https://example.com for more info";
string url = message.GetLink();
// Returns: "https://example.com"
```

#### Breakdown
```csharp
public static string GetLink(this string text)
{
    // text: Text containing URL
    // Throws: Exception if no valid URL found
    // Returns: Extracted URL

    int startIndex = text.IndexOf("https://");
    if (startIndex == -1) startIndex = text.IndexOf("http://");
    if (startIndex == -1)
        throw new Exception($"No Link found in message {text}");

    string potentialLink = text.Substring(startIndex);

    // Find end of URL (space, newline, quote, etc.)
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

#### Purpose
Extracts 6-digit OTP code from text.

#### Example
```csharp
string message = "Your verification code is 123456";
string otp = message.GetOTP();
// Returns: "123456"
```

#### Breakdown
```csharp
public static string GetOTP(this string text)
{
    // text: Text containing 6-digit OTP
    // Throws: Exception if no OTP found
    // Returns: 6-digit OTP code

    Match match = Regex.Match(text, @"\b\d{6}\b");
    if (match.Success)
        return match.Value;
    else
        throw new Exception($"Fmail: OTP not found in [{text}]");
}
```

---

### CleanFilePath

#### Purpose
Removes invalid characters from string to make it safe for use as filename.

#### Example
```csharp
string dirty = "file<name>:test?.txt";
string clean = dirty.CleanFilePath();
// Returns: "filenametest.txt"
```

#### Breakdown
```csharp
public static string CleanFilePath(this string text)
{
    // text: String potentially containing invalid filename characters
    // Returns: String with invalid characters removed

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

#### Purpose
Extracts filename from URL or HTML attribute.

#### Example
```csharp
string url = "https://example.com/path/image.jpg?param=value";
string name = StringExtensions.GetFileNameFromUrl(url);
// Returns: "image"

string nameWithExt = StringExtensions.GetFileNameFromUrl(url, withExtension: true);
// Returns: "image.jpg"
```

#### Breakdown
```csharp
public static string GetFileNameFromUrl(string input, bool withExtension = false)
{
    // input: URL or HTML attribute string
    // withExtension: Include file extension in result (default: false)
    // Returns: Filename extracted from URL

    try
    {
        // Try to find URL in string
        var urlMatch = Regex.Match(input, @"(?:src|href)=[""']?([^""'\s>]+)", RegexOptions.IgnoreCase);
        var url = urlMatch.Success ? urlMatch.Groups[1].Value : input;

        // Extract filename (last segment)
        var fileMatch = Regex.Match(url, @"([^/\\?#]+)(?:\?[^/]*)?$");
        if (fileMatch.Success)
        {
            var fileName = fileMatch.Groups[1].Value;

            if (withExtension)
                return fileName;

            // Remove extension
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

#### Purpose
Parses URL query parameters into formatted string or extracts Ethereum chain parameters.

#### Example
```csharp
string url = "https://example.com?name=John&age=30";
string result = url.ConvertUrl();
// Returns:
// name: John
// age: 30

string oneline = url.ConvertUrl(oneline: true);
// Returns: "name: John | age: 30 | "
```

#### Breakdown
```csharp
public static string ConvertUrl(this string url, bool oneline = false)
{
    // url: URL with query parameters
    // oneline: Format output as single line (default: false)
    // Returns: Formatted parameter string or error message

    if (string.IsNullOrEmpty(url))
        return "Error: URL is empty or null";

    string queryString = url.Contains("?") ? url.Substring(url.IndexOf('?') + 1) : string.Empty;
    if (string.IsNullOrEmpty(queryString))
        return "Error: No query parameters found in URL";

    // Handle hash fragments
    if (queryString.Contains("#"))
    {
        int hashIndex = queryString.IndexOf('#');
        int nextQueryIndex = queryString.IndexOf('?', hashIndex);
        if (nextQueryIndex != -1)
            queryString = queryString.Substring(nextQueryIndex + 1);
        else
            queryString = queryString.Substring(0, hashIndex);
    }

    // Parse parameters
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

    // Special handling for Ethereum chain parameters
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

    // Format output
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

#### Purpose
Generates a random secure password with mixed character types.

#### Example
```csharp
string password = StringExtensions.NewPassword(12);
// Returns: e.g., "aB3$xY9@pLmK"
```

#### Breakdown
```csharp
public static string NewPassword(int length)
{
    // length: Password length (minimum 8 characters)
    // Throws: ArgumentException if length < 8
    // Returns: Random password with lowercase, uppercase, numbers, and special characters

    if (length < 8)
        throw new ArgumentException("Length must be at least 8 characters.");

    string lowercase = "abcdefghijklmnopqrstuvwxyz";
    string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    string numbers = "0123456789";
    string special = "!@#$%^&*()";
    string allChars = lowercase + uppercase + numbers + special;
    Random random = new Random();
    StringBuilder password = new StringBuilder();

    // Ensure at least one character from each required set
    password.Append(lowercase[random.Next(lowercase.Length)]);
    password.Append(uppercase[random.Next(uppercase.Length)]);
    password.Append(numbers[random.Next(numbers.Length)]);
    password.Append(special[random.Next(special.Length)]);

    // Fill remaining length
    for (int i = 4; i < length; i++)
    {
        password.Append(allChars[random.Next(allChars.Length)]);
    }

    // Shuffle password
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

### Purpose
Loads JSON string into ZennoPoster project's JSON object, with fallback for indexed format.

### Example
```csharp
// Simple JSON
project.ToJson("{\"status\":\"ok\"}");

// Indexed JSON (multiple objects numbered)
string multiJson = "1:{\"user\":\"john\"}\n2:{\"user\":\"jane\"}";
project.ToJson(multiJson, objIndex: 2);  // Loads second object
```

### Breakdown
```csharp
public static void ToJson(
    this IZennoPosterProjectModel project,
    string json,
    bool thrw = false,
    int objIndex = 1)
{
    // project: ZennoPoster project instance
    // json: JSON string or indexed JSON format
    // thrw: Throw exception on failure (default: false)
    // objIndex: Index of object to load from indexed format (default: 1)
    // Returns: void

    try
    {
        // Try direct JSON parsing
        project.Json.FromString(json);
        return;
    }
    catch (Exception ex)
    {
        project.SendWarningToLog(ex.Message);
    }

    try
    {
        // Try indexed format (e.g., "1:{json}\n2:{json}")
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
