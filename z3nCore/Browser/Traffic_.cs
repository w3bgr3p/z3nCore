using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    /// <summary>
    /// Работа с трафиком браузера - поиск и извлечение данных из HTTP запросов/ответов
    /// </summary>
    public partial class Traffic
    {
        #region Fields & Constructor

        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;
        private readonly bool _showLog;

        // Внутренний кэш (скрыт от пользователя)
        private List<TrafficElement> _cache;
        private DateTime _cacheTime;
        private const int CACHE_LIFETIME_SECONDS = 2;

        public Traffic(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _showLog = log;
            _logger = new Logger(project, log: log, classEmoji: "🌎");
            _instance.UseTrafficMonitoring = true;
        }

        #endregion

        #region Find Traffic Elements (Поиск элементов трафика)

        /// <summary>
        /// Найти первый элемент трафика по URL (с ожиданием)
        /// </summary>
        /// <param name="url">URL или его часть для поиска</param>
        /// <param name="exactMatch">true = точное совпадение, false = содержит подстроку</param>
        /// <param name="timeoutSeconds">Таймаут ожидания в секундах</param>
        /// <param name="retryDelaySeconds">Задержка между попытками</param>
        public TrafficElement FindTrafficElement(string url, bool exactMatch = false, 
            int timeoutSeconds = 15, int retryDelaySeconds = 1)
        {
            _project.Deadline();
            _instance.UseTrafficMonitoring = true;

            var startTime = DateTime.Now;
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);
            int attemptNumber = 0;

            while (DateTime.Now - startTime < timeout)
            {
                _project.Deadline(timeoutSeconds);
                attemptNumber++;

                if (_showLog) _logger.Send($"Attempt #{attemptNumber} searching URL: {url}");

                var element = SearchInCache(url, exactMatch);
                if (element != null)
                {
                    if (_showLog) _logger.Send($"✓ Found traffic for: {url}");
                    return element;
                }

                Thread.Sleep(1000 * retryDelaySeconds);
            }

            throw new TimeoutException(
                $"Traffic element not found for URL '{url}' within {timeoutSeconds} seconds");
        }

        /// <summary>
        /// Найти все элементы трафика по URL (без ожидания, работает с текущим кэшем)
        /// </summary>
        /// <param name="url">URL или его часть для поиска</param>
        /// <param name="exactMatch">true = точное совпадение, false = содержит подстроку</param>
        public List<TrafficElement> FindAllTrafficElements(string url, bool exactMatch = false)
        {
            UpdateCacheIfNeeded();

            var matches = new List<TrafficElement>();

            foreach (var element in _cache)
            {
                bool isMatch = exactMatch 
                    ? element.Url == url 
                    : element.Url.Contains(url);

                if (isMatch)
                {
                    matches.Add(element);
                }
            }

            if (_showLog) _logger.Send($"Found {matches.Count} traffic elements for: {url}");

            return matches;
        }

        /// <summary>
        /// Получить весь текущий трафик (все элементы)
        /// </summary>
        public List<TrafficElement> GetAllTraffic()
        {
            UpdateCacheIfNeeded();
            return new List<TrafficElement>(_cache);
        }

        #endregion

        #region Get Specific Data (Получение конкретных данных - короткие пути)

        /// <summary>
        /// Получить тело ответа (response body) по URL
        /// </summary>
        public string GetResponseBody(string url, bool exactMatch = false, int timeoutSeconds = 15)
        {
            var element = FindTrafficElement(url, exactMatch, timeoutSeconds);
            return element.ResponseBody;
        }

        /// <summary>
        /// Получить тело запроса (request body) по URL
        /// </summary>
        public string GetRequestBody(string url, bool exactMatch = false, int timeoutSeconds = 15)
        {
            var element = FindTrafficElement(url, exactMatch, timeoutSeconds);
            return element.RequestBody;
        }

        /// <summary>
        /// Получить заголовок из запроса (request header)
        /// </summary>
        public string GetRequestHeader(string url, string headerName, bool exactMatch = false, 
            int timeoutSeconds = 15)
        {
            var element = FindTrafficElement(url, exactMatch, timeoutSeconds);
            return element.GetRequestHeader(headerName);
        }

        /// <summary>
        /// Получить заголовок из ответа (response header)
        /// </summary>
        public string GetResponseHeader(string url, string headerName, bool exactMatch = false, 
            int timeoutSeconds = 15)
        {
            var element = FindTrafficElement(url, exactMatch, timeoutSeconds);
            return element.GetResponseHeader(headerName);
        }

        /// <summary>
        /// Получить все заголовки запроса в виде словаря
        /// </summary>
        public Dictionary<string, string> GetAllRequestHeaders(string url, bool exactMatch = false, 
            int timeoutSeconds = 15)
        {
            var element = FindTrafficElement(url, exactMatch, timeoutSeconds);
            return element.GetAllRequestHeaders();
        }

        /// <summary>
        /// Получить все заголовки ответа в виде словаря
        /// </summary>
        public Dictionary<string, string> GetAllResponseHeaders(string url, bool exactMatch = false, 
            int timeoutSeconds = 15)
        {
            var element = FindTrafficElement(url, exactMatch, timeoutSeconds);
            return element.GetAllResponseHeaders();
        }

        #endregion

        #region Page Actions (Действия со страницей)

        /// <summary>
        /// Перезагрузить страницу и обновить кэш трафика
        /// </summary>
        public Traffic ReloadPage(int delaySeconds = 1)
        {
            _project.Deadline();

            _instance.ActiveTab.MainDocument.EvaluateScript("location.reload(true)");
            if (_instance.ActiveTab.IsBusy) _instance.ActiveTab.WaitDownloading();
            
            Thread.Sleep(1000 * delaySeconds);
            
            ForceRefreshCache();

            return this;
        }

        /// <summary>
        /// Явно обновить кэш трафика (обычно не требуется - обновляется автоматически)
        /// </summary>
        public Traffic RefreshTrafficCache()
        {
            ForceRefreshCache();
            return this;
        }

        #endregion

        #region Internal Cache Management (Внутреннее управление кэшем - скрыто от API)

        private void UpdateCacheIfNeeded()
        {
            bool cacheExpired = _cache == null || 
                                (DateTime.Now - _cacheTime).TotalSeconds > CACHE_LIFETIME_SECONDS;

            if (cacheExpired)
            {
                ForceRefreshCache();
            }
        }

        private void ForceRefreshCache()
        {
            var rawTraffic = _instance.ActiveTab.GetTraffic();
            _cache = new List<TrafficElement>();

            foreach (var item in rawTraffic)
            {
                // Пропускаем OPTIONS запросы
                if (item.Method == "OPTIONS") continue;

                _cache.Add(ConvertToTrafficElement(item));
            }

            _cacheTime = DateTime.Now;

            if (_showLog) _logger.Send($"Cache refreshed: {_cache.Count} elements");
        }

        private TrafficElement SearchInCache(string url, bool exactMatch)
        {
            UpdateCacheIfNeeded();

            foreach (var element in _cache)
            {
                bool isMatch = exactMatch 
                    ? element.Url == url 
                    : element.Url.Contains(url);

                if (isMatch)
                {
                    return element;
                }
            }

            // Не нашли - принудительно обновляем кэш и ищем еще раз
            ForceRefreshCache();

            foreach (var element in _cache)
            {
                bool isMatch = exactMatch 
                    ? element.Url == url 
                    : element.Url.Contains(url);

                if (isMatch)
                {
                    return element;
                }
            }

            return null;
        }

        private TrafficElement ConvertToTrafficElement(dynamic rawItem)
        {
            var responseBody = rawItem.ResponseBody == null
                ? string.Empty
                : Encoding.UTF8.GetString(rawItem.ResponseBody, 0, rawItem.ResponseBody.Length);

            return new TrafficElement(_project)
            {
                Method = rawItem.Method ?? string.Empty,
                StatusCode = rawItem.ResultCode.ToString(),
                Url = rawItem.Url ?? string.Empty,
                ResponseContentType = rawItem.ResponseContentType ?? string.Empty,
                RequestHeaders = rawItem.RequestHeaders ?? string.Empty,
                RequestCookies = rawItem.RequestCookies ?? string.Empty,
                RequestBody = rawItem.RequestBody ?? string.Empty,
                ResponseHeaders = rawItem.ResponseHeaders ?? string.Empty,
                ResponseCookies = rawItem.ResponseCookies ?? string.Empty,
                ResponseBody = responseBody
            };
        }

        #endregion

        #region Nested Class - TrafficElement

        /// <summary>
        /// Один элемент трафика (HTTP запрос + ответ)
        /// </summary>
        public class TrafficElement
        {
            private readonly IZennoPosterProjectModel _project;

            internal TrafficElement(IZennoPosterProjectModel project)
            {
                _project = project;
            }

            // HTTP Request
            public string Method { get; internal set; }
            public string Url { get; internal set; }
            public string RequestHeaders { get; internal set; }
            public string RequestCookies { get; internal set; }
            public string RequestBody { get; internal set; }

            // HTTP Response
            public string StatusCode { get; internal set; }
            public string ResponseContentType { get; internal set; }
            public string ResponseHeaders { get; internal set; }
            public string ResponseCookies { get; internal set; }
            public string ResponseBody { get; internal set; }

            /// <summary>
            /// Распарсить ResponseBody как JSON в project.Json
            /// </summary>
            public TrafficElement ParseResponseBodyAsJson()
            {
                if (!string.IsNullOrEmpty(ResponseBody))
                {
                    _project.Json.FromString(ResponseBody);
                }
                return this;
            }

            /// <summary>
            /// Распарсить RequestBody как JSON в project.Json
            /// </summary>
            public TrafficElement ParseRequestBodyAsJson()
            {
                if (!string.IsNullOrEmpty(RequestBody))
                {
                    _project.Json.FromString(RequestBody);
                }
                return this;
            }

            /// <summary>
            /// Получить конкретный заголовок из запроса
            /// </summary>
            public string GetRequestHeader(string headerName)
            {
                var headers = ParseHeaders(RequestHeaders);
                var key = headerName.ToLower();
                return headers.ContainsKey(key) ? headers[key] : null;
            }

            /// <summary>
            /// Получить конкретный заголовок из ответа
            /// </summary>
            public string GetResponseHeader(string headerName)
            {
                var headers = ParseHeaders(ResponseHeaders);
                var key = headerName.ToLower();
                return headers.ContainsKey(key) ? headers[key] : null;
            }

            /// <summary>
            /// Получить все заголовки запроса в виде словаря
            /// </summary>
            public Dictionary<string, string> GetAllRequestHeaders()
            {
                return ParseHeaders(RequestHeaders);
            }

            /// <summary>
            /// Получить все заголовки ответа в виде словаря
            /// </summary>
            public Dictionary<string, string> GetAllResponseHeaders()
            {
                return ParseHeaders(ResponseHeaders);
            }

            private Dictionary<string, string> ParseHeaders(string headersString)
            {
                var headers = new Dictionary<string, string>();
                if (string.IsNullOrWhiteSpace(headersString)) return headers;

                foreach (var line in headersString.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    var colonIndex = trimmed.IndexOf(':');
                    if (colonIndex <= 0) continue;

                    var key = trimmed.Substring(0, colonIndex).Trim().ToLower();
                    var value = trimmed.Substring(colonIndex + 1).Trim();
                    headers[key] = value;
                }

                return headers;
            }
        }

        #endregion
    }
    public partial class Traffic
    {
        #region Obsolete - Legacy API Support

        /// <summary>
        /// [УСТАРЕЛО] Используйте FindTrafficElement() который возвращает TrafficElement
        /// </summary>
        [Obsolete("Используйте FindTrafficElement(url) который возвращает TrafficElement. " +
                  "Пример: var element = traffic.FindTrafficElement(url); string body = element.ResponseBody;")]
        public TrafficData Get(string url, bool reload = false, bool strict = true, 
            int timeoutSeconds = 15, int delaySeconds = 1)
        {
            if (reload)
            {
                ReloadPage(delaySeconds);
            }

            var element = FindTrafficElement(url, exactMatch: strict, 
                timeoutSeconds: timeoutSeconds, retryDelaySeconds: delaySeconds);

            return new TrafficData(element);
        }

        /// <summary>
        /// [УСТАРЕЛО] Используйте GetResponseBody() или FindTrafficElement().ResponseBody
        /// </summary>
        [Obsolete("Используйте методы:\n" +
                  "- GetResponseBody(url) для получения тела ответа\n" +
                  "- GetRequestBody(url) для получения тела запроса\n" +
                  "- GetRequestHeader(url, headerName) для заголовков\n" +
                  "- FindTrafficElement(url) для получения полного элемента")]
        public string Get(string url, string parametr, bool reload = false, bool parse = false, 
            int deadline = 15, int delay = 3)
        {
            var validParameters = new[]
            {
                "Method", "ResultCode", "Url", "ResponseContentType", "RequestHeaders", 
                "RequestCookies", "RequestBody", "ResponseHeaders", "ResponseCookies", "ResponseBody"
            };
            
            if (!validParameters.Contains(parametr))
                throw new ArgumentException(
                    $"Invalid parameter: '{parametr}'. Valid parameters are: {string.Join(", ", validParameters)}");

            if (reload)
            {
                ReloadPage(delay);
            }

            var element = FindTrafficElement(url, exactMatch: false, 
                timeoutSeconds: deadline, retryDelaySeconds: delay);
            
            var result = GetElementField(element, parametr);

            if (parse && !string.IsNullOrEmpty(result))
            {
                _project.Json.FromString(result);
            }

            return result;
        }

        /// <summary>
        /// [УСТАРЕЛО] Кэш теперь управляется автоматически
        /// </summary>
        [Obsolete("Кэш трафика теперь управляется автоматически. " +
                  "Для явного обновления используйте RefreshTrafficCache()")]
        public Traffic Snapshot(bool reload = false, int delaySeconds = 1)
        {
            if (reload)
            {
                ReloadPage(delaySeconds);
            }
            else
            {
                RefreshTrafficCache();
            }

            return this;
        }

        /// <summary>
        /// [УСТАРЕЛО] Используйте FindAllTrafficElements()
        /// </summary>
        [Obsolete("Используйте FindAllTrafficElements(url, exactMatch) который возвращает List<TrafficElement>")]
        public List<TrafficData> GetAll(string url, bool strict = false)
        {
            var elements = FindAllTrafficElements(url, exactMatch: strict);
            return elements.Select(e => new TrafficData(e)).ToList();
        }

        /// <summary>
        /// [УСТАРЕЛО] Кэш теперь управляется автоматически
        /// </summary>
        [Obsolete("Кэш трафика управляется автоматически и не требует ручной очистки. " +
                  "Этот метод больше не нужен.")]
        public Traffic ClearCache()
        {
            // Принудительно обновляем кэш (хотя это и не обязательно)
            ForceRefreshCache();
            return this;
        }

        /// <summary>
        /// [УСТАРЕЛО] Используйте GetRequestHeader()
        /// </summary>
        [Obsolete("Используйте GetRequestHeader(url, headerName) или GetResponseHeader(url, headerName)")]
        public string GetHeader(string url, string headerName = "Authorization", 
            bool reload = false, int timeoutSeconds = 15)
        {
            if (reload)
            {
                ReloadPage();
            }

            return GetRequestHeader(url, headerName, exactMatch: false, timeoutSeconds: timeoutSeconds);
        }

        /// <summary>
        /// [УСТАРЕЛО] Используйте FindTrafficElement() который возвращает TrafficElement
        /// </summary>
        [Obsolete("Используйте FindTrafficElement(url) который возвращает TrafficElement с прямым доступом к полям")]
        public Dictionary<string, string> GetDictionary(string url, bool reload = false, 
            bool strict = true, int deadline = 10)
        {
            if (reload)
            {
                ReloadPage();
            }

            var element = FindTrafficElement(url, exactMatch: strict, timeoutSeconds: deadline);

            return new Dictionary<string, string>
            {
                { "Method", element.Method },
                { "ResultCode", element.StatusCode },
                { "Url", element.Url },
                { "ResponseContentType", element.ResponseContentType },
                { "RequestHeaders", element.RequestHeaders },
                { "RequestCookies", element.RequestCookies },
                { "RequestBody", element.RequestBody },
                { "ResponseHeaders", element.ResponseHeaders },
                { "ResponseCookies", element.ResponseCookies },
                { "ResponseBody", element.ResponseBody }
            };
        }

        /// <summary>
        /// [УСТАРЕЛО] Используйте специализированные методы Get*()
        /// </summary>
        [Obsolete("Используйте:\n" +
                  "- GetResponseBody(url) для ResponseBody\n" +
                  "- GetRequestHeader(url, headerName) для заголовков\n" +
                  "- FindTrafficElement(url).Method для метода и т.д.")]
        public string GetParam(string url, string parametr, bool reload = false, int deadline = 10)
        {
            if (reload)
            {
                ReloadPage();
            }

            var element = FindTrafficElement(url, exactMatch: false, timeoutSeconds: deadline);
            return GetElementField(element, parametr);
        }

        private string GetElementField(TrafficElement element, string fieldName)
        {
            switch (fieldName)
            {
                case "Method": return element.Method;
                case "ResultCode": return element.StatusCode;
                case "Url": return element.Url;
                case "ResponseContentType": return element.ResponseContentType;
                case "RequestHeaders": return element.RequestHeaders;
                case "RequestCookies": return element.RequestCookies;
                case "RequestBody": return element.RequestBody;
                case "ResponseHeaders": return element.ResponseHeaders;
                case "ResponseCookies": return element.ResponseCookies;
                case "ResponseBody": return element.ResponseBody;
                default:
                    throw new ArgumentException($"Unknown field: '{fieldName}'");
            }
        }

        #endregion

        #region Obsolete - TrafficData Wrapper

        /// <summary>
        /// [УСТАРЕЛО] Обертка для обратной совместимости. Используйте TrafficElement
        /// </summary>
        [Obsolete("Используйте TrafficElement вместо TrafficData")]
        public class TrafficData
        {
            private readonly TrafficElement _element;

            internal TrafficData(TrafficElement element)
            {
                _element = element;
            }

            public string Method => _element.Method;
            public string ResultCode => _element.StatusCode;
            public string Url => _element.Url;
            public string ResponseContentType => _element.ResponseContentType;
            public string RequestHeaders => _element.RequestHeaders;
            public string RequestCookies => _element.RequestCookies;
            public string ResponseHeaders => _element.ResponseHeaders;
            public string ResponseCookies => _element.ResponseCookies;
            public string RequestBody => _element.RequestBody;
            public string ResponseBody => _element.ResponseBody;

            [Obsolete("Используйте ParseResponseBodyAsJson()")]
            public TrafficData ParseResponseJson()
            {
                _element.ParseResponseBodyAsJson();
                return this;
            }

            [Obsolete("Используйте ParseRequestBodyAsJson()")]
            public TrafficData ParseRequestJson()
            {
                _element.ParseRequestBodyAsJson();
                return this;
            }

            public string GetRequestHeader(string headerName)
            {
                return _element.GetRequestHeader(headerName);
            }

            public string GetResponseHeader(string headerName)
            {
                return _element.GetResponseHeader(headerName);
            }
        }

        #endregion
    }

    #region Extension Methods

    public static partial class ProjectExtensions
    {
        /// <summary>
        /// Получить заголовки запроса и сохранить в переменную проекта
        /// </summary>
        public static void SaveRequestHeadersToVariable(this IZennoPosterProjectModel project, 
            Instance instance, string url, bool exactMatch = false, bool log = false)
        {
            var traffic = new Traffic(project, instance, log: log);
            var element = traffic.FindTrafficElement(url, exactMatch);
            
            var cleanHeaders = new StringBuilder();
            foreach (string header in element.RequestHeaders.Split('\n'))
            {
                // Пропускаем псевдо-заголовки HTTP/2
                if (header.StartsWith(":")) continue;
                if (string.IsNullOrWhiteSpace(header)) continue;
                
                cleanHeaders.AppendLine(header.Trim());
            }
            
            project.Var("headers", cleanHeaders.ToString());
            
            if (log) project.log($"Headers saved to variable 'headers':\n{cleanHeaders}");
        }

        /// <summary>
        /// Получить заголовки и сохранить в переменную проекта и/или БД
        /// </summary>
        public static void CollectRequestHeaders(this IZennoPosterProjectModel project, 
            Instance instance, string url, bool exactMatch = false, 
            bool saveToVariable = true, bool saveToDatabase = true, bool log = false)
        {
            var traffic = new Traffic(project, instance, log: log);
            var element = traffic.FindTrafficElement(url, exactMatch);
            
            var cleanHeaders = new StringBuilder();
            int headerCount = 0;
            
            foreach (string header in element.RequestHeaders.Split('\n'))
            {
                // Пропускаем псевдо-заголовки HTTP/2
                if (header.StartsWith(":")) continue;
                if (string.IsNullOrWhiteSpace(header)) continue;
                
                cleanHeaders.AppendLine(header.Trim());
                headerCount++;
            }
            
            var headersText = cleanHeaders.ToString();
            
            if (log) 
                project.log($"[SUCCESS]: collected={headerCount}, length={headersText.Length}\n{headersText}");
            
            if (saveToVariable) 
                project.Var("headers", headersText);
            
            if (saveToDatabase) 
                project.DbUpd($"headers = '{headersText}'");
        }
    }

    #endregion
}