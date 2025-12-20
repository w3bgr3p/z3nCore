using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;


namespace z3nCore.Api
{
    public class CapMonsterCloud : IDisposable
    {
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://api.capmonster.cloud";
        private readonly HttpClient _httpClient;

        public CapMonsterCloud(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        /// <summary>
        /// Решает ReCaptcha v2 (синхронный метод)
        /// </summary>
        public string SolveRecaptchaV2(string websiteUrl, string websiteKey, string proxyType = null, string proxyAddress = null, int proxyPort = 0, string proxyLogin = null, string proxyPassword = null)
        {
            return Task.Run(async () =>
                await SolveRecaptchaV2Async(websiteUrl, websiteKey, proxyType, proxyAddress, proxyPort, proxyLogin, proxyPassword)
                    .ConfigureAwait(false)
            ).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Решает ReCaptcha v2 (асинхронный метод)
        /// </summary>
        public async Task<string> SolveRecaptchaV2Async(
            string websiteUrl, 
            string websiteKey, 
            string proxyType = null, 
            string proxyAddress = null, 
            int proxyPort = 0, 
            string proxyLogin = null, 
            string proxyPassword = null)
        {
            // Создаем задачу
            string taskId = await CreateRecaptchaV2TaskAsync(websiteUrl, websiteKey, proxyType, proxyAddress, proxyPort, proxyLogin, proxyPassword);

            // Ждем результат
            return await GetTaskResultAsync(taskId);
        }

        // ============== PRIVATE METHODS ==============

        private async Task<string> CreateRecaptchaV2TaskAsync(
            string websiteUrl,
            string websiteKey,
            string proxyType,
            string proxyAddress,
            int proxyPort,
            string proxyLogin,
            string proxyPassword)
        {
            var task = new JObject
            {
                ["type"] = string.IsNullOrEmpty(proxyType) ? "NoCaptchaTaskProxyless" : "NoCaptchaTask",
                ["websiteURL"] = websiteUrl,
                ["websiteKey"] = websiteKey
            };

            // Если передан прокси - добавляем параметры
            if (!string.IsNullOrEmpty(proxyType))
            {
                task["proxyType"] = proxyType;
                task["proxyAddress"] = proxyAddress;
                task["proxyPort"] = proxyPort;
                
                if (!string.IsNullOrEmpty(proxyLogin))
                {
                    task["proxyLogin"] = proxyLogin;
                    task["proxyPassword"] = proxyPassword;
                }
            }

            var taskData = new JObject
            {
                ["clientKey"] = _apiKey,
                ["task"] = task
            };

            string jsonRequest = taskData.ToString(Formatting.None);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_apiUrl}/createTask", content);
            string responseText = await response.Content.ReadAsStringAsync();

            var jsonResponse = JObject.Parse(responseText);

            int errorId = jsonResponse["errorId"]?.Value<int>() ?? 1;
            if (errorId != 0)
            {
                string errorCode = jsonResponse["errorCode"]?.Value<string>() ?? "UNKNOWN_ERROR";
                string errorDescription = jsonResponse["errorDescription"]?.Value<string>() ?? "Unknown error";
                throw new Exception($"Ошибка создания задачи CapMonster: [{errorCode}] {errorDescription}");
            }

            return jsonResponse["taskId"].Value<string>();
        }

        private async Task<string> GetTaskResultAsync(string taskId, int maxAttempts = 60)
        {
            var requestData = new JObject
            {
                ["clientKey"] = _apiKey,
                ["taskId"] = taskId
            };

            string jsonRequest = requestData.ToString(Formatting.None);

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                await Task.Delay(3000);

                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_apiUrl}/getTaskResult", content);
                string responseText = await response.Content.ReadAsStringAsync();

                var jsonResponse = JObject.Parse(responseText);

                int errorId = jsonResponse["errorId"]?.Value<int>() ?? 1;
                if (errorId != 0)
                {
                    string errorCode = jsonResponse["errorCode"]?.Value<string>() ?? "UNKNOWN_ERROR";
                    string errorDescription = jsonResponse["errorDescription"]?.Value<string>() ?? "Unknown error";
                    throw new Exception($"Ошибка получения результата CapMonster: [{errorCode}] {errorDescription}");
                }

                string status = jsonResponse["status"]?.Value<string>();

                if (status == "ready")
                {
                    return jsonResponse["solution"]["gRecaptchaResponse"].Value<string>();
                }

                if (status == "processing")
                {
                    continue; // Капча еще решается
                }

                throw new Exception($"Неизвестный статус задачи CapMonster: {status}");
            }

            throw new TimeoutException($"Не удалось получить результат CapMonster за {maxAttempts * 3} секунд");
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}

namespace z3nCore
{
    using Api;
    using ZennoLab.InterfacesLibrary.ProjectModel;

    public static partial class CaptchaExtensions
    {
        /// <summary>
        /// Решает ReCaptcha v2 через CapMonster Cloud
        /// </summary>
        public static string SolveRecaptchaV2WithCapMonster(
            this IZennoPosterProjectModel project, 
            string websiteUrl, 
            string websiteKey,
            string proxy = null)
        {
            var api_key = project.DbGet("apikey", "_api", where: "id = 'capmonster'");

            using (var solver = new CapMonsterCloud(api_key))
            {
                // Если прокси не задан или "+" - решаем без прокси
                if (string.IsNullOrEmpty(proxy))
                {
                    return solver.SolveRecaptchaV2(websiteUrl, websiteKey);
                }
                if ( proxy == "+")
                {
                    proxy = project.DbGet("proxy", "_instance");
                }

                // Парсим прокси формата "type://login:pass@ip:port"
                var proxyParts = ParseProxy(proxy);
                
                return solver.SolveRecaptchaV2(
                    websiteUrl, 
                    websiteKey,
                    proxyParts.type,
                    proxyParts.address,
                    proxyParts.port,
                    proxyParts.login,
                    proxyParts.password
                );
            }
        }

        private static (string type, string address, int port, string login, string password) ParseProxy(string proxy)
        {
            // Формат: http://user:pass@ip:port или http://ip:port
            var parts = proxy.Split(new[] { "://" }, StringSplitOptions.None);
            if (parts.Length != 2) throw new Exception("Неверный формат прокси");

            string type = parts[0].ToUpper(); // HTTP, HTTPS, SOCKS4, SOCKS5
            string rest = parts[1];

            string login = null;
            string password = null;
            string address;
            int port;

            if (rest.Contains("@"))
            {
                var authParts = rest.Split('@');
                var credentials = authParts[0].Split(':');
                login = credentials[0];
                password = credentials.Length > 1 ? credentials[1] : null;
                rest = authParts[1];
            }

            var addressParts = rest.Split(':');
            address = addressParts[0];
            port = int.Parse(addressParts[1]);

            return (type, address, port, login, password);
        }
    }
}