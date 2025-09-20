
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


namespace z3nCore
{
    public static class InstanceExtensions
    {
        private static readonly object ClipboardLock = new object();
        private static readonly SemaphoreSlim ClipboardSemaphore = new SemaphoreSlim(1, 1);
        private static readonly object LockObject = new object();
        
        private class ElementNotFoundException : Exception
        {
            public ElementNotFoundException(string message) : base(message) { }
        }
        
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

                    var elements = instance.ActiveTab.FindElementsByAttribute(tag, attribute, pattern, mode).ToList();
                    if (elements.Count != 0)
                    {
                        var last = elements[elements.Count - 1];
                        return last;
                    }

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
                        throw new ElementNotFoundException($"{comment} not found in {deadline}s: {lastExceptionMessage}");
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
            //!!!OLD METHOD WILL BE DEPRECATED SOON!!! use "instance.CFSolve" instead 
            
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
            //!!!OLD METHOD WILL BE DEPRECATED SOON!!! use "instance.CFToken" instead 
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



        public static void ClearShit(this Instance instance, string domain)
        {
            instance.CloseAllTabs();
            instance.ClearCache(domain);
            instance.ClearCookie(domain);
            Thread.Sleep(500);
            instance.ActiveTab.Navigate("about:blank", "");
        }



        public static void CloseExtraTabs(this Instance instance, bool blank = false, int tabToKeep = 1)
        {
            for (; ; ) { try { instance.AllTabs[tabToKeep].Close(); Thread.Sleep(1000); } catch { break; } }
            Thread.Sleep(500);
            if (blank)instance.ActiveTab.Navigate("about:blank", "");
        }
        public static void Go(this Instance instance, string url, bool strict = false)
        {
            bool go = false;
            string current = instance.ActiveTab.URL;
            if (strict) if (current != url) go = true;
            if (!strict) if (!current.Contains(url)) go = true;
            if (go) instance.ActiveTab.Navigate(url, "");
        }
        public static void F5(this Instance instance)
        {
            instance.ActiveTab.MainDocument.EvaluateScript("location.reload(true)");
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





    }





}
