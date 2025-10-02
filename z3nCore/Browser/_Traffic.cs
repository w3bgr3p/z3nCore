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
     public class __Traffic
    {
        #region Fields & Constructor

        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;
        private readonly bool _showLog;

        public __Traffic(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _showLog = log;
            _logger = new Logger(project, log: log, classEmoji: "🌎");
            _instance.UseTrafficMonitoring = true;
        }

        #endregion

        #region Public API - Новый упрощенный интерфейс

        /// <summary>
        /// Получить данные трафика по URL с удобным доступом к полям
        /// </summary>
        /// <example>
        /// var traffic = new Traffic(project, instance).Get("api/endpoint");
        /// var body = traffic.ResponseBody;
        /// var headers = traffic.RequestHeaders;
        /// </example>
        public TrafficData Get(string url, bool reload = false, bool strict = true, int timeoutSeconds = 15, int delaySeconds = 1)
        {
            _project.Deadline();
            _instance.UseTrafficMonitoring = true;

            if (reload)
            {
                _instance.ActiveTab.MainDocument.EvaluateScript("location.reload(true)");
                if (_instance.ActiveTab.IsBusy) _instance.ActiveTab.WaitDownloading();
                Thread.Sleep(1000 * delaySeconds);
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
        /// Быстрый доступ к конкретному полю трафика (для обратной совместимости)
        /// </summary>
        [Obsolete("Используйте Get(url).ResponseBody вместо GetField(url, \"ResponseBody\")")]
        public string GetField(string url, string fieldName, bool reload = false, int timeoutSeconds = 15)
        {
            var data = Get(url, reload, strict: false, timeoutSeconds: timeoutSeconds);
            return GetFieldValue(data, fieldName);
        }

        /// <summary>
        /// Получить конкретный заголовок из RequestHeaders
        /// </summary>
        public string GetHeader(string url, string headerName = "Authorization", bool reload = false, int timeoutSeconds = 15)
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
        /// Попытка найти трафик по URL
        /// </summary>
        private TrafficData TryFindTraffic(string url, bool strict)
        {
            // GetTraffic() возвращает IEnumerable с элементами, у которых есть свойства:
            // Method, ResultCode, Url, ResponseContentType, RequestHeaders, RequestBody и т.д.
            var traffic = _instance.ActiveTab.GetTraffic();

            foreach (var item in traffic)
            {
                // Проверка совпадения URL
                bool urlMatches = strict ? item.Url == url : item.Url.Contains(url);
                if (!urlMatches) continue;

                // Пропускаем OPTIONS запросы
                if (item.Method == "OPTIONS") continue;

                // Нашли подходящий трафик - парсим
                return ParseTrafficItem(item);
            }

            return null;
        }

        /// <summary>
        /// Парсинг одного элемента трафика в TrafficData
        /// Работает с dynamic типом из GetTraffic()
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

        /// <summary>
        /// Данные трафика с типизированным доступом к полям
        /// Потокобезопасен - каждый поток получает свою копию
        /// </summary>
        public class TrafficData
        {
            public string Method { get; internal set; }
            public string ResultCode { get; internal set; }
            public string Url { get; internal set; }
            public string ResponseContentType { get; internal set; }
            
            // Headers & Cookies
            public string RequestHeaders { get; internal set; }
            public string RequestCookies { get; internal set; }
            public string ResponseHeaders { get; internal set; }
            public string ResponseCookies { get; internal set; }
            
            // Bodies
            public string RequestBody { get; internal set; }
            public string ResponseBody { get; internal set; }

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
            
            //new
            private readonly IZennoPosterProjectModel _project;

            // Добавляем конструктор с project
            internal TrafficData(IZennoPosterProjectModel project)
            {
                _project = project;
            }

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
            //
            
        }

        #endregion

        #region Legacy Methods - Сохранены для обратной совместимости

        /// <summary>
        /// УСТАРЕЛО: Используйте Get(url).ResponseBody или Get(url, parse: true)
        /// </summary>
        [Obsolete("Get(url)")]
        public string Get(string url, string parametr, bool reload = false, bool parse = false, int deadline = 15, int delay = 3)
        {
            var validParameters = new[] { "Method", "ResultCode", "Url", "ResponseContentType", "RequestHeaders", "RequestCookies", "RequestBody", "ResponseHeaders", "ResponseCookies", "ResponseBody" };
            if (!validParameters.Contains(parametr))
                throw new ArgumentException($"Invalid parameter: '{parametr}'. Valid parameters are: {string.Join(", ", validParameters)}");

            var data = Get(url, reload, strict: false, timeoutSeconds: deadline, delaySeconds: delay);
            var result = GetFieldValue(data, parametr);

            if (parse && !string.IsNullOrEmpty(result))
            {
                _project.Json.FromString(result);
            }

            return result;
        }

        /// <summary>
        /// УСТАРЕЛО: Используйте Get(url) и работайте с TrafficData
        /// </summary>
        [Obsolete("Get(url) returns TrafficData")]
        public Dictionary<string, string> GetDictionary(string url, bool reload = false, bool strict = true, int deadline = 10)
        {
            var data = Get(url, reload, strict, timeoutSeconds: deadline);
            
            return new Dictionary<string, string>
            {
                {"Method", data.Method},
                {"ResultCode", data.ResultCode},
                {"Url", data.Url},
                {"ResponseContentType", data.ResponseContentType},
                {"RequestHeaders", data.RequestHeaders},
                {"RequestCookies", data.RequestCookies},
                {"RequestBody", data.RequestBody},
                {"ResponseHeaders", data.ResponseHeaders},
                {"ResponseCookies", data.ResponseCookies},
                {"ResponseBody", data.ResponseBody}
            };
        }

        /// <summary>
        /// УСТАРЕЛО: Используйте Get(url).GetRequestHeader(headerName)
        /// </summary>
        [Obsolete("Get(url).GetRequestHeader(headerName) || GetHeader(url, headerName)")]
        public string GetParam(string url, string parametr, bool reload = false, int deadline = 10)
        {
            var data = Get(url, reload, strict: false, timeoutSeconds: deadline);
            return GetFieldValue(data, parametr);
        }

        #endregion
        
    }
}