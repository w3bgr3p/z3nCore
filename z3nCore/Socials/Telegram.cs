using System;
using System.Linq;
using System.Runtime.CompilerServices;

using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class Telegram
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly bool _logShow;
        private readonly Logger _log;
        private string _token;
        private string _group;
        private string _topic;
        private readonly NetHttp _http;

        public Telegram(IZennoPosterProjectModel project, bool log = false, string token = null, string group = null, string topic = null)
        {
            _project = project;
            _logShow = log;
            _log = new Logger(project, log: _logShow, classEmoji: "🚀");
            _token = token;
            _group = group;
            _topic = topic;
            _http = new NetHttp(project, log: false);
            LoadCreds();
        }

        private void LoadCreds()
        {
            if (string.IsNullOrEmpty(_token) || string.IsNullOrEmpty(_group) || string.IsNullOrEmpty(_topic))
            {
                var creds = _project.DbGetColumns("apikey, extra", "_api", where: "id = 'tg_logger'", log:true);
                if (string.IsNullOrEmpty(_token)) _token = creds["apikey"];
                if (string.IsNullOrEmpty(_group)) _group = creds["extra"].Split('/')[0];
                if (string.IsNullOrEmpty(_topic)) _topic = creds["extra"].Split('/')[1];
            }
        }

        private string GetMessageLink(string response)
        {
            try
            {
                // Парсим JSON ответ для получения message_id
                var match = System.Text.RegularExpressions.Regex.Match(response, @"""message_id"":(\d+)");
                if (match.Success)
                {
                    string messageId = match.Groups[1].Value;
                    // Формируем ссылку на сообщение
                    return $"https://t.me/c/{_group.Replace("-100", "")}/{messageId}";
                }
            }
            catch { }
            return null;
        }

        public void Report()
        {
            string time = _project.ExecuteMacro(DateTime.Now.ToString("MM-dd HH:mm"));
            string report = "";

            if (!string.IsNullOrEmpty(_project.Variables["failReport"].Value))
            {
                string encodedFailReport = Uri.EscapeDataString(_project.Variables["failReport"].Value);
                string failUrl = $"https://api.telegram.org/bot{_token}/sendMessage?chat_id={_group}&text={encodedFailReport}&reply_to_message_id={_topic}&parse_mode=MarkdownV2";
                _http.GET(failUrl);
            }
            else
            {
                report = $"✅️ [{time}]{_project.Name}";
                string successReport = $"✅️  \\#{_project.Name.EscapeMarkdown()} \\#{_project.Variables["acc0"].Value} \n";
                string encodedReport = Uri.EscapeDataString(successReport);
                string url = $"https://api.telegram.org/bot{_token}/sendMessage?chat_id={_group}&text={encodedReport}&reply_to_message_id={_topic}&parse_mode=MarkdownV2";
                _http.GET(url);
            }
            string toLog = $"✔️ All jobs done. Elapsed: {_project.TimeElapsed()}s \n███ ██ ██  ██ █  █  █  ▓▓▓ ▓▓ ▓▓  ▓  ▓  ▓  ▒▒▒ ▒▒ ▒▒ ▒  ▒  ░░░ ░░  ░░ ░ ░ ░ ░ ░ ░  ░  ░  ░   ░   ░   ░    ░    ░    ░     ░        ░          ░";
        }

        public string SendMarkdown(string message, bool useMarkdownV2 = false, bool disableWebPagePreview = true, bool replyToTopic = true, bool log = false)
        {
            _log.Send($"Sending markdown message (length: {message.Length})");

            try
            {
                string processedMessage = message;
                string encodedMessage = Uri.EscapeDataString(processedMessage);
                string parseMode = useMarkdownV2 ? "MarkdownV2" : "Markdown";
                string url = $"https://api.telegram.org/bot{_token}/sendMessage" +
                            $"?chat_id={_group}" +
                            $"&text={encodedMessage}" +
                            $"&parse_mode={parseMode}" +
                            $"&disable_web_page_preview={disableWebPagePreview.ToString().ToLower()}";

                if (replyToTopic && !string.IsNullOrEmpty(_topic))
                {
                    url += $"&reply_to_message_id={_topic}";
                }

                string response = _http.GET(url);
                
                if (response.Contains("\"ok\":true"))
                {
                    string messageLink = GetMessageLink(response);
                    _log.Send($"✅ Message sent: {messageLink}");
                    return messageLink;
                }
                else
                {
                    _log.Send($"⚠️ Telegram API error: {response}");
                    return response;
                }
            }
            catch (Exception ex)
            {
                string error = $"❌ Exception: {ex.Message}";
                _log.Send(error);
                return error;
            }
        }

        public string SendCommitsSummary(string summary, bool log = false)
        {
            _log.Send("Sending commits summary");
            string formatted = PrepareCommitsSummary(summary);
            return SendLongMessage(formatted, useMarkdown: false, log: log);
        }

        private string PrepareCommitsSummary(string summary)
        {
            if (summary.Contains("---"))
            {
                int startIndex = summary.IndexOf("---");
                int endIndex = summary.IndexOf("---", startIndex + 3);
                if (endIndex > startIndex)
                {
                    summary = summary.Substring(endIndex + 3).Trim();
                }
            }

            summary = summary
                .Replace("## ", "📌 ")
                .Replace("# ", "🔹 ");

            return summary;
        }

        public string SendLongMessage(string message, bool useMarkdown = false, bool log = false)
        {
            const int maxLength = 4000;

            if (message.Length <= maxLength)
            {
                return useMarkdown 
                    ? SendMarkdown(message, useMarkdownV2: false, log: log)
                    : SendPlainText(message, log: log);
            }

            _log.Send($"Message too long ({message.Length} chars), splitting...");

            string[] parts = SplitMessage(message, maxLength);
            var results = new System.Collections.Generic.List<string>();
            
            for (int i = 0; i < parts.Length; i++)
            {
                _log.Send($"Sending part {i + 1}/{parts.Length}");
                
                string result = useMarkdown 
                    ? SendMarkdown(parts[i], useMarkdownV2: false, log: log)
                    : SendPlainText(parts[i], log: log);
                
                results.Add(result);

                // Если это ошибка (не ссылка), прерываем отправку
                if (!result.StartsWith("https://"))
                {
                    _log.Send($"Failed to send part {i + 1}, stopping");
                    break;
                }

                if (i < parts.Length - 1)
                {
                    System.Threading.Thread.Sleep(500);
                }
            }

            // Возвращаем все ссылки через запятую или последнюю ошибку
            if (results.All(r => r.StartsWith("https://")))
            {
                return string.Join(", ", results);
            }
            else
            {
                return results.Last(r => !r.StartsWith("https://"));
            }
        }

        private string SendPlainText(string message, bool log = false)
        {
            try
            {
                string encodedMessage = Uri.EscapeDataString(message);
                
                string url = $"https://api.telegram.org/bot{_token}/sendMessage" +
                            $"?chat_id={_group}" +
                            $"&text={encodedMessage}";

                if (!string.IsNullOrEmpty(_topic))
                {
                    url += $"&reply_to_message_id={_topic}";
                }

                string response = _http.GET(url);
                
                if (response.Contains("\"ok\":true"))
                {
                    string messageLink = GetMessageLink(response);
                    return messageLink;
                }
                else
                {
                    return response;
                }
            }
            catch (Exception ex)
            {
                return $"❌ Exception: {ex.Message}";
            }
        }

        private string[] SplitMessage(string message, int maxLength)
        {
            var parts = new System.Collections.Generic.List<string>();
            string[] paragraphs = message.Split(new[] { "\n\n" }, StringSplitOptions.None);
            string currentPart = "";
            
            foreach (string paragraph in paragraphs)
            {
                if (currentPart.Length + paragraph.Length + 2 > maxLength)
                {
                    if (!string.IsNullOrEmpty(currentPart))
                    {
                        parts.Add(currentPart.Trim());
                        currentPart = "";
                    }
                    
                    if (paragraph.Length > maxLength)
                    {
                        string[] lines = paragraph.Split('\n');
                        foreach (string line in lines)
                        {
                            if (currentPart.Length + line.Length + 1 > maxLength)
                            {
                                parts.Add(currentPart.Trim());
                                currentPart = line + "\n";
                            }
                            else
                            {
                                currentPart += line + "\n";
                            }
                        }
                    }
                    else
                    {
                        currentPart = paragraph + "\n\n";
                    }
                }
                else
                {
                    currentPart += paragraph + "\n\n";
                }
            }
            
            if (!string.IsNullOrEmpty(currentPart))
            {
                parts.Add(currentPart.Trim());
            }
            
            return parts.ToArray();
        }
    }

}
