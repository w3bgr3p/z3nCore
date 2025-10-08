using System;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.Enums.Http;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
   
    public static class Requests
    {
        private static readonly object LockObject = new object();
        public static string GET(
            this IZennoPosterProjectModel project,
            string url,
            string proxy = "",
            string[] headers = null,
            bool log = false,
            bool parseJson = false,
            int deadline = 15,
            bool thrw = false)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var logger = new Logger(project, log, classEmoji: "↑↓");
            string debugProxy = proxy;

            try
            {
                string fullResponse;
                lock (LockObject)
                {
                    string proxyString = ParseProxy(project, proxy, logger);
                    
                    fullResponse = ZennoPoster.HTTP.Request(
                        HttpMethod.GET,
                        url,
                        "",
                        "application/json",
                        proxyString,
                        "UTF-8",
                        ResponceType.HeaderAndBody,
                        deadline * 1000,
                        "",
                        project.Profile.UserAgent,
                        true,
                        5,
                        headers,
                        "",
                        false,
                        false,
                        project.Profile.CookieContainer);
                }

                // Парсим ответ: извлекаем статус и body
                int statusCode;
                string body;
                ParseResponse(fullResponse, out statusCode, out body);

                // Логируем статус если включен log
                if (log)
                {
                    LogStatus(logger, statusCode, url, debugProxy);
                    logger.Send($"response: [{body}]");
                }

                // Проверяем статус код
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

                // Парсим JSON если нужно и статус успешный
                if (parseJson)
                {
                    ParseJson(project, body, logger);
                }

                return body.Trim();
            }
            catch (Exception e)
            {
                string errorMessage = $"Error: {e.Message}";
                logger.Send($"!W RequestErr: [{e.Message}] url:[{url}] (proxy: [{debugProxy})]");
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
            bool log = false,
            bool parseJson = false,
            int deadline = 15,
            bool thrw = false)
        {
            var logger = new Logger(project, log, classEmoji: "↑↓");
            string debugProxy = proxy;
            
            if (headers == null)
            {
                try
                {
                    headers = project.Var("headers").Split('\n');
                }
                catch { }
            }

            try
            {
                string fullResponse;
                lock (LockObject)
                {
                    string proxyString = ParseProxy(project, proxy, logger);
                    
                    fullResponse = ZennoPoster.HTTP.Request(
                        HttpMethod.POST,
                        url,
                        body,
                        "application/json",
                        proxyString,
                        "UTF-8",
                        ResponceType.HeaderAndBody,
                        deadline * 1000,
                        "",
                        project.Profile.UserAgent,
                        true,
                        5,
                        headers,
                        "",
                        false,
                        false,
                        project.Profile.CookieContainer);
                }

                int statusCode;
                string responseBody;
                ParseResponse(fullResponse, out statusCode, out responseBody);

                if (log)
                {
                    LogStatus(logger, statusCode, url, debugProxy);
                    logger.Send($"response: [{responseBody}]");
                }

                // Проверяем статус код
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

                // Парсим JSON если нужно и статус успешный
                if (parseJson)
                {
                    ParseJson(project, responseBody, logger);
                }

                return responseBody.Trim();
            }
            catch (Exception e)
            {
                string errorMessage = $"Error: {e.Message}";
                logger.Send($"!W RequestErr: [{e.Message}] url:[{url}] (proxy: [{debugProxy})]");
                if (thrw) throw;
                return errorMessage;
            }
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
            statusCode = 200; // По умолчанию
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
        
        
        public static string GET_(
            this IZennoPosterProjectModel project,
            string url,            
            string proxy = "",
            string[] headers = null,
            bool log = false,
            bool parseJson = false,
            int deadline = 15,
            bool throwOnFail = false)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var logger = new Logger(project, log, classEmoji: "↑↓");
            string debugProxy = proxy;

            try
            {
                string response;

                lock (LockObject)
                {
                    string proxyString = ParseProxy(project, proxy, logger);
                    response = ZennoPoster.HTTP.Request(
                        ZennoLab.InterfacesLibrary.Enums.Http.HttpMethod.GET,
                        url,
                        "application/json",
                        "",
                        proxyString,
                        "UTF-8",
                        ResponceType.BodyOnly,
                        deadline * 1000,
                        "",
                        project.Profile.UserAgent,
                        true,
                        5,
                        headers,
                        "",
                        false,
                        false,
                        project.Profile.CookieContainer);
                    if (parseJson) ParseJson(project, response, logger);
                    if (log) logger.Send($"response: [{response}]");
                }


                return response.Trim();
            }
            catch (Exception e)
            {
                logger.Send($"!W RequestErr: [{e.Message}] url:[{url}] (proxy: [{debugProxy})]");
                if (throwOnFail) throw;
                return string.Empty;
            }
        }

        public static string POST_(
            this IZennoPosterProjectModel project,
            string url,
            string body,            
            string proxy = "",
            string[] headers = null,
            bool log = false,
            bool parseJson = false,
            int deadline = 15,
            bool throwOnFail = false)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var logger = new Logger(project, log, classEmoji: "↑↓");
            string debugProxy = proxy;

            try
            {
                string response;
                lock (LockObject)
                {
                    string proxyString = ParseProxy(project, proxy, logger);
                    response = ZennoPoster.HTTP.Request(
                        ZennoLab.InterfacesLibrary.Enums.Http.HttpMethod.POST,
                        url,
                        body,
                        "application/json",
                        proxyString,
                        "UTF-8",
                        ResponceType.BodyOnly,
                        deadline * 1000,
                        "",
                        project.Profile.UserAgent,
                        true,
                        5,
                        headers,
                        "",
                        false,
                        false,
                        project.Profile.CookieContainer);
                }

                //if (log) logger.Send($"body sent: [{body}]");
                if (parseJson) ParseJson(project, response, logger);
                if (log) logger.Send($"response: [{response}]");
                return response.Trim();
            }
            catch (Exception e)
            {
                logger.Send($"!W RequestErr: [{e.Message}] url:[{url}] (proxy: [{debugProxy})]");
                if (throwOnFail) throw;
                return string.Empty;
            }
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

                if (proxyString.Contains("@")) // Proxy with authorization (login:pass@proxy:port)
                {
                    string[] parts = proxyString.Split('@');
                    string credentials = parts[0];
                    string proxyHost = parts[1];
                    string[] creds = credentials.Split(':');
                    return $"http://{creds[0]}:{creds[1]}@{proxyHost}";
                }
                else // Proxy without authorization (proxy:port)
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

        public static void SetProxy(this IZennoPosterProjectModel project,  Instance instance, string proxyString = null)
        {

            string proxy = ParseProxy(project, proxyString);

            if (string.IsNullOrEmpty(proxy)) throw new Exception("!W EMPTY Proxy");
            long uTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string ipLocal = project.GET($"http://api.ipify.org/");

            while (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - uTime < 60)
            {
                instance.SetProxy(proxy, true, true, true, true); Thread.Sleep(2000);
                string ipProxy = project.GET($"http://api.ipify.org/", proxy);
                project.log($"local:[{ipLocal}]?proxyfied:[{ipProxy}]");
                project.Variables["ip"].Value = ipProxy;
                project.Variables["proxy"].Value = proxy;
                if (ipLocal != ipProxy) return;
            }
            project.log("!W badProxy");
            throw new Exception("!W badProxy");
        }
    }
}



