using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using AForge.Imaging;
using ZennoLab.CommandCenter;


    /// <summary>
    /// Canvas & Image Recognition methods for ZennoPoster automation
    /// </summary>
   namespace z3nCore
{
    public static partial class InstanceExtensions
    {
        private static Random _r = new Random();

        #region Image Conversion & Validation

        /// <summary>AForge requires 24bppRgb/32bppRgb/8bppIndexed, converts to 24bppRgb with white background</summary>
        public static Bitmap ConvertToSupportedFormat(Bitmap source)
        {
            if (source.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb)
                return source;

            Bitmap converted = new Bitmap(source.Width, source.Height,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(converted))
            {
                g.Clear(Color.White);
                g.DrawImage(source, 0, 0, source.Width, source.Height);
            }

            return converted;
        }

        private static string IsImg(string imgInput)
        {
            if (imgInput.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                imgInput.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                imgInput.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                imgInput.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                imgInput.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
                imgInput.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToBase64String(File.ReadAllBytes(imgInput));
            }

            return imgInput;
        }

        private static int[] GetViewportSize(Instance instance)
        {
            string js = @"
                return JSON.stringify({
                    width: window.innerWidth,
                    height: window.innerHeight
                });
            ";
            string result = instance.ActiveTab.MainDocument.EvaluateScript(js);

            var match = Regex.Match(result, @"""width"":(\d+),""height"":(\d+)");
            int width = int.Parse(match.Groups[1].Value);
            int height = int.Parse(match.Groups[2].Value);

            return new int[] { width, height };
        }

        #endregion

        #region Image Recognition - Native

        /// <summary>searchArea: [x, y, width, height]</summary>
        public static int[] FindImg(this Instance instance, string imgFile, int[] searchArea, float threshold = 0.99f, bool thrw = true)
        {
            Tab tab = instance.ActiveTab;
            if (tab.IsBusy) tab.WaitDownloading();

            string image = IsImg(imgFile);
            Rectangle searchRect = new Rectangle(searchArea[0], searchArea[1], searchArea[2], searchArea[3]);
            Rectangle[] searchAreas = new Rectangle[] { searchRect };

            string rectStr = tab.FindImage(image, searchAreas, (int)(threshold * 100));

            if (string.IsNullOrEmpty(rectStr))
            {
                if (thrw)
                    throw new Exception($"Изображение {imgFile} не найдено в указанной области.");
                else
                    return null;
            }

            string[] parts = rectStr.Split(',');
            if (parts.Length != 4)
                throw new Exception($"Некорректный формат координат: {rectStr}");

            int left = Convert.ToInt32(parts[0].Trim());
            int top = Convert.ToInt32(parts[1].Trim());
            int width = Convert.ToInt32(parts[2].Trim());
            int height = Convert.ToInt32(parts[3].Trim());

            int centerX = left + width / 2;
            int centerY = top + height / 2;

            if (centerX == 0 && centerY == 0)
                throw new Exception("Изображение не найдено (координаты 0,0).");

            return new int[] { centerX, centerY };
        }

        #endregion

        #region Image Recognition - AForge

        /// <summary>Single screenshot approach, memory-optimized</summary>
        public static int[] FindImgFast(this Instance instance, string imgFile, int[] searchArea,
            float threshold = 0.99f, bool thrw = true)
        {
            Tab tab = instance.ActiveTab;
            if (tab.IsBusy) tab.WaitDownloading();

            Bitmap fullScreenshot = null;
            Bitmap screenshot = null;
            Bitmap convertedScreenshot = null;
            Bitmap templateBmp = null;
            Bitmap convertedTemplate = null;

            try
            {
                string fullBase64 = tab.GetPagePreview();
                byte[] fullBytes = Convert.FromBase64String(fullBase64);

                using (var ms = new MemoryStream(fullBytes))
                {
                    fullScreenshot = new Bitmap(ms);
                }

                System.Drawing.Rectangle cropRect = new System.Drawing.Rectangle(
                    searchArea[0], searchArea[1], searchArea[2], searchArea[3]
                );

                screenshot = fullScreenshot.Clone(cropRect, fullScreenshot.PixelFormat);
                fullScreenshot.Dispose();
                fullScreenshot = null;

                convertedScreenshot = ConvertToSupportedFormat(screenshot);
                if (convertedScreenshot != screenshot)
                {
                    screenshot.Dispose();
                    screenshot = null;
                }

                string templateBase64 = IsImg(imgFile);
                byte[] templateBytes = Convert.FromBase64String(templateBase64);

                using (var ms = new MemoryStream(templateBytes))
                {
                    templateBmp = new Bitmap(ms);
                }

                convertedTemplate = ConvertToSupportedFormat(templateBmp);
                if (convertedTemplate != templateBmp)
                {
                    templateBmp.Dispose();
                    templateBmp = null;
                }

                var matcher = new ExhaustiveTemplateMatching(threshold);
                TemplateMatch[] matches = matcher.ProcessImage(convertedScreenshot, convertedTemplate);

                if (matches == null || matches.Length == 0)
                {
                    if (thrw)
                        throw new Exception($"Image not found: {imgFile}");
                    else
                        return null;
                }

                var bestMatch = matches[0];
                int centerX = searchArea[0] + bestMatch.Rectangle.X + bestMatch.Rectangle.Width / 2;
                int centerY = searchArea[1] + bestMatch.Rectangle.Y + bestMatch.Rectangle.Height / 2;

                return new int[] { centerX, centerY };
            }
            finally
            {
                if (convertedScreenshot != null) convertedScreenshot.Dispose();
                if (convertedTemplate != null) convertedTemplate.Dispose();
                if (screenshot != null) screenshot.Dispose();
                if (templateBmp != null) templateBmp.Dispose();
                if (fullScreenshot != null) fullScreenshot.Dispose();

                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>Finds all matches, filters by minDistance to avoid duplicates</summary>
        public static Dictionary<string, List<int[]>> FindAllInScreenshot(
            this Instance instance,
            Dictionary<string, string> templates,
            int[] searchArea,
            float threshold = 0.9f,
            int minDistance = 30)
        {
            Tab tab = instance.ActiveTab;
            if (tab.IsBusy) tab.WaitDownloading();

            string fullBase64 = tab.GetPagePreview();
            byte[] fullBytes = Convert.FromBase64String(fullBase64);

            Bitmap fullScreenshot;
            using (var ms = new MemoryStream(fullBytes))
            {
                fullScreenshot = new Bitmap(ms);
            }

            System.Drawing.Rectangle cropRect = new System.Drawing.Rectangle(
                searchArea[0], searchArea[1], searchArea[2], searchArea[3]
            );

            Bitmap screenshot = fullScreenshot.Clone(cropRect, fullScreenshot.PixelFormat);
            fullScreenshot.Dispose();

            Bitmap convertedScreenshot = ConvertToSupportedFormat(screenshot);
            if (convertedScreenshot != screenshot)
                screenshot.Dispose();

            var results = new Dictionary<string, List<int[]>>();
            var matcher = new ExhaustiveTemplateMatching(threshold);

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

                    Bitmap convertedTemplate = ConvertToSupportedFormat(templateBmp);
                    if (convertedTemplate != templateBmp)
                        templateBmp.Dispose();

                    TemplateMatch[] matches = matcher.ProcessImage(convertedScreenshot, convertedTemplate);

                    if (matches != null && matches.Length > 0)
                    {
                        var filteredCoords = new List<int[]>();
                        var sortedMatches = matches.OrderByDescending(m => m.Similarity).ToArray();

                        foreach (var match in sortedMatches)
                        {
                            int centerX = searchArea[0] + match.Rectangle.X + match.Rectangle.Width / 2;
                            int centerY = searchArea[1] + match.Rectangle.Y + match.Rectangle.Height / 2;

                            bool tooClose = false;
                            foreach (var existing in filteredCoords)
                            {
                                int dx = centerX - existing[0];
                                int dy = centerY - existing[1];
                                double distance = Math.Sqrt(dx * dx + dy * dy);

                                if (distance < minDistance)
                                {
                                    tooClose = true;
                                    break;
                                }
                            }

                            if (!tooClose)
                            {
                                filteredCoords.Add(new int[] { centerX, centerY });
                            }
                        }

                        results[kvp.Key] = filteredCoords;
                    }

                    convertedTemplate.Dispose();
                }
                catch
                {
                }
            }

            convertedScreenshot.Dispose();
            return results;
        }

        /// <summary>Single screenshot, multiple templates search</summary>
        public static Dictionary<string, int[]> FindMultipleInScreenshot(this Instance instance,
            Dictionary<string, string> templates, int[] searchArea, float threshold = 0.95f)
        {
            Tab tab = instance.ActiveTab;
            if (tab.IsBusy) tab.WaitDownloading();

            string fullBase64 = tab.GetPagePreview();
            byte[] fullBytes = Convert.FromBase64String(fullBase64);

            Bitmap fullScreenshot;
            using (var ms = new MemoryStream(fullBytes))
            {
                fullScreenshot = new Bitmap(ms);
            }

            System.Drawing.Rectangle cropRect = new System.Drawing.Rectangle(
                searchArea[0], searchArea[1], searchArea[2], searchArea[3]
            );

            Bitmap screenshot = fullScreenshot.Clone(cropRect, fullScreenshot.PixelFormat);
            fullScreenshot.Dispose();

            Bitmap convertedScreenshot = ConvertToSupportedFormat(screenshot);
            if (convertedScreenshot != screenshot)
                screenshot.Dispose();

            var results = new Dictionary<string, int[]>();
            var matcher = new ExhaustiveTemplateMatching(threshold);

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

                    Bitmap convertedTemplate = ConvertToSupportedFormat(templateBmp);
                    if (convertedTemplate != templateBmp)
                        templateBmp.Dispose();

                    TemplateMatch[] matches = matcher.ProcessImage(convertedScreenshot, convertedTemplate);

                    if (matches != null && matches.Length > 0)
                    {
                        var bestMatch = matches[0];

                        int centerX = searchArea[0] + bestMatch.Rectangle.X + bestMatch.Rectangle.Width / 2;
                        int centerY = searchArea[1] + bestMatch.Rectangle.Y + bestMatch.Rectangle.Height / 2;

                        results[kvp.Key] = new int[] { centerX, centerY };
                    }

                    convertedTemplate.Dispose();
                }
                catch
                {
                }
            }

            convertedScreenshot.Dispose();
            return results;
        }

        /// <summary>Single screenshot, each template has its own search area</summary>
        public static Dictionary<string, int[]> FindMultipleInMultipleAreas(
            this Instance instance,
            Dictionary<string, (string template, int[] area)> templatesWithAreas,
            float threshold = 0.95f)
        {
            Tab tab = instance.ActiveTab;
            if (tab.IsBusy) tab.WaitDownloading();

            string fullBase64 = tab.GetPagePreview();
            byte[] fullBytes = Convert.FromBase64String(fullBase64);

            Bitmap fullScreenshot;
            using (var ms = new MemoryStream(fullBytes))
            {
                fullScreenshot = new Bitmap(ms);
            }

            Bitmap convertedFullScreenshot = ConvertToSupportedFormat(fullScreenshot);
            if (convertedFullScreenshot != fullScreenshot)
                fullScreenshot.Dispose();

            var results = new Dictionary<string, int[]>();
            var matcher = new ExhaustiveTemplateMatching(threshold);

            foreach (var kvp in templatesWithAreas)
            {
                string name = kvp.Key;
                string templateBase64 = kvp.Value.template;
                int[] searchArea = kvp.Value.area;

                try
                {
                    System.Drawing.Rectangle cropRect = new System.Drawing.Rectangle(
                        searchArea[0], searchArea[1], searchArea[2], searchArea[3]
                    );

                    Bitmap cropped = convertedFullScreenshot.Clone(cropRect, convertedFullScreenshot.PixelFormat);

                    byte[] templateBytes = Convert.FromBase64String(templateBase64);
                    Bitmap templateBmp;
                    using (var ms = new MemoryStream(templateBytes))
                    {
                        templateBmp = new Bitmap(ms);
                    }

                    Bitmap convertedTemplate = ConvertToSupportedFormat(templateBmp);
                    if (convertedTemplate != templateBmp)
                        templateBmp.Dispose();

                    TemplateMatch[] matches = matcher.ProcessImage(cropped, convertedTemplate);

                    if (matches != null && matches.Length > 0)
                    {
                        var bestMatch = matches[0];

                        int centerX = searchArea[0] + bestMatch.Rectangle.X + bestMatch.Rectangle.Width / 2;
                        int centerY = searchArea[1] + bestMatch.Rectangle.Y + bestMatch.Rectangle.Height / 2;

                        results[name] = new int[] { centerX, centerY };
                    }

                    cropped.Dispose();
                    convertedTemplate.Dispose();
                }
                catch
                {
                }
            }

            convertedFullScreenshot.Dispose();
            return results;
        }

        /// <summary>Reuses cached screenshot base64, no Instance required</summary>
        public static Dictionary<string, int[]> FindMultipleInCachedScreenshot(string base64Screenshot,
            Dictionary<string, string> templates, int[] searchArea, float threshold = 0.95f)
        {
            byte[] fullBytes = Convert.FromBase64String(base64Screenshot);

            Bitmap fullScreenshot = null;
            Bitmap screenshot = null;
            Bitmap convertedScreenshot = null;

            try
            {
                using (var ms = new MemoryStream(fullBytes))
                {
                    fullScreenshot = new Bitmap(ms);
                }

                System.Drawing.Rectangle cropRect = new System.Drawing.Rectangle(
                    searchArea[0], searchArea[1], searchArea[2], searchArea[3]
                );

                screenshot = fullScreenshot.Clone(cropRect, fullScreenshot.PixelFormat);
                fullScreenshot.Dispose();
                fullScreenshot = null;

                convertedScreenshot = ConvertToSupportedFormat(screenshot);
                if (convertedScreenshot != screenshot)
                {
                    screenshot.Dispose();
                    screenshot = null;
                }

                var results = new Dictionary<string, int[]>();
                var matcher = new ExhaustiveTemplateMatching(threshold);

                foreach (var kvp in templates)
                {
                    Bitmap templateBmp = null;
                    Bitmap convertedTemplate = null;

                    try
                    {
                        byte[] templateBytes = Convert.FromBase64String(kvp.Value);

                        using (var ms = new MemoryStream(templateBytes))
                        {
                            templateBmp = new Bitmap(ms);
                        }

                        convertedTemplate = ConvertToSupportedFormat(templateBmp);
                        if (convertedTemplate != templateBmp)
                        {
                            templateBmp.Dispose();
                            templateBmp = null;
                        }

                        TemplateMatch[] matches = matcher.ProcessImage(convertedScreenshot, convertedTemplate);

                        if (matches != null && matches.Length > 0)
                        {
                            var bestMatch = matches[0];

                            int centerX = searchArea[0] + bestMatch.Rectangle.X + bestMatch.Rectangle.Width / 2;
                            int centerY = searchArea[1] + bestMatch.Rectangle.Y + bestMatch.Rectangle.Height / 2;

                            results[kvp.Key] = new int[] { centerX, centerY };
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        if (convertedTemplate != null) convertedTemplate.Dispose();
                        if (templateBmp != null) templateBmp.Dispose();
                    }
                }

                return results;
            }
            finally
            {
                if (convertedScreenshot != null) convertedScreenshot.Dispose();
                if (screenshot != null) screenshot.Dispose();
                if (fullScreenshot != null) fullScreenshot.Dispose();
            }
        }

        #endregion

        #region Click & Tap Actions

        public static int[] TapImg(this Instance instance, string imgFile, int[] searchArea, float threshold = 0.99f,
            bool nativeSearch = false, int delay = 0)
        {
            int[] coords = nativeSearch
                ? instance.FindImg(imgFile, searchArea, threshold)
                : instance.FindImgFast(imgFile, searchArea, threshold);
            Thread.Sleep(delay * 1000);
            instance.ActiveTab.Touch.Touch(coords[0], coords[1]);
            return coords;
        }

        public static int[] ClickImg(this Instance instance, string imgFile, int[] searchArea, float threshold = 0.99f,
            bool nativeSearch = true, int delay = 0)
        {
            int[] coords = nativeSearch
                ? instance.FindImg(imgFile, searchArea, threshold)
                : instance.FindImgFast(imgFile, searchArea, threshold);
            Rectangle clickPoint = new Rectangle(coords[0], coords[1], 1, 1);
            Thread.Sleep(delay * 1000);
            instance.ActiveTab.RiseEvent("click", clickPoint, "Left");
            return coords;
        }

        #endregion

        #region Viewport & Positioning

        public static int[] GetCenter(this Instance instance)
        {
            int[] viewport = GetViewportSize(instance);
            return new int[] { viewport[0] / 2, viewport[1] / 2 };
        }

        public static int[] MousePOsCenter(this Instance instance, bool moveMouse = false)
        {
            int[] center = instance.GetCenter();

            instance.UseFullMouseEmulation = true;
            var pos = new Point(center[0], center[1]);
            if (moveMouse)
                instance.ActiveTab.FullEmulationMouseMove(center[0], center[1]);
            else
                instance.ActiveTab.FullEmulationMouseCurrentPosition = pos;

            return center;
        }

        public static int[] TapCenter(this Instance instance)
        {
            int[] center = instance.GetCenter();
            instance.ActiveTab.Touch.Touch(center[0], center[1]);
            return center;
        }

        public static int[] ClickCenter(this Instance instance)
        {
            int[] center = instance.GetCenter();
            Rectangle clickPoint = new Rectangle(center[0], center[1], 1, 1);
            instance.ActiveTab.RiseEvent("click", clickPoint, "Left");
            return center;
        }

        /// <summary>Returns [x, y, width, height]. If width=0 and height=0, returns full viewport</summary>
        public static int[] CenterArea(this Instance instance, int width = 0, int height = 0)
        {
            int[] viewportSize = GetViewportSize(instance);

            if (width == 0 && height == 0)
            {
                return new int[] { 0, 0, viewportSize[0], viewportSize[1] };
            }

            if (height == 0) height = width;

            int centerX = viewportSize[0] / 2;
            int centerY = viewportSize[1] / 2;

            int x = centerX - width / 2;
            int y = centerY - height / 2;

            return new int[] { x, y, width, height };
        }

        #endregion

        #region Swipe Actions
        

        /// <summary>direction: left, right, up, down. Random if null. Coordinates limited by bounds [x, y, width, height]</summary>
        public static int[] SwipeFromCenter(this Instance instance, int distance, string direction = null, int[] bounds = null)
        {
            int[] center = instance.GetCenter();
            int centerX = center[0];
            int centerY = center[1];

            if (string.IsNullOrEmpty(direction))
            {
                string[] directions = { "left", "up", "right", "down" };
                direction = directions[_r.Next(directions.Length)];
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

            // Ограничиваем координаты границами
            if (bounds != null)
            {
                int minX = bounds[0];
                int minY = bounds[1];
                int maxX = bounds[0] + bounds[2];
                int maxY = bounds[1] + bounds[3];

                toX = Math.Max(minX, Math.Min(maxX, toX));
                toY = Math.Max(minY, Math.Min(maxY, toY));
            }

            instance.ActiveTab.Touch.SwipeBetween(centerX, centerY, toX, toY);
            return new int[] { toX, toY };
        }
        
        public static int[] SwipeImgToCenter(this Instance instance, string imgFile, int[] searchArea,
            float threshold = 0.95f, bool nativeSearch = false)
        {
            int[] coords = nativeSearch
                ? instance.FindImg(imgFile, searchArea, threshold)
                : instance.FindImgFast(imgFile, searchArea, threshold);
            int[] center = instance.GetCenter();
            instance.ActiveTab.Touch.SwipeBetween(coords[0], coords[1], center[0], center[1]);
            return center;
        }

        #endregion
    }
}
