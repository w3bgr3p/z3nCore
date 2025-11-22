using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Global.ZennoLab.Json;
using Newtonsoft.Json.Linq;
using ZennoLab.InterfacesLibrary.ProjectModel;
using ZennoLab.CommandCenter;
using System.Text;
using System.Globalization;
using System.Buffers;

namespace z3nCore
{
    


    public class Cookies
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;

        public Cookies(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _logger = new Logger(project, log: log, classEmoji: "⚡");
        }

        // ============================================
        // НОВЫЕ МЕТОДЫ ДЛЯ BASE64 КОДИРОВАНИЯ
        // ============================================
        
        private static readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;
        
        /// <summary>
        /// Кодирует JSON в Base64 для безопасного сохранения в БД
        /// </summary>
        private static string EncodeForDb(string cookiesJson)
        {
            if (string.IsNullOrEmpty(cookiesJson))
                return string.Empty;
            
            int maxByteCount = Encoding.UTF8.GetMaxByteCount(cookiesJson.Length);
            byte[] buffer = _bytePool.Rent(maxByteCount);
            
            try
            {
                int actualBytes = Encoding.UTF8.GetBytes(cookiesJson, 0, cookiesJson.Length, buffer, 0);
                return Convert.ToBase64String(buffer, 0, actualBytes);
            }
            finally
            {
                _bytePool.Return(buffer);
            }
        }
        
        /// <summary>
        /// Декодирует Base64 обратно в JSON при чтении из БД
        /// </summary>
        private static string DecodeFromDb(string base64Cookies)
        {
            if (string.IsNullOrEmpty(base64Cookies))
                return string.Empty;
            
            try
            {
                byte[] bytes = Convert.FromBase64String(base64Cookies);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                // Если это не Base64, возможно старый формат - возвращаем как есть
                return base64Cookies;
            }
        }

        // ============================================
        // МОДИФИЦИРОВАННЫЕ МЕТОДЫ
        // ============================================

        public void Save(string source = null, string jsonPath = null)
        {
            switch (source)
            {
                case "project":
                    SaveProjectFast();
                    return;
                    
                case "all":
                    SaveAllFast(jsonPath);
                    return;
                    
                default:
                    _logger.Send($"ERROR: Unsupported source '{source}'");
                    throw new Exception($"unsupported input {source}. Use [null|project|all]");
            }
        }

        public void SaveAllFast(string jsonPath = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            string tempFile = Path.Combine(Path.GetTempPath(), $"cookies_{Guid.NewGuid()}.txt");
            
            try
            {
                // 1. Сохраняем куки в файл
                _instance.SaveCookie(tempFile);
                var saveTime = sw.ElapsedMilliseconds;
                _logger.Send($"Native SaveCookie: {saveTime}ms");

                // Проверяем что файл создался
                if (!File.Exists(tempFile))
                {
                    _logger.Send($"ERROR: Cookie file was not created: {tempFile}");
                    throw new FileNotFoundException($"Cookie file was not created: {tempFile}");
                }

                // 2. Читаем файл
                sw.Restart();
                string cookieContent = File.ReadAllText(tempFile);
                var readTime = sw.ElapsedMilliseconds;
                _logger.Send($"File read: {readTime}ms, size: {cookieContent.Length / 1024.0:F2} KB");

                // 3. Конвертируем в JSON
                sw.Restart();
                string jsonCookies = ConvertToJson(cookieContent);
                var convertTime = sw.ElapsedMilliseconds;
                _logger.Send($"Conversion to JSON: {convertTime}ms, {jsonCookies.Length / 1024.0:F2} KB");

                // 4. Сохраняем в БД с Base64 кодированием
                sw.Restart();
                string encoded = EncodeForDb(jsonCookies);
                _project.DbUpd($"cookies = '{encoded}'", "_instance");
                var dbTime = sw.ElapsedMilliseconds;
                _logger.Send($"DB save (Base64 encoded): {dbTime}ms, encoded size: {encoded.Length / 1024.0:F2} KB");

                // 5. Сохраняем в файл если нужно (обычный JSON, не Base64)
                if (!string.IsNullOrEmpty(jsonPath))
                {
                    sw.Restart();
                    File.WriteAllText(jsonPath, jsonCookies);
                    _logger.Send($"JSON file saved to {jsonPath}: {sw.ElapsedMilliseconds}ms");
                }

                var totalTime = saveTime + readTime + convertTime + dbTime;
                _logger.Send($"✅ TOTAL TIME: {totalTime}ms ({totalTime / 1000.0:F1}s)");
            }
            catch (Exception ex)
            {
                _logger.Send($"ERROR in SaveAllFast: {ex.Message}");
                throw;
            }
            finally
            {
                // Удаляем временный файл
                if (File.Exists(tempFile))
                {
                    try
                    {
                        File.Delete(tempFile);
                        _logger.Send($"Temp file deleted: {tempFile}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Send($"WARNING: Could not delete temp file: {ex.Message}");
                    }
                }
            }
        }

        public void SaveProjectFast()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string currentDomain = _instance.ActiveTab.MainDomain;

            string tempFile = Path.Combine(Path.GetTempPath(), $"cookies_{Guid.NewGuid()}.txt");
            
            try
            {
                _instance.SaveCookie(tempFile);
                
                // Проверяем что файл создался
                if (!File.Exists(tempFile))
                {
                    _logger.Send($"ERROR: Cookie file was not created: {tempFile}");
                    throw new FileNotFoundException($"Cookie file was not created: {tempFile}");
                }

                string cookieContent = File.ReadAllText(tempFile);
                string jsonCookies = ConvertToJson(cookieContent, domainFilter: currentDomain);

                // Кодируем в Base64 перед сохранением в БД
                string encoded = EncodeForDb(jsonCookies);
                _project.DbUpd($"cookies = '{encoded}'");

                _logger.Send($"✅ Project cookies saved in {sw.ElapsedMilliseconds}ms (Base64 encoded)");
            }
            catch (Exception ex)
            {
                _logger.Send($"ERROR in SaveProjectFast: {ex.Message}");
                throw;
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    try
                    {
                        File.Delete(tempFile);
                        _logger.Send($"Temp file deleted: {tempFile}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Send($"WARNING: Could not delete temp file: {ex.Message}");
                    }
                }
            }
        }
        
        public static string CookieFix(string brokenJson)
        {
            string fixedJson = brokenJson.Replace("\"\"value\":\"", "\"value\":\"");
            fixedJson = fixedJson.Replace("\" =", "=");
            try
            {
                JArray cookies = JArray.Parse(fixedJson);
                foreach (JObject cookie in cookies)
                {
                    if (cookie["id"] != null)
                    {
                        cookie["id"] = 1;
                    }
                }
                return JsonConvert.SerializeObject(cookies, Formatting.Indented);
            }
            catch (JsonReaderException ex)
            {
                return $"Ошибка парсинга JSON: {ex.Message}";
            }
        }
        
        private string ConvertToJson(string content, string domainFilter = null)
        {
            var cookies = new List<object>();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split('\t');

                // Формат (по вашему примеру):
                // 0: domain (www.google.com)
                // 1: includeSubdomains (TRUE/FALSE) 
                // 2: path (/recaptcha)
                // 3: isSecure (TRUE/FALSE)
                // 4: expiry (04/17/2026 00:43:42 или пусто)
                // 5: name (_GRECAPTCHA)
                // 6: value (09AG7bz...)
                // 7: httpOnly (TRUE/FALSE)
                // 8: секундный httpOnly? (FALSE)
                // 9: sameSite (None/Lax/Strict)
                // 10: priority (High/Medium/Low)
                // 11: пусто
                // 12: port (443/80)
                // 13: схема (Secure/NonSecure)
                // 14: пусто
                // 15: еще что-то (FALSE)

                if (parts.Length < 7) continue;

                try
                {
                    var domain = parts[0];

                    // Фильтруем по домену если нужно
                    if (!string.IsNullOrEmpty(domainFilter) && !domain.Contains(domainFilter))
                        continue;

                    var includeSubdomains = parts[1].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                    var path = parts[2];
                    var secure = parts[3].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                    var expiryStr = parts[4];
                    var name = parts[5];
                    var value = parts.Length > 6 ? parts[6] : "";

                    // Дополнительные поля
                    var httpOnly = false;
                    var sameSite = "Unspecified";

                    if (parts.Length > 7)
                        httpOnly = parts[7].Equals("TRUE", StringComparison.OrdinalIgnoreCase);

                    if (parts.Length > 9)
                        sameSite = parts[9];

                    // Конвертируем дату
                    double? expirationDate = null;
                    bool isSession = string.IsNullOrEmpty(expiryStr);

                    if (!isSession)
                    {
                        // Формат: MM/dd/yyyy HH:mm:ss
                        if (DateTime.TryParseExact(expiryStr, "MM/dd/yyyy HH:mm:ss",
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry))
                        {
                            expirationDate = new DateTimeOffset(expiry).ToUnixTimeSeconds();
                        }
                    }

                    cookies.Add(new
                    {
                        domain = domain,
                        expirationDate = expirationDate,
                        hostOnly = !includeSubdomains,
                        httpOnly = httpOnly,
                        name = name,
                        path = path,
                        sameSite = sameSite,
                        secure = secure,
                        session = isSession,
                        storeId = (string)null,
                        value = value,
                        id = (domain + name + path).GetHashCode()
                    });
                }
                catch (Exception ex)
                {
                    _logger.Send($"Failed to parse cookie: {ex.Message}");
                }
            }

            _logger.Send($"Converted {cookies.Count} cookies");
            return JsonConvert.SerializeObject(cookies, Formatting.None);
        }

        public void Set(string cookieSource = null, string jsonPath = null)
        {
            if (string.IsNullOrEmpty(jsonPath))
                cookieSource = "fromFile";

            if (cookieSource == null)
                cookieSource = "dbMain";

            switch (cookieSource)
            {
                case "dbMain":
                    cookieSource = _project.SqlGet("cookies", "_instance");
                    if (!string.IsNullOrEmpty(cookieSource))
                    {
                        // Декодируем из Base64
                        cookieSource = DecodeFromDb(cookieSource);
                    }
                    break;
                case "dbProject":
                    cookieSource = _project.SqlGet("cookies");
                    if (!string.IsNullOrEmpty(cookieSource))
                    {
                        // Декодируем из Base64
                        cookieSource = DecodeFromDb(cookieSource);
                    }
                    break;
                case "fromFile":
                    if (string.IsNullOrEmpty(jsonPath))
                        jsonPath = _project.PathCookies();
                    cookieSource = File.ReadAllText(jsonPath);
                    break;
            }

            _instance.SetCookie(cookieSource);
        }
        
        public string Get(string domainFilter = "")
        {
            _logger.Send($"Get() called with filter: '{domainFilter}'");
            
            if (domainFilter == ".") 
            {
                domainFilter = _instance.ActiveTab.MainDomain;
                _logger.Send($"Filter '.' resolved to main domain: '{domainFilter}'");
            }
            
            var cookieContainer = _project.Profile.CookieContainer;
            _logger.Send($"Cookie container accessed, total domains: {cookieContainer.Domains.Count()}");
            
            var cookieList = new List<object>();
            int domainCount = 0;
            int totalCookies = 0;
            int filteredDomains = 0;

            foreach (var domain in cookieContainer.Domains)
            {
                domainCount++;
                
                if (string.IsNullOrEmpty(domainFilter) || domain.Contains(domainFilter))
                {
                    filteredDomains++;
                    _logger.Send($"Processing domain #{filteredDomains}: '{domain}'");
                    
                    var cookies = cookieContainer.Get(domain);
                    int cookiesInDomain = cookies.Count();
                    totalCookies += cookiesInDomain;
                    
                    _logger.Send($"Domain '{domain}' has {cookiesInDomain} cookies");
                    
                    cookieList.AddRange(cookies.Select(cookie => new
                    {
                        domain = cookie.Host,
                        expirationDate = cookie.Expiry == DateTime.MinValue ? (double?)null : new DateTimeOffset(cookie.Expiry).ToUnixTimeSeconds(),
                        hostOnly = !cookie.IsDomain,
                        httpOnly = cookie.IsHttpOnly,
                        name = cookie.Name,
                        path = cookie.Path,
                        sameSite = cookie.SameSite.ToString(),
                        secure = cookie.IsSecure,
                        session = cookie.IsSession,
                        storeId = (string)null,
                        value = cookie.Value,
                        id = cookie.GetHashCode()
                    }));
                    
                    _logger.Send($"Added {cookiesInDomain} cookies to list (total now: {cookieList.Count})");
                }
            }
            
            _logger.Send($"Filtering complete: {filteredDomains} domains matched out of {domainCount} total, {totalCookies} cookies collected");
            _logger.Send($"Starting JSON serialization of {cookieList.Count} cookie objects...");
            
            string cookiesJson = Global.ZennoLab.Json.JsonConvert.SerializeObject(cookieList, formatting: Formatting.None);
            
            _logger.Send($"Serialization complete: {cookiesJson.Length} characters, {cookiesJson.Length / 1024.0:F2} KB");
            
            return cookiesJson;
        }
        
        public string GetByJs(string domainFilter = "", bool log = false)
        {
            string jsCode = @"
    var cookies = document.cookie.split('; ').map(function(cookie) {
	    var parts = cookie.split('=');
	    var name = parts[0];
	    var value = parts.slice(1).join('=');
	    return {
		    'domain': window.location.hostname,
		    'name': name,
		    'value': value,
		    'path': '/', 
		    'expirationDate': null, 
		    'hostOnly': true,
		    'httpOnly': false,
		    'secure': window.location.protocol === 'https:',
		    'session': false,
		    'sameSite': 'Unspecified',
		    'storeId': null,
		    'id': 1
	    };
    });
    return JSON.stringify(cookies);
    ";

            string jsonResult = _instance.ActiveTab.MainDocument.EvaluateScript(jsCode).ToString();
            if (log) _project.log(jsonResult);
            var escapedJson = jsonResult.Replace("\r\n", "").Replace("\n", "").Replace("\r", "").Replace(" ", "").Trim();
            _project.Json.FromString(jsonResult);
            return escapedJson;
        }

        public void SetByJs(string cookiesJson, bool log = false)
        {
            try
            {
                JArray cookies = JArray.Parse(cookiesJson);

                var uniqueCookies = cookies
                    .GroupBy(c => new { Domain = c["domain"].ToString(), Name = c["name"].ToString() })
                    .Select(g => g.Last())
                    .ToList();

                string currentDomain = _instance.ActiveTab.Domain;
                string[] domainParts = currentDomain.Split('.');
                string parentDomain = "." + string.Join(".", domainParts.Skip(domainParts.Length - 2));

                string jsCode = "";
                int cookieCount = 0;
                foreach (JObject cookie in uniqueCookies)
                {
                    string domain = cookie["domain"].ToString();
                    string name = cookie["name"].ToString();
                    string value = cookie["value"].ToString();

                    if (domain == currentDomain || domain == "." + currentDomain)
                    {
                        string path = cookie["path"]?.ToString() ?? "/";
                        string expires;

                        if (cookie["expirationDate"] != null && cookie["expirationDate"].Type != JTokenType.Null)
                        {
                            double expValue = double.Parse(cookie["expirationDate"].ToString());
                            if (expValue < DateTimeOffset.UtcNow.ToUnixTimeSeconds()) 
                                expires = DateTimeOffset.UtcNow.AddYears(1).ToString("R");
                            else 
                                expires = DateTimeOffset.FromUnixTimeSeconds((long)expValue).ToString("R");
                        }
                        else
                            expires = DateTimeOffset.UtcNow.AddYears(1).ToString("R");

                        jsCode += $"document.cookie = '{name}={value}; domain={parentDomain}; path={path}; expires={expires}'; Secure';\n";
                        cookieCount++;
                    }
                }
                
                _logger.Send($"Found cookies for {currentDomain}: [{cookieCount}] runingJs...\n + {jsCode}");

                if (!string.IsNullOrEmpty(jsCode))
                {
                    _instance.ActiveTab.MainDocument.EvaluateScript(jsCode);
                }
                else _logger.Send($"!W No cookies Found for {currentDomain}");
            }
            catch (Exception ex)
            {
                _logger.Send($"!W cant't parse JSON: [{cookiesJson}]  {ex.Message}");
            }
        }
    }

    
    
    public static partial class ProjectExtensions
    {
        public static string DbCookies(this IZennoPosterProjectModel project)
        {
            var rawCookies = project.SqlGet("cookies", "_instance");
        
            // Декодируем из Base64 если нужно
            if (!string.IsNullOrEmpty(rawCookies))
            {
                try
                {
                    byte[] bytes = Convert.FromBase64String(rawCookies);
                    rawCookies = Encoding.UTF8.GetString(bytes);
                }
                catch (FormatException)
                {
                    // Если это не Base64, возможно старый формат - используем как есть
                }
            }
        
            return Cookies.CookieFix(rawCookies);
        }
    }


}
