using System;
using System.Collections.Generic;
using System.Linq;
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
            bool parse = false,
            bool parseJson = false,
            int deadline = 30,
            bool thrw = false,
            bool useNetHttp = false)  
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (parseJson)
            {
                parse = parseJson;
                project.warn("using obsolete parameter \"parseJson\", change to   \"parse\" ASAP");
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
            bool log = false,
            bool parse = false,
            bool parseJson = false,
            int deadline = 30,
            bool thrw = false,
            bool useNetHttp = false) 
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (parseJson)
            {
                parse = parseJson;
                project.warn("using obsolete parameter \"parseJson\", change to   \"parse\" ASAP");
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
            int deadline,
            Logger logger,
            out int statusCode)
        {
            string fullResponse;
            
            lock (LockObject)
            {
                string proxyString = ParseProxy(project, proxy, logger);

                if (headers == null)
                {
                    try
                    {
                        headers = project.Var("headers").Split('\n');
                    }
                    catch { }
                }

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

            string body;
            ParseResponse(fullResponse, out statusCode, out body);
            return body;
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
        
        private static string ExecutePostViaZennoPoster(
            IZennoPosterProjectModel project,
            string url,
            string body,
            string proxy,
            string[] headers,
            int deadline,
            Logger logger,
            out int statusCode)
        {
            string fullResponse;

            lock (LockObject)
            {
                string proxyString = ParseProxy(project, proxy, logger);
                headers = BuildHeaders(project, headers);

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
                    true,
                    project.Profile.CookieContainer);
            }

            string responseBody;
            ParseResponse(fullResponse, out statusCode, out responseBody);
            return responseBody;
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


        private static Dictionary<string, string> ConvertHeadersToDictionary(string[] headersArray)
        {
            var forbiddenHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "authority", "method", "path", "scheme",
                "host", "content-length", "connection", "upgrade",
                "proxy-connection", "transfer-encoding"
            };

            var headersDic = new Dictionary<string, string>();

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

        /// <summary>
        /// Установить и проверить прокси в Instance
        /// </summary>
        public static void SetProxy(this IZennoPosterProjectModel project, Instance instance, string proxyString = null)
        {
            string proxy = ParseProxy(project, proxyString);

            if (string.IsNullOrEmpty(proxy)) throw new Exception("!W EMPTY Proxy");
            
            long uTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string ipLocal = project.GET($"http://api.ipify.org/");

            while (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - uTime < 60)
            {
                instance.SetProxy(proxy, true, true, true, true);
                Thread.Sleep(2000);
                
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

    public static partial class ProjectExtensions
    {
        // Здесь могут быть дополнительные extension методы
    }
}