using System;
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

    }

}
