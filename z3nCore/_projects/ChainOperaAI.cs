using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class ChainOperaAI
    {

        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;
        private readonly object LockObject = new object();
        
        public ChainOperaAI(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _logger = new Logger(project, log: log, classEmoji: "ChainOpera");

        }
        public void GetAuthData()
        {
            try
            {

                _instance.HeClick(("button", "innertext", "0x", "regexp", 0), deadline: 5);
                var headers =
                    new Traffic(_project, _instance).Get("https://chat.chainopera.ai/userCenter/api/", "RequestHeaders");

                foreach (string header in headers.Split('\n'))
                {
                    if (header.ToLower().Contains("authorization"))
                    {
                        _project.Var("token", header.Split(':')[1]);
                    }

                    if (header.ToLower().Contains("cookie"))
                    {
                        _project.Var("cookie", header.Split(':')[1]);
                    }

                }

                if (_project.Var("token") == "") throw new Exception("catched empty token");
                
                _instance.HeClick(
                    ("button", "class",
                        "inline-flex\\ items-center\\ justify-center\\ whitespace-nowrap\\ text-sm\\ font-medium\\ transition-colors\\ focus-visible:outline-hidden\\ focus-visible:ring-1\\ focus-visible:ring-primary\\ disabled:pointer-events-none\\ disabled:opacity-50\\ border-none\\ hover:bg-accent\\ hover:text-accent-foreground\\ active:bg-transparent\\ h-9\\ w-9\\ p-0\\ absolute\\ left-4\\ top-3\\ z-2\\ rounded-full\\ shadow-md",
                        "regexp", 0), deadline: 5, thr0w: false);
            }
            catch (Exception ex)
            {
                _project.SendWarningToLog(ex.Message);
                throw;
            }


        }
        public string ReqGet(string path, bool parse = true, bool log = false)
        {

            string token = _project.Variables["token"].Value;
            string cookie = _project.Variables["cookie"].Value;

            var headers = new Dictionary<string, string>
            {
                { "authority", "chat.chainopera.ai" },
                { "authorization", $"{token}" },
                { "method", "GET" },
                { "path", path },
                { "accept", "application/json, text/plain, */*" },
                { "accept-encoding", "gzip, deflate, br" },
                { "accept-language", "en-US,en;q=0.9" },
                { "content-type", "application/json" },
                { "origin", "https://chat.chainopera.ai" },
                { "priority", "u=1, i" },

                { "sec-ch-ua", "\"Chromium\";v=\"134\", \"Not:A-Brand\";v=\"24\", \"Google Chrome\";v=\"134\"" },
                { "sec-ch-ua-mobile", "?0" },
                { "sec-ch-ua-platform", "\"Windows\"" },
                { "sec-fetch-dest", "empty" },
                { "sec-fetch-mode", "cors" },
                { "sec-fetch-site", "same-site" },

                { "cookie", $"{cookie}" },
            };



            string[] headerArray = headers.Select(header => $"{header.Key}:{header.Value}").ToArray();
            string url = $"https://chat.chainopera.ai{path}";
            string response;

            try
            {
                response = _project.GET(url, "+", headerArray, log: false, parse);
                _logger.Send(response);
            }
            catch (Exception ex)
            {
                _project.SendErrorToLog($"Err HTTPreq: {ex.Message}");
                throw;
            }

            return response;


        }
        public string ReqGetAgent(string path, bool parse = false, bool log = false)
        {

            string token = _project.Variables["token"].Value;
            string cookie = _project.Variables["cookie"].Value;

            var headers = new Dictionary<string, string>
            {
                { "authority", "agent.chainopera.ai" },
                { "authorization", $"{token}" },
                { "method", "GET" },
                { "path", path },
                { "accept", "application/json, text/plain, */*" },
                { "accept-encoding", "gzip, deflate, br" },
                { "accept-language", "en-US,en;q=0.9" },
                { "content-type", "application/json" },
                { "origin", "https://agent.chainopera.ai" },
                { "priority", "u=1, i" },

                { "sec-ch-ua", "\"Chromium\";v=\"134\", \"Not:A-Brand\";v=\"24\", \"Google Chrome\";v=\"134\"" },
                { "sec-ch-ua-mobile", "?0" },
                { "sec-ch-ua-platform", "\"Windows\"" },
                { "sec-fetch-dest", "empty" },
                { "sec-fetch-mode", "cors" },
                { "sec-fetch-site", "same-site" },

                { "cookie", $"{cookie}" },
            };



            string[] headerArray = headers.Select(header => $"{header.Key}:{header.Value}").ToArray();
            string url = $"https://agent.chainopera.ai{path}";
            string response;

            try
            {
                response = _project.GET(url, "+", headerArray, log: false, parse);
                _logger.Send(response);
            }
            catch (Exception ex)
            {
                _project.SendErrorToLog($"Err HTTPreq: {ex.Message}");
                throw;
            }

            return response;


        }

        public bool IsCheckedIn()
        {
            ReqGet("/userCenter/api/v1/ai/terminal/getRateLimit", true);
            bool checkedIn =  _project.Json.data; 
            return checkedIn;
        }
        public decimal ChekInPrice()
        {
            ReqGet("/userCenter/api/v1/ai/terminal/checkInNetworkList", true);
            var checkInAmount =  _project.Json.data[0].checkInAmount; 
            return decimal.Parse(checkInAmount);
        }

        public double PromptRatio()
        {
            ReqGet("/userCenter/api/v1/ai/terminal/getPromptPoints", true);
            var today =  _project.Json.data.todayPoints; 
            var ratio =  _project.Json.data.pointsRatio; 
            return ratio;
        }
        public double AgentRatio()
        {
            ReqGet("/userCenter/api/v1/client/points/interaction/getPoints", true);
            var ratio =  _project.Json.data.pointsRatio; 
            return ratio;
        }

        public void Login()
        {
            try
            {
                _instance.Go("https://chat.chainopera.ai/");
                _instance.HeClick(("button", "innertext", "Login", "regexp", 1));
                _instance.HeClick(("span", "innertext", "Zerion", "regexp", 0));
                new ZerionWallet(_project,_instance, true).Connect();
            }

            catch (Exception ex)
            {
                _project.SendWarningToLog(ex.Message);
                throw;
            }
            
        }

        public int UpdatePrompts()
        {
            _logger.Send("renewing prompts");
            
            if (_instance.ActiveTab.FindElementByAttribute("span", "class", "ml-2\\ text-\\[\\#797B7A]\\ hover:text-primary-500", "regexp", 0).IsVoid)
                _instance.HeClick(("button", "innertext", "Chats", "regexp", 1));
            _instance.HeGet(("span", "class", "ml-2\\ text-\\[\\#797B7A]\\ hover:text-primary-500", "regexp", 0));
            var prompts = _instance.ActiveTab.FindElementsByAttribute("span", "class", "ml-2\\ text-\\[\\#797B7A]\\ hover:text-primary-500", "regexp").ToList();
            var localPrompts = new List<string>();
            lock (LockObject)
            {
                string localPath = Path.Combine(_project.Path,".data","web3prompts.txt");//$"{project.Path}.data\\web3prompts.txt"; 
                localPrompts = File.ReadAllLines(localPath).ToList();

                foreach (HtmlElement prompt in prompts)
                {
	
                    if (!localPrompts.Contains(prompt.InnerText.Trim())) 
                        localPrompts.Add ( prompt.InnerText.Trim() );
                }
                File.WriteAllLines(localPath, localPrompts);
            }
            
            var promptsList = _project.Lists["prompts"];
            promptsList.Clear();

            foreach (var prompt in localPrompts) promptsList.Add(prompt);
            return localPrompts.Count;
        }

        public void InputPrompt()
        {
            var prompts = _project.Lists["prompts"];
            var prompt = prompts[new Random().Next(0,prompts.Count - 1)];
            _instance.HeSet(("textarea", "fulltagname", "textarea", "regexp", 0),prompt);
            
        }

        public void SendPromptAndWait()
        {
            _project.Deadline();
            var classTofind = "p-1.5\\ size-7";
            var d = "M3 3H13V13H3V3Z";
            
            waitPrevious:
            _project.Deadline(10);
            Thread.Sleep(1000);
            if (!_instance.HeGet(("button", "class", classTofind, "regexp", 0), atr:"InnerHtml").Contains(d))
            {
                _instance.HeClick(("button", "class", classTofind, "regexp", 0));
            }
            else 
                goto waitPrevious;

            _project.SendInfoToLog("requestSent");
            
            waitCurrent:
            _project.Deadline(120);
            Thread.Sleep(1000);
            if (_instance.HeGet(("button", "class", classTofind, "regexp", 0), atr:"InnerHtml").Contains(d))
            {
                goto waitCurrent;
            }

        }

        public string ChooseAgentFromDb()
        {
            
            _instance.HeClick(("button", "innertext", "Discover", "regexp", 0));

            var agent = _project.DbGet("agent",where:"agent != '' ORDER BY RANDOM() LIMIT 1;");


            _instance.HeSet(("input:text", "placeholder", "Search\\ Agents", "regexp", 0),agent);
            Thread.Sleep(3000);

            var d = "M16 28.0586C22.6274 28.0586 28 22.686 28 16.0586L28 12.0586C28 5.43118 22.6274 0.0585937 16 0.0585936L12 0.0585936C5.37258 0.0585935 2.69829e-07 5.43118 1.90798e-07 12.0586L1.43099e-07 16.0586C6.40674e-08 22.686 5.37258 28.0586 12 28.0586L16 28.0586Z";

            if (!_instance.HeGet(("button", "class", "transform\\ transition-all\\ duration-300\\ ease-in-out\\ scale-100", "regexp", 0), atr:"InnerHtml").Contains(d))
            {
                _instance.HeClick(("button", "class", "transform\\ transition-all\\ duration-300\\ ease-in-out\\ scale-100", "regexp", 0));
            }

            _instance.HeClick(("li", "class", "flex\\ items-center\\ w-full\\ relative\\ hover:bg-gray-50\\ transition-colors\\ duration-200\\ cursor-pointer", "regexp", 0));
            _instance.HeClick(("button", "innertext", "\\+\\ Try\\ Agent", "regexp", 0));
            _logger.Send($"working with  {agent}");
            return agent;
        }

        public string PohState()
        {
            var poh = "";
            var tasks = ReqGet("/userCenter/api/v1/client/points/getTaskList");
            _project.Json.FromString(tasks);
            string undone = string.Empty;
            foreach (var task in _project.Json.data)
            {
                string taskTitle = task.taskTitle;
                bool completeStatus = task.completeStatus;
                if (!completeStatus){
                    undone = taskTitle;
                    poh += taskTitle + "; ";
                }
            }
            if (poh == "") poh = "ok";
            return poh;

        }

    }
}
