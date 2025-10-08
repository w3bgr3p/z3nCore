using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class NetHttp
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;
        private readonly bool _logShow;

        public NetHttp(IZennoPosterProjectModel project, bool log = false)
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
            {
                return null;
            }
            if (proxyString == "+")
                proxyString = _project.SqlGet("proxy", "_instance");
            try
            {
                WebProxy proxy = new WebProxy();

                if (proxyString.Contains("//")) proxyString = proxyString.Split('/')[2];

                if (proxyString.Contains("@")) // Прокси с авторизацией (login:pass@proxy:port)
                {
                    string[] parts = proxyString.Split('@');
                    string credentials = parts[0];
                    string proxyHost = parts[1];

                    proxy.Address = new Uri("http://" + proxyHost);
                    string[] creds = credentials.Split(':');
                    proxy.Credentials = new NetworkCredential(creds[0], creds[1]);

                }
                else // Прокси без авторизации (proxy:port)
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
                "authority",    // HTTP/2 псевдо-заголовок :authority
                "method",       // HTTP/2 псевдо-заголовок :method  
                "path",         // HTTP/2 псевдо-заголовок :path
                "scheme",       // HTTP/2 псевдо-заголовок :scheme
                "host",         // Автоматически устанавливается HttpClient
                "content-length", // Автоматически вычисляется
                "connection",   // Управляется HttpClient
                "upgrade",      // Управляется HttpClient
                "proxy-connection", // Управляется HttpClient
                "transfer-encoding" // Управляется HttpClient
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
        private bool IsRestrictedHeader(string headerName)
        {
            var restrictedHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // HTTP/2 псевдо-заголовки
                "authority", "method", "path", "scheme",
        
                // Заголовки, управляемые HttpClient
                "host", "content-length", "connection", "upgrade", 
                "proxy-connection", "transfer-encoding",
        
                // Заголовки контента для GET-запросов
                "content-type", "content-encoding", "content-language",
        
                // Другие проблемные заголовки
                "expect", "if-modified-since", "range",// "referer"
            };
    
            return restrictedHeaders.Contains(headerName);
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
        public string GET(
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

                    // Добавляем User-Agent отдельно
                    client.DefaultRequestHeaders.Add("User-Agent", _project.Profile.UserAgent);
                    debugHeaders += $"User-Agent: {_project.Profile.UserAgent}\n";

                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            try
                            {
                                // Пропускаем проблемные заголовки
                                if (IsRestrictedHeader(header.Key))
                                {
                                    _logger.Send($"Skipping restricted header: {header.Key}");
                                    continue;
                                }

                                // Специальная обработка для cookie
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

                    HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult();
                    
                    // Сохраняем код статуса для обработки ошибок
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

                    string result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
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

        public string POST(
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
                    var content = new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json");

                    var requestHeaders = BuildHeaders(headers);

                    foreach (var header in requestHeaders)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                        debugHeaders += $"{header.Key}: {header.Value}; ";
                    }
                    debugHeaders += "Content-Type: application/json; charset=UTF-8; ";

                    _logger.Send(body);

                    HttpResponseMessage response = client.PostAsync(url, content).GetAwaiter().GetResult();
                    
                    // Сохраняем код статуса для обработки ошибок
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

                    string result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
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
        
        
        
        public string GET_(
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

                    // Добавляем User-Agent отдельно
                    client.DefaultRequestHeaders.Add("User-Agent", _project.Profile.UserAgent);
                    debugHeaders += $"User-Agent: {_project.Profile.UserAgent}\n";

                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            try
                            {
                                // Пропускаем проблемные заголовки
                                if (IsRestrictedHeader(header.Key))
                                {
                                    _logger.Send($"Skipping restricted header: {header.Key}");
                                    continue;
                                }

                                // Специальная обработка для cookie
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

                    HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();

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

                    string result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (parse) ParseJson(result);
                    _logger.Send(result);
                    return result.Trim();
                }
            }
            catch (HttpRequestException e)
            {
                _logger.Send($"ErrFromServer: [{e.Message}] \nurl:[{url}]  \nheaders: [{debugHeaders}]");
                if (throwOnFail) throw;
                return e.Message.Replace("Response status code does not indicate success:", "").Trim('.').Trim();
            }
            catch (Exception e)
            {
                _logger.Send($"!W [GET] ErrSending: [{e.Message}] \nurl:[{url}]  \nheaders: [{debugHeaders}]");
                if (throwOnFail) throw;
                return string.Empty;
            }
        }

        public string POST_(
            string url,
            string body,
            string proxyString = "",
            Dictionary<string, string> headers = null,
            bool parse = false,
            int deadline = 15,
            [CallerMemberName] string callerName = "",
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
                        var content = new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json");

                        var requestHeaders = BuildHeaders(headers);

                        foreach (var header in requestHeaders)
                        {
                            client.DefaultRequestHeaders.Add(header.Key, header.Value);
                            debugHeaders += $"{header.Key}: {header.Value}; ";
                        }
                        debugHeaders += "Content-Type: application/json; charset=UTF-8; ";

                        _logger.Send(body);

                        HttpResponseMessage response = client.PostAsync(url, content).GetAwaiter().GetResult();
                        response.EnsureSuccessStatusCode();

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

                        string result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        _logger.Send(result);
                        if (parse) ParseJson(result);
                        return result.Trim();
                    }
                }
                catch (HttpRequestException e)
                {
                    _logger.Send($"[POST] SERVER Err: [{e.Message}] url:[{url}] (proxy: {(proxyString)}), headers: [{debugHeaders.Trim()}]");
                    if (throwOnFail) throw;
                    return e.Message.Replace("Response status code does not indicate success:", "").Trim('.').Trim();
                }
                catch (Exception e)
                {
                    _logger.Send($"!W [POST] RequestErr: [{e.Message}] url:[{url}] (proxy: {(proxyString)}) headers: [{debugHeaders.Trim()}]");
                    if (throwOnFail) throw;
                    return string.Empty;
                }
            }


        public string PUT(
            string url,
            string body = "",
            string proxyString = "",
            Dictionary<string, string> headers = null,
            bool parse = false,
            [CallerMemberName] string callerName = "")
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
                    var content = string.IsNullOrEmpty(body) ? null : new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json");

                    StringBuilder headersString = new StringBuilder();
                    headersString.AppendLine("[debugRequestHeaders]:");

                    string defaultUserAgent = _project.Profile.UserAgent;
                    if (headers == null || !headers.ContainsKey("User-Agent"))
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", defaultUserAgent);
                        headersString.AppendLine($"User-Agent: {defaultUserAgent} (default)");
                    }

                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            client.DefaultRequestHeaders.Add(header.Key, header.Value);
                            headersString.AppendLine($"{header.Key}: {header.Value}");
                        }
                    }

                    if (content != null)
                    {
                        headersString.AppendLine($"Content-Type: application/json; charset=UTF-8");
                        _logger.Send(body);
                    }

                    HttpResponseMessage response = client.PutAsync(url, content).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();

                    StringBuilder responseHeadersString = new StringBuilder();
                    responseHeadersString.AppendLine("[debugResponseHeaders]:");
                    foreach (var header in response.Headers)
                    {
                        var value = string.Join(", ", header.Value);
                        responseHeadersString.AppendLine($"{header.Key}: {value}");
                    }

                    string cookies = "";
                    if (response.Headers.TryGetValues("Set-Cookie", out var cookieValues))
                    {
                        cookies = cookieValues.Aggregate((a, b) => a + "; " + b);
                        _logger.Send("Set-Cookie found: {cookies}");
                    }

                    try
                    {
                        _project.Variables["debugCookies"].Value = cookies;
                    }
                    catch { }

                    string result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
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
        
        public string DELETE(
            string url,
            string proxyString = "",
            Dictionary<string, string> headers = null,
            [CallerMemberName] string callerName = "")
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

                    StringBuilder headersString = new StringBuilder();
                    headersString.AppendLine("[debugRequestHeaders]:");

                    string defaultUserAgent = _project.Profile.UserAgent;
                    if (headers == null || !headers.ContainsKey("User-Agent"))
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", defaultUserAgent);
                        headersString.AppendLine($"User-Agent: {defaultUserAgent} (default)");
                    }

                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            client.DefaultRequestHeaders.Add(header.Key, header.Value);
                            headersString.AppendLine($"{header.Key}: {header.Value}");
                            debugHeaders += $"{header.Key}: {header.Value}";
                        }
                    }

                    HttpResponseMessage response = client.DeleteAsync(url).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();

                    StringBuilder responseHeadersString = new StringBuilder();
                    responseHeadersString.AppendLine("[debugResponseHeaders]:");
                    foreach (var header in response.Headers)
                    {
                        var value = string.Join(", ", header.Value);
                        responseHeadersString.AppendLine($"{header.Key}: {value}");
                    }

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

                    string result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    _logger.Send(result);
                    return result.Trim();
                }
            }
            catch (HttpRequestException e)
            {
                _logger.Send($"!W [DELETE] RequestErr: [{e.Message}] url:[{url}] (proxy: {proxyString}), Headers\n{debugHeaders.Trim()}");
                return e.Message;
            }
            catch (Exception e)
            {
                _logger.Send($"!W [DELETE] UnknownErr: [{e.Message}] url:[{url}] (proxy: {proxyString})");
                return $"Ошибка: {e.Message}";
            }
        }


        public bool CheckProxy(string proxyString = null)
        {

            if (string.IsNullOrEmpty(proxyString))
                proxyString = _project.SqlGet("proxy", "_instance");

            //WebProxy proxy = ParseProxy(proxyString);

            string ipLocal = GET("http://api.ipify.org/", null);
            string ipProxified = GET("http://api.ipify.org/", proxyString);

            //_logger.Send($"ipLocal: {ipLocal}, ipProxified: {ipProxified}");

            if (ipProxified != ipLocal)
            {
                _logger.Send($"proxy `validated: {ipProxified}");
                _project.Var("proxy", proxyString);
                return true;
            }
            else if (ipProxified.StartsWith("Ошибка") || ipProxified == "Прокси не настроен")
            {
                _logger.Send($"!W proxy error: {ipProxified}");

            }
            else if (ipLocal == ipProxified)
            {
                _logger.Send($"!W ip still same. ipLocal: [{ipLocal}], ipProxified: [{ipProxified}]. Proxy was not applyed");
            }
            return false;
        }

        public bool ProxySet(Instance instance, string proxyString = null)
        {

            if (string.IsNullOrEmpty(proxyString))
                proxyString = _project.SqlGet("proxy", "_instance");


            string ipLocal = GET("http://api.ipify.org/", null);
            string ipProxified = GET("http://api.ipify.org/", proxyString);

            if (string.IsNullOrEmpty(ipProxified) || !System.Net.IPAddress.TryParse(ipProxified, out _))
            {
                _logger.Send($"!W proxy error: Invalid or empty IP [{ipProxified}]");
                return false;
            }

            if (ipProxified != ipLocal)
            {
                _logger.Send($"proxy `validated: {ipProxified}");
                _project.Var("proxy", proxyString);
                instance.SetProxy(proxyString, true, true, true, true);
                return true;
            }
            else if (ipProxified.StartsWith("Ошибка") || ipProxified == "Proxy not Set")
            {
                _logger.Send($"!W proxy error: {ipProxified}");
            }
            else if (ipLocal == ipProxified)
            {
                _logger.Send($"!W ip still same. ipLocal: [{ipLocal}], ipProxified: [{ipProxified}]. Proxy was not applyed");
            }
            return false;
        }
    }

    public static partial class ProjectExtensions
    {
        private static Dictionary<string, string> HeadersConvert(string[] headersArray)
        {
            var forbiddenHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "authority",    // HTTP/2 псевдо-заголовок :authority
                "method",       // HTTP/2 псевдо-заголовок :method  
                "path",         // HTTP/2 псевдо-заголовок :path
                "scheme",       // HTTP/2 псевдо-заголовок :scheme
                "host",         // Автоматически устанавливается HttpClient
                "content-length", // Автоматически вычисляется
                "connection",   // Управляется HttpClient
                "upgrade",      // Управляется HttpClient
                "proxy-connection", // Управляется HttpClient
                "transfer-encoding" // Управляется HttpClient
            };

            var adaptedHeaders = new Dictionary<string, string>();

            foreach (var header in headersArray)
            {
                // Пропускаем пустые строки
                if (string.IsNullOrWhiteSpace(header)) continue;
        
                // Пропускаем HTTP/2 псевдо-заголовки (начинаются с :)
                if (header.StartsWith(":")) continue;

                // Разделяем на ключ и значение (только по первому двоеточию)
                var colonIndex = header.IndexOf(':');
                if (colonIndex == -1) continue; // Нет двоеточия - пропускаем

                var key = header.Substring(0, colonIndex).Trim();
                var value = header.Substring(colonIndex + 1).Trim();

                // Проверяем, не входит ли в список запрещенных
                if (forbiddenHeaders.Contains(key)) continue;

                // Добавляем заголовок (если ключ уже есть - перезаписываем)
                adaptedHeaders[key] = value;
            }

            return adaptedHeaders;
        }

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
        
        public static string NetPost(this IZennoPosterProjectModel project, string url,
            string body,
            string proxyString = "",
            string[] headers = null,
            bool parse = false,
            int deadline = 15,
            bool thrw = false)
        {
            var headersDic = HeadersConvert(headers);
            return new NetHttp(project).POST(url, body, proxyString, headersDic, parse, deadline, throwOnFail:thrw);
        }
        
    }
    
}
