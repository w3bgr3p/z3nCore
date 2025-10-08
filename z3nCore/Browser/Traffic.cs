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
    /// Класс для работы с трафиком браузера ZennoPoster
    /// Потокобезопасен через изоляцию project/instance
    /// </summary>
    public class Traffic
    {
        #region Fields & Constructor

        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;
        private readonly bool _showLog;

        // Кэш распарсенного трафика
        private List<TrafficData> _cachedTraffic;
        private DateTime _cacheTime;

        public Traffic(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _showLog = log;
            _logger = new Logger(project, log: log, classEmoji: "🌎");
            _instance.UseTrafficMonitoring = true;
        }

        #endregion

        #region Public API - Основной интерфейс

        /// <summary>
        /// Получить данные трафика по URL с удобным доступом к полям
        /// </summary>
        public TrafficData Get(string url, bool reload = false, bool strict = true, int timeoutSeconds = 15,
            int delaySeconds = 1)
        {
            _project.Deadline();
            _instance.UseTrafficMonitoring = true;

            if (reload)
            {
                _instance.ActiveTab.MainDocument.EvaluateScript("location.reload(true)");
                if (_instance.ActiveTab.IsBusy) _instance.ActiveTab.WaitDownloading();
                Thread.Sleep(1000 * delaySeconds);
                ClearCache(); // Сбрасываем кэш при reload
            }

            var startTime = DateTime.Now;
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);
            int attempt = 0;

            while (DateTime.Now - startTime < timeout)
            {
                _project.Deadline(timeoutSeconds);
                attempt++;

                if (_showLog) _logger.Send($"Попытка #{attempt} поиска URL: {url}");

                var trafficData = TryFindTraffic(url, strict);
                if (trafficData != null)
                {
                    if (_showLog) _logger.Send($"✓ Найден трафик для {url}");
                    return trafficData;
                }

                Thread.Sleep(1000 * delaySeconds);
            }

            throw new TimeoutException($"Трафик для URL '{url}' не найден за {timeoutSeconds} секунд");
        }

        /// <summary>
        /// Сделать снапшот трафика для работы с несколькими URL
        /// </summary>
        /// <example>
        /// var traffic = new Traffic(project, instance).Snapshot();
        /// var data1 = traffic.Get("api/endpoint1");
        /// var data2 = traffic.Get("api/endpoint2");
        /// var data3 = traffic.Get("api/endpoint3");
        /// </example>
        public Traffic Snapshot(bool reload = false, int delaySeconds = 1)
        {
            _project.Deadline();
            _instance.UseTrafficMonitoring = true;

            if (reload)
            {
                _instance.ActiveTab.MainDocument.EvaluateScript("location.reload(true)");
                if (_instance.ActiveTab.IsBusy) _instance.ActiveTab.WaitDownloading();
                Thread.Sleep(1000 * delaySeconds);
            }

            RefreshCache();

            _logger.Send($"Snapped records: {_cachedTraffic.Count} ");

            return this;
        }

        /// <summary>
        /// Получить все записи трафика, соответствующие URL
        /// </summary>
        public List<TrafficData> GetAll(string url, bool strict = false)
        {
            EnsureCacheExists();

            var results = new List<TrafficData>();

            foreach (var item in _cachedTraffic)
            {
                bool matches = strict ? item.Url == url : item.Url.Contains(url);
                if (matches)
                {
                    results.Add(item);
                }
            }

            if (_showLog) _logger.Send($"Найдено {results.Count} записей для URL: {url}");

            return results;
        }

        /// <summary>
        /// Очистить кэш трафика
        /// </summary>
        public Traffic ClearCache()
        {
            _cachedTraffic = null;
            _cacheTime = DateTime.MinValue;

            if (_showLog) _logger.Send("Кэш трафика очищен");

            return this;
        }

        /// <summary>
        /// Получить конкретный заголовок из RequestHeaders
        /// </summary>
        public string GetHeader(string url, string headerName = "Authorization", bool reload = false,
            int timeoutSeconds = 15)
        {
            var trafficData = Get(url, reload, strict: false, timeoutSeconds: timeoutSeconds);
            var headers = ParseHeaders(trafficData.RequestHeaders);

            var headerKey = headerName.ToLower();
            if (headers.ContainsKey(headerKey))
                return headers[headerKey];

            throw new KeyNotFoundException($"Заголовок '{headerName}' не найден в трафике для {url}");
        }

        #endregion

        #region Private Implementation

        /// <summary>
        /// Обновить кэш трафика
        /// </summary>
        private void RefreshCache()
        {
            var rawTraffic = _instance.ActiveTab.GetTraffic();
            _cachedTraffic = new List<TrafficData>();

            foreach (var item in rawTraffic)
            {
                // Пропускаем OPTIONS запросы
                if (item.Method == "OPTIONS") continue;

                _cachedTraffic.Add(ParseTrafficItem(item));
            }

            _cacheTime = DateTime.Now;
        }

        /// <summary>
        /// Убедиться что кэш существует
        /// </summary>
        private void EnsureCacheExists()
        {
            if (_cachedTraffic == null)
            {
                RefreshCache();
            }
        }

        /// <summary>
        /// Попытка найти трафик по URL
        /// </summary>
        private TrafficData TryFindTraffic(string url, bool strict)
        {
            // Если кэш устарел или не существует - обновляем
            EnsureCacheExists();

            foreach (var item in _cachedTraffic)
            {
                bool urlMatches = strict ? item.Url == url : item.Url.Contains(url);
                if (urlMatches)
                {
                    return item;
                }
            }

            // Не нашли в кэше - обновляем кэш и пробуем еще раз
            RefreshCache();

            foreach (var item in _cachedTraffic)
            {
                bool urlMatches = strict ? item.Url == url : item.Url.Contains(url);
                if (urlMatches)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// Парсинг одного элемента трафика в TrafficData
        /// </summary>
        private TrafficData ParseTrafficItem(dynamic item)
        {
            var responseBody = item.ResponseBody == null
                ? string.Empty
                : Encoding.UTF8.GetString(item.ResponseBody, 0, item.ResponseBody.Length);

            return new TrafficData(_project)
            {
                Method = item.Method ?? string.Empty,
                ResultCode = item.ResultCode.ToString(),
                Url = item.Url ?? string.Empty,
                ResponseContentType = item.ResponseContentType ?? string.Empty,
                RequestHeaders = item.RequestHeaders ?? string.Empty,
                RequestCookies = item.RequestCookies ?? string.Empty,
                RequestBody = item.RequestBody ?? string.Empty,
                ResponseHeaders = item.ResponseHeaders ?? string.Empty,
                ResponseCookies = item.ResponseCookies ?? string.Empty,
                ResponseBody = responseBody
            };
        }

        /// <summary>
        /// Парсинг строки заголовков в словарь
        /// </summary>
        private Dictionary<string, string> ParseHeaders(string headersString)
        {
            var headers = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(headersString))
                return headers;

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

        /// <summary>
        /// Получение значения поля по имени (для обратной совместимости)
        /// </summary>
        private string GetFieldValue(TrafficData data, string fieldName)
        {
            switch (fieldName)
            {
                case "Method": return data.Method;
                case "ResultCode": return data.ResultCode;
                case "Url": return data.Url;
                case "ResponseContentType": return data.ResponseContentType;
                case "RequestHeaders": return data.RequestHeaders;
                case "RequestCookies": return data.RequestCookies;
                case "RequestBody": return data.RequestBody;
                case "ResponseHeaders": return data.ResponseHeaders;
                case "ResponseCookies": return data.ResponseCookies;
                case "ResponseBody": return data.ResponseBody;
                default:
                    throw new ArgumentException($"Неизвестное поле: '{fieldName}'");
            }
        }

        #endregion

        #region Nested Class - TrafficData

        public class TrafficData
        {
            private readonly IZennoPosterProjectModel _project;

            internal TrafficData(IZennoPosterProjectModel project)
            {
                _project = project;
            }

            public string Method { get; internal set; }
            public string ResultCode { get; internal set; }
            public string Url { get; internal set; }
            public string ResponseContentType { get; internal set; }

            public string RequestHeaders { get; internal set; }
            public string RequestCookies { get; internal set; }
            public string ResponseHeaders { get; internal set; }
            public string ResponseCookies { get; internal set; }

            public string RequestBody { get; internal set; }
            public string ResponseBody { get; internal set; }

            /// <summary>
            /// Распарсить ResponseBody как JSON
            /// </summary>
            public TrafficData ParseResponseJson()
            {
                if (!string.IsNullOrEmpty(ResponseBody))
                {
                    _project.Json.FromString(ResponseBody);
                }

                return this;
            }

            /// <summary>
            /// Распарсить RequestBody как JSON
            /// </summary>
            public TrafficData ParseRequestJson()
            {
                if (!string.IsNullOrEmpty(RequestBody))
                {
                    _project.Json.FromString(RequestBody);
                }

                return this;
            }

            /// <summary>
            /// Получить конкретный заголовок из RequestHeaders
            /// </summary>
            public string GetRequestHeader(string headerName)
            {
                var headers = ParseHeadersInternal(RequestHeaders);
                var key = headerName.ToLower();
                return headers.ContainsKey(key) ? headers[key] : null;
            }

            /// <summary>
            /// Получить конкретный заголовок из ResponseHeaders
            /// </summary>
            public string GetResponseHeader(string headerName)
            {
                var headers = ParseHeadersInternal(ResponseHeaders);
                var key = headerName.ToLower();
                return headers.ContainsKey(key) ? headers[key] : null;
            }

            private Dictionary<string, string> ParseHeadersInternal(string headersString)
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

        #region Legacy Methods

        [Obsolete("Используйте Get(url)")]
        public string Get(string url, string parametr, bool reload = false, bool parse = false, int deadline = 15,
            int delay = 3)
        {
            var validParameters = new[]
            {
                "Method", "ResultCode", "Url", "ResponseContentType", "RequestHeaders", "RequestCookies", "RequestBody",
                "ResponseHeaders", "ResponseCookies", "ResponseBody"
            };
            if (!validParameters.Contains(parametr))
                throw new ArgumentException(
                    $"Invalid parameter: '{parametr}'. Valid parameters are: {string.Join(", ", validParameters)}");

            var data = Get(url, reload, strict: false, timeoutSeconds: deadline, delaySeconds: delay);
            var result = GetFieldValue(data, parametr);

            if (parse && !string.IsNullOrEmpty(result))
            {
                _project.Json.FromString(result);
            }

            return result;
        }

        [Obsolete("Используйте Get(url) который возвращает TrafficData")]
        public Dictionary<string, string> GetDictionary(string url, bool reload = false, bool strict = true,
            int deadline = 10)
        {
            var data = Get(url, reload, strict, timeoutSeconds: deadline);

            return new Dictionary<string, string>
            {
                { "Method", data.Method },
                { "ResultCode", data.ResultCode },
                { "Url", data.Url },
                { "ResponseContentType", data.ResponseContentType },
                { "RequestHeaders", data.RequestHeaders },
                { "RequestCookies", data.RequestCookies },
                { "RequestBody", data.RequestBody },
                { "ResponseHeaders", data.ResponseHeaders },
                { "ResponseCookies", data.ResponseCookies },
                { "ResponseBody", data.ResponseBody }
            };
        }

        [Obsolete("Используйте Get(url).GetRequestHeader(headerName)")]
        public string GetParam(string url, string parametr, bool reload = false, int deadline = 10)
        {
            var data = Get(url, reload, strict: false, timeoutSeconds: deadline);
            return GetFieldValue(data, parametr);
        }

        #endregion
    }
    public static partial class ProjectExtensions
    {
        public static void HeadersToProject(this IZennoPosterProjectModel project, Instance instance, string url, bool strict= false)
        {
            var headers  = new Traffic(project, instance).Get(url, strict:strict).RequestHeaders.Split('\n');
            var refactoredHeaders = new StringBuilder();
            foreach (string header in headers)
            {
                if (header.StartsWith(":")) continue;
                refactoredHeaders.AppendLine(header);
            }
            project.Var("headers", refactoredHeaders.ToString());
        }
        
        public static void GetHeaders(this IZennoPosterProjectModel project, Instance instance, string url, bool strict= false ,bool toProject = true, bool toDb = true, bool log = false)
        {
            var _project = project;
            var _instance = instance;
			
            var traffic = new Traffic(_project, _instance, log: log).Snapshot();
            var result = new StringBuilder();
    
            int apiCount = 0;
            foreach (var h in traffic.Get(url, strict: strict).RequestHeaders.Split('\n'))
            {
                if (!h.StartsWith(":") && !string.IsNullOrWhiteSpace(h))
                {
                    result.AppendLine(h.Trim());
                    apiCount++;
                }
            }
            var headers = result.ToString();
            if (log) _project.log($"[SUCCSESS]: collected={apiCount}, length={headers.Length}\n{headers}");
            if (toProject)_project.Var("headers", headers);
            if (toDb)_project.DbUpd($"headers = '{headers}'");
        }
    }
    
    
    
    
}