using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Buffers;
using Global.ZennoLab.Json;
using Newtonsoft.Json.Linq;
using ZennoLab.InterfacesLibrary.ProjectModel;
using ZennoLab.CommandCenter;

namespace z3nCore
{
    public class Cookies
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;

        private static readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;

        public Cookies(IZennoPosterProjectModel project, Instance instance = null, bool log = false)
        {
            _project = project;
            _instance = instance;
            _logger = new Logger(project, log: log, classEmoji: "🍪");
        }

        private void RequireInstance(string methodName)
        {
            if (_instance == null)
                throw new InvalidOperationException($"{methodName} requires Instance but it was not provided to Cookies constructor");
        }

        public void SaveAll(string jsonPath = null, string table = "_instance")
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            byte[] exportedBytes = _project.Profile.CookieContainer.Export();
            string cookieContent = Encoding.UTF8.GetString(exportedBytes);
            _logger.Send($"Profile export: {sw.ElapsedMilliseconds}ms, {cookieContent.Length / 1024.0:F2} KB");

            sw.Restart();
            string jsonCookies = NetscapeToJson(cookieContent);
            _logger.Send($"Conversion: {sw.ElapsedMilliseconds}ms, {jsonCookies.Length / 1024.0:F2} KB");

            sw.Restart();
            string encoded = EncodeForDb(jsonCookies);
            _project.DbUpd($"cookies = '{encoded}'", table);
            _logger.Send($"DB save: {sw.ElapsedMilliseconds}ms, {encoded.Length / 1024.0:F2} KB");

            if (!string.IsNullOrEmpty(jsonPath))
            {
                File.WriteAllText(jsonPath, jsonCookies);
                _logger.Send($"JSON file: {jsonPath}");
            }

            _logger.Send($"✅ Total: {sw.ElapsedMilliseconds}ms");
        }

        public void SaveCurrent(string table = "profiles")
        {
            RequireInstance("SaveCurrent");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            string currentDomain = _instance.ActiveTab.MainDomain;

            byte[] exportedBytes = _project.Profile.CookieContainer.Export();
            string cookieContent = Encoding.UTF8.GetString(exportedBytes);
            
            string jsonCookies = NetscapeToJson(cookieContent, domainFilter: currentDomain);
            string encoded = EncodeForDb(jsonCookies);
            
            _project.DbUpd($"cookies = '{encoded}'", table);
            _logger.Send($"✅ Domain '{currentDomain}' cookies saved: {sw.ElapsedMilliseconds}ms");
        }

        public void Load(string source = "dbMain", string jsonPath = null)
        {
            RequireInstance("Load");

            string cookieData = null;

            switch (source)
            {
                case "dbMain":
                    cookieData = _project.SqlGet("cookies", "_instance");
                    break;
                case "dbProject":
                    cookieData = _project.SqlGet("cookies");
                    break;
                case "fromFile":
                    if (string.IsNullOrEmpty(jsonPath))
                        jsonPath = _project.PathCookies();
                    cookieData = File.ReadAllText(jsonPath);
                    break;
                default:
                    throw new ArgumentException($"Unknown source: {source}. Use 'dbMain', 'dbProject', or 'fromFile'");
            }

            if (string.IsNullOrEmpty(cookieData))
            {
                _logger.Send($"No cookies found in {source}");
                return;
            }

            cookieData = DecodeFromDb(cookieData);
            string netscape = JsonToNetscape(cookieData);
            _instance.SetCookie(netscape);
            
            _logger.Send($"✅ Cookies loaded from {source}");
        }

        public string Get(string domainFilter = "")
        {
            if (domainFilter == ".")
            {
                RequireInstance("Get with domainFilter='.'");
                domainFilter = _instance.ActiveTab.MainDomain;
            }

            var cookieContainer = _project.Profile.CookieContainer;
            var cookieList = new List<object>();

            foreach (var domain in cookieContainer.Domains)
            {
                if (string.IsNullOrEmpty(domainFilter) || domain.Contains(domainFilter))
                {
                    var cookies = cookieContainer.Get(domain);
                    
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
                }
            }

            string cookiesJson = Global.ZennoLab.Json.JsonConvert.SerializeObject(cookieList, formatting: Formatting.None);
            _logger.Send($"Retrieved {cookieList.Count} cookies, {cookiesJson.Length / 1024.0:F2} KB");
            
            return cookiesJson;
        }

        public string GetByJs()
        {
            RequireInstance("GetByJs");

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
            string result = _instance.ActiveTab.MainDocument.EvaluateScript(jsCode).ToString();
            _logger.Send($"JS extracted {result.Length} chars");
            return result.Replace("\r\n", "").Replace("\n", "").Replace("\r", "").Trim();
        }

        public void SetByJs(string cookiesJson)
        {
            RequireInstance("SetByJs");

            var cookies = JArray.Parse(cookiesJson);
            var uniqueCookies = cookies
                .GroupBy(c => new { Domain = c["domain"].ToString(), Name = c["name"].ToString() })
                .Select(g => g.Last())
                .ToList();

            string currentDomain = _instance.ActiveTab.Domain;
            string[] domainParts = currentDomain.Split('.');
            string parentDomain = "." + string.Join(".", domainParts.Skip(domainParts.Length - 2));

            var jsLines = new List<string>();
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
                        expires = expValue < DateTimeOffset.UtcNow.ToUnixTimeSeconds() 
                            ? DateTimeOffset.UtcNow.AddYears(1).ToString("R")
                            : DateTimeOffset.FromUnixTimeSeconds((long)expValue).ToString("R");
                    }
                    else
                    {
                        expires = DateTimeOffset.UtcNow.AddYears(1).ToString("R");
                    }

                    jsLines.Add($"document.cookie = '{name}={value}; domain={parentDomain}; path={path}; expires={expires}; Secure';");
                    cookieCount++;
                }
            }

            if (jsLines.Count > 0)
            {
                string jsCode = string.Join("\n", jsLines);
                _instance.ActiveTab.MainDocument.EvaluateScript(jsCode);
                _logger.Send($"✅ JS set {cookieCount} cookies for {currentDomain}");
            }
            else
            {
                _logger.Send($"No cookies found for {currentDomain}");
            }
        }

        private static string NetscapeToJson(string content, string domainFilter = null)
        {
            var cookies = new List<object>();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split('\t');
                if (parts.Length < 7) continue;

                try
                {
                    var domain = parts[0];
                    if (!string.IsNullOrEmpty(domainFilter) && !domain.Contains(domainFilter))
                        continue;

                    var includeSubdomains = parts[1].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                    var path = parts[2];
                    var secure = parts[3].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                    var expiryStr = parts[4];
                    var name = parts[5];
                    var value = parts.Length > 6 ? parts[6] : "";

                    var httpOnly = parts.Length > 7 && parts[7].Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                    var sameSite = parts.Length > 9 ? parts[9] : "Unspecified";

                    double? expirationDate = null;
                    bool isSession = string.IsNullOrEmpty(expiryStr);

                    if (!isSession && DateTime.TryParseExact(expiryStr, "MM/dd/yyyy HH:mm:ss",
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry))
                    {
                        expirationDate = new DateTimeOffset(expiry).ToUnixTimeSeconds();
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
                catch { }
            }

            return Global.ZennoLab.Json.JsonConvert.SerializeObject(cookies, Global.ZennoLab.Json.Formatting.None);
        }

        private static string JsonToNetscape(string jsonCookies)
        {
            var cookies = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(jsonCookies);
            var lines = new List<string>();
    
            foreach (var cookie in cookies)
            {
                string domain = cookie.domain.ToString();
                string flag = domain.StartsWith(".") ? "TRUE" : "FALSE";
                string path = cookie.path.ToString();
                string secure = cookie.secure.ToString().ToUpper();
        
                string expiration;
                if (cookie.expirationDate == null || cookie.session == true)
                {
                    expiration = "01/01/2030 00:00:00";
                }
                else
                {
                    double timestamp = (double)cookie.expirationDate;
                    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
                    expiration = dateTime.ToString("MM/dd/yyyy HH:mm:ss");
                }
        
                string name = cookie.name.ToString();
                string value = cookie.value.ToString();
                string httpOnly = cookie.httpOnly.ToString().ToUpper();
        
                string line = $"{domain}\t{flag}\t{path}\t{secure}\t{expiration}\t{name}\t{value}\t{httpOnly}\tFALSE";
                lines.Add(line);
            }
            return string.Join("\n", lines);
        }

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
                return base64Cookies;
            }
        }
    }

    public static class ProjectCookieExtensions
    {
        public static void SaveCookiesToDb(this IZennoPosterProjectModel project,  string table = "_instance")
        {
            new Cookies(project).SaveAll(table: table);
        }

        public static string GetCookies(this IZennoPosterProjectModel project, string domainFilter = "")
        {
            return new Cookies(project).Get(domainFilter);
        }

        public static string GetCookiesJson(this IZennoPosterProjectModel project)
        {
            return new Cookies(project).Get();
        }
    }

    public static class InstanceCookieExtensions
    {
        public static void SaveCookies(this Instance instance, IZennoPosterProjectModel project, string table = "_instance")
        {
            new Cookies(project, instance).SaveAll(table: table);
        }

        public static void SaveCurrentCookies(this Instance instance, IZennoPosterProjectModel project)
        {
            new Cookies(project, instance).SaveCurrent();
        }

        public static void LoadCookies(this Instance instance, IZennoPosterProjectModel project, string source = "dbMain")
        {
            new Cookies(project, instance).Load(source);
        }

        public static void SetCookies(this Instance instance, IZennoPosterProjectModel project)
        {
            string cookies = project.Var("cookies");
            
            if (string.IsNullOrWhiteSpace(cookies))
            {
                cookies = project.DbGet("cookies", "_instance");
                if (!string.IsNullOrWhiteSpace(cookies))
                {
                    try
                    {
                        byte[] bytes = Convert.FromBase64String(cookies);
                        cookies = Encoding.UTF8.GetString(bytes);
                    }
                    catch (FormatException) { }
                    
                    project.Var("cookies", cookies);
                }
            }
            
            if (string.IsNullOrWhiteSpace(cookies))
            {
                project.warn("cookies is empty");
                return;
            }

            var netscape = JsonToNetscape(cookies);
            instance.SetCookie(netscape);
        }

        private static string JsonToNetscape(string jsonCookies)
        {
            var cookies = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(jsonCookies);
            var lines = new List<string>();
    
            foreach (var cookie in cookies)
            {
                string domain = cookie.domain.ToString();
                string flag = domain.StartsWith(".") ? "TRUE" : "FALSE";
                string path = cookie.path.ToString();
                string secure = cookie.secure.ToString().ToUpper();
        
                string expiration;
                if (cookie.expirationDate == null || cookie.session == true)
                {
                    expiration = "01/01/2030 00:00:00";
                }
                else
                {
                    double timestamp = (double)cookie.expirationDate;
                    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
                    expiration = dateTime.ToString("MM/dd/yyyy HH:mm:ss");
                }
        
                string name = cookie.name.ToString();
                string value = cookie.value.ToString();
                string httpOnly = cookie.httpOnly.ToString().ToUpper();
        
                string line = $"{domain}\t{flag}\t{path}\t{secure}\t{expiration}\t{name}\t{value}\t{httpOnly}\tFALSE";
                lines.Add(line);
            }
            return string.Join("\n", lines);
        }
    }
}