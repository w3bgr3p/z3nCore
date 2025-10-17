using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class FirstMail
    {

        private readonly IZennoPosterProjectModel _project;
        private readonly Logger _logger;

        private string _key;
        private string _login;
        private string _pass;
        private string _proxy;
        private bool _log;

        public FirstMail(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project;
            _logger = new Logger(project, log: log, classEmoji: "FirstMail");
            LoadKeys();
            _log = log;
        }

        private void LoadKeys()
        {
            var creds = _project.DbGetColumns("apikey, apisecret, passphrase, proxy", "_api", where: "id = 'firstmail'");

            _key = creds["apikey"];
            _login = Uri.EscapeDataString(creds["apisecret"]);
            _pass = Uri.EscapeDataString(creds["passphrase"]);
            _proxy = creds["proxy"];

        }
        public async Task<string> Request(string url)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri(url),
                Headers =
                    {
                        { "accept", "application/json" },
                        { "X-API-KEY", _key },
                    },
            };
            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                return body;
            }
        }
        public string Delete(string email, bool seen = false)
        {
            string url = $"https://api.firstmail.ltd/v1/mail/delete?username={_login} &password= {_pass}";
            string additional = seen ? "seen=true" : null;
            url += additional;
            string result = Request(url).GetAwaiter().GetResult();
            _project.Json.FromString(result);
            return result;
        }
        public string GetOne(string email)
        {
            string url = $"https://api.firstmail.ltd/v1/mail/one?username={_login}&password={_pass}";
            string result = Request(url).GetAwaiter().GetResult();
            _project.Json.FromString(result);
            return result;
        }
        public string GetAll(string email)
        {
            string url = $"https://api.firstmail.ltd/v1/get/messages?username={_login} &password= {_pass}";
            string result = Request(url).GetAwaiter().GetResult();
            _project.Json.FromString(result);
            return result;
        }
        
        public string GetMail(string email)
        {

            string url = $"https://api.firstmail.ltd/v1/mail/one?username={_login}&password={_pass}";

            string[] headers = new string[]
            {
                $"accept: application/json",
                $"X-API-KEY: {_key}"
            };

            string result = _project.GET(url,_proxy, headers, log:_log);
            _project.Json.FromString(result);
            return result;

        }    
        public string GetOTP(string email)
        {

            string json = GetMail(email);
            _project.Json.FromString(json);
            string deliveredTo = _project.Json.to[0];
            string text = _project.Json.text;
            string subject = _project.Json.subject;
            string html = _project.Json.html;


            if (!deliveredTo.Contains(email)) throw new Exception($"Fmail: Email {email} not found in last message");
            else
            {
                
                Match match = Regex.Match(subject, @"\b\d{6}\b");
                if (match.Success) 
                    return match.Value;

                match = Regex.Match(text, @"\b\d{6}\b");
                if (match.Success)
                    return match.Value;

                match = Regex.Match(html, @"\b\d{6}\b");
                if (match.Success) 
                    return match.Value;
                else throw new Exception("Fmail: OTP not found in message with correct email");
            }

        }
        public string GetLink(string email)
        {
            string json = GetMail(email);
            _project.Json.FromString(json);
            string deliveredTo = _project.Json.to[0];
            string text = _project.Json.text;

            if (!deliveredTo.Contains(email))
                throw new Exception($"Fmail: Email {email} not found in last message");

            int startIndex = text.IndexOf("https://");
            if (startIndex == -1) startIndex = text.IndexOf("http://");
            if (startIndex == -1) throw new Exception($"No Link found in message {text}");

            string potentialLink = text.Substring(startIndex);
            int endIndex = potentialLink.IndexOfAny(new[] { ' ', '\n', '\r', '\t', '"' });
            if (endIndex != -1)
                potentialLink = potentialLink.Substring(0, endIndex);

            return Uri.TryCreate(potentialLink, UriKind.Absolute, out _)
                ? potentialLink
                : throw new Exception($"No Link found in message {text}");
        }
    }
}
