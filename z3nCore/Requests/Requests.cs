using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.Enums.Http;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Newtonsoft.Json.Linq;
namespace z3nCore
{
    public static class Requests
    {
        private static readonly object LockObject = new object();

        private static string GetCookiesForRequest(IZennoPosterProjectModel project, string url)
        {
            string cookiesJson = project.Var("cookies");
    
            if (string.IsNullOrEmpty(cookiesJson))
            {
                string cookiesBase64 = project.DbGet("cookies", "_instance");
                if (!string.IsNullOrEmpty(cookiesBase64))
                {
                    cookiesJson = cookiesBase64.FromBase64();
                    project.Var("cookies",cookiesJson);
                }
            }
    
            if (string.IsNullOrEmpty(cookiesJson))
            {
                return null;
            }
    
            string domain = ExtractDomain(url);
            if (string.IsNullOrEmpty(domain))
            {
                return null;
            }
    
            var cookiePairs = new List<string>();
    
            try
            {
                JArray cookiesArray = JArray.Parse(cookiesJson);
        
                for (int i = 0; i < cookiesArray.Count; i++)
                {
                    string cookieDomain = cookiesArray[i]["domain"]?.ToString() ?? "";
            
                    if (IsDomainMatch(domain, cookieDomain))
                    {
                        string name = cookiesArray[i]["name"]?.ToString() ?? "";
                        string value = cookiesArray[i]["value"]?.ToString() ?? "";
                
                        if (!string.IsNullOrEmpty(name))
                        {
                            cookiePairs.Add($"{name}={value}");
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
    
            if (cookiePairs.Count == 0)
            {
                return null;
            }
    
            return string.Join("; ", cookiePairs) + ";";
        }

        private static string ExtractDomain(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                return uri.Host;
            }
            catch
            {
                return null;
            }
        }


        private static bool IsDomainMatch(string requestDomain, string cookieDomain)
        {
            if (string.IsNullOrEmpty(requestDomain) || string.IsNullOrEmpty(cookieDomain))
            {
                return false;
            }
    
            if (cookieDomain.StartsWith("."))
            {
                return requestDomain.EndsWith(cookieDomain.Substring(1)) || 
                       requestDomain == cookieDomain.Substring(1);
            }
    
            return requestDomain == cookieDomain;
        }
        private static readonly HashSet<string> AUTOMATIC_HEADERS = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "host",
            "connection",
            "proxy-connection",
            "content-length",
            "transfer-encoding",
            "expect",
            "upgrade",
            "te"
        };

        // Псевдо-заголовки HTTP/2
        private static readonly HashSet<string> HTTP2_PSEUDO_HEADERS = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ":authority",
            ":method",
            ":path",
            ":scheme"
        };
        
       public static string GET(
            this IZennoPosterProjectModel project,
            string url,
            string proxy = "",
            string[] headers = null,
            string cookies = null,
            bool log = false,
            bool parse = false,
            bool parseJson = false,
            int deadline = 30,
            bool thrw = false,
            bool useNetHttp = false,
            bool returnSuccessWithStatus = false)  // ← НОВЫЙ ПАРАМЕТР
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (parseJson)
            {
                parse = parseJson;
                project.warn("using obsolete parameter \"parseJson\", change to \"parse\" ASAP");
            }

            var logger = new Logger(project, log, classEmoji: "↑↓");
            string debugProxy = proxy;

            try
            {
                string body;
                int statusCode;

                if (useNetHttp)
                {
                    body = ExecuteGetViaNetHttp(
                        project, 
                        url, 
                        proxy, 
                        headers, 
                        deadline, 
                        thrw, 
                        logger, 
                        out statusCode);
                }
                else
                {
                    body = ExecuteGetViaZennoPoster(
                        project, 
                        url, 
                        proxy, 
                        headers, 
                        cookies,
                        deadline, 
                        logger, 
                        out statusCode);
                }

                if (log)
                {
                    LogStatus(logger, statusCode, url, debugProxy);
                    logger.Send($"response: [{body}]");
                }

                if (statusCode < 200 || statusCode >= 300)
                {
                    string errorMessage = FormatErrorMessage(statusCode, body);
                    logger.Send($"!W HTTP Error: [{errorMessage}] url:[{url}] proxy:[{debugProxy}]");

                    if (thrw)
                    {
                        throw new Exception(errorMessage);
                    }
                    return errorMessage;
                }

                // ← НОВАЯ ЛОГИКА
                if (returnSuccessWithStatus)
                {
                    return $"{statusCode}\r\n\r\n{body.Trim()}";
                }

                if (parse)
                {
                    ParseJson(project, body, logger);
                }

                return body.Trim();
            }
            catch (Exception e)
            {
                string errorMessage = $"Error: {e.Message}";
                logger.Send($"!W RequestErr: [{e.Message}] url:[{url}] (proxy: [{debugProxy}])");
                if (thrw) throw;
                return errorMessage;
            }
        }

        public static string POST(
            this IZennoPosterProjectModel project,
            string url,
            string body,
            string proxy = "",
            string[] headers = null,
            string cookies = null,
            bool log = false,
            bool parse = false,
            bool parseJson = false,
            int deadline = 30,
            bool thrw = false,
            bool useNetHttp = false,
            bool returnSuccessWithStatus = false) // ← НОВЫЙ ПАРАМЕТР
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (parseJson)
            {
                parse = parseJson;
                project.warn("using obsolete parameter \"parseJson\", change to \"parse\" ASAP");
            }

            var logger = new Logger(project, log, classEmoji: "↑↓");
            string debugProxy = proxy;

            try
            {
                string responseBody;
                int statusCode;

                if (useNetHttp)
                {
                    responseBody = ExecutePostViaNetHttp(
                        project,
                        url,
                        body,
                        proxy,
                        headers,
                        deadline,
                        thrw,
                        logger,
                        out statusCode);
                }
                else
                {
                    responseBody = ExecutePostViaZennoPoster(
                        project,
                        url,
                        body,
                        proxy,
                        headers,
                        cookies,
                        deadline,
                        logger,
                        out statusCode);
                }

                if (log)
                {
                    LogStatus(logger, statusCode, url, debugProxy);
                    logger.Send($"response: [{responseBody}]");
                }

                if (statusCode < 200 || statusCode >= 300)
                {
                    string errorMessage = FormatErrorMessage(statusCode, responseBody);
                    logger.Send($"!W HTTP Error: [{errorMessage}] url:[{url}] proxy:[{debugProxy}]");

                    if (thrw)
                    {
                        throw new Exception(errorMessage);
                    }
                    return errorMessage;
                }

                // ← НОВАЯ ЛОГИКА
                if (returnSuccessWithStatus)
                {
                    return $"{statusCode}\r\n\r\n{responseBody.Trim()}";
                }

                if (parse)
                {
                    ParseJson(project, responseBody, logger);
                }

                return responseBody.Trim();
            }
            catch (Exception e)
            {
                string errorMessage = $"Error: {e.Message}";
                logger.Send($"!W RequestErr: [{e.Message}] url:[{url}] (proxy: [{debugProxy}])");
                if (thrw) throw;
                return errorMessage;
            }
        }


        
        private static string ExecuteGetViaZennoPoster(
            IZennoPosterProjectModel project,
            string url,
            string proxy,
            string[] headers,
            string cookies,
            int deadline,
            Logger logger,
            out int statusCode)
        {
            string fullResponse;
            
            // Если cookies не переданы явно, используем GetCookiesForRequest
            if (string.IsNullOrEmpty(cookies))
            {
                cookies = GetCookiesForRequest(project, url);
            }
            
            // Если GetCookiesForRequest вернул null, используем CookieContainer
            // Иначе передаем null в CookieContainer чтобы использовать строку cookies
            bool useCookieContainer = string.IsNullOrEmpty(cookies);
            
            lock (LockObject)
            {
                string proxyString = ParseProxy(project, proxy, logger);
                headers = PrepareHeaders(project, headers, out string userAgent, out string contentType);

                fullResponse = ZennoPoster.HTTP.Request(
                    HttpMethod.GET,
                    url,
                    "",
                    contentType,
                    proxyString,
                    "UTF-8",
                    ResponceType.HeaderAndBody,
                    deadline * 1000,
                    cookies ?? "",
                    userAgent,
                    true,
                    5,
                    headers,
                    "",
                    false,
                    false,
                    useCookieContainer ? project.Profile.CookieContainer : null);
            }

            string body;
            ParseResponse(fullResponse, out statusCode, out body);
            return body;
        }
        private static string ExecutePostViaZennoPoster(
            IZennoPosterProjectModel project,
            string url,
            string body,
            string proxy,
            string[] headers,
            string cookies,
            int deadline,
            Logger logger,
            out int statusCode)
        {
            string fullResponse;
            
            if (string.IsNullOrEmpty(cookies))
            {
                cookies = GetCookiesForRequest(project, url);
            }
            

            bool useCookieContainer = string.IsNullOrEmpty(cookies);
            
            lock (LockObject)
            {
                string proxyString = ParseProxy(project, proxy, logger);
                headers = PrepareHeaders(project, headers, out string userAgent, out string contentType);

                fullResponse = ZennoPoster.HTTP.Request(
                    HttpMethod.POST,
                    url,
                    body,
                    contentType,
                    proxyString,
                    "UTF-8",
                    ResponceType.HeaderAndBody,
                    deadline * 1000,
                    cookies ?? "",
                    userAgent,
                    true,
                    5,
                    headers,
                    "",
                    false,
                    true,
                    useCookieContainer ? project.Profile.CookieContainer : null);
            }

            string responseBody;
            ParseResponse(fullResponse, out statusCode, out responseBody);
            return responseBody;
        }
        
        private static string ExecuteGetViaNetHttp(
            IZennoPosterProjectModel project,
            string url,
            string proxy,
            string[] headers,
            int deadline,
            bool thrw,
            Logger logger,
            out int statusCode)
        {
            var netHttp = new NetHttp(project, log: false);

            Dictionary<string, string> headersDic = null;
            if (headers != null && headers.Length > 0)
            {
                headersDic = ConvertHeadersToDictionary(headers);
            }
            else
            {
                try
                {
                    var headersArray = project.Var("headers").Split('\n');
                    headersDic = ConvertHeadersToDictionary(headersArray);
                }
                catch { }
            }

            if (headersDic == null)
            {
                headersDic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            if (!headersDic.ContainsKey("Cookie"))
            {
                string cookies = GetCookiesForRequest(project, url);
                if (!string.IsNullOrEmpty(cookies))
                {
                    headersDic["Cookie"] = cookies.TrimEnd(';', ' ');
                }
            }

            string response = netHttp.GET(
                url,
                proxy,
                headersDic,
                parse: false,
                deadline: deadline,
                throwOnFail: false 
            );
            
            statusCode = TryParseStatusFromNetHttpResponse(response);

            return response;
        }

        private static string ExecutePostViaNetHttp(
            IZennoPosterProjectModel project,
            string url,
            string body,
            string proxy,
            string[] headers,
            int deadline,
            bool thrw,
            Logger logger,
            out int statusCode)
        {
            var netHttp = new NetHttp(project, log: false);

            Dictionary<string, string> headersDic = null;
            if (headers != null && headers.Length > 0)
            {
                headersDic = ConvertHeadersToDictionary(headers);
            }
            else
            {
                try
                {
                    var headersArray = project.Var("headers").Split('\n');
                    headersDic = ConvertHeadersToDictionary(headersArray);
                }
                catch { }
            }

            if (headersDic == null)
            {
                headersDic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            if (!headersDic.ContainsKey("Cookie"))
            {
                string cookies = GetCookiesForRequest(project, url);
                if (!string.IsNullOrEmpty(cookies))
                {
                    headersDic["Cookie"] = cookies.TrimEnd(';', ' ');
                }
            }

            string response = netHttp.POST(
                url,
                body,
                proxy,
                headersDic,
                parse: false,
                deadline: deadline,
                throwOnFail: false
            );

            statusCode = TryParseStatusFromNetHttpResponse(response);

            return response;
        }

        /// <summary>
        /// Подготовить заголовки для ZennoPoster.HTTP.Request
        /// Извлекает User-Agent и Content-Type из headers, фильтрует системные заголовки
        /// </summary>
        private static string[] PrepareHeaders(
            IZennoPosterProjectModel project,
            string[] headers,
            out string userAgent,
            out string contentType)
        {
            userAgent = project.Profile.UserAgent;
            contentType = "application/json";

            if (headers == null || headers.Length == 0)
            {
                try
                {
                    headers = project.Var("headers").Split('\n');
                }
                catch
                {
                    headers = new string[0];
                }
            }

            if (headers.Length == 0)
            {
                return headers;
            }

            var normalizedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var header in headers)
            {
                if (string.IsNullOrWhiteSpace(header)) continue;

                if (header.TrimStart().StartsWith(":")) continue;

                var colonIndex = header.IndexOf(':');
                if (colonIndex == -1) continue;

                var key = header.Substring(0, colonIndex).Trim();
                var value = header.Substring(colonIndex + 1).Trim();

                if (AUTOMATIC_HEADERS.Contains(key)) continue;

                if (key.Equals("user-agent", StringComparison.OrdinalIgnoreCase))
                {
                    userAgent = value;
                    continue; 
                }

                if (key.Equals("content-type", StringComparison.OrdinalIgnoreCase))
                {
                    contentType = value;
                    continue; 
                }

                normalizedHeaders[key] = value;
            }

            var cleanHeaders = new List<string>();
            foreach (var kvp in normalizedHeaders)
            {
                cleanHeaders.Add($"{kvp.Key}: {kvp.Value}");
            }

            return cleanHeaders.ToArray();
        }

        private static Dictionary<string, string> ConvertHeadersToDictionary(string[] headersArray)
        {
            var forbiddenHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "authority", "method", "path", "scheme",
                "host", "content-length", "connection", "upgrade",
                "proxy-connection", "transfer-encoding"
            };

            var headersDic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (headersArray == null) return headersDic;

            foreach (var header in headersArray)
            {
                if (string.IsNullOrWhiteSpace(header)) continue;
                if (header.StartsWith(":")) continue;

                var colonIndex = header.IndexOf(':');
                if (colonIndex == -1) continue;

                var key = header.Substring(0, colonIndex).Trim();
                var value = header.Substring(colonIndex + 1).Trim();

                if (forbiddenHeaders.Contains(key)) continue;

                // Перезапишем дубли с разным регистром
                headersDic[key] = value;
            }

            return headersDic;
        }

        private static int TryParseStatusFromNetHttpResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
                return 0;

            if (response.Contains("!!!"))
            {
                var parts = response.Split(new[] { "!!!" }, StringSplitOptions.None);
                if (parts.Length > 0)
                {
                    int code;
                    if (int.TryParse(parts[0].Trim(), out code))
                    {
                        return code;
                    }
                }
            }

            if (response.StartsWith("Error:") || response.StartsWith("Ошибка:"))
            {
                return 0;
            }

            return 200;
        }

        private static string FormatErrorMessage(int statusCode, string body)
        {
            string statusText = GetStatusText(statusCode);

            string bodyPreview = body.Length > 100 ? body.Substring(0, 100) + "..." : body;

            if (string.IsNullOrWhiteSpace(body))
            {
                return $"{statusCode} {statusText}";
            }

            return $"{statusCode} {statusText}: {bodyPreview}";
        }

        private static string GetStatusText(int statusCode)
        {
            switch (statusCode)
            {
                case 0: return "Connection Failed";
                case 400: return "Bad Request";
                case 401: return "Unauthorized";
                case 403: return "Forbidden";
                case 404: return "Not Found";
                case 405: return "Method Not Allowed";
                case 408: return "Request Timeout";
                case 429: return "Too Many Requests";
                case 500: return "Internal Server Error";
                case 502: return "Bad Gateway";
                case 503: return "Service Unavailable";
                case 504: return "Gateway Timeout";
                default:
                    if (statusCode >= 400 && statusCode < 500) return "Client Error";
                    if (statusCode >= 500) return "Server Error";
                    return "Unknown Error";
            }
        }

        private static void ParseResponse(string fullResponse, out int statusCode, out string body)
        {
            statusCode = 200;
            body = string.Empty;

            try
            {
                if (string.IsNullOrEmpty(fullResponse))
                {
                    statusCode = 0;
                    return;
                }

                int firstLineEnd = fullResponse.IndexOf("\r\n");
                if (firstLineEnd == -1)
                {
                    body = fullResponse.Trim();
                    return;
                }

                string statusLine = fullResponse.Substring(0, firstLineEnd);

                string[] parts = statusLine.Split(' ');
                if (parts.Length >= 2)
                {
                    int.TryParse(parts[1], out statusCode);
                }

                int bodyStart = fullResponse.IndexOf("\r\n\r\n");
                if (bodyStart != -1)
                {
                    body = fullResponse.Substring(bodyStart + 4).Trim();
                }
            }
            catch
            {
                statusCode = 200;
                body = fullResponse.Trim();
            }
        }

        private static void LogStatus(Logger logger, int statusCode, string url, string proxy)
        {
            if (statusCode >= 200 && statusCode < 300)
            {
                logger.Send($"✓ HTTP {statusCode}");
            }
            else if (statusCode == 429)
            {
                logger.Send($"!W HTTP 429 Rate Limited | url:[{url}] proxy:[{proxy}]");
            }
            else if (statusCode >= 400 && statusCode < 500)
            {
                logger.Send($"!W HTTP {statusCode} Client Error | url:[{url}] proxy:[{proxy}]");
            }
            else if (statusCode >= 500)
            {
                logger.Send($"!W HTTP {statusCode} Server Error | url:[{url}] proxy:[{proxy}]");
            }
            else if (statusCode == 0)
            {
                logger.Send($"!W HTTP Request Failed | url:[{url}] proxy:[{proxy}]");
            }
        }

        private static string[] BuildHeaders(IZennoPosterProjectModel project, string[] headers = null)
        {
            if (headers == null || headers.Length == 0)
            {
                return project.Var("headers").Split('\n');
            }
            else return headers;
        }

        private static string ParseProxy(IZennoPosterProjectModel project, string proxyString, Logger logger = null)
        {
            if (string.IsNullOrEmpty(proxyString)) return "";

            if (proxyString == "+")
            {
                string projectProxy = project.Var("proxy");
                if (!string.IsNullOrEmpty(projectProxy))
                    proxyString = projectProxy;
                else
                {
                    proxyString = project.SqlGet("proxy", "_instance");
                    logger?.Send($"Proxy retrieved from SQL: [{proxyString}]");
                }
            }

            try
            {
                if (proxyString.Contains("//"))
                {
                    proxyString = proxyString.Split('/')[2];
                }

                if (proxyString.Contains("@"))
                {
                    string[] parts = proxyString.Split('@');
                    string credentials = parts[0];
                    string proxyHost = parts[1];
                    string[] creds = credentials.Split(':');
                    return $"http://{creds[0]}:{creds[1]}@{proxyHost}";
                }
                else
                {
                    return $"http://{proxyString}";
                }
            }
            catch (Exception e)
            {
                logger?.Send($"Proxy parsing error: [{e.Message}] [{proxyString}]");
                return "";
            }
        }

        private static void ParseJson(IZennoPosterProjectModel project, string json, Logger logger = null)
        {
            try
            {
                project.Json.FromString(json);
            }
            catch (Exception ex)
            {
                logger?.Send($"[!W JSON parsing error: {ex.Message}] [{json}]");
            }
        }

        private static string Cookies(this IZennoPosterProjectModel project,string cookie = null)
        {
            if (string.IsNullOrEmpty(cookie)) cookie = project.Var("cookie");
            return cookie;
        }

    }

    public static partial class ProjectExtensions
    {
        // Здесь могут быть дополнительные extension методы
    }
}