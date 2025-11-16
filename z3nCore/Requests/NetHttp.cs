using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    /// <summary>
    /// Основной класс для HTTP запросов с ASYNC методами
    /// Используй этот класс если можешь работать с async/await
    /// </summary>
    public class NetHttpAsync
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;
        private readonly bool _logShow;

        public NetHttpAsync(IZennoPosterProjectModel project, bool log = false)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _logShow = log;
            _logger = new Logger(project, log: log, classEmoji: "↑↓");
        }

        private void Log(string message, [CallerMemberName] string callerName = "", bool forceLog = false)
        {
            if (!_logShow && !forceLog) return;
            _logger.Send($"({callerName}) [{message}]");
        }

        private void ParseJson(string json)
        {
            try
            {
                _project.Json.FromString(json);
            }
            catch (Exception ex)
            {
                _logger.Send($"[!W {ex.Message}] [{json}]");
            }
        }

        private WebProxy ParseProxy(string proxyString, [CallerMemberName] string callerName = "")
        {
            if (string.IsNullOrEmpty(proxyString))
                return null;

            if (proxyString == "+")
                proxyString = _project.SqlGet("proxy", "_instance");

            try
            {
                WebProxy proxy = new WebProxy();

                if (proxyString.Contains("//"))
                    proxyString = proxyString.Split('/')[2];

                if (proxyString.Contains("@"))
                {
                    string[] parts = proxyString.Split('@');
                    string credentials = parts[0];
                    string proxyHost = parts[1];

                    proxy.Address = new Uri("http://" + proxyHost);
                    string[] creds = credentials.Split(':');
                    proxy.Credentials = new NetworkCredential(creds[0], creds[1]);
                }
                else
                {
                    proxy.Address = new Uri("http://" + proxyString);
                }

                return proxy;
            }
            catch (Exception e)
            {
                _logger.Send(e.Message + $"[{proxyString}]");
                return null;
            }
        }

        private bool IsRestrictedHeader(string headerName)
        {
            var restrictedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "authority", "method", "path", "scheme",
                "host", "content-length", "connection", "upgrade",
                "proxy-connection", "transfer-encoding",
                "content-type", "content-encoding", "content-language",
                "expect", "if-modified-since", "range"
            };

            return restrictedHeaders.Contains(headerName);
        }

        /// <summary>
        /// ASYNC GET запрос
        /// </summary>
        public async Task<string> GetAsync(
            string url,
            string proxyString = "",
            Dictionary<string, string> headers = null,
            bool parse = false,
            int deadline = 15,
            bool throwOnFail = false)
        {
            string debugHeaders = "";
            try
            {
                WebProxy proxy = ParseProxy(proxyString);
                var handler = new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = proxy != null
                };

                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(deadline);
                    client.DefaultRequestHeaders.Add("User-Agent", _project.Profile.UserAgent);
                    debugHeaders += $"User-Agent: {_project.Profile.UserAgent}\n";

                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            try
                            {
                                if (IsRestrictedHeader(header.Key))
                                {
                                    _logger.Send($"Skipping restricted header: {header.Key}");
                                    continue;
                                }

                                if (header.Key.ToLower() == "cookie")
                                {
                                    client.DefaultRequestHeaders.Add("Cookie", header.Value);
                                    debugHeaders += $"{header.Key}: {header.Value}\n";
                                }
                                else
                                {
                                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
                                    debugHeaders += $"{header.Key}: {header.Value}\n";
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.Send($"Failed to add header {header.Key}: {ex.Message}");
                            }
                        }
                    }

                    // ✅ ПРАВИЛЬНО: async вызов
                    HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);

                    int statusCode = (int)response.StatusCode;

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorMessage = $"{statusCode} !!! {response.ReasonPhrase}";
                        _logger.Send($"ErrFromServer: [{errorMessage}] \nurl:[{url}]  \nheaders: [{debugHeaders}]");
                        if (throwOnFail)
                        {
                            response.EnsureSuccessStatusCode();
                        }
                        return errorMessage;
                    }

                    string responseHeaders = string.Join("; ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));

                    string cookies = "";
                    if (response.Headers.TryGetValues("Set-Cookie", out var cookieValues))
                    {
                        cookies = string.Join("; ", cookieValues);
                        _logger.Send($"Set-Cookie found: {cookies}");
                    }

                    try
                    {
                        _project.Variables["debugCookies"].Value = cookies;
                    }
                    catch { }

                    // ✅ ПРАВИЛЬНО: async чтение
                    string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    
                    if (parse) ParseJson(result);
                    _logger.Send(result);
                    return result.Trim();
                }
            }
            catch (HttpRequestException e)
            {
                string errorMessage = e.Message.Contains("Response status code")
                    ? e.Message.Replace("Response status code does not indicate success:", "").Trim('.').Trim()
                    : e.Message;
                _logger.Send($"ErrFromServer: [{errorMessage}] \nurl:[{url}]  \nheaders: [{debugHeaders}]");
                if (throwOnFail) throw;
                return errorMessage;
            }
            catch (Exception e)
            {
                _logger.Send($"!W [GET] ErrSending: [{e.Message}] \nurl:[{url}]  \nheaders: [{debugHeaders}]");
                if (throwOnFail) throw;
                return $"Error: {e.Message}";
            }
        }

        /// <summary>
        /// ASYNC POST запрос
        /// </summary>
        public async Task<string> PostAsync(
            string url,
            string body,
            string proxyString = "",
            Dictionary<string, string> headers = null,
            bool parse = false,
            int deadline = 15,
            bool throwOnFail = false)
        {
            string debugHeaders = "";
            try
            {
                WebProxy proxy = ParseProxy(proxyString);
                var handler = new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = proxy != null
                };

                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(deadline);
                    var content = new StringContent(body, Encoding.UTF8, "application/json");

                    var requestHeaders = BuildHeaders(headers);

                    foreach (var header in requestHeaders)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                        debugHeaders += $"{header.Key}: {header.Value}; ";
                    }
                    debugHeaders += "Content-Type: application/json; charset=UTF-8; ";

                    _logger.Send(body);

                    // ✅ ПРАВИЛЬНО: async вызов
                    HttpResponseMessage response = await client.PostAsync(url, content).ConfigureAwait(false);

                    int statusCode = (int)response.StatusCode;

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorMessage = $"{statusCode} !!! {response.ReasonPhrase}";
                        _logger.Send($"[POST] SERVER Err: [{errorMessage}] url:[{url}] (proxy: {proxyString}), headers: [{debugHeaders.Trim()}]");
                        if (throwOnFail)
                        {
                            response.EnsureSuccessStatusCode();
                        }
                        return errorMessage;
                    }

                    string responseHeaders = string.Join("; ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));

                    string cookies = "";
                    if (response.Headers.TryGetValues("Set-Cookie", out var cookieValues))
                    {
                        cookies = string.Join("; ", cookieValues);
                        _logger.Send($"Set-Cookie found: {cookies}");
                    }

                    try
                    {
                        _project.Variables["debugCookies"].Value = cookies;
                    }
                    catch { }

                    // ✅ ПРАВИЛЬНО: async чтение
                    string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    
                    _logger.Send(result);
                    if (parse) ParseJson(result);
                    return result.Trim();
                }
            }
            catch (HttpRequestException e)
            {
                string errorMessage = e.Message.Contains("Response status code")
                    ? e.Message.Replace("Response status code does not indicate success:", "").Trim('.').Trim()
                    : e.Message;
                _logger.Send($"[POST] SERVER Err: [{errorMessage}] url:[{url}] (proxy: {proxyString}), headers: [{debugHeaders.Trim()}]");
                if (throwOnFail) throw;
                return errorMessage;
            }
            catch (Exception e)
            {
                _logger.Send($"!W [POST] RequestErr: [{e.Message}] url:[{url}] (proxy: {proxyString}) headers: [{debugHeaders.Trim()}]");
                if (throwOnFail) throw;
                return $"Error: {e.Message}";
            }
        }

        /// <summary>
        /// ASYNC PUT запрос
        /// </summary>
        public async Task<string> PutAsync(
            string url,
            string body = "",
            string proxyString = "",
            Dictionary<string, string> headers = null,
            bool parse = false)
        {
            try
            {
                WebProxy proxy = ParseProxy(proxyString);
                var handler = new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = proxy != null
                };

                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    var content = string.IsNullOrEmpty(body) ? null : new StringContent(body, Encoding.UTF8, "application/json");

                    string defaultUserAgent = _project.Profile.UserAgent;
                    if (headers == null || !headers.ContainsKey("User-Agent"))
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", defaultUserAgent);
                    }

                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            client.DefaultRequestHeaders.Add(header.Key, header.Value);
                        }
                    }

                    if (content != null)
                    {
                        _logger.Send(body);
                    }

                    // ✅ ПРАВИЛЬНО: async вызов
                    HttpResponseMessage response = await client.PutAsync(url, content).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();

                    string cookies = "";
                    if (response.Headers.TryGetValues("Set-Cookie", out var cookieValues))
                    {
                        cookies = cookieValues.Aggregate((a, b) => a + "; " + b);
                        _logger.Send($"Set-Cookie found: {cookies}");
                    }

                    try
                    {
                        _project.Variables["debugCookies"].Value = cookies;
                    }
                    catch { }

                    // ✅ ПРАВИЛЬНО: async чтение
                    string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    
                    _logger.Send(result);
                    if (parse) ParseJson(result);
                    return result.Trim();
                }
            }
            catch (HttpRequestException e)
            {
                _logger.Send($"!W RequestErr: [{e.Message}] url:[{url}] (proxy: {(proxyString != "" ? proxyString : "noProxy")})");
                return e.Message;
            }
            catch (Exception e)
            {
                _logger.Send($"!W UnknownErr: [{e.Message}] url:[{url}] (proxy: {(proxyString != "" ? proxyString : "noProxy")})");
                return $"Ошибка: {e.Message}";
            }
        }

        /// <summary>
        /// ASYNC DELETE запрос
        /// </summary>
        public async Task<string> DeleteAsync(
            string url,
            string proxyString = "",
            Dictionary<string, string> headers = null)
        {
            string debugHeaders = null;
            try
            {
                WebProxy proxy = ParseProxy(proxyString);
                var handler = new HttpClientHandler
                {
                    Proxy = proxy,
                    UseProxy = proxy != null
                };

                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    string defaultUserAgent = _project.Profile.UserAgent;
                    if (headers == null || !headers.ContainsKey("User-Agent"))
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", defaultUserAgent);
                    }

                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            client.DefaultRequestHeaders.Add(header.Key, header.Value);
                            debugHeaders += $"{header.Key}: {header.Value}";
                        }
                    }

                    // ✅ ПРАВИЛЬНО: async вызов
                    HttpResponseMessage response = await client.DeleteAsync(url).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();

                    string cookies = "";
                    if (response.Headers.TryGetValues("Set-Cookie", out var cookieValues))
                    {
                        cookies = cookieValues.Aggregate((a, b) => a + "; " + b);
                        _logger.Send($"Set-Cookie found: {cookies}");
                    }

                    try
                    {
                        _project.Variables["debugCookies"].Value = cookies;
                    }
                    catch { }

                    // ✅ ПРАВИЛЬНО: async чтение
                    string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    
                    _logger.Send(result);
                    return result.Trim();
                }
            }
            catch (HttpRequestException e)
            {
                _logger.Send($"!W [DELETE] RequestErr: [{e.Message}] url:[{url}] (proxy: {proxyString}), Headers\n{debugHeaders?.Trim()}");
                return e.Message;
            }
            catch (Exception e)
            {
                _logger.Send($"!W [DELETE] UnknownErr: [{e.Message}] url:[{url}] (proxy: {proxyString})");
                return $"Ошибка: {e.Message}";
            }
        }

        private Dictionary<string, string> BuildHeaders(Dictionary<string, string> inputHeaders = null)
        {
            var defaultHeaders = new Dictionary<string, string>
            {
                { "User-Agent", _project.Profile.UserAgent },
            };

            if (inputHeaders == null || inputHeaders.Count == 0)
            {
                return defaultHeaders;
            }

            var forbiddenHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "authority", "method", "path", "scheme",
                "host", "content-length", "connection", "upgrade",
                "proxy-connection", "transfer-encoding"
            };

            var mergedHeaders = new Dictionary<string, string>(defaultHeaders);

            foreach (var header in inputHeaders)
            {
                if (!forbiddenHeaders.Contains(header.Key))
                {
                    mergedHeaders[header.Key] = header.Value;
                }
                else
                {
                    _logger.Send($"Skipping forbidden header: {header.Key}");
                }
            }

            return mergedHeaders;
        }
    }

    /// <summary>
    /// СИНХРОННЫЕ ОБЕРТКИ для ZennoPoster Project (не поддерживает async)
    /// ⚠️ ВНИМАНИЕ: Используй NetHttpAsync если можешь работать с async/await
    /// Этот класс - только адаптер для legacy кода
    /// </summary>
    public class NetHttp
    {
        private readonly NetHttpAsync _asyncClient;

        public NetHttp(IZennoPosterProjectModel project, bool log = false)
        {
            _asyncClient = new NetHttpAsync(project, log);
        }

        /// <summary>
        /// Синхронная обертка для GET (для ZennoPoster)
        /// ⚠️ Блокирует поток! Используй NetHttpAsync.GetAsync() если возможно
        /// </summary>
        public string GET(
            string url,
            string proxyString = "",
            Dictionary<string, string> headers = null,
            bool parse = false,
            int deadline = 15,
            bool throwOnFail = false)
        {
            // ✅ Task.Run + ConfigureAwait(false) избегает deadlock
            return Task.Run(async () =>
                await _asyncClient.GetAsync(url, proxyString, headers, parse, deadline, throwOnFail)
                    .ConfigureAwait(false)
            ).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Синхронная обертка для POST (для ZennoPoster)
        /// ⚠️ Блокирует поток! Используй NetHttpAsync.PostAsync() если возможно
        /// </summary>
        public string POST(
            string url,
            string body,
            string proxyString = "",
            Dictionary<string, string> headers = null,
            bool parse = false,
            int deadline = 15,
            bool throwOnFail = false)
        {
            // ✅ Task.Run + ConfigureAwait(false) избегает deadlock
            return Task.Run(async () =>
                await _asyncClient.PostAsync(url, body, proxyString, headers, parse, deadline, throwOnFail)
                    .ConfigureAwait(false)
            ).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Синхронная обертка для PUT (для ZennoPoster)
        /// ⚠️ Блокирует поток! Используй NetHttpAsync.PutAsync() если возможно
        /// </summary>
        public string PUT(
            string url,
            string body = "",
            string proxyString = "",
            Dictionary<string, string> headers = null,
            bool parse = false)
        {
            // ✅ Task.Run + ConfigureAwait(false) избегает deadlock
            return Task.Run(async () =>
                await _asyncClient.PutAsync(url, body, proxyString, headers, parse)
                    .ConfigureAwait(false)
            ).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Синхронная обертка для DELETE (для ZennoPoster)
        /// ⚠️ Блокирует поток! Используй NetHttpAsync.DeleteAsync() если возможно
        /// </summary>
        public string DELETE(
            string url,
            string proxyString = "",
            Dictionary<string, string> headers = null)
        {
            // ✅ Task.Run + ConfigureAwait(false) избегает deadlock
            return Task.Run(async () =>
                await _asyncClient.DeleteAsync(url, proxyString, headers)
                    .ConfigureAwait(false)
            ).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Проверка прокси (синхронная)
        /// </summary>
        public bool CheckProxy(string proxyString = null)
        {
            string ipLocal = GET("http://api.ipify.org/", null);
            string ipProxified = GET("http://api.ipify.org/", proxyString);

            if (ipProxified != ipLocal)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Установка прокси в Instance (синхронная)
        /// </summary>
        public bool ProxySet(Instance instance, string proxyString = null)
        {
            string ipLocal = GET("http://api.ipify.org/", null);
            string ipProxified = GET("http://api.ipify.org/", proxyString);

            if (string.IsNullOrEmpty(ipProxified) || !System.Net.IPAddress.TryParse(ipProxified, out _))
            {
                return false;
            }

            if (ipProxified != ipLocal)
            {
                instance.SetProxy(proxyString, true, true, true, true);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Extension методы для удобного вызова из Project
    /// Остаются синхронными для совместимости с ZennoPoster
    /// </summary>
    public static partial class ProjectExtensions
    {
        private static Dictionary<string, string> HeadersConvert(string[] headersArray)
        {
            var forbiddenHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "authority", "method", "path", "scheme",
                "host", "content-length", "connection", "upgrade",
                "proxy-connection", "transfer-encoding"
            };

            var adaptedHeaders = new Dictionary<string, string>();

            if (headersArray == null) return adaptedHeaders;

            foreach (var header in headersArray)
            {
                if (string.IsNullOrWhiteSpace(header)) continue;
                if (header.StartsWith(":")) continue;

                var colonIndex = header.IndexOf(':');
                if (colonIndex == -1) continue;

                var key = header.Substring(0, colonIndex).Trim();
                var value = header.Substring(colonIndex + 1).Trim();

                if (forbiddenHeaders.Contains(key)) continue;

                adaptedHeaders[key] = value;
            }

            return adaptedHeaders;
        }

        /// <summary>
        /// Extension метод для GET из ZennoPoster Project
        /// </summary>
        public static string NetGet(this IZennoPosterProjectModel project, string url,
            string proxyString = "",
            string[] headers = null,
            bool parse = false,
            int deadline = 15,
            bool thrw = false)
        {
            var headersDic = HeadersConvert(headers);
            return new NetHttp(project).GET(url, proxyString, headersDic, parse, deadline, thrw);
        }

        /// <summary>
        /// Extension метод для POST из ZennoPoster Project
        /// </summary>
        public static string NetPost(this IZennoPosterProjectModel project, string url,
            string body,
            string proxyString = "",
            string[] headers = null,
            bool parse = false,
            int deadline = 15,
            bool thrw = false)
        {
            var headersDic = HeadersConvert(headers);
            return new NetHttp(project).POST(url, body, proxyString, headersDic, parse, deadline, throwOnFail: thrw);
        }
    }
}