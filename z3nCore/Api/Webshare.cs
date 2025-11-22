using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace z3nCore.Api
{
    public class Webshare : IDisposable
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://proxy.webshare.io/api";

        public Webshare(string apiKey)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
            _httpClient.DefaultRequestHeaders.Add("Authorization", _apiKey);
        }

        public async Task<List<string>> GetProxyListAsync()
        {
            // Получаем plan_id
            var planResponse = await _httpClient.GetStringAsync($"{BaseUrl}/v2/subscription/plan/");
            var planJson = JObject.Parse(planResponse);
            var planId = planJson["results"]?[0]?["id"]?.ToString();
            
            if (string.IsNullOrEmpty(planId))
                throw new Exception("Failed to get plan_id from API");

            // Получаем токен
            var configResponse = await _httpClient.GetStringAsync($"{BaseUrl}/v3/proxy/config?plan_id={planId}");
            var configJson = JObject.Parse(configResponse);
            var token = configJson["proxy_list_download_token"]?.ToString();
            
            if (string.IsNullOrEmpty(token))
                throw new Exception("Failed to get download token from API");

            // Скачиваем список прокси
            var proxyListResponse = await _httpClient.GetStringAsync(
                $"{BaseUrl}/v2/proxy/list/download/{token}/-/any/username/direct/"
            );

            return proxyListResponse
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();
        }

        public List<string> GetProxyList()
        {
            return GetProxyListAsync().GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}