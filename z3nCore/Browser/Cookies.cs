using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Global.ZennoLab.Json;
using Newtonsoft.Json.Linq;
using ZennoLab.InterfacesLibrary.ProjectModel;
using ZennoLab.CommandCenter;


namespace z3nCore
{
    public class Cookies
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;
        
        private readonly object LockObject = new object();

        public Cookies(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {

            _project = project;
            _instance = instance;
            _logger = new Logger(project, log: log, classEmoji: "🍪");

        }

        public void Set(string cookieSourse = null, string jsonPath = null )
        {
            if (string.IsNullOrEmpty(jsonPath)) 
                cookieSourse = "fromFile";

            if (cookieSourse == null)
                cookieSourse = "dbMain";
            
            switch (cookieSourse) 
            {
                case "dbMain":
                    cookieSourse = _project.SqlGet("cookies", "_instance");
                    break;

                case "dbProject":
                    cookieSourse = _project.SqlGet("cookies");
                    break;

                case "fromFile":
                    if(string.IsNullOrEmpty(jsonPath)) 
                        jsonPath = _project.PathCookies();
                    cookieSourse = File.ReadAllText(jsonPath);
                    break;
                
                default:                   
                    break;
            }
            _instance.SetCookie(cookieSourse);

        }
        public string Get(string domainFilter = "")
        {
            _logger.Send($"Get() called with filter: '{domainFilter}'");
            
            if (domainFilter == ".") 
            {
                domainFilter = _instance.ActiveTab.MainDomain;
                _logger.Send($"Filter '.' resolved to main domain: '{domainFilter}'");
            }
            
            var cookieContainer = _project.Profile.CookieContainer;
            _logger.Send($"Cookie container accessed, total domains: {cookieContainer.Domains.Count()}");
            
            var cookieList = new List<object>();
            int domainCount = 0;
            int totalCookies = 0;
            int filteredDomains = 0;

            foreach (var domain in cookieContainer.Domains)
            {
                domainCount++;
                
                if (string.IsNullOrEmpty(domainFilter) || domain.Contains(domainFilter))
                {
                    filteredDomains++;
                    _logger.Send($"Processing domain #{filteredDomains}: '{domain}'");
                    
                    var cookies = cookieContainer.Get(domain);
                    int cookiesInDomain = cookies.Count();
                    totalCookies += cookiesInDomain;
                    
                    _logger.Send($"Domain '{domain}' has {cookiesInDomain} cookies");
                    
                    cookieList.AddRange(cookies.Select(cookie => new
                    {
                        domain = cookie.Host,
                        expirationDate = cookie.Expiry == DateTime.MinValue ? (double?)null : new DateTimeOffset(cookie.Expiry).ToUnixTimeSeconds(),
                        hostOnly = !cookie.IsDomain,
                        httpOnly = cookie.IsHttpOnly,
                        name = cookie.Name,
                        path = cookie.Path,
                        sameSite = cookie.SameSite.ToString(),
                        secure = cookie.IsSecure,
                        session = cookie.IsSession,
                        storeId = (string)null,
                        value = cookie.Value,
                        id = cookie.GetHashCode()
                    }));
                    
                    _logger.Send($"Added {cookiesInDomain} cookies to list (total now: {cookieList.Count})");
                }
            }
            
            _logger.Send($"Filtering complete: {filteredDomains} domains matched out of {domainCount} total, {totalCookies} cookies collected");
            _logger.Send($"Starting JSON serialization of {cookieList.Count} cookie objects...");
            
            string cookiesJson = Global.ZennoLab.Json.JsonConvert.SerializeObject(cookieList, formatting: Formatting.None);
            
            _logger.Send($"Serialization complete: {cookiesJson.Length} characters, {cookiesJson.Length / 1024.0:F2} KB");
            
            return cookiesJson;
        }
        public void Save(string source = null, string jsonPath = null)
        {
            _logger.Send($"Save() called with source: '{source ?? "null"}', jsonPath: '{jsonPath ?? "null"}'");
            
            DateTime called = DateTime.UtcNow;
            
            if (string.IsNullOrEmpty(source))
            {
                source = "project";
                _logger.Send($"Source was empty, defaulted to: '{source}'");
            }

            string cookies = null;
            
            switch (source)
            {
                case "project":
                    _logger.Send("Mode: project - getting cookies for current domain only");
                    
                    cookies = Get(".");
                    _logger.Send($"Get() returned {cookies.Length} characters");
                    
                    _logger.Send("Escaping single quotes for SQL...");
                    cookies = cookies.Replace("'", "''").Trim();
                    _logger.Send($"After escaping: {cookies.Length} characters");
                    
                    _logger.Send("Updating project database...");
                    _project.DbUpd($"cookies = '{cookies}'");
                    _logger.Send("Project database updated successfully");
                    
                    return;
                    
                case "all":
                    _logger.Send("Mode: all - getting ALL cookies from profile");
                    
                    cookies = Get();
                    _logger.Send($"Get() returned {cookies.Length} characters, {cookies.Length / 1024.0:F2} KB");
                    
                    _logger.Send("Escaping single quotes for SQL...");
                    cookies = cookies.Replace("'", "''").Trim();
                    _logger.Send($"After escaping: {cookies.Length} characters");
                    
                    _logger.Send("Updating instance database...");
                    _project.DbUpd($"cookies = '{cookies}'", "_instance");
                    _logger.Send("Instance database updated successfully");
                    
                    if (!string.IsNullOrEmpty(jsonPath))
                    {
                        _logger.Send($"Writing cookies to file: '{jsonPath}'");
                        
                        lock (LockObject) 
                        { 
                            File.WriteAllText(jsonPath, cookies); 
                        }
                        
                        var fileInfo = new FileInfo(jsonPath);
                        _logger.Send($"File written successfully: {fileInfo.Length} bytes, {fileInfo.Length / 1024.0:F2} KB");
                    }
                    else
                    {
                        _logger.Send("No jsonPath provided, skipping file write");
                    }
                    
                    return;
                    
                default:
                    _logger.Send($"ERROR: Unsupported source '{source}'");
                    throw new Exception($"unsupported input {source}. Use [null|project|all]");
            }
        }
        
        
        public string GetByJs(string domainFilter = "", bool log = false)
        {
            string jsCode = @"
		var cookies = document.cookie.split('; ').map(function(cookie) {
			var parts = cookie.split('=');
			var name = parts[0];
			var value = parts.slice(1).join('=');
			return {
				'domain': window.location.hostname,
				'name': name,
				'value': value,
				'path': '/', 
				'expirationDate': null, 
				'hostOnly': true,
				'httpOnly': false,
				'secure': window.location.protocol === 'https:',
				'session': false,
				'sameSite': 'Unspecified',
				'storeId': null,
				'id': 1
			};
		});
		return JSON.stringify(cookies);
		";

            string jsonResult = _instance.ActiveTab.MainDocument.EvaluateScript(jsCode).ToString();
            if (log) _project.log(jsonResult);
            var escapedJson = jsonResult.Replace("\r\n", "").Replace("\n", "").Replace("\r", "").Replace(" ", "").Replace("'", "''").Trim();
            _project.Json.FromString(jsonResult);
            return escapedJson;
        }
        public void SetByJs(string cookiesJson, bool log = false)
        {
            try
            {
                JArray cookies = JArray.Parse(cookiesJson);

                var uniqueCookies = cookies
                    .GroupBy(c => new { Domain = c["domain"].ToString(), Name = c["name"].ToString() })
                    .Select(g => g.Last())
                    .ToList();

                string currentDomain = _instance.ActiveTab.Domain;

                string[] domainParts = currentDomain.Split('.');
                string parentDomain = "." + string.Join(".", domainParts.Skip(domainParts.Length - 2));


                string jsCode = "";
                int cookieCount = 0;
                foreach (JObject cookie in uniqueCookies)
                {
                    string domain = cookie["domain"].ToString();
                    string name = cookie["name"].ToString();
                    string value = cookie["value"].ToString();

                    if (domain == currentDomain || domain == "." + currentDomain)
                    {
                        string path = cookie["path"]?.ToString() ?? "/";
                        string expires;

                        if (cookie["expirationDate"] != null && cookie["expirationDate"].Type != JTokenType.Null)
                        {
                            double expValue = double.Parse(cookie["expirationDate"].ToString());
                            if (expValue < DateTimeOffset.UtcNow.ToUnixTimeSeconds()) expires = DateTimeOffset.UtcNow.AddYears(1).ToString("R");
                            else expires = DateTimeOffset.FromUnixTimeSeconds((long)expValue).ToString("R");
                        }
                        else
                            expires = DateTimeOffset.UtcNow.AddYears(1).ToString("R");

                        jsCode += $"document.cookie = '{name}={value}; domain={parentDomain}; path={path}; expires={expires}'; Secure';\n";
                        cookieCount++;
                    }
                }
                _logger.Send($"Found cookies for {currentDomain}: [{cookieCount}] runingJs...\n + {jsCode}");

                if (!string.IsNullOrEmpty(jsCode))
                {
                    _instance.ActiveTab.MainDocument.EvaluateScript(jsCode);
                }
                else _logger.Send($"!W No cookies Found for {currentDomain}");
            }
            catch (Exception ex)
            {
                _logger.Send($"!W cant't parse JSON: [{cookiesJson}]  {ex.Message}");
            }
        }

    }
}
