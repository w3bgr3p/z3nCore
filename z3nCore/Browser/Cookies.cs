using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Buffers;
using System.CodeDom.Compiler;
using Global.ZennoLab.Json;
using Newtonsoft.Json.Linq;
using ZennoLab.InterfacesLibrary.ProjectModel;
using ZennoLab.CommandCenter;

namespace z3nCore
{

    public static class Cookies
    {
        public static string ConvertCookieFormat(string input, string output = null)
        {
            input = input?.Trim();

            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Input is empty");

            // Определяем текущий формат
            bool isJson = input.StartsWith("[") || input.StartsWith("{");
            bool isNetscape = input.Contains("\t");

            if (!isJson && !isNetscape)
                throw new ArgumentException("Unknown input format");

            // Если output не указан - автоматическая конвертация
            if (string.IsNullOrEmpty(output))
            {
                return isJson ? JsonToNetscape(input) : NetscapeToJson(input);
            }

            // Если output указан - проверяем совпадение
            output = output.ToLower().Trim();

            if ((output == "json" && isJson) || (output == "netscape" && isNetscape))
            {
                // Формат уже соответствует желаемому
                return input;
            }

            // Конвертация в указанный формат
            if (output == "json")
            {
                return NetscapeToJson(input);
            }
            else if (output == "netscape")
            {
                return JsonToNetscape(input);
            }
            else
            {
                throw new ArgumentException($"Unknown output format: {output}. Use 'json' or 'netscape'");
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
                catch
                {
                }
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

        public static string GetCookies(this Instance instance, string domainFilter = null, string format = "json")
        {
            if (domainFilter == ".")
                domainFilter = instance.ActiveTab.MainDomain;

            var netscapeCookies = (string.IsNullOrEmpty(domainFilter))
                ? instance.GetCookie()
                : instance.GetCookie(domainFilter);
            if (format == "json")
                return NetscapeToJson(netscapeCookies);
            else if (format == "netscape")
                return netscapeCookies;
            else if (format == "base64Json")
                return NetscapeToJson(netscapeCookies).ToBase64();
            else if (format == "base64Netscape")
                return netscapeCookies.ToBase64();
            else throw new ArgumentException($"Unknown format: {format}");


        }

        public static void SaveAllCookies(this IZennoPosterProjectModel project, Instance instance,
            string jsonPath = null, string table = "_instance", bool saveJsonToDb = false)
        {

            var netscapeCookies = instance.GetCookie();
            string jsonCookies = NetscapeToJson(netscapeCookies);
            string base64Cookies = (saveJsonToDb) ? jsonCookies.ToBase64() : netscapeCookies.ToBase64();
            project.DbUpd($"cookies = '{base64Cookies}'", table);
            if (!string.IsNullOrEmpty(jsonPath))
                File.WriteAllText(jsonPath, jsonCookies);

        }

        public static void SaveDomainCookies(this IZennoPosterProjectModel project, Instance instance,
            string domain = null, string jsonPath = null, string tableName = "_instance", bool saveJsonToDb = false)
        {
            if (string.IsNullOrEmpty(domain))
                domain = instance.ActiveTab.MainDomain;
            var netscapeCookies = instance.GetCookie();
            string jsonCookies = NetscapeToJson(netscapeCookies);
            string base64Cookies = (saveJsonToDb) ? jsonCookies.ToBase64() : netscapeCookies.ToBase64();
            project.DbUpd($"cookies = '{base64Cookies}'", tableName);
            if (!string.IsNullOrEmpty(jsonPath))
                File.WriteAllText(jsonPath, jsonCookies);
        }

        public static void LoadCookies(this IZennoPosterProjectModel project, Instance instance, string jsonPath = null,
            string table = "_instance", bool isJsonInDb = false)
        {
            string netscapeCookies = null;

            if (!string.IsNullOrEmpty(jsonPath))
            {
                netscapeCookies = JsonToNetscape(File.ReadAllText(jsonPath));
            }
            else
            {
                var dbCookies = project.DbGet("cookies", table).FromBase64();
                netscapeCookies = ConvertCookieFormat(dbCookies, "netscape");
            }

            instance.SetCookie(netscapeCookies);

        }

        public static string GetCookiesByJs(this Instance instance)
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
            string result = instance.ActiveTab.MainDocument.EvaluateScript(jsCode).ToString();
            return result.Replace("\r\n", "").Replace("\n", "").Replace("\r", "").Trim();
        }

        public static void SetCookiesByJs(this Instance instance, string cookiesJson)
        {
            var cookies = JArray.Parse(cookiesJson);
            var uniqueCookies = cookies
                .GroupBy(c => new { Domain = c["domain"].ToString(), Name = c["name"].ToString() })
                .Select(g => g.Last())
                .ToList();

            string currentDomain = instance.ActiveTab.Domain;
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

                    jsLines.Add(
                        $"document.cookie = '{name}={value}; domain={parentDomain}; path={path}; expires={expires}; Secure';");
                    cookieCount++;
                }
            }

            if (jsLines.Count > 0)
            {
                string jsCode = string.Join("\n", jsLines);
                instance.ActiveTab.MainDocument.EvaluateScript(jsCode);
            }
            else
            {

            }
        }

    }
}






