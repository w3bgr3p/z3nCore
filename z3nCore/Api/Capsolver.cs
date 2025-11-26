using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace z3nCore.Api
{
    /// <summary>
    /// Capsolver API for solving Turnstile captcha
    /// </summary>
    public class Capsolver : IDisposable
    {
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://api.capsolver.com";
        private readonly HttpClient _httpClient;

        public Capsolver(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(3);
        }

        #region Public API (Sync)

        /// <summary>
        /// Solve Turnstile captcha (Sync wrapper)
        /// </summary>
        /// <param name="websiteURL">Website URL where captcha is located</param>
        /// <param name="websiteKey">Turnstile site key</param>
        /// <param name="proxy">Proxy in format: http://user:pass@ip:port (optional)</param>
        /// <returns>Captcha token</returns>
        public string SolveTurnstile(string websiteURL, string websiteKey, string proxy = null)
        {
            return Task.Run(async () =>
                await SolveTurnstileAsync(websiteURL, websiteKey, proxy).ConfigureAwait(false)
            ).GetAwaiter().GetResult();
        }

        #endregion

        #region Public API (Async)

        /// <summary>
        /// Solve Turnstile captcha (Async)
        /// </summary>
        /// <param name="websiteURL">Website URL where captcha is located</param>
        /// <param name="websiteKey">Turnstile site key</param>
        /// <param name="proxy">Proxy in format: http://user:pass@ip:port (optional)</param>
        /// <returns>Captcha token</returns>
        public async Task<string> SolveTurnstileAsync(string websiteURL, string websiteKey, string proxy = null)
        {
            // Create task
            string taskId = await CreateTurnstileTaskAsync(websiteURL, websiteKey, proxy);

            // Wait for result
            return await GetTaskResultAsync(taskId);
        }

        #endregion

        #region Private Implementation

        private async Task<string> CreateTurnstileTaskAsync(string websiteURL, string websiteKey, string proxy)
        {
            var taskData = new
            {
                clientKey = _apiKey,
                task = new
                {
                    type = string.IsNullOrEmpty(proxy) ? "AntiTurnstileTaskProxyLess" : "AntiTurnstileTask",
                    websiteURL = websiteURL,
                    websiteKey = websiteKey,
                    proxy = proxy
                }
            };

            string jsonRequest = JsonConvert.SerializeObject(taskData, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_apiUrl}/createTask", content);
            string responseText = await response.Content.ReadAsStringAsync();

            var jsonResponse = JObject.Parse(responseText);

            int errorId = jsonResponse["errorId"]?.Value<int>() ?? 1;
            if (errorId != 0)
            {
                string errorCode = jsonResponse["errorCode"]?.Value<string>() ?? "UNKNOWN_ERROR";
                string errorDescription = jsonResponse["errorDescription"]?.Value<string>() ?? "Unknown error";
                throw new Exception($"Capsolver create task error: [{errorCode}] {errorDescription}");
            }

            string taskId = jsonResponse["taskId"]?.Value<string>();
            if (string.IsNullOrEmpty(taskId))
            {
                throw new Exception($"Capsolver: taskId is null or empty. Response: {responseText}");
            }

            return taskId;
        }

        private async Task<string> GetTaskResultAsync(string taskId, int maxAttempts = 40)
        {
            var requestData = new
            {
                clientKey = _apiKey,
                taskId = taskId
            };

            string jsonRequest = JsonConvert.SerializeObject(requestData);

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                await Task.Delay(3000); // Wait 3 seconds between checks

                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_apiUrl}/getTaskResult", content);
                string responseText = await response.Content.ReadAsStringAsync();

                var jsonResponse = JObject.Parse(responseText);

                int errorId = jsonResponse["errorId"]?.Value<int>() ?? 1;
                if (errorId != 0)
                {
                    string errorCode = jsonResponse["errorCode"]?.Value<string>() ?? "UNKNOWN_ERROR";
                    string errorDescription = jsonResponse["errorDescription"]?.Value<string>() ?? "Unknown error";
                    throw new Exception($"Capsolver get result error: [{errorCode}] {errorDescription}");
                }

                string status = jsonResponse["status"]?.Value<string>();

                if (status == "ready")
                {
                    string token = jsonResponse["solution"]?["token"]?.Value<string>();
                    if (string.IsNullOrEmpty(token))
                    {
                        throw new Exception($"Capsolver: token is null or empty. Response: {responseText}");
                    }
                    return token;
                }

                if (status == "processing")
                {
                    continue; // Captcha is still being solved
                }

                throw new Exception($"Capsolver: unknown task status: {status}");
            }

            throw new TimeoutException($"Capsolver: failed to get result in {maxAttempts * 3} seconds");
        }

        #endregion

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}