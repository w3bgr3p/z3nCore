using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Newtonsoft.Json;
using System.Xml;
using System.Net.Http;
using Formatting = Newtonsoft.Json.Formatting;
using System.Text.RegularExpressions;
using System.Threading.Tasks;



namespace z3nCore.Utilities
{
    public class RssNewsItem
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string FullText { get; set; }
        public string Description { get; set; }
        public DateTime PubDate { get; set; }
        public string Source { get; set; }
    }

    public class RssNewsParser
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _log;
        
        public RssNewsParser(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project;
            _log = new Logger(project, log: log, classEmoji: "RSS");
        }
        private static readonly HttpClient httpClient = new HttpClient();
        
        static RssNewsParser()
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            httpClient.Timeout = TimeSpan.FromSeconds(30);
        }
        
        
        private readonly Dictionary<string, string> rssSources = new Dictionary<string, string>
        {
            { "Decrypt", "https://decrypt.co/feed" },
            { "Bitcoin Magazine", "https://bitcoinmagazine.com/.rss/full/" },
            { "CryptoSlate", "https://cryptoslate.com/feed/" },
            { "BeInCrypto", "https://beincrypto.com/feed/" },
            { "U.Today", "https://u.today/rss" },
            { "Bitcoinist", "https://bitcoinist.com/feed/" },
            { "NewsBTC", "https://www.newsbtc.com/feed/" },
            { "Blockworks", "https://blockworks.co/feed" },
            { "CoinJournal", "https://coinjournal.net/feed/" },
            { "AMBCrypto", "https://ambcrypto.com/feed/" }
        };

        public async Task ParseAndSaveNewsAsync()
        {
            List<RssNewsItem> todayNews = new List<RssNewsItem>();
            DateTime today = DateTime.Today;

            foreach (KeyValuePair<string, string> source in rssSources)
            {
                try
                {
                    List<RssNewsItem> news = await ParseRssFeedAsync(source.Key, source.Value);
                    List<RssNewsItem> todayItems = news.Where(n => n.PubDate.Date == today).ToList();
                    todayNews.AddRange(todayItems);
                    
                    _log.Send($"Получено {todayItems.Count} новостей за сегодня с {source.Key}");
                }
                catch (Exception ex)
                {
                    _log.Send($"Ошибка при парсинге {source.Key}: {ex.Message}");
                }
            }

            if (todayNews.Any())
            {
                ClearNewsDirectory();
                
                SaveNewsToSeparateFiles(todayNews);
                
                _log.Send($"\nВсего сохранено {todayNews.Count} новостей за сегодня");
            }
            else
            {
                _log.Send("Новостей за сегодня не найдено");
            }
        }
        
        private void ClearNewsDirectory()
        {
            var outputPath = Path.Combine(_project.Path, ".data", "news");
            if (Directory.Exists(outputPath))
            {
                foreach (string file in Directory.GetFiles(outputPath))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        _log.Send($"Ошибка удаления файла {file}: {ex.Message}");
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(outputPath);
            }
        }
        
        private void SaveNewsToSeparateFiles(List<RssNewsItem> news)
        {
            var outputPath = Path.Combine(_project.Path, ".data", "news");
            
            var sortedNews = news.OrderByDescending(n => n.PubDate).ToList();
            
            for (int i = 0; i < sortedNews.Count; i++)
            {
                int fileNumber = i + 1;
                RssNewsItem item = sortedNews[i];
                
                SaveSingleNewsToTextFile(item, fileNumber, outputPath);
                
                SaveSingleNewsToJsonFile(item, fileNumber, outputPath);
            }
            
            _log.Send($"Сохранено {sortedNews.Count} файлов (TXT и JSON)");
        }
        
        private void SaveSingleNewsToTextFile(RssNewsItem item, int number, string outputPath)
        {
            try
            {
                string fileName = Path.Combine(outputPath, $"{number}.txt");
                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"Источник: {item.Source}");
                sb.AppendLine($"Заголовок: {item.Title}");
                sb.AppendLine($"Дата: {item.PubDate:dd.MM.yyyy HH:mm}");
                sb.AppendLine($"Ссылка: {item.Link}");
                sb.AppendLine();
                sb.AppendLine("=== ОПИСАНИЕ ===");
                sb.AppendLine(item.Description);
                sb.AppendLine();
                sb.AppendLine("=== ПОЛНЫЙ ТЕКСТ ===");
                sb.AppendLine(item.FullText);

                File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _log.Send($"Ошибка сохранения TXT файла #{number}: {ex.Message}");
            }
        }
        
        private void SaveSingleNewsToJsonFile(RssNewsItem item, int number, string outputPath)
        {
            try
            {
                string fileName = Path.Combine(outputPath, $"{number}.json");
                string json = JsonConvert.SerializeObject(item, Formatting.Indented);
                
                File.WriteAllText(fileName, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _log.Send($"Ошибка сохранения JSON файла #{number}: {ex.Message}");
            }
        }
        
        private async Task<List<RssNewsItem>> ParseRssFeedAsync(string sourceName, string rssUrl)
        {
            List<RssNewsItem> news = new List<RssNewsItem>();
            
            string response = await httpClient.GetStringAsync(rssUrl);
            
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(response);

            XmlNodeList itemNodes = xmlDoc.GetElementsByTagName("item");

            foreach (XmlNode itemNode in itemNodes)
            {
                try
                {
                    string title = GetNodeValue(itemNode, "title");
                    string link = GetNodeValue(itemNode, "link");
                    string description = GetNodeValue(itemNode, "description");
                    string pubDateStr = GetNodeValue(itemNode, "pubDate");

                    RssNewsItem newsItem = new RssNewsItem
                    {
                        Title = string.IsNullOrEmpty(title) ? "Без заголовка" : title,
                        Link = link,
                        Description = StripHtml(description),
                        FullText = await FetchFullArticleAsync(link),
                        PubDate = ParsePubDate(pubDateStr),
                        Source = sourceName
                    };
                    
                    news.Add(newsItem);
                }
                catch (Exception ex)
                {
                    _log.Send($"Ошибка при парсинге элемента: {ex.Message}");
                }
            }

            return news;
        }
        
        private string GetNodeValue(XmlNode parentNode, string nodeName)
        {
            XmlNode node = parentNode.SelectSingleNode(nodeName);
            return node?.InnerText ?? string.Empty;
        }

        private DateTime ParsePubDate(string pubDateString)
        {
            if (string.IsNullOrEmpty(pubDateString))
                return DateTime.MinValue;

            DateTime result;
            if (DateTime.TryParse(pubDateString, out result))
                return result;

            return DateTime.MinValue;
        }

        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            string result = Regex.Replace(html, "<.*?>", string.Empty);
            return System.Net.WebUtility.HtmlDecode(result);
        }
        
        private async Task<string> FetchFullArticleAsync(string url)
    {
        try
        {
            await Task.Delay(1000);
            string html = await httpClient.GetStringAsync(url);

            var matches = Regex.Matches(html, @"<p[^>]*>(.*?)</p>", RegexOptions.Singleline);
            StringBuilder articleText = new StringBuilder();

            foreach (Match match in matches)
            {
                string paragraph = StripHtml(match.Groups[1].Value);
                if (!string.IsNullOrWhiteSpace(paragraph) && paragraph.Length > 50)
                {
                    articleText.AppendLine(paragraph);
                    articleText.AppendLine();
                }
            }

            string rawText = articleText.ToString().Trim();
            
            string cleanedText = CleanArticleText(rawText);
            
            return cleanedText;
        }
        catch (Exception ex)
        {
            _log.Send($"Ошибка загрузки статьи {url}: {ex.Message}");
            return string.Empty;
        }
    }

        private string CleanArticleText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Удаляем JSON блоки
            text = Regex.Replace(text, @"\{[^}]{100,}\}", "", RegexOptions.Singleline);
            text = Regex.Replace(text, @"data-[a-z-]+=""[^""]*""", "", RegexOptions.IgnoreCase);
            
            // Удаляем CSS блоки
            text = Regex.Replace(text, @"\.[\w-]+\s*\{[^}]*\}", "", RegexOptions.Singleline);
            text = Regex.Replace(text, @"(fill|stroke|stroke-width|stroke-miterlimit|isolation|fill-rule):\s*[^;]+;?", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\b(fill|stroke|stroke-width|none|evenodd|isolate):\s*[#\w\d()]+;?", "", RegexOptions.IgnoreCase);
            
            // Удаляем SVG и HTML атрибуты
            text = Regex.Replace(text, @"<path[^>]*d=""[^""]+""[^>]*>", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"d=""[MmLlHhVvCcSsQqTtAaZz0-9,.\s-]+""", "", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"(aria-label|autocomplete|role|xmlns|version|viewBox|xml:space|enable-background|style|class|id)=""[^""]*""", "", RegexOptions.IgnoreCase);
            
            // Удаляем JavaScript
            text = Regex.Replace(text, @"window\.addEventListener\([^)]+\)", "", RegexOptions.Singleline);
            text = Regex.Replace(text, @"jQuery\([^)]+\)[^;]+;", "", RegexOptions.Singleline);
            
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            StringBuilder cleaned = new StringBuilder();
            
            // Для отслеживания заголовка статьи (первое длинное предложение считаем заголовком)
            string articleTitle = "";
            bool titleFound = false;
            
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;
                
                // Определяем заголовок статьи (первая длинная строка > 40 символов)
                if (!titleFound && trimmedLine.Length > 40)
                {
                    articleTitle = trimmedLine;
                    titleFound = true;
                }
                
                // Пропускаем повторения заголовка
                if (titleFound && trimmedLine == articleTitle)
                    continue;
                
                if (IsCssOrSvgLine(trimmedLine))
                    continue;
                
                if (IsHtmlFormLine(trimmedLine))
                    continue;
                    
                // Минимальная длина строки - 40 символов (было 30)
                if (trimmedLine.Length < 40)
                    continue;
                
                if (IsGarbageLine(trimmedLine))
                    continue;
                
                // Пропускаем строки с email
                if (Regex.IsMatch(trimmedLine, @"[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}", RegexOptions.IgnoreCase))
                    continue;
                
                // Пропускаем строки, которые являются ссылками на другие статьи
                if (IsRelatedArticleLink(trimmedLine))
                    continue;
                
                cleaned.AppendLine(trimmedLine);
            }
            
            string result = cleaned.ToString();
            
            // Удаляем множественные пустые строки
            result = Regex.Replace(result, @"(\r?\n){3,}", "\n\n");
            result = result.Trim();
            
            return result;
        }

        private bool IsRelatedArticleLink(string line)
        {
            // Типичные паттерны заголовков других статей в конце страницы
            string[] relatedPatterns = new[]
            {
                @"faces ""[^""]+""",  // Trump faces "pay-for-crime"
                @"– (Bulls|Bears|Why|What|How)",  // Bitcoin – Bulls, mind THESE
                @"adds \w+ for",  // Fidelity adds Solana for
                @"files for \$\d+[BMK]",  // files for $1B
                @"Should \w+ .+ start",  // Should Bitcoin bears start
                @"battles to stay",  // Bitcoin battles to stay
                @"\w+ ETF momentum",  // global ETF momentum
                @"U\.S\. clients as",  // U.S. clients as
                @"public offering to",  // public offering to
                @"short squeeze soon",  // short squeeze soon
                @"aren't celebrating yet",  // aren't celebrating yet
                @"after pardoning",  // after pardoning
                @"mind THESE \d+ levels",  // mind THESE 2 levels
                @"breakout attempt$",  // breakout attempt
                @"heats up$",  // heats up
                @"bolster \w+ treasury$",  // bolster HYPE treasury
            };
            
            foreach (string pattern in relatedPatterns)
            {
                if (Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase))
                    return true;
            }
            
            // Проверка на типичную структуру ссылки (Название – Описание)
            if (Regex.IsMatch(line, @"^[\w\s]+ – [\w\s]+$"))
                return true;
            
            return false;
        }

        private bool IsHtmlFormLine(string line)
        {
            string[] htmlPatterns = new[]
            {
                @"^<(form|input|div|svg|path)",
                @"aria-label=",
                @"autocomplete=",
                @"data-asl",
                @"xmlns=",
                @"viewBox=",
                @"enable-background=",
                @"xml:space=",
                @"id='ajax",
                @"class=""asl_",
                @"class='vertical",
                @"name='options'",
                @"type=""checkbox""",
                @"type='search'",
                @"placeholder=",
                @"value=""exact""",
                @"checked=""checked""",
                @"display:none",
                @"!important"
            };
            
            foreach (string pattern in htmlPatterns)
            {
                if (Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase))
                    return true;
            }
            
            return false;
        }

        private bool IsCssOrSvgLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return true;
                
            string[] cssPatterns = new[]
            {
                @"^\.cls-\d+",
                @"^\.st\d+",
                @"^fill:",
                @"^stroke:",
                @"^stroke-width:",
                @"^stroke-miterlimit:",
                @"^isolation:",
                @"^fill-rule:",
                @"^\{\s*$",
                @"^\}\s*$",
                @"^[\w-]+:\s*[#\w\d()]+;?\s*$",
                @"^""[a-z_]+"":",
                @"ew0KCS",
                @"^data-",
                @"xmlns:",
                @"M\d+\.?\d*,\d+\.?\d*[LlHhVvCcSs]",
                @"^style=",
                @"^class=",
                @"^id=",
            };
            
            foreach (string pattern in cssPatterns)
            {
                if (Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase))
                    return true;
            }
            
            if (line.Contains("fill:") || line.Contains("stroke:") || 
                line.Contains(".cls-") || line.Contains(".st0") || 
                line.Contains(".st1") || line.Contains("xmlns") ||
                line.Contains("viewBox") || line.Contains("data-asl") ||
                line.Contains("jQuery") || line.Contains("window.") || 
                line.Contains("addEventListener") || line.Contains("function()"))
                return true;
            
            return false;
        }

        private bool IsGarbageLine(string line)
        {
            int specialChars = line.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
            int totalChars = line.Length;
            
            if (totalChars > 0 && (double)specialChars / totalChars > 0.4)
                return true;
            
            string[] navigationPatterns = new[]
            {
                @"^(News|Market|Coins|Videos|Deep Dives|University|Event Calendar|Podcast|Discover|Trump|Binance|Fidelity|Hyperliquid|Should Bitcoin)",
                @"^(About|Team|Disclosures|Manifesto|Terms|Privacy|Contact|Careers|Partner)",
                @"^SUBSCRIBE TO",
                @"^© A next-generation",
                @"^\d{4} (Decrypt|AMBCrypto|Crypto)",
                @"Price data by",
                @"Image: Shutterstock",
                @"Create an account",
                @"In brief",
                @"^Active Currencies \d+",
                @"^Bitcoin Share \d+",
                @"^24h Market Cap",
                @"Share \d+%",
                @"should not be interpreted as investment advice",
                @"Trading, buying or selling",
                @"do their own research",
                @"meant to be informational",
                @"@ambcrypto\.com$",
                @"^(partners|editor|advertise)@",
                @"Discover how \w+ is",  // Discover how Digitap is
                @"checking out their project",  // checking out their project
                @"their project here:",  // their project here:
            };
            
            foreach (string pattern in navigationPatterns)
            {
                if (Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase))
                    return true;
            }
            
            return false;
        }
        
        public void ParseAndSaveNewsSync()
        {
            ParseAndSaveNewsAsync().GetAwaiter().GetResult();
        }
    }
}