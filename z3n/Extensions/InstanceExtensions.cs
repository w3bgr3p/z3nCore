﻿
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.Enums.Browser;
using ZennoLab.InterfacesLibrary.ProjectModel;
using ZennoLab.Macros;


namespace z3n
{
    public static class InstanceExtensions
    {
        private static readonly object ClipboardLock = new object();
        private static readonly SemaphoreSlim ClipboardSemaphore = new SemaphoreSlim(1, 1);
        private static readonly object LockObject = new object();
        public static HtmlElement GetHe(this Instance instance, object obj, string method = "")
        {

            if (obj is HtmlElement element)
            {
                if (element.IsVoid) throw new Exception("Provided HtmlElement is void");
                return element;
            }

            Type inputType = obj.GetType();
            int objLength = inputType.GetFields().Length;

            if (objLength == 2)
            {
                string value = inputType.GetField("Item1").GetValue(obj).ToString();
                method = inputType.GetField("Item2").GetValue(obj).ToString();

                if (method == "id")
                {
                    HtmlElement he = instance.ActiveTab.FindElementById(value);
                    if (he.IsVoid)
                    {
                        throw new Exception($"No element by id='{value}'");
                    }
                    return he;
                }
                else if (method == "name")
                {
                    HtmlElement he = instance.ActiveTab.FindElementByName(value);
                    if (he.IsVoid)
                    {
                        throw new Exception($"No element by name='{value}'");
                    }
                    return he;
                }
                else
                {
                    throw new Exception($"Unsupported method for tuple: {method}");
                }
            }
            else if (objLength == 5)
            {
                string tag = inputType.GetField("Item1").GetValue(obj).ToString();
                string attribute = inputType.GetField("Item2").GetValue(obj).ToString();
                string pattern = inputType.GetField("Item3").GetValue(obj).ToString();
                string mode = inputType.GetField("Item4").GetValue(obj).ToString();
                object posObj = inputType.GetField("Item5").GetValue(obj);
                int pos;
                if (!int.TryParse(posObj.ToString(), out pos)) throw new ArgumentException("5th element of Tupple must be (int).");

                if (method == "last")
                {
                    int index = 0;
                    while (true)
                    {
                        HtmlElement he = instance.ActiveTab.FindElementByAttribute(tag, attribute, pattern, mode, index);
                        if (he.IsVoid)
                        {
                            he = instance.ActiveTab.FindElementByAttribute(tag, attribute, pattern, mode, index - 1);
                            if (he.IsVoid)
                            {
                                throw new Exception($"No element by: tag='{tag}', attribute='{attribute}', pattern='{pattern}', mode='{mode}'");
                            }
                            return he;
                        }
                        index++;
                    }
                }
                else
                {
                    HtmlElement he = instance.ActiveTab.FindElementByAttribute(tag, attribute, pattern, mode, pos);
                    if (he.IsVoid)
                    {
                        throw new Exception($"No element by: tag='{tag}', attribute='{attribute}', pattern='{pattern}', mode='{mode}', pos={pos}");
                    }
                    return he;
                }
            }

            throw new ArgumentException($"Unsupported type: {obj?.GetType()?.ToString() ?? "null"}");
        }

        //new
        public static string HeGet(this Instance instance, object obj, string method = "", int deadline = 10, string atr = "innertext", int delay = 1, string comment = "", bool thr0w = true)
        {
            DateTime functionStart = DateTime.Now;
            string lastExceptionMessage = "";

            while (true)
            {
                if ((DateTime.Now - functionStart).TotalSeconds > deadline)
                {
                    if (method == "!")
                    {
                        return null;
                    }
                    else if (thr0w)
                    {
                        throw new TimeoutException($"{comment} not found in {deadline}s: {lastExceptionMessage}");
                    }
                    else
                    {
                        return null;
                    }
                }

                try
                {
                    HtmlElement he = instance.GetHe(obj, method);
                    if (method == "!")
                    {
                        throw new Exception($"{comment} element detected when it should not be: {atr}='{he.GetAttribute(atr)}'");
                    }
                    else
                    {
                        Thread.Sleep(delay * 1000);
                        return he.GetAttribute(atr);
                    }
                }
                catch (Exception ex)
                {
                    lastExceptionMessage = ex.Message;
                    if (method == "!" && ex.Message.Contains("no element by"))
                    {
                        // Элемент не найден — это нормально, продолжаем ждать
                    }
                    else if (method != "!")
                    {
                        // Обычное поведение: элемент не найден, записываем ошибку и ждём
                    }
                    else
                    {
                        // Неожиданная ошибка при method = "!", пробрасываем её
                        throw;
                    }
                }

                Thread.Sleep(500);
            }
        }
        public static void HeClick(this Instance instance, object obj, string method = "", int deadline = 10, int delay = 1, string comment = "", bool thr0w = true, int emu = 0)
        {
            bool emuSnap = instance.UseFullMouseEmulation;
            if (emu > 0) instance.UseFullMouseEmulation = true;
            if (emu < 0) instance.UseFullMouseEmulation = false;
            
            DateTime functionStart = DateTime.Now;
            string lastExceptionMessage = "";

            while (true)
            {
                if ((DateTime.Now - functionStart).TotalSeconds > deadline)
                {
                    if (thr0w) throw new TimeoutException($"{comment} not found in {deadline}s: {lastExceptionMessage}");
                    else return;
                }

                try
                {
                    HtmlElement he = instance.GetHe(obj, method);
                    Thread.Sleep(delay * 1000);
                    he.RiseEvent("click", instance.EmulationLevel);
                    instance.UseFullMouseEmulation = emuSnap;
                    break;
                }
                catch (Exception ex)
                {
                    lastExceptionMessage = ex.Message;
                    instance.UseFullMouseEmulation = emuSnap;
                }
                Thread.Sleep(500);
            }

            if (method == "clickOut")
            {
                if ((DateTime.Now - functionStart).TotalSeconds > deadline)
                {
                    instance.UseFullMouseEmulation = emuSnap;
                    if (thr0w) throw new TimeoutException($"{comment} not found in {deadline}s: {lastExceptionMessage}");
                    else return;
                }
                while (true)
                {
                    try
                    {
                        HtmlElement he = instance.GetHe(obj, method);
                        Thread.Sleep(delay * 1000);
                        he.RiseEvent("click", instance.EmulationLevel);
                        continue;
                    }
                    catch
                    {
                        instance.UseFullMouseEmulation = emuSnap;
                        break;
                    }
                }

            }

        }
        public static void HeSet(this Instance instance, object obj, string value, string method = "id", int deadline = 10, int delay = 1, string comment = "", bool thr0w = true)
        {
            DateTime functionStart = DateTime.Now;
            string lastExceptionMessage = "";

            while (true)
            {
                if ((DateTime.Now - functionStart).TotalSeconds > deadline)
                {
                    if (thr0w) throw new TimeoutException($"{comment} not found in {deadline}s: {lastExceptionMessage}");
                    else return;
                }

                try
                {
                    HtmlElement he = instance.GetHe(obj, method);
                    Thread.Sleep(delay * 1000);
                    instance.WaitFieldEmulationDelay(); // Mimics WaitSetValue behavior
                    he.SetValue(value, "Full", false);
                    break;
                }
                catch (Exception ex)
                {
                    lastExceptionMessage = ex.Message;
                }

                Thread.Sleep(500);
            }
        }
        public static void HeDrop(this Instance instance, Func<ZennoLab.CommandCenter.HtmlElement> elementSearch)
        {
            HtmlElement he = elementSearch();
            HtmlElement heParent = he.ParentElement; heParent.RemoveChild(he);
        }

        public static string GetTx(this Instance instance, string format = "hash", int deadline = 5)
        {
            string pattern = @"0x[a-fA-F0-9]{64}";
            Regex regex = new Regex(pattern);
            DateTime functionStart = DateTime.Now;

        wait:

            if ((DateTime.Now - functionStart).TotalSeconds > deadline)
            {
                throw new Exception($"Hashes not found in {deadline}s");
            }

            var Hrefs = instance.ActiveTab.FindElementsByAttribute("a", "href", "https://", "regexp").ToList();

            if (Hrefs.Count == 0) goto wait;

            var Txs = new List<string>();
            foreach (HtmlElement tx in Hrefs)
            {
                string href = tx.GetAttribute("href");
                if (regex.IsMatch(href)) Txs.Add(href);

            }
            if (Txs.Count == 0) goto wait;


            if (format == "link")
                return Txs[0];

            else if (format == "hash")
                return Txs[0].GetTxHash();

            else if (format == "all")
                return string.Join("\n", Txs);
            else
                throw new Exception($"unsupported format [{format}]");
        }

        //js
        public static string JsClick(this Instance instance, string selector, int delay = 2)
        {
            Thread.Sleep(1000 * delay);
            selector = TextProcessing.Replace(selector, "\"", "'", "Text", "All");
            try
            {
                string jsCode = $@"
					(function() {{
						var element = {selector};
						if (!element) {{
							throw new Error(""Элемент не найден по селектору: {selector.Replace("\"", "\"\"")}"");
						}}
						element.click();
						return 'Click successful';
					}})();
				";

                string result = instance.ActiveTab.MainDocument.EvaluateScript(jsCode);
                return result;
            }
            catch (Exception ex)
            {
                return $"{ex.Message}";
            }
        }
        public static string JsSet(this Instance instance, string selector, string value, int delay = 2)
        {
            Thread.Sleep(1000 * delay);
            selector = TextProcessing.Replace(selector, "\"", "'", "Text", "All");
            try
            {
                string escapedValue = value.Replace("\"", "\"\"");

                string jsCode = $@"
					(function() {{
						var element = {selector};
						if (!element) {{
							throw new Error(""Элемент не найден по селектору: {selector.Replace("\"", "\"\"")}"");
						}}
						element.value = ""{escapedValue}"";
						var event = new Event('input', {{ bubbles: true }});
						element.dispatchEvent(event);
						return 'Value set successfully';
					}})();
				";

                string result = instance.ActiveTab.MainDocument.EvaluateScript(jsCode);
                return result;
            }
            catch (Exception ex)
            {
                return $"{ex.Message}";
            }
        }
        public static string JsPost(this Instance instance, string script, int delay = 0)
        {
            Thread.Sleep(1000 * delay);
            var jsCode = TextProcessing.Replace(script, "\"", "'", "Text", "All");
            try
            {
                string result = instance.ActiveTab.MainDocument.EvaluateScript(jsCode);
                return result;
            }
            catch (Exception ex)
            {
                return $"{ex.Message}";
            }
        }

        //cf
        public static void ClFlv2(this Instance instance)
        {
            Random rnd = new Random(); string strX = ""; string strY = ""; Thread.Sleep(3000);
            HtmlElement he1 = instance.ActiveTab.FindElementById("cf-turnstile");
            HtmlElement he2 = instance.ActiveTab.FindElementByAttribute("div", "outerhtml", "<div><input type=\"hidden\" name=\"cf-turnstile-response\"", "regexp", 4);
            if (he1.IsVoid && he2.IsVoid) return;
            else if (!he1.IsVoid)
            {
                strX = he1.GetAttribute("leftInbrowser"); strY = he1.GetAttribute("topInbrowser");
            }
            else if (!he2.IsVoid)
            {
                strX = he2.GetAttribute("leftInbrowser"); strY = he2.GetAttribute("topInbrowser");
            }

            int rndX = rnd.Next(23, 26); int x = (int.Parse(strX) + rndX);
            int rndY = rnd.Next(27, 31); int y = (int.Parse(strY) + rndY);
            Thread.Sleep(rnd.Next(4, 5) * 1000);
            instance.WaitFieldEmulationDelay();
            instance.Click(x, x, y, y, "Left", "Normal");
            Thread.Sleep(rnd.Next(3, 4) * 1000);

        }
        public static string ClFl(this Instance instance, int deadline = 60, bool strict = false)
        {
            DateTime timeout = DateTime.Now.AddSeconds(deadline);
            while (true)
            {
                if (DateTime.Now > timeout) throw new Exception($"!W CF timeout");
                Random rnd = new Random();

                Thread.Sleep(rnd.Next(3, 4) * 1000);

                var token = instance.HeGet(("cf-turnstile-response", "name"), atr: "value");
                if (!string.IsNullOrEmpty(token)) return token;

                string strX = ""; string strY = "";

                try
                {
                    var cfBox = instance.GetHe(("cf-turnstile", "id"));
                    strX = cfBox.GetAttribute("leftInbrowser"); strY = cfBox.GetAttribute("topInbrowser");
                }
                catch
                {
                    var cfBox = instance.GetHe(("div", "outerhtml", "<div><input type=\"hidden\" name=\"cf-turnstile-response\"", "regexp", 4));
                    strX = cfBox.GetAttribute("leftInbrowser"); strY = cfBox.GetAttribute("topInbrowser");
                }

                int x = (int.Parse(strX) + rnd.Next(23, 26));
                int y = (int.Parse(strY) + rnd.Next(27, 31));
                instance.Click(x, x, y, y, "Left", "Normal");

            }
        }

        public static void CloseExtraTabs(this Instance instance, bool blank = false, int tabToKeep = 1)
        {
            for (; ; ) { try { instance.AllTabs[tabToKeep].Close(); Thread.Sleep(1000); } catch { break; } }
            Thread.Sleep(500);
            if (blank)instance.ActiveTab.Navigate("about:blank", "");
        }

        public static void CtrlV(this Instance instance, string ToPaste)
        {
            lock (new object()) 
            {
                string originalClipboard = null;
                try
                {
                    if (System.Windows.Forms.Clipboard.ContainsText())
                        originalClipboard = System.Windows.Forms.Clipboard.GetText();

                    System.Windows.Forms.Clipboard.SetText(ToPaste);
                    instance.ActiveTab.KeyEvent("v", "press", "ctrl");

                    if (!string.IsNullOrEmpty(originalClipboard))
                        System.Windows.Forms.Clipboard.SetText(originalClipboard);
                }
                catch { }
            }
        }
        public static void Go(this Instance instance, string url, bool strict = false)
        {
            bool go = false;
            string current = instance.ActiveTab.URL;
            if (strict) if (current != url) go = true;
            if (!strict) if (!current.Contains(url)) go = true;
            if (go) instance.ActiveTab.Navigate(url, "");
        }

        //cookies
        public static string GetCookies(this Instance instance, IZennoPosterProjectModel project, string domainFilter = "")
        {
            if (domainFilter == ".") domainFilter = instance.ActiveTab.MainDomain;
            var cookieContainer = project.Profile.CookieContainer;
            var cookieList = new List<object>();

            foreach (var domain in cookieContainer.Domains)
            {
                if (string.IsNullOrEmpty(domainFilter) || domain.Contains(domainFilter))
                {
                    var cookies = cookieContainer.Get(domain);
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
                }
            }
            string cookiesJson = Global.ZennoLab.Json.JsonConvert.SerializeObject(cookieList, Global.ZennoLab.Json.Formatting.Indented);

            cookiesJson = cookiesJson.Replace("\r\n", "").Replace("\n", "").Replace("\r", "").Replace(" ", "");
            project.Json.FromString(cookiesJson);
            return cookiesJson;
        }
        public static string GetCookiesJs(this Instance instance, IZennoPosterProjectModel project, string domainFilter = "", bool log = false)
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

            string jsonResult = instance.ActiveTab.MainDocument.EvaluateScript(jsCode).ToString();
            if (log) project.L0g(jsonResult);
            JArray cookiesArray = JArray.Parse(jsonResult);
            var escapedJson = jsonResult.Replace("\r\n", "").Replace("\n", "").Replace("\r", "").Replace(" ", "").Replace("'", "''").Trim();
            project.Json.FromString(jsonResult);
            return escapedJson;
        }
        public static void SetCookiesJs(this Instance instance, IZennoPosterProjectModel project, string cookiesJson, bool log = false)
        {
            try
            {
                JArray cookies = JArray.Parse(cookiesJson);

                var uniqueCookies = cookies
                    .GroupBy(c => new { Domain = c["domain"].ToString(), Name = c["name"].ToString() })
                    .Select(g => g.Last())
                    .ToList();

                string currentDomain = instance.ActiveTab.Domain;
                //long currentUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                //long weekFromNowUnixTime = currentUnixTime + (7 * 24 * 60 * 60);
                //string weekFromNow = DateTimeOffset.UtcNow.AddYears(1).ToString("R");

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
                if (log) project.L0g( $"Found cookies for {currentDomain}: [{cookieCount}] runingJs...\n + {jsCode}");

                if (!string.IsNullOrEmpty(jsCode))
                {
                    instance.ActiveTab.MainDocument.EvaluateScript(jsCode);
                }
                else project.L0g( $"!W No cookies Found for {currentDomain}");
            }
            catch (Exception ex)
            {
                project.L0g( $"!W cant't parse JSON: [{cookiesJson}]  {ex.Message}");
            }
        }

        public static void SetDisplay(this Instance instance, string webGl, IZennoPosterProjectModel project)
        {
            if (!string.IsNullOrEmpty(webGl))
            {
                var jsonObject = JObject.Parse(webGl);
                var mapping = new Dictionary<string, string>
                {
                    {"Renderer", "RENDERER"},
                    {"Vendor", "VENDOR"},
                    {"Version", "VERSION"},
                    {"ShadingLanguageVersion", "SHADING_LANGUAGE_VERSION"},
                    {"UnmaskedRenderer", "UNMASKED_RENDERER_WEBGL"},
                    {"UnmaskedVendor", "UNMASKED_VENDOR"},
                    {"MaxCombinedTextureImageUnits", "MAX_COMBINED_TEXTURE_IMAGE_UNITS"},
                    {"MaxCubeMapTextureSize", "MAX_CUBE_MAP_TEXTURE_SIZE"},
                    {"MaxFragmentUniformVectors", "MAX_FRAGMENT_UNIFORM_VECTORS"},
                    {"MaxTextureSize", "MAX_TEXTURE_SIZE"},
                    {"MaxVertexAttribs", "MAX_VERTEX_ATTRIBS"}
                };

                foreach (var pair in mapping)
                {
                    string value = "";
                    if (jsonObject["parameters"]["default"][pair.Value] != null) value = jsonObject["parameters"]["default"][pair.Value].ToString();
                    else if (jsonObject["parameters"]["webgl"][pair.Value] != null) value = jsonObject["parameters"]["webgl"][pair.Value].ToString();
                    else if (jsonObject["parameters"]["webgl2"][pair.Value] != null) value = jsonObject["parameters"]["webgl2"][pair.Value].ToString();
                    if (!string.IsNullOrEmpty(value)) instance.WebGLPreferences.Set((WebGLPreference)Enum.Parse(typeof(WebGLPreference), pair.Key), value);

                }
            }
            else project.L0g("!W WebGL string is empty. Please parse WebGL data into the database. Otherwise, any antifraud system will fuck you up like it’s a piece of cake.");
        
            try
            {
                instance.SetWindowSize(1280, 720);
                project.Profile.AcceptLanguage = "en-US,en;q=0.9";
                project.Profile.Language = "EN";
                project.Profile.UserAgentBrowserLanguage = "en-US";
                instance.UseMedia = false;

            }
            catch (Exception ex)
            {
                try { project.GlobalVariables[$"ZBS", $"Thread{project.Variables["acc0"].Value}"].Value = null; } catch { }
                project.Variables["acc0"].Value = "";
                project.L0g(ex.Message, thr0w: true);
            }

        }
        public static void SetProxy(this Instance instance, string proxy, IZennoPosterProjectModel project)
		{
            if (string.IsNullOrEmpty(proxy))
            {
                project.L0g("!W proxy string is EMPTY");
                throw new Exception("!W EMPTY Proxy");
            }

                long uTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); 
			string ipLocal = project.GET($"http://api.ipify.org/");

			while (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - uTime < 60)
			{
				instance.SetProxy(proxy, true, true, true, true); Thread.Sleep(2000);
				string ipProxy = project.GET($"http://api.ipify.org/",proxy);
                project.L0g($"local:[{ipLocal}]?proxyfied:[{ipProxy}]");
				project.Variables["ip"].Value = ipProxy;
				project.Variables["proxy"].Value = proxy;
				if (ipLocal != ipProxy) return;
			}

            project.L0g( "!W badProxy");
            throw new Exception("!W badProxy");
		}

        public static void Stargate(this Instance instance, string srcChain, string dstChain, string srcToken = "0xEeeeeEeeeEeEeeEeEeEeeEEEeeeeEeeeeeeeEEeE", string dstToken = "0xEeeeeEeeeEeEeeEeEeEeeEEEeeeeEeeeeeeeEEeE")
        {

            string url = "https://stargate.finance/bridge?" + $"srcChain={srcChain}" + $"&srcToken={srcToken}" + $"&dstChain={dstChain}" + $"&dstToken={dstToken}";
            instance.ActiveTab.Navigate(url, "");

        }
        public static void Relay(this Instance instance, string fromChainId, string to, string toCurrency = "0x0000000000000000000000000000000000000000", string fromCurrency = "0x0000000000000000000000000000000000000000")
        {

            string url = $"https://relay.link/bridge/{to}?fromChainId={fromChainId}&toCurrency={toCurrency}&fromCurrency={fromCurrency}";
            instance.ActiveTab.Navigate(url, "");

        }

        public static string Traffic(this Instance instance, string url,  string parametr , IZennoPosterProjectModel project, bool reload = false, string method = null)
        {
            project.Deadline();
            instance.UseTrafficMonitoring = true;
            if (reload) instance.ActiveTab.MainDocument.EvaluateScript("location.reload(true)");

            get:
            project.Deadline(10);
            Thread.Sleep(1000);
            var traffic = instance.ActiveTab.GetTraffic();
            //project.SendInfoToLog(traffic.Count().ToString());
            var data = new Dictionary<string, string>();
            string param;
            foreach (var t in traffic)
            {
                if (t.Url.Contains(url))
                {
                    //project.SendInfoToLog("found url");
                                    
                    var Method = t.Method;
                    var ResultCode = t.ResultCode.ToString();
                    var Url = t.Url;
                    var ResponseContentType = t.ResponseContentType;
                    var RequestHeaders = t.RequestHeaders;
                    var RequestCookies = t.RequestCookies;
                    var RequestBody = t.RequestBody;
                    var ResponseHeaders = t.ResponseHeaders;
                    var ResponseCookies = t.ResponseCookies;
                    var ResponseBody = t.ResponseBody == null ? "" : Encoding.UTF8.GetString(t.ResponseBody, 0, t.ResponseBody.Length);

                    if (Method == "OPTIONS") continue;
                    data.Add("Method", Method);
                    data.Add("ResultCode", ResultCode);
                    data.Add("Url", Url);
                    data.Add("ResponseContentType", ResponseContentType);
                    data.Add("RequestHeaders", RequestHeaders);
                    data.Add("RequestCookies", RequestCookies);
                    data.Add("RequestBody", RequestBody);
                    data.Add("ResponseHeaders", ResponseHeaders);
                    data.Add("ResponseCookies", ResponseCookies);
                    data.Add("ResponseBody", ResponseBody);
                    break;
                }

            }
            if (data.Count == 0) goto get;

            if (string.IsNullOrEmpty(parametr)) 
                return string.Join("\n", data.Select(kvp => $"{kvp.Key}-{kvp.Value}"));
            else data.TryGetValue ( parametr, out param);
                return param;
            //return data.TryGetValue(parametr, out string param);


        }


        public static void ExtSwitch(this Instance instance, IZennoPosterProjectModel project,string toUse = "", bool log = false)
        {
            project.L0g($"switching extentions  {toUse}");

            if (instance.BrowserType.ToString() == "Chromium")
            {
                var wlt = new Wlt(project, instance, log);
                string fileName = $"One-Click-Extensions-Manager.crx";
                var managerId = "pbgjpgbpljobkekbhnnmlikbbfhbhmem";
                instance.ExtAdd(project, managerId, fileName);
                wlt.Install(managerId, fileName, log);

                var em = instance.UseFullMouseEmulation;

                int i = 0; string extStatus = "enabled";

                while (instance.ActiveTab.URL != "chrome-extension://pbgjpgbpljobkekbhnnmlikbbfhbhmem/index.html")
                {
                    instance.ActiveTab.Navigate("chrome-extension://pbgjpgbpljobkekbhnnmlikbbfhbhmem/index.html", "");
                    instance.CloseExtraTabs();
                    project.L0g($"URL is correct {instance.ActiveTab.URL}");
                }

                while (!instance.ActiveTab.FindElementByAttribute("button", "class", "ext-name", "regexp", i).IsVoid)
                {
                    string extName = Regex.Replace(instance.ActiveTab.FindElementByAttribute("button", "class", "ext-name", "regexp", i).GetAttribute("innertext"), @" Wallet", "");
                    string outerHtml = instance.ActiveTab.FindElementByAttribute("li", "class", "ext\\ type-normal", "regexp", i).GetAttribute("outerhtml");
                    string extId = Regex.Match(outerHtml, @"extension-icon/([a-z0-9]+)").Groups[1].Value;
                    if (outerHtml.Contains("disabled")) extStatus = "disabled";
                    if (toUse.Contains(extName) && extStatus == "disabled" || toUse.Contains(extId) && extStatus == "disabled" || !toUse.Contains(extName) && !toUse.Contains(extId) && extStatus == "enabled")
                        instance.HeClick(("button", "class", "ext-name", "regexp", i));
                    i++;
                }

                instance.CloseExtraTabs();
                instance.UseFullMouseEmulation = em;
                project.L0g($"Enabled  {toUse}");
            }

        }
        public static bool ExtAdd(this Instance instance, IZennoPosterProjectModel project, string extId, string fileName)
        {
            string path = $"{project.Path}.crx\\{fileName}";

            if (!File.Exists(path))
            {
                project.L0g($"File not found: {path}");
                throw new FileNotFoundException($"CRX file not found: {path}");
            }

            var extListString = string.Join("\n", instance.GetAllExtensions().Select(x => $"{x.Name}:{x.Id}"));
            if (!extListString.Contains(extId))
            {
                project.L0g($"installing {path}");
                instance.InstallCrxExtension(path);
                return true;
            }
            return false;
        }

        public static void ExtRm(this Instance instance, string[] ExtToRemove)
        {
            if (ExtToRemove != null && ExtToRemove.Length > 0)
            foreach (string ext in ExtToRemove)
                try { instance.UninstallExtension(ext); } catch { }
        }


        public static void F5(this Instance instance)
        {
            instance.ActiveTab.MainDocument.EvaluateScript("location.reload(true)");
        }


    }


}
