using System;
using System.Collections.Generic;
using System.Linq;
using ZennoLab.InterfacesLibrary.ProjectModel;



namespace z3nCore.Api
{
    public class AI
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;
        private string _apiKey;
        private string _url;
        private string _model;

        public AI(IZennoPosterProjectModel project, string provider = "aiio", string model = null, bool log = false)
        {
            _project = project;
            _logger = new Logger(project, log: log, classEmoji: "AI");
            SetProvider(provider);
            _model = model;
        }

        private void SetProvider(string provider)
        {
            _url = "https://api.intelligence.io.solutions/api/v1/chat/completions";
            _apiKey = _project.SqlGet("api", "__aiio");
            if (string.IsNullOrEmpty(_apiKey))
                throw new Exception($"aiio key not found for {_project.Var("acc0")}");
        }
        
        private static readonly Dictionary<string, string[]> ModelCapabilities = new Dictionary<string, string[]>
        {
            // Reasoning модели (специализация на логических рассуждениях)
            ["moonshotai/Kimi-K2-Thinking"] = new[] { "text", "code", "reasoning" },
            ["deepseek-ai/DeepSeek-R1-0528"] = new[] { "text", "code", "reasoning" },
            ["Qwen/Qwen3-235B-A22B-Thinking-2507"] = new[] { "text", "code", "reasoning" },
        
            // Мультимодальные (текст + изображения)
            ["meta-llama/Llama-3.2-90B-Vision-Instruct"] = new[] { "text", "img", "code" },
            ["Qwen/Qwen2.5-VL-32B-Instruct"] = new[] { "text", "img", "code" },
        
            // Специализированные на коде
            ["Intel/Qwen3-Coder-480B-A35B-Instruct-int4-mixed-ar"] = new[] { "code", "text" },
            ["mistralai/Devstral-Small-2505"] = new[] { "code", "text" },
            ["mistralai/Magistral-Small-2506"] = new[] { "code", "text" },
        
            // Топовые универсальные модели (70B+)
            ["deepseek-ai/DeepSeek-V3.2"] = new[] { "text", "code" },
            ["zai-org/GLM-4.6"] = new[] { "text", "code" },
            ["openai/gpt-oss-120b"] = new[] { "text", "code" },
            ["Qwen/Qwen3-Next-80B-A3B-Instruct"] = new[] { "text", "code" },
            ["meta-llama/Llama-4-Maverick-17B-128E-Instruct-FP8"] = new[] { "text", "code" },
            ["meta-llama/Llama-3.3-70B-Instruct"] = new[] { "text", "code" },
            ["mistralai/Mistral-Large-Instruct-2411"] = new[] { "text", "code" },
        
            // Быстрые/компактные модели
            ["moonshotai/Kimi-K2-Instruct-0905"] = new[] { "text", "code" },
            ["openai/gpt-oss-20b"] = new[] { "text", "code" },
            ["mistralai/Mistral-Nemo-Instruct-2407"] = new[] { "text", "code" }
        };
        
        public static List<string> SetModels(string @using = "all")
        {
            var filter = @using.ToLower().Trim();
        
            if (filter == "all")
            {
                return ModelCapabilities.Keys.ToList();
            }
        
            return ModelCapabilities
                .Where(kvp => kvp.Value.Contains(filter))
                .Select(kvp => kvp.Key)
                .ToList();
        }
        public static string[] GetModelCapabilities(string modelName)
        {
            return ModelCapabilities.TryGetValue(modelName, out var caps) ? caps : Array.Empty<string>();
        }
        
        public static bool SupportsCapability(string modelName, string capability)
        {
            return ModelCapabilities.TryGetValue(modelName, out var caps) && 
                   caps.Contains(capability.ToLower());
        }

        public string Query(string systemContent, string userContent, string aiModel = "rnd", bool log = false, double temperature_ = 0.8, double top_p_ = 0.9, double top_k_ = 0, int presence_penalty_ = 0,int frequency_penalty_ = 1)
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
                temperature = temperature_,
                top_p = top_p_,
                top_k = top_k_,
                stream = false,
                presence_penalty = presence_penalty_,
                frequency_penalty = frequency_penalty_
            };

            string jsonBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody, Newtonsoft.Json.Formatting.None);

            string[] headers = new string[]
            {
                "Content-Type: application/json",
                $"Authorization: Bearer {_apiKey}"
            };

            string response = _project.POST(_url, jsonBody, "", headers, log:log);
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



        public string GoogleAppeal(bool log = false)
        {
            string content = "Generate short brief appeal messge (200 symbols) explaining reasons only for google support explainig situation, return only text of generated message";
            string systemContent = "You are a bit stupid man - user, and sometimes you making mistakes in grammar. Also You are a man \"not realy in IT\". Your account was banned by google. You don't understand why it was happend. 100% you did not wanted to violate any rules even if it happened, but you suppose it was google antifraud mistake";
            return Query(systemContent, content);

        }
    }
}
