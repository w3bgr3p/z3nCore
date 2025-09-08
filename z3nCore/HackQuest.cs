using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ZennoLab.CommandCenter;
using System.IO;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class HackQuest
    {

        public static List<string> AnsList (int lenght )
        {
            var result = new List<string>();
            for (int i = 0; i < (1 << lenght); i++)
            {
                string s = Convert.ToString(i, 2).PadLeft(lenght, '0');
                result.Add(s);
            }

            result.Remove("0000");
            return result;
        }

        public static string GetPos(IZennoPosterProjectModel project, Instance instance)
        {
            var position = "";
            var variants =  instance.ActiveTab.FindElementByAttribute("div", "class", "\\ flex\\ w-full\\ flex-wrap\\ items-stretch\\ gap-4", "regexp", 0).GetChildren(false).ToList();
            foreach (var variant in variants)
            {
                var btnClass = variant.GetAttribute("class");
                if (btnClass == "flex cursor-pointer items-center gap-4 rounded-xl border p-4 text-neutral-800 transition-all duration-200 max-sm:w-full sm:w-[calc((100%-1rem)/2)] border-destructive-600 bg-destructive-50")
                {
                    position += "!";
                }
                else if (btnClass == "flex cursor-pointer items-center gap-4 rounded-xl border p-4 text-neutral-800 transition-all duration-200 max-sm:w-full sm:w-[calc((100%-1rem)/2)] border-primary-600 bg-primary-50")
                {
                    position += "1";
                }		
                else if (btnClass == "flex cursor-pointer items-center gap-4 rounded-xl border border-neutral-300 p-4 text-neutral-800 transition-all duration-200 max-sm:w-full sm:w-[calc((100%-1rem)/2)] hover:bg-neutral-100")
                {
                    position += "0";
                }				
                else if (btnClass == "flex cursor-pointer items-center gap-4 rounded-xl border p-4 text-neutral-800 transition-all duration-200 max-sm:w-full sm:w-[calc((100%-1rem)/2)] border-success-600 bg-success-50")
                {
                    position += "+";
                }	               
                
            }
            return position;
        }

        public static string SetPos(IZennoPosterProjectModel project, Instance instance, string answer)
        {
            var position = "";
            var variants =  instance.ActiveTab.FindElementByAttribute("div", "class", "\\ flex\\ w-full\\ flex-wrap\\ items-stretch\\ gap-4", "regexp", 0).GetChildren(false).ToList();
            var chars = answer.Select(c => c.ToString()).ToList();
            int charIndex = 0;
            foreach (var variant in variants)
            {
                var ans = chars[charIndex];
                var btnClass = variant.GetAttribute("class");
                if (btnClass == "flex cursor-pointer items-center gap-4 rounded-xl border p-4 text-neutral-800 transition-all duration-200 max-sm:w-full sm:w-[calc((100%-1rem)/2)] border-destructive-600 bg-destructive-50")
                {
                    position = "!";
                }
                else if (btnClass == "flex cursor-pointer items-center gap-4 rounded-xl border p-4 text-neutral-800 transition-all duration-200 max-sm:w-full sm:w-[calc((100%-1rem)/2)] border-primary-600 bg-primary-50")
                {
                    position = "1";
                }		
                else if (btnClass == "flex cursor-pointer items-center gap-4 rounded-xl border border-neutral-300 p-4 text-neutral-800 transition-all duration-200 max-sm:w-full sm:w-[calc((100%-1rem)/2)] hover:bg-neutral-100")
                {
                    position = "0";
                }
                if (ans != position) 
                    instance.HeClick(variant);
                charIndex++;
            }
            return "";

        }

        public static string ChoiceFillButton(IZennoPosterProjectModel project, Instance instance)
        {
            var paragraps =  instance.ActiveTab.FindElementsByAttribute("div", "datatype", "paragraph", "regexp").ToList();
            var quest = paragraps[paragraps.Count - 1];
            HtmlElement button = null;
            foreach (var he in quest.GetChildren(false))
            {
                if (he.FullTagName == "button")
                {
                    button = he;
                    break;
                }
            }
            var btnClass = button.GetAttribute("class");
            if (btnClass == "mx-1 inline-flex h-6 min-w-20 items-center justify-center rounded border align-middle text-xs border-destructive-600 bg-destructive-50")
            {
               return ("wrong");
            }
            else if (btnClass == "mx-1 inline-flex h-6 min-w-20 items-center justify-center rounded border border-primary-600 bg-primary-100 align-middle text-xs")
            {
                return ("unset");
            }          
            
            else if (btnClass.Contains("success"))
            {
                string toList = $"{quest.InnerText.Trim().Replace("\n", "|").Trim('|')}";
                return toList;
            }
            else return "undefined";
        }

        public static HtmlElement Quest(IZennoPosterProjectModel project, Instance instance)
        {
            var minusStep = 1;
            var paragraps =  instance.ActiveTab.FindElementsByAttribute("div", "datatype", "paragraph", "regexp").ToList();
            get:
            var quest = paragraps[paragraps.Count - minusStep];
            if (quest.InnerText == "")
            {
                minusStep ++;
                goto get;
            }

            return quest;
        }
        
        public static HtmlElement ChoiceFillBtn(IZennoPosterProjectModel project, Instance instance)
        {
            var quest = Quest(project, instance);
            foreach (var he in quest.GetChildren(false))
            {
                if (he.FullTagName == "button") 
                    return he;
            }
            project.Throw("notFound");
            return null;
        }
        
        public static string QuestType(IZennoPosterProjectModel project, Instance instance)
        {
            var questMode = instance.HeGet(("span", "class", "body-s\\ inline-block\\ whitespace-nowrap\\ text-neutral-500", "regexp", 0));
            return questMode;
        }

        public static void DropQuackCoin(IZennoPosterProjectModel project, Instance instance, int maxDelay = 5)
        {
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            long startTime = currentTime;
            int difference = 0;
            work:
            var r = new Random().Next(1,maxDelay);
            Thread.Sleep(1000 * r);
            var coins = int.Parse(instance.HeGet(("span", "class", "font-bold\\ text-neutral-500\\ text-sm", "regexp", 0)));
            if (coins >= 5 ) 
            {
                var stats = instance.HeGet(("div", "class", "body-xs\\ relative\\ flex\\ h-5\\ w-full\\ items-center\\ justify-center\\ text-neutral-800", "regexp", 0)).Split('\n');
                
                var lvl = stats[0];
                var pts = stats[2];
                currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                difference = (int)(currentTime - startTime);
                project.SendInfoToLog($"time : {difference}s, coins: {coins} ,  lvl: {lvl}, points: {pts}", true);
                instance.HeClick(("span", "innertext", "Drop\\ Quack\\ Coin", "regexp", 0));
                goto work;
            }



        }

        public static string BtnState(HtmlElement variant)
        {
            var btnClass = variant.GetAttribute("class");
            var state = "undefined";
            if (btnClass == "flex cursor-pointer items-center gap-4 rounded-xl border p-4 text-neutral-800 transition-all duration-200 max-sm:w-full sm:w-[calc((100%-1rem)/2)] border-destructive-600 bg-destructive-50")
            {
                state = "wrong";
            }
            else if (btnClass == "flex cursor-pointer items-center gap-4 rounded-xl border p-4 text-neutral-800 transition-all duration-200 max-sm:w-full sm:w-[calc((100%-1rem)/2)] border-primary-600 bg-primary-50")
            {
                state = "set";
            }		
            else if (btnClass == "flex cursor-pointer items-center gap-4 rounded-xl border border-neutral-300 p-4 text-neutral-800 transition-all duration-200 max-sm:w-full sm:w-[calc((100%-1rem)/2)] hover:bg-neutral-100")
            {
                state = "unset";
            }
            else if (btnClass.Contains("success"))
            {
                state = "correct";
            }
            return state;

        }
        
        public static string QuizBtnState(HtmlElement variant)
        {
            var btnClass = variant.GetAttribute("class");
            var state = "undefined";
            if (btnClass == "flex cursor-pointer items-center gap-4 rounded-xl border p-4 text-neutral-800 transition-all duration-200 max-sm:w-full sm:w-[calc((100%-1rem)/2)] border-destructive-600 bg-destructive-50")
            {
                state = "wrong";
            }
            else if (btnClass == "flex cursor-pointer items-center gap-4 rounded-xl border p-4 text-neutral-800 transition-all duration-200 max-sm:w-full sm:w-[calc((100%-1rem)/2)] border-primary-600 bg-primary-50")
            {
                state = "set";
            }		
            else if (btnClass == "flex cursor-pointer items-center gap-4 rounded-xl border border-neutral-300 p-4 text-neutral-800 transition-all duration-200 max-sm:w-full sm:w-[calc((100%-1rem)/2)] hover:bg-neutral-100")
            {
                state = "unset";
            }
            else if (btnClass.Contains("success"))
            {
                state = "correct";
            }
            return state;

        }
        public static string QuizBtnsState(IZennoPosterProjectModel project, Instance instance)
        {
            var	answers =  instance.ActiveTab.FindElementByAttribute("div", "class", "\\ flex\\ w-full\\ flex-wrap\\ items-stretch\\ gap-4", "regexp", 0).GetChildren(false).ToList();
            var result = new List<string>();
            foreach (var ans in answers) 
            {
                var btnState = HackQuest.BtnState(ans);
                switch(btnState)
                {
                    case "wrong":
                        result.Add(ans.InnerText+"✖");
                        continue;
                    case "unset":
                        result.Add(ans.InnerText+"□");
                        continue;
                    case "set":
                        result.Add(ans.InnerText+"■");
                        continue;
                    case "correct":
                        result.Add(ans.InnerText+"✔");
                        continue;
                    default:
                        project.SendWarningToLog($"{btnState} | {ans.InnerText}");
                        continue;
                }
                
                
                
            }

            string resultS = string.Join("○",result);
            return resultS;

        }

    }
    

    
    public class AI2
    {
        protected readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;
        private protected string _apiKey;
        private protected string _url;
        private protected string _model;

        public AI2(IZennoPosterProjectModel project, string provider, string model = null, bool log = false)
        {
            _project = project;
            _logger = new Logger(project, log: log, classEmoji: "AI");
            SetProvider(provider);
            _model = model;
        }

        private void SetProvider(string provider)
        {

            switch (provider)
            {
                case "perplexity":
                    _url = "https://api.perplexity.ai/chat/completions";
                    _apiKey = _project.SqlGet("apikey", "_api", where: $"key = '{provider}'");
                    break;
                case "aiio":
                    _url = "https://api.intelligence.io.solutions/api/v1/chat/completions";
                    _apiKey = _project.SqlGet("api", "__aiio");
                    if (string.IsNullOrEmpty(_apiKey))
                        throw new Exception($"aiio key not found for {_project.Var("acc0")}");
                    break;
                default:
                    throw new Exception($"unknown provider {provider}");
            }
        }

        public string Query(string systemContent, string userContent, string aiModel = "rnd", bool log = false)
        {
            if (_model != null) aiModel = _model;
            if (aiModel == "rnd") aiModel = ZennoLab.Macros.TextProcessing.Spintax("{deepseek-ai/DeepSeek-R1-0528|meta-llama/Llama-4-Maverick-17B-128E-Instruct-FP8|Qwen/Qwen3-235B-A22B-FP8|meta-llama/Llama-3.2-90B-Vision-Instruct|Qwen/Qwen2.5-VL-32B-Instruct|google/gemma-3-27b-it|meta-llama/Llama-3.3-70B-Instruct|mistralai/Devstral-Small-2505|mistralai/Magistral-Small-2506|deepseek-ai/DeepSeek-R1-Distill-Llama-70B|netease-youdao/Confucius-o1-14B|nvidia/AceMath-7B-Instruct|deepseek-ai/DeepSeek-R1-Distill-Qwen-32B|mistralai/Mistral-Large-Instruct-2411|microsoft/phi-4|bespokelabs/Bespoke-Stratos-32B|THUDM/glm-4-9b-chat|CohereForAI/aya-expanse-32b|openbmb/MiniCPM3-4B|mistralai/Ministral-8B-Instruct-2410|ibm-granite/granite-3.1-8b-instruct}", false);
            _logger.Send(aiModel);
            var requestBody = new
            {
                model = aiModel, 
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = systemContent
                    },
                    new
                    {
                        role = "user",
                        content = userContent
                    }
                },
                temperature = 0.8,
                top_p = 0.9,
                top_k = 0,
                stream = false,
                presence_penalty = 0,
                frequency_penalty = 1
            };

            string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody, Newtonsoft.Json.Formatting.None);

            string[] headers = new string[]
            {
                "Content-Type: application/json",
                $"Authorization: Bearer {_apiKey}"
            };

            string response = _project.POST(_url, jsonBody, "", headers, log);
            _logger.Send($"Full response: {response}");

            try
            {
                var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(response);
                string Text = jsonResponse["choices"][0]["message"]["content"].ToString();
                _logger.Send(Text);
                return Text;
            }
            catch (Exception ex)
            {
                _logger.Send($"!W Error parsing response: {ex.Message}");
                throw;
            }
        }

        public string GenerateTweet(string content, string bio = "", bool log = false)
        {
            string systemContent = string.IsNullOrEmpty(bio)
                            ? "You are a social media account. Generate tweets that reflect a generic social media persona."
                            : $"You are a social media account with the bio: '{bio}'. Generate tweets that reflect this persona, incorporating themes relevant to bio.";

        gen:
            string tweetText = Query(systemContent, content);
            if (tweetText.Length > 220)
            {
                _logger.Send($"tweet is over 220sym `y");
                goto gen;
            }
            return tweetText;

        }

        public string OptimizeCode(string content, bool log = false)
        {
            string systemContent = "You are a web3 developer. Optimize the following code. Return only the optimized code. Do not add explanations, comments, or formatting. Output code only, in plain text.";
            return Query(systemContent, content,log:log);

        }

        public string GoogleAppeal(bool log = false)
        {
            string content = "Generate short brief appeal messge (200 symbols) explaining reasons only for google support explainig situation, return only text of generated message";
            string systemContent = "You are a bit stupid man - user, and sometimes you making mistakes in grammar. Also You are a man \"not realy in IT\". Your account was banned by google. You don't understand why it was happend. 100% you did not wanted to violate any rules even if it happened, but you suppose it was google antifraud mistake";
            return Query(systemContent, content);

        }

        public string QueryWithImage(string systemContent, string userContent, string imagePath, string aiModel = "rnd", bool log = false)
        {
            // Чтение BMP в byte[], конвертация в base64
            byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath); // BMP -> byte[]
            string imgBase64 = Convert.ToBase64String(imageBytes);       // byte[] -> base64

            // Формируем messages вручную через массив объектов одинаковой структуры
            var messages = new object[]
            {
                new { role = "system", content = systemContent, image = (string)null },
                new { role = "user", content = userContent, image = imgBase64 }
            };

            var requestBody = new
            {
                model = _model ?? aiModel,
                messages = messages,
                temperature = 0.8,
                top_p = 0.9,
                top_k = 0,
                stream = false,
                presence_penalty = 0,
                frequency_penalty = 1
            };

            string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody, Newtonsoft.Json.Formatting.None);
            string[] headers = new string[]
            {
                "Content-Type: application/json",
                $"Authorization: Bearer {_apiKey}"
            };
            string response = _project.POST(_url, jsonBody, "", headers, log);
            _logger.Send($"Full response: {response}");
            try
            {
                var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(response);
                string Text = jsonResponse["choices"][0]["message"]["content"].ToString();
                _logger.Send(Text);
                return Text;
            }
            catch (Exception ex)
            {
                _logger.Send($"!W Error parsing response: {ex.Message}");
                throw;
            }
        }

        public string QueryWithImage2(string systemContent, string userContent, string imagePath, string aiModel = "rnd", bool log = false)
        {
            if (_model != null) aiModel = _model;
            if (aiModel == "rnd") aiModel = ZennoLab.Macros.TextProcessing.Spintax("{meta-llama/Llama-3.2-90B-Vision-Instruct|Qwen/Qwen2.5-VL-32B-Instruct}", false);
            _logger.Send(aiModel);

            if (!File.Exists(imagePath))
            {
                throw new Exception("Image file not found: " + imagePath);
            }

            byte[] imageBytes = File.ReadAllBytes(imagePath);
            string base64Image = Convert.ToBase64String(imageBytes);
            string mimeType = "image/bmp";

            string extension = Path.GetExtension(imagePath).ToLower();
            switch (extension)
            {
                case ".bmp":
                    mimeType = "image/bmp";
                    break;
                case ".jpg":
                case ".jpeg":
                    mimeType = "image/jpeg";
                    break;
                case ".png":
                    mimeType = "image/png";
                    break;
                case ".gif":
                    mimeType = "image/gif";
                    break;
            }

            string jsonBody = "{\"model\":\"" + aiModel + "\",\"messages\":[{\"role\":\"system\",\"content\":\"" + systemContent.Replace("\"", "\\\"") + "\"},{\"role\":\"user\",\"content\":[{\"type\":\"text\",\"text\":\"" + userContent.Replace("\"", "\\\"") + "\"},{\"type\":\"image_url\",\"image_url\":{\"url\":\"data:" + mimeType + ";base64," + base64Image + "\"}}]}],\"temperature\":0.8,\"top_p\":0.9,\"stream\":false,\"max_tokens\":1000}";
            
            string[] headers = new string[]
            {
                "Content-Type: application/json",
                "Authorization: Bearer " + _apiKey
            };

            string response = _project.POST(_url, jsonBody, "", headers, log);
            _logger.Send("Full response: " + response);

            try
            {
                var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(response);
                string Text = jsonResponse["choices"][0]["message"]["content"].ToString();
                _logger.Send(Text);
                return Text;
            }
            catch (Exception ex)
            {
                _logger.Send("!W Error parsing response: " + ex.Message);
                throw;
            }
        }
    }
    
    
}
