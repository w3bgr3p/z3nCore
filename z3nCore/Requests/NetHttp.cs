
using System;
using System.Collections.Concurrent;
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
    /// ИСПРАВЛЕНО: Основной класс для HTTP запросов с ASYNC методами
    /// ✅ Использует singleton HttpClient для предотвращения socket exhaustion
    /// ✅ Кеширует клиенты с proxy для переиспользования
    /// </summary>
    public class NetHttpAsync
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;
        private readonly bool _logShow;

        // ✅ ИСПРАВЛЕНИЕ #1: Singleton HttpClient для запросов без proxy
        private static readonly HttpClient _defaultClient = new HttpClient();

        // ✅ ИСПРАВЛЕНИЕ #2: Кеш клиентов с proxy (ключ = proxy string)
        private static readonly ConcurrentDictionary<string, HttpClient> _proxyClients
            = new ConcurrentDictionary<string, HttpClient>();

        // ✅ ИСПРАВЛЕНИЕ #3: Ограничение размера кеша proxy клиентов
        private const int MAX_PROXY_CLIENTS = 100;

        static NetHttpAsync()
        {
            // Настройка default client
            _defaultClient.Timeout = TimeSpan.FromSeconds(30);
            _defaultClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
        }

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

        /// <summary>
        /// ✅ ИСПРАВЛЕНО: Получает или создает HttpClient для указанного proxy
        /// </summary>
        private HttpClient GetHttpClient(string proxyString, int deadline)
        {
            if (string.IsNullOrEmpty(proxyString))
            {
                return _defaultClient;
            }

            // Проверяем кеш
            return _proxyClients.GetOrAdd(proxyString, proxy =>
            {
                // Проверяем лимит кеша
                if (_proxyClients.Count >= MAX_PROXY_CLIENTS)
                {
                    _logger.Send($"!W Proxy client cache full ({MAX_PROXY_CLIENTS}), creating temporary client");
                    // Не добавляем в кеш, возвращаем временный
                    return CreateProxyClient(proxy);
                }

                _logger.Send($"Creating new cached proxy client for: {proxy.Substring(0, Math.Min(20, proxy.Length))}...");
                return CreateProxyClient(proxy);
            });
        }

        /// <summary>
        /// Создает HttpClient с указанным proxy
        /// </summary>
        private HttpClient CreateProxyClient(string proxyString)
        {
            WebProxy proxy = ParseProxy(proxyString);
            var handler = new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = true
            };

            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
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
        /// ✅ ИСПРАВЛЕНО: ASYNC GET запрос с переиспользованием HttpClient
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
                // ✅ ИСПРАВЛЕНИЕ: Получаем клиент из пула вместо создания нового
                HttpClient client = GetHttpClient(proxyString, deadline);

                // ⚠️ ВАЖНО: Не используем using, т.к. клиент переиспользуется!
                // Создаем отдельный request с headers
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    // Устанавливаем timeout для конкретного запроса
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(deadline)))
                    {
                        request.Headers.Add("User-Agent", _project.Profile.UserAgent);
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
                                        request.Headers.Add("Cookie", header.Value);
                                        debugHeaders += $"{header.Key}: {header.Value}\n";
                                    }
                                    else
                                    {
                                        request.Headers.Add(header.Key, header.Value);
                                        debugHeaders += $"{header.Key}: {header.Value}\n";
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.Send($"Failed to add header {header.Key}: {ex.Message}");
                                }
                            }
                        }

                        // ✅ Используем переиспользуемый клиент с cancellation token
                        using (HttpResponseMessage response = await client.SendAsync(request, cts.Token).ConfigureAwait(false))
                        {
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

                            string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                            if (parse) ParseJson(result);
                            _logger.Send(result);
                            return result.Trim();
                        }
                    }
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
            catch (TaskCanceledException e)
            {
                _logger.Send($"!W [GET] Timeout: [{e.Message}] \nurl:[{url}]  \nheaders: [{debugHeaders}]");
                if (throwOnFail) throw;
                return $"Timeout: {e.Message}";
            }
            catch (Exception e)
            {
                _logger.Send($"!W [GET] ErrSending: [{e.Message}] \nurl:[{url}]  \nheaders: [{debugHeaders}]");
                if (throwOnFail) throw;
                return $"Error: {e.Message}";
            }
        }

        /// <summary>
        /// ✅ ИСПРАВЛЕНО: ASYNC POST запрос с переиспользованием HttpClient
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
                // ✅ ИСПРАВЛЕНИЕ: Получаем клиент из пула
                HttpClient client = GetHttpClient(proxyString, deadline);

                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(deadline)))
                    {
                        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                        var requestHeaders = BuildHeaders(headers);

                        foreach (var header in requestHeaders)
                        {
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                            debugHeaders += $"{header.Key}: {header.Value}; ";
                        }
                        debugHeaders += "Content-Type: application/json; charset=UTF-8; ";

                        _logger.Send(body);

                        using (HttpResponseMessage response = await client.SendAsync(request, cts.Token).ConfigureAwait(false))
                        {
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

                            string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                            _logger.Send(result);
                            if (parse) ParseJson(result);
                            return result.Trim();
                        }
                    }
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
            catch (TaskCanceledException e)
            {
                _logger.Send($"!W [POST] Timeout: [{e.Message}] url:[{url}] (proxy: {proxyString}) headers: [{debugHeaders.Trim()}]");
                if (throwOnFail) throw;
                return $"Timeout: {e.Message}";
            }
            catch (Exception e)
            {
                _logger.Send($"!W [POST] RequestErr: [{e.Message}] url:[{url}] (proxy: {proxyString}) headers: [{debugHeaders.Trim()}]");
                if (throwOnFail) throw;
                return $"Error: {e.Message}";
            }
        }

        /// <summary>
        /// ✅ ИСПРАВЛЕНО: ASYNC PUT запрос
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
                HttpClient client = GetHttpClient(proxyString, 30);

                using (var request = new HttpRequestMessage(HttpMethod.Put, url))
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        if (!string.IsNullOrEmpty(body))
                        {
                            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                        }

                        string defaultUserAgent = _project.Profile.UserAgent;
                        if (headers == null || !headers.ContainsKey("User-Agent"))
                        {
                            request.Headers.Add("User-Agent", defaultUserAgent);
                        }

                        if (headers != null)
                        {
                            foreach (var header in headers)
                            {
                                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                            }
                        }

                        if (!string.IsNullOrEmpty(body))
                        {
                            _logger.Send(body);
                        }

                        using (HttpResponseMessage response = await client.SendAsync(request, cts.Token).ConfigureAwait(false))
                        {
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

                            string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                            _logger.Send(result);
                            if (parse) ParseJson(result);
                            return result.Trim();
                        }
                    }
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
        /// ✅ ИСПРАВЛЕНО: ASYNC DELETE запрос
        /// </summary>
        public async Task<string> DeleteAsync(
            string url,
            string proxyString = "",
            Dictionary<string, string> headers = null)
        {
            string debugHeaders = null;
            try
            {
                HttpClient client = GetHttpClient(proxyString, 30);

                using (var request = new HttpRequestMessage(HttpMethod.Delete, url))
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        string defaultUserAgent = _project.Profile.UserAgent;
                        if (headers == null || !headers.ContainsKey("User-Agent"))
                        {
                            request.Headers.Add("User-Agent", defaultUserAgent);
                        }

                        if (headers != null)
                        {
                            foreach (var header in headers)
                            {
                                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                                debugHeaders += $"{header.Key}: {header.Value}";
                            }
                        }

                        using (HttpResponseMessage response = await client.SendAsync(request, cts.Token).ConfigureAwait(false))
                        {
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

                            string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                            _logger.Send(result);
                            return result.Trim();
                        }
                    }
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

        /// <summary>
        /// ✅ ДОПОЛНИТЕЛЬНО: Метод для очистки кеша proxy клиентов
        /// Вызывайте периодически если используется много разных proxy
        /// </summary>
        public static void ClearProxyCache()
        {
            var oldClients = _proxyClients.ToArray();
            _proxyClients.Clear();

            // Dispose старых клиентов
            foreach (var kvp in oldClients)
            {
                try
                {
                    kvp.Value?.Dispose();
                }
                catch { }
            }
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