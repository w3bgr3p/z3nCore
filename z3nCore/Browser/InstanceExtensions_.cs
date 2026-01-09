using AForge.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
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
        private static readonly object _clipboardLock = new object();
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

                if (method == "random")
                {
                    var elements = instance.ActiveTab.FindElementsByAttribute(tag, attribute, pattern, mode).ToList();
                    if (elements.Count == 0)
                    {
                        throw new Exception($"no elements for random: tag='{tag}', attr='{attribute}', pattern='{pattern}'");
                    }
                    return elements.Rnd();
                }
                
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

        public static void WriteToScript(this HtmlElement he, string pathToScript, string action )
        {
            if (!string.IsNullOrEmpty(pathToScript))
            {
                string line = action + "\t" + he.GetXPath() + "\n"; 
                File.AppendAllText(pathToScript, line);
            }
        }

        //new
        public static string HeGet(this Instance instance, object obj, string method = "", int deadline = 10, string atr = "innertext", int delay = 1, bool thrw = true, bool thr0w = true, bool waitTillVoid = false, string pathToScript = null)
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
                    he.WriteToScript(pathToScript, "get");
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
        public static string HeCatch(this Instance instance, object obj, string method = "", int deadline = 10, string atr = "innertext", int delay = 1, string pathToScript = null)
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

        public static void HeMultiClick(this Instance instance,List<object> selectors)
        {
            foreach (var selector in selectors) instance.HeClick(selector);
        }

        public static void HeClick(this Instance instance, object obj, string method = "", int deadline = 10, double delay = 1, string comment = "", bool thrw = true , bool thr0w = true, int emu = 0, string pathToScript = null)
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
                    he.WriteToScript(pathToScript, "click");
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
        public static void HeSet(this Instance instance, object obj, string value, string method = "id", int deadline = 10, double delay = 1, string comment = "", bool thrw = true, bool thr0w = true, string pathToScript = null)
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
                    he.WriteToScript(pathToScript, "set");
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

        public static void CloseNewTab(this Instance instance, int deadline = 10, int tabIndex = 2, bool thrw = true)
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
            if (thrw)
                throw new Exception("no new tab found");
            
        }
        public static void Go(this Instance instance, string url, bool strict = false, bool waitTdle = false , bool newTab = false)
        {
           
            if (newTab)
            {
                Tab tab = instance.NewTab("new");
            }
            
            bool go = false;
            string current = instance.ActiveTab.URL;
            if (strict) if (current != url) go = true;
            if (!strict) if (!current.Contains(url)) go = true;
            if (go) instance.ActiveTab.Navigate(url, "");
            
            if (instance.ActiveTab.IsBusy && waitTdle) instance.ActiveTab.WaitDownloading();
            
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
            lock (_clipboardLock)
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
        
        
        public static void UpFromFolder(this Instance instance, string pathProfile,bool useProfile = false,
            ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType browserType = ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.Chromium)
        {
            
            ZennoLab.CommandCenter.Classes.BuiltInBrowserLaunchSettings settings =
                (ZennoLab.CommandCenter.Classes.BuiltInBrowserLaunchSettings)ZennoLab.CommandCenter.Classes.BrowserLaunchSettingsFactory.Create(browserType);
            settings.CachePath = pathProfile; 
            settings.ConvertProfileFolder = true;
            settings.UseProfile = useProfile;
            instance.Launch(settings);
        }
        public static void UpEmpty(this Instance instance)
        {
            instance.Launch(ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.Chromium, false);
        }       
        public static void Down(this Instance instance, int pauseAfterMs = 5000)
        {
            try {instance.Launch(ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.WithoutBrowser, false);} catch{ }
            Thread.Sleep(pauseAfterMs);
        }
        public static string SaveCookies(this Instance instance)
        {
            string tmp = Path.Combine(
                Path.GetTempPath(),
                $"cookies_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.txt"
            );

            try
            {
                instance.SaveCookie(tmp);
                var cookieContent = File.ReadAllText(tmp);
                return cookieContent;
            }
            finally
            {
                try
                {
                    if (File.Exists(tmp))
                    {
                        File.Delete(tmp);
                    }
                }
                catch
                {
                }
            }
        }

        #region Canvas

        private static string IsImg(string imgInput)
        {
            // Путь должен содержать расширение изображения
            if (imgInput.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                imgInput.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                imgInput.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                imgInput.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                imgInput.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                imgInput.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            {
                // Если это путь - читаем и конвертируем
                return Convert.ToBase64String(System.IO.File.ReadAllBytes(imgInput));
            }
            else
            {
                // Если это не путь - возвращаем как есть (уже Base64)
                return imgInput;
            }
        }

        /// <summary>
        /// Получить размер viewport браузера
        /// </summary>
        private static int[] GetViewportSize(Instance instance)
        {
            string js = @"
                return JSON.stringify({
                    width: window.innerWidth,
                    height: window.innerHeight
                });
            ";
            string result = instance.ActiveTab.MainDocument.EvaluateScript(js);
            
            var match = System.Text.RegularExpressions.Regex.Match(result, @"""width"":(\d+),""height"":(\d+)");
            int width = int.Parse(match.Groups[1].Value);
            int height = int.Parse(match.Groups[2].Value);
            
            return new int[] { width, height };
        }

        /// <summary>
        /// Найти изображение на экране и вернуть координаты его центра
        /// </summary>
        /// <returns>Координаты центра найденного изображения [x, y]</returns>
        public static int[] FindImg(this Instance instance, string imgFile, int[] searchArea)
        {
            Tab tab = instance.ActiveTab;
            if (tab.IsBusy) tab.WaitDownloading();

            string image = IsImg(imgFile);
            System.Drawing.Rectangle searchRect = new System.Drawing.Rectangle(searchArea[0], searchArea[1], searchArea[2], searchArea[3]); 
            System.Drawing.Rectangle[] searchAreas = new System.Drawing.Rectangle[] { searchRect };

            string rectStr = tab.FindImage(image, searchAreas, 99);

            if (string.IsNullOrEmpty(rectStr))
            {
                throw new Exception($"Изображение {imgFile} не найдено в указанной области.");
            }

            string[] parts = rectStr.Split(',');
            if (parts.Length != 4)
            {
                throw new Exception($"Некорректный формат координат: {rectStr}");
            }

            int left   = Convert.ToInt32(parts[0].Trim());
            int top    = Convert.ToInt32(parts[1].Trim());
            int width  = Convert.ToInt32(parts[2].Trim());
            int height = Convert.ToInt32(parts[3].Trim());

            int centerX = left + width / 2;
            int centerY = top + height / 2;

            if (centerX == 0 && centerY == 0)
            {
                throw new Exception("Изображение не найдено (координаты 0,0).");
            }

            return new int[] { centerX, centerY };
        }

        /// <summary>
        /// Найти изображение и кликнуть по его центру
        /// </summary>
        /// <returns>Координаты клика [x, y]</returns>
        public static int[] ClickImg(this Instance instance, string imgFile, int[] searchArea)
        {
            int[] coords = instance.FindImg(imgFile, searchArea);
            
            System.Drawing.Rectangle clickPoint = new System.Drawing.Rectangle(coords[0], coords[1], 1, 1);
            instance.ActiveTab.RiseEvent("click", clickPoint, "Left");
            
            return coords;
        }

        /// <summary>
        /// Найти изображение и тапнуть по его центру
        /// </summary>
        /// <returns>Координаты тапа [x, y]</returns>
        public static int[] TouchImg(this Instance instance, string imgFile, int[] searchArea)
        {
            int[] coords = instance.FindImg(imgFile, searchArea);
            
            instance.ActiveTab.Touch.Touch(coords[0], coords[1]);
            
            return coords;
        }

        /// <summary>
        /// Получить координаты центра экрана
        /// </summary>
        /// <returns>Координаты центра [x, y]</returns>
        public static int[] GetCenter(this Instance instance)
        {
            int[] viewport = GetViewportSize(instance);
            return new int[] { viewport[0] / 2, viewport[1] / 2 };
        }

        /// <summary>
        /// Переместить курсор в центр экрана
        /// </summary>
        /// <returns>Координаты центра [x, y]</returns>
        public static int[] MouseCenter(this Instance instance,bool moveMouse = false)
        {
            int[] center = instance.GetCenter();
            
            instance.UseFullMouseEmulation = true;
            var pos = new System.Drawing.Point(center[0], center[1]);
            if (moveMouse) 
                instance.ActiveTab.FullEmulationMouseMove(center[0], center[1]);
            else
                instance.ActiveTab.FullEmulationMouseCurrentPosition = pos;
            
            return center;
        }

        /// <summary>
        /// Получить область вокруг центра экрана
        /// </summary>
        /// <returns>Область поиска [x, y, width, height]</returns>
        /// <summary>
        /// Получить область вокруг центра экрана
        /// </summary>
        /// <returns>Область поиска [x, y, width, height]</returns>
        public static int[] CenterArea(this Instance instance, int width = 0, int height = 0)
        {
            int[] viewportSize = GetViewportSize(instance);
            
            if (width == 0 && height == 0)
            {
                // Если не указаны размеры - возвращаем весь viewport
                return new int[] { 0, 0, viewportSize[0], viewportSize[1] };
            }
            
            if (height == 0) height = width;

            int centerX = viewportSize[0] / 2;
            int centerY = viewportSize[1] / 2;

            int x = centerX - width / 2;
            int y = centerY - height / 2;

            return new int[] { x, y, width, height };
        }

        private static Random _random = new Random();

        /// <summary>
        /// Свайп от центра экрана в указанном направлении
        /// </summary>
        /// <returns>Координаты конечной точки свайпа [x, y]</returns>
        public static int[] SwipeFromCenter(this Instance instance, int distance, string direction = null)
        {
            int[] center = instance.GetCenter();
            int centerX = center[0];
            int centerY = center[1];

            if (string.IsNullOrEmpty(direction))
            {
                string[] directions = { "left", "up", "right", "down" };
                direction = directions[_random.Next(directions.Length)];
            }

            int toX = centerX;
            int toY = centerY;

            switch (direction.ToLower())
            {
                case "left":
                    toX = centerX - distance;
                    break;
                case "right":
                    toX = centerX + distance;
                    break;
                case "up":
                    toY = centerY - distance;
                    break;
                case "down":
                    toY = centerY + distance;
                    break;
                default:
                    throw new ArgumentException($"Неизвестное направление: {direction}. Используй: left, up, right, down");
            }

            instance.ActiveTab.Touch.SwipeBetween(centerX, centerY, toX, toY);
            
            return new int[] { toX, toY };
        }

        /// <summary>
        /// Найти изображение и свайпнуть его в центр экрана
        /// </summary>
        /// <returns>Координаты центра экрана [x, y]</returns>
        public static int[] SwipeImgToCenter(this Instance instance, string imgFile, int[] searchArea)
        {
            int[] imgCoords = instance.FindImg(imgFile, searchArea);
            int[] center = instance.GetCenter();

            instance.ActiveTab.Touch.SwipeBetween(imgCoords[0], imgCoords[1], center[0], center[1]);

            return center;
        }

        /// <summary>
        /// Тапнуть по центру экрана
        /// </summary>
        /// <returns>Координаты тапа [x, y]</returns>
        public static int[] TouchCenter(this Instance instance)
        {
            int[] center = instance.GetCenter();
            instance.ActiveTab.Touch.Touch(center[0], center[1]);
            return center;
        }

        /// <summary>
        /// Кликнуть по центру экрана
        /// </summary>
        /// <returns>Координаты клика [x, y]</returns>
        public static int[] ClickCenter(this Instance instance)
        {
            int[] center = instance.GetCenter();
            System.Drawing.Rectangle clickPoint = new System.Drawing.Rectangle(center[0], center[1], 1, 1);
            instance.ActiveTab.RiseEvent("click", clickPoint, "Left");
            return center;
        }

        public static Dictionary<string, int[]> FindMultipleInScreenshot(
            this Instance instance,
            Dictionary<string, string> templates,
            int[] searchArea,
            float threshold = 0.95f)
        {
            Tab tab = instance.ActiveTab;
            if (tab.IsBusy) tab.WaitDownloading();
            
            // 1. Получаем полный скриншот страницы
            string fullBase64 = tab.GetPagePreview();
            byte[] fullBytes = Convert.FromBase64String(fullBase64);
            
            Bitmap fullScreenshot;
            using (var ms = new MemoryStream(fullBytes))
            {
                fullScreenshot = new Bitmap(ms);
            }
            
            // 2. Вырезаем нужную область
            System.Drawing.Rectangle cropRect = new System.Drawing.Rectangle(
                searchArea[0], searchArea[1], searchArea[2], searchArea[3]
            );
            
            Bitmap screenshot = fullScreenshot.Clone(cropRect, fullScreenshot.PixelFormat);
            fullScreenshot.Dispose();
            
            var results = new Dictionary<string, int[]>();
            var matcher = new ExhaustiveTemplateMatching(threshold);
            
            // 3. Ищем ВСЕ шаблоны в ОДНОМ скриншоте
            foreach (var kvp in templates)
            {
                try
                {
                    byte[] templateBytes = Convert.FromBase64String(kvp.Value);
                    Bitmap templateBmp;
                    using (var ms = new MemoryStream(templateBytes))
                    {
                        templateBmp = new Bitmap(ms);
                    }
                    
                    TemplateMatch[] matches = matcher.ProcessImage(screenshot, templateBmp);
                    
                    if (matches != null && matches.Length > 0)
                    {
                        var bestMatch = matches[0];
                        
                        // Координаты относительно экрана
                        int centerX = searchArea[0] + bestMatch.Rectangle.X + bestMatch.Rectangle.Width / 2;
                        int centerY = searchArea[1] + bestMatch.Rectangle.Y + bestMatch.Rectangle.Height / 2;
                        
                        results[kvp.Key] = new int[] { centerX, centerY };
                    }
                    
                    templateBmp.Dispose();
                }
                catch { }
            }
            
            screenshot.Dispose();
            return results;
        }
        
        #endregion
                        
                

    }




}
