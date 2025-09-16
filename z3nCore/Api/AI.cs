using System;

using ZennoLab.InterfacesLibrary.ProjectModel;



namespace z3nCore.Api
{
    public class AI
    {
        protected readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;
        private protected string _apiKey;
        private protected string _url;
        private protected string _model;

        public AI(IZennoPosterProjectModel project, string provider, string model = null, bool log = false)
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
    }
}
