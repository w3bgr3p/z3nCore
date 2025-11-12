using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore.Api
{
    public class AntiCaptcha
    {
        private readonly string _apiKey;
        private readonly string _apiUrl = "https://api.anti-captcha.com";
        private readonly HttpClient _httpClient;

        public AntiCaptcha(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        /// <summary>
        /// Решает капчу с изображения
        /// </summary>
        /// <param name="imagePath">Путь к файлу изображения</param>
        /// <param name="numeric">0 - любые символы, 1 - только цифры, 2 - любые буквы кроме цифр</param>
        /// <param name="minLength">Минимальная длина ответа (0 - без ограничений)</param>
        /// <param name="maxLength">Максимальная длина ответа (0 - без ограничений)</param>
        /// <param name="phrase">Требуется ли пробел в ответе</param>
        /// <param name="caseSensitive">Учитывать ли регистр</param>
        /// <param name="math">Математическая операция</param>
        /// <returns>Текст с капчи</returns>
        public string SolveCaptcha(
            string imagePath,
            int numeric = 0,
            int minLength = 0,
            int maxLength = 0,
            bool phrase = false,
            bool caseSensitive = true,
            bool math = false)
        {
            // Читаем файл и конвертируем в base64
            byte[] imageBytes = File.ReadAllBytes(imagePath);
            string base64Image = Convert.ToBase64String(imageBytes);

            return SolveCaptchaFromBase64(base64Image, numeric, minLength, maxLength, phrase, caseSensitive, math);
        }

        /// <summary>
        /// Решает капчу из base64 строки
        /// </summary>
        public string SolveCaptchaFromBase64(
            string base64Image,
            int numeric = 0,
            int minLength = 0,
            int maxLength = 0,
            bool phrase = false,
            bool caseSensitive = true,
            bool math = false)
        {
            // Создаем задачу
            int taskId = CreateTask(base64Image, numeric, minLength, maxLength, phrase, caseSensitive, math);

            // Ждем результат
            return GetTaskResult(taskId);
        }

        /// <summary>
        /// Создает задачу на решение капчи
        /// </summary>
        private int CreateTask(
            string base64Image,
            int numeric,
            int minLength,
            int maxLength,
            bool phrase,
            bool caseSensitive,
            bool math)
        {
            var taskData = new
            {
                clientKey = _apiKey,
                task = new
                {
                    type = "ImageToTextTask",
                    body = base64Image,
                    phrase = phrase,
                    @case = caseSensitive,
                    numeric = numeric,
                    math = math,
                    minLength = minLength,
                    maxLength = maxLength
                }
            };

            string jsonRequest = JsonConvert.SerializeObject(taskData);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = _httpClient.PostAsync($"{_apiUrl}/createTask", content).Result;
            string responseText = response.Content.ReadAsStringAsync().Result;

            var jsonResponse = JObject.Parse(responseText);

            int errorId = jsonResponse["errorId"].Value<int>();
            if (errorId != 0)
            {
                string errorCode = jsonResponse["errorCode"]?.Value<string>() ?? "UNKNOWN_ERROR";
                string errorDescription = jsonResponse["errorDescription"]?.Value<string>() ?? "Unknown error";
                throw new Exception($"Ошибка создания задачи: [{errorCode}] {errorDescription}");
            }

            return jsonResponse["taskId"].Value<int>();
        }

        /// <summary>
        /// Получает результат решения капчи
        /// </summary>
        private string GetTaskResult(int taskId, int maxAttempts = 60)
        {
            var requestData = new
            {
                clientKey = _apiKey,
                taskId = taskId
            };

            string jsonRequest = JsonConvert.SerializeObject(requestData);

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Thread.Sleep(3000); // Ждем 3 секунды между запросами

                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                var response = _httpClient.PostAsync($"{_apiUrl}/getTaskResult", content).Result;
                string responseText = response.Content.ReadAsStringAsync().Result;

                var jsonResponse = JObject.Parse(responseText);

                int errorId = jsonResponse["errorId"].Value<int>();
                if (errorId != 0)
                {
                    string errorCode = jsonResponse["errorCode"]?.Value<string>() ?? "UNKNOWN_ERROR";
                    string errorDescription = jsonResponse["errorDescription"]?.Value<string>() ?? "Unknown error";
                    throw new Exception($"Ошибка получения результата: [{errorCode}] {errorDescription}");
                }

                string status = jsonResponse["status"].Value<string>();

                if (status == "ready")
                {
                    return jsonResponse["solution"]["text"].Value<string>();
                }

                if (status == "processing")
                {
                    continue; // Капча еще решается
                }

                throw new Exception($"Неизвестный статус задачи: {status}");
            }

            throw new TimeoutException($"Не удалось получить результат за {maxAttempts * 3} секунд");
        }

        /// <summary>
        /// Освобождает ресурсы
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public static partial class CaptchaExtensions
    {
        public static string SolveHeWithAntiCaptcha(this HtmlElement he, IZennoPosterProjectModel project)
        {
            var api_key = project.DbGet("apikey", "_api", where:"id = 'anticaptcha'");
            var solver = new AntiCaptcha(api_key);
            
            var bitmap = he.DrawAsBitmap(false);
            string base64;
            using (var ms = new System.IO.MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                base64 =  Convert.ToBase64String(ms.ToArray());
            }
            

            string result = solver.SolveCaptchaFromBase64(base64);
            return result;
        }

        public static string SolveCaptchaFromUrl(IZennoPosterProjectModel project , string url, string proxy = "+")
        {
            var api_key = project.DbGet("apikey", "_api", where:"id = 'anticaptcha'");
            var solver = new AntiCaptcha(api_key);
            var req = new NetHttp(project, true);
            req.GET(url, proxy, parse:true);
            var svg = project.Json.captcha;
            string base64 = svg.SvgToBase64();
            string result = solver.SolveCaptchaFromBase64(base64);
            return result;
        }



    }
    
    
}