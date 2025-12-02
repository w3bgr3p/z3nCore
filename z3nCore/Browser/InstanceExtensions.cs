
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
using z3nCore.Utilities;

namespace z3nCore
{
    public static partial class InstanceExtensions
    {
        private static readonly object ClipboardLock = new object();
        private static readonly SemaphoreSlim ClipboardSemaphore = new SemaphoreSlim(1, 1);
        private static readonly object LockObject = new object();
        private static readonly Sleeper _clickSleep = new Sleeper(1008, 1337);
        private static readonly Sleeper _inputSleep = new Sleeper(1337, 2077);
        
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
                        throw new Exception($"no element by id='{value}'");
                    }
                    return he;
                }
                else if (method == "name")
                {
                    HtmlElement he = instance.ActiveTab.FindElementByName(value);
                    if (he.IsVoid)
                    {
                        throw new Exception($"no element by name='{value}'");
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
                                throw new Exception($"no element by: tag='{tag}', attribute='{attribute}', pattern='{pattern}', mode='{mode}'");
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
                        throw new Exception($"no element by: tag='{tag}', attribute='{attribute}', pattern='{pattern}', mode='{mode}', pos={pos}");
                    }
                    return he;
                }
            }

            throw new ArgumentException($"Unsupported type: {obj?.GetType()?.ToString() ?? "null"}");
        }
        
        //new
        public static string HeGet(this Instance instance, object obj, string method = "", int deadline = 10, string atr = "innertext", int delay = 1, bool thrw = true, bool thr0w = true, bool waitTillVoid = false)
        {
            DateTime functionStart = DateTime.Now;
            string lastExceptionMessage = "";

            if (!thr0w)
            {
                thrw = thr0w;
            }
    
            while (true)
            {
                if ((DateTime.Now - functionStart).TotalSeconds > deadline)
                {
                    if (waitTillVoid)
                    {
                        return null;
                    }
                    else if (thrw)
                    {
                        string url = instance.ActiveTab.URL;
                        throw new ElementNotFoundException($"not found in {deadline}s: {lastExceptionMessage}. URL is: {url}");
                    }
                    else
                    {
                        return null;
                    }
                }

                try
                {
                    HtmlElement he = instance.GetHe(obj, method);
                    if (waitTillVoid)
                    {
                        throw new Exception($"element detected when it should not be: {atr}='{he.GetAttribute(atr)}'");
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
                    if (waitTillVoid && ex.Message.Contains("no element by"))
                    {
                        // Элемент не найден — это нормально, продолжаем ждать
                    }
                    else if (!waitTillVoid)
                    {
                        // Обычное поведение: элемент не найден, записываем ошибку и ждём
                    }
                    else
                    {
                        // Неожиданная ошибка при waitTillVoid, пробрасываем её
                        throw;
                    }
                }

                Thread.Sleep(500);
            }
        }
        public static string HeCatch(this Instance instance, object obj, string method = "", int deadline = 10, string atr = "innertext", int delay = 1)
        {
            DateTime functionStart = DateTime.Now;
            string lastExceptionMessage = "";


            while (true)
            {
                if ((DateTime.Now - functionStart).TotalSeconds > deadline)
                {
                    // Timeout - элемент не появился, всё хорошо
                    return null;
                }

                try
                {
                    HtmlElement he = instance.GetHe(obj, method);
                    // Элемент найден - это ПЛОХО, выбрасываем исключение
                    throw new Exception($"error detected: {atr}='{he.GetAttribute(atr)}'");
                }
                catch (Exception ex)
                {
                    lastExceptionMessage = ex.Message;
                    if (ex.Message.Contains("no element by"))
                    {
                        // Элемент не найден - это хорошо, продолжаем ждать
                    }
                    else
                    {
                        // Это реальная ошибка или наше исключение "element detected"
                        throw;
                    }
                }

                Thread.Sleep(500);
            }
        }

        
        public static void HeClick(this Instance instance, object obj, string method = "", int deadline = 10, double delay = 1, string comment = "", bool thrw = true , bool thr0w = true, int emu = 0)
        {
            bool emuSnap = instance.UseFullMouseEmulation;
            if (emu > 0) instance.UseFullMouseEmulation = true;
            if (emu < 0) instance.UseFullMouseEmulation = false;
            
            if (!thr0w)
            {
                thrw = thr0w;
            }
            
            DateTime functionStart = DateTime.Now;
            string lastExceptionMessage = "";

            while (true)
            {
                if ((DateTime.Now - functionStart).TotalSeconds > deadline)
                {
                    if (thrw) throw new TimeoutException($"{comment} not found in {deadline}s: {lastExceptionMessage}");
                    else return;
                }

                try
                {
                    HtmlElement he = instance.GetHe(obj, method);
                    //Thread.Sleep(delay * 1000);
                    _clickSleep.Sleep(delay);
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
                        _clickSleep.Sleep(delay);
                        //Thread.Sleep(delay * 1000);
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
        public static void HeSet(this Instance instance, object obj, string value, string method = "id", int deadline = 10, double delay = 1, string comment = "", bool thrw = true, bool thr0w = true)
        {
            DateTime functionStart = DateTime.Now;
            string lastExceptionMessage = "";

            if (!thr0w)
            {
                thrw = thr0w;
            }
            while (true)
            {
                if ((DateTime.Now - functionStart).TotalSeconds > deadline)
                {
                    if (thrw) throw new TimeoutException($"{comment} not found in {deadline}s: {lastExceptionMessage}");
                    else return;
                }

                try
                {
                    HtmlElement he = instance.GetHe(obj, method);
                    _inputSleep.Sleep(delay);

                    //Thread.Sleep(delay * 1000);
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
        public static void HeDrop(this Instance instance, object obj, string method = "", int deadline = 10,   bool thrw = true)
        {
            DateTime functionStart = DateTime.Now;
            string lastExceptionMessage = "";

            while (true)
            {
                if ((DateTime.Now - functionStart).TotalSeconds > deadline)
                {
                    if (thrw) throw new TimeoutException($" not found in {deadline}s: {lastExceptionMessage}");
                    else return;
                }
                try
                {
                    HtmlElement he = instance.GetHe(obj, method);
                    HtmlElement heParent = he.ParentElement; heParent.RemoveChild(he);
                    break;
                }
                catch (Exception ex)
                {
                    lastExceptionMessage = ex.Message;
                }
                Thread.Sleep(500);
            }
           
        }

        //js
        public static string JsClick(this Instance instance, string selector, double delayX = 1.0)
        {
            _clickSleep.Sleep(delayX);
            try
            {
                string escapedSelector = selector
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"");

                string jsCode = $@"
                (function() {{
                    function findElement(selector) {{
                        // Сначала пытаемся найти в обычном DOM
                        let element = document.querySelector(selector);
                        if (element) return element;
                        
                        // Если не нашли, ищем во всех shadow roots
                        function searchInShadowRoots(root) {{
                            // Проверяем текущий уровень
                            let el = root.querySelector(selector);
                            if (el) return el;
                            
                            // Ищем все элементы с shadowRoot
                            let allElements = root.querySelectorAll('*');
                            for (let elem of allElements) {{
                                if (elem.shadowRoot) {{
                                    let found = searchInShadowRoots(elem.shadowRoot);
                                    if (found) return found;
                                }}
                            }}
                            return null;
                        }}
                        
                        return searchInShadowRoots(document);
                    }}
                    
                    var element = findElement(""{escapedSelector}"");
                    if (!element) {{
                        throw new Error(""Элемент не найден по селектору: {escapedSelector}"");
                    }}
                    
                    element.scrollIntoView({{ block: 'center' }});
                    
                    if (element.focus) {{
                        element.focus();
                    }}
                    
                    var clickEvent = new MouseEvent('click', {{
                        bubbles: true,
                        cancelable: true,
                        view: window,
                        button: 0,
                        composed: true  // важно для shadow DOM
                    }});
                    element.dispatchEvent(clickEvent);
                    
                    return 'Click successful';
                }})();
                ";

                string result = instance.ActiveTab.MainDocument.EvaluateScript(jsCode);
                return result;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
        public static string JsSet(this Instance instance, string selector, string value, double delayX = 1.0)
        {
            _inputSleep.Sleep(delayX);
            try
            {
                string escapedValue = value
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
                
                string escapedSelector = selector
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"");

                string jsCode = $@"
                    (function() {{
                        var element = document.querySelector(""{escapedSelector}"");
                        if (!element) {{
                            throw new Error(""Элемент не найден по селектору: {escapedSelector}"");
                        }}
                        
                        element.scrollIntoView({{ block: 'center' }});
                        
                        var clickEvent = new MouseEvent('click', {{
                            bubbles: true,
                            cancelable: true,
                            view: window
                        }});
                        element.dispatchEvent(clickEvent);
                        
                        element.focus();
                        
                        var focusinEvent = new FocusEvent('focusin', {{ bubbles: true }});
                        element.dispatchEvent(focusinEvent);
                        
                        element.value = '';
                        
                        document.execCommand('insertText', false, ""{escapedValue}"");
                        
                        var inputEvent = new Event('input', {{ bubbles: true }});
                        var changeEvent = new Event('change', {{ bubbles: true }});
                        element.dispatchEvent(inputEvent);
                        element.dispatchEvent(changeEvent);
                        
                        return 'Value set successfully';
                    }})();
                    ";

                string result = instance.ActiveTab.MainDocument.EvaluateScript(jsCode);
                return result;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
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
        public static void CloseNewTab(this Instance instance, int deadline = 10, int tabIndex = 2)
        {
            int i = 0;

            while (i < deadline)
            {
                i++;
                Thread.Sleep(1000);
                if (instance.AllTabs.ToList().Count == tabIndex)
                {
                    instance.CloseExtraTabs();
                    return;
                }
                    
            }
            throw new Exception("no new tab found");
        }
        public static void Go(this Instance instance, string url, bool strict = false, bool WaitTillLoad = false )
        {
            bool go = false;
            string current = instance.ActiveTab.URL;
            if (strict) if (current != url) go = true;
            if (!strict) if (!current.Contains(url)) go = true;
            if (go) instance.ActiveTab.Navigate(url, "");
            if (instance.ActiveTab.IsBusy && WaitTillLoad) instance.ActiveTab.WaitDownloading();
            
        }
        public static void F5(this Instance instance, bool WaitTillLoad = true)
        {
            instance.ActiveTab.MainDocument.EvaluateScript("location.reload(true)");
            if (instance.ActiveTab.IsBusy && WaitTillLoad) instance.ActiveTab.WaitDownloading();
        }

        public static void ScrollDown(this Instance instance, int y = 420)
        {
            bool emu = instance.UseFullMouseEmulation;
            instance.UseFullMouseEmulation = true;
            instance.ActiveTab.FullEmulationMouseWheel(0, y);
            instance.UseFullMouseEmulation = emu;
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

    public static partial class Fallback
    {
        public static void ClFlv2(this Instance instance)
        {
            instance.CFSolve();
        }
        public static string ClFl(this Instance instance, int deadline = 60, bool strict = false)
        {
           return instance.CFToken();
        }

        
        
    }


}
