

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
            bool throwOnFail = false)
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
                    
                    // Изменено: HeaderAndBody вместо BodyOnly для получения статуса
                    fullResponse = ZennoPoster.HTTP.Request(
                        HttpMethod.GET,
                        url,
                        "",
                        "application/json",
                        proxyString,
                        "UTF-8",
                        ResponceType.HeaderAndBody,  // ← было BodyOnly
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

                // Парсим JSON если нужно и статус успешный
                if (parseJson && statusCode >= 200 && statusCode < 300)
                {
                    ParseJson(project, body, logger);
                }

                // Бросаем исключение если статус плохой и требуется
                if (throwOnFail && (statusCode < 200 || statusCode >= 300))
                {
                    throw new Exception($"HTTP {statusCode}: {body}");
                }

                return body.Trim();
            }
            catch (Exception e)
            {
                logger.Send($"!W RequestErr: [{e.Message}] url:[{url}] (proxy: [{debugProxy})]");
                if (throwOnFail) throw;
                return string.Empty;
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
            bool throwOnFail = false)
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
                    
                    // Изменено: HeaderAndBody вместо BodyOnly для получения статуса
                    fullResponse = ZennoPoster.HTTP.Request(
                        HttpMethod.POST,
                        url,
                        body,
                        "application/json",
                        proxyString,
                        "UTF-8",
                        ResponceType.HeaderAndBody,  // ← было BodyOnly
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
                string responseBody;
                ParseResponse(fullResponse, out statusCode, out responseBody);

                // Логируем статус если включен log
                if (log)
                {
                    LogStatus(logger, statusCode, url, debugProxy);
                    logger.Send($"response: [{responseBody}]");
                }

                // Парсим JSON если нужно и статус успешный
                if (parseJson && statusCode >= 200 && statusCode < 300)
                {
                    ParseJson(project, responseBody, logger);
                }

                // Бросаем исключение если статус плохой и требуется
                if (throwOnFail && (statusCode < 200 || statusCode >= 300))
                {
                    throw new Exception($"HTTP {statusCode}: {responseBody}");
                }

                return responseBody.Trim();
            }
            catch (Exception e)
            {
                logger.Send($"!W RequestErr: [{e.Message}] url:[{url}] (proxy: [{debugProxy})]");
                if (throwOnFail) throw;
                return string.Empty;
            }
        }

        #region Private Helpers

        /// <summary>
        /// Парсит полный ответ и извлекает статус-код и body
        /// </summary>
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

                // Ищем первую строку со статусом "HTTP/1.1 200 OK"
                int firstLineEnd = fullResponse.IndexOf("\r\n");
                if (firstLineEnd == -1)
                {
                    // Нет заголовков - весь ответ это body
                    body = fullResponse.Trim();
                    return;
                }

                string statusLine = fullResponse.Substring(0, firstLineEnd);
                
                // Извлекаем код из "HTTP/1.1 200 OK"
                string[] parts = statusLine.Split(' ');
                if (parts.Length >= 2)
                {
                    int.TryParse(parts[1], out statusCode);
                }

                // Body находится после пустой строки \r\n\r\n
                int bodyStart = fullResponse.IndexOf("\r\n\r\n");
                if (bodyStart != -1)
                {
                    body = fullResponse.Substring(bodyStart + 4).Trim();
                }
            }
            catch
            {
                // Если парсинг провалился - возвращаем весь ответ
                statusCode = 200;
                body = fullResponse.Trim();
            }
        }

        /// <summary>
        /// Логирует HTTP статус
        /// </summary>
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
                    proxyString = project.DbGet("proxy", "_instance");
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

        #endregion

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
}