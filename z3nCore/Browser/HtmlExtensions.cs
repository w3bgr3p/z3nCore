using System;
using System.Collections.Generic;
using ZennoLab.CommandCenter;
using ZXing;

namespace z3nCore
{
    public static class HtmlExtensions
    {
        public static string DecodeQr(HtmlElement element)
        {
            try
            {
                var bitmap = element.DrawPartAsBitmap(0, 0, 200, 200, true);
                var reader = new BarcodeReader();
                var result = reader.Decode(bitmap);
                if (result == null || string.IsNullOrEmpty(result.Text)) return "qrIsNull";
                return result.Text;
            }
            catch (Exception) { return "qrError"; }
        }
        public static string GetTxHash(HtmlElement element)
        {
            string hash;
            
            try
            {
                string link = element.GetAttribute("href");
                if (!string.IsNullOrEmpty(link))
                {
                    int lastSlashIndex = link.LastIndexOf('/');
                    if (lastSlashIndex == -1) hash = link;

                    else if (lastSlashIndex == link.Length - 1) hash = string.Empty;
                    else hash = link.Substring(lastSlashIndex + 1);
                }
                else throw new Exception("empty Element");
            }
            catch (Exception ex) {
                throw new Exception(ex.Message);
            }
            return hash;
        }

        
        
        
        public static string GetXPath(this HtmlElement element)
        {
            if (element.IsVoid || element.IsNull)
                return string.Empty;
            
            List<string> parts = new List<string>();
            HtmlElement current = element;
            
            while (current != null && !current.IsVoid && !current.IsNull)
            {
                string part = BuildXPathPart(current);
                parts.Insert(0, part);
                
                HtmlElement parent = current.ParentElement;
                
                if (parent == null || parent.IsVoid || parent.IsNull)
                    break;
                    
                if (parent.TagName.ToLower() == "body")
                {
                    parts.Insert(0, "body");
                    break;
                }
                
                current = parent;
            }
            
            return "//*" + (parts.Count > 0 ? "/" + string.Join("/", parts) : "");
        }
        private static string BuildXPathPart(HtmlElement element)
        {
            string tag = element.TagName.ToLower();
            
            string id = element.GetAttribute("id");
            if (!string.IsNullOrEmpty(id))
                return tag + "[@id='" + id + "']";
            
            string className = element.GetAttribute("class");
            if (!string.IsNullOrEmpty(className))
            {
                string firstClass = className.Split(' ')[0].Trim();
                if (!string.IsNullOrEmpty(firstClass))
                    return tag + "[starts-with(@class,'" + firstClass + "')]";
            }
            
            string name = element.GetAttribute("name");
            if (!string.IsNullOrEmpty(name))
                return tag + "[@name='" + name + "']";
            
            int position = GetElementPosition(element);
            return tag + "[" + position + "]";
        }
        private static int GetElementPosition(HtmlElement element)
        {
            HtmlElement parent = element.ParentElement;
            if (parent == null || parent.IsVoid || parent.IsNull)
                return 1;
            
            string targetTag = element.TagName.ToLower();
            HtmlElementCollection siblings = parent.FindChildrenByTags(targetTag);
            
            if (siblings.IsVoid  || siblings.Count == 0)
                return 1;
            
            string targetOuterHtml = element.OuterHtml;
            
            for (int i = 0; i < siblings.Count; i++)
            {
                HtmlElement sibling = siblings.GetByNumber(i);
                if (sibling.OuterHtml == targetOuterHtml)
                    return i + 1;
            }
            
            return 1;
        }
        public static bool VerifyXPath(Tab tab, HtmlElement originalElement, string xpath)
        {
            if (string.IsNullOrEmpty(xpath))
                return false;
            
            HtmlElement foundElement = tab.FindElementByXPath(xpath, 0);
            
            if (foundElement.IsVoid || foundElement.IsNull)
                return false;
            
            return foundElement.OuterHtml == originalElement.OuterHtml;
        }
                
    }
    
}


