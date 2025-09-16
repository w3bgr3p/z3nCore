using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{


    public class Traffic
    {
        private readonly IZennoPosterProjectModel _project;
        private readonly Instance _instance;
        private readonly Logger _logger;
        private readonly bool _showLog;

        public Traffic(IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            _project = project;
            _instance = instance;
            _showLog = log;
            _logger = new Logger(project, log: log, classEmoji: "🌎");
            _instance.UseTrafficMonitoring = true;
        }


        public string Get(string url, string parametr, bool reload = false, bool parse = false, int deadline = 15, int delay = 3)
        {
            var validParameters = new[] { "Method", "ResultCode", "Url", "ResponseContentType", "RequestHeaders", "RequestCookies", "RequestBody", "ResponseHeaders", "ResponseCookies", "ResponseBody" };
            if (!validParameters.Contains(parametr))
                throw new ArgumentException($"Invalid parameter: '{parametr}'. Valid parameters are: {string.Join(", ", validParameters)}");


            _project.Deadline();
            if (reload)
            {
                _instance.ActiveTab.MainDocument.EvaluateScript("location.reload(true)");
                if (_instance.ActiveTab.IsBusy) _instance.ActiveTab.WaitDownloading();
                
                Thread.Sleep(1000 * delay);

            }
            int i = 0;

            while (true)
            {
                _project.Deadline(deadline);
                Thread.Sleep(1000 * delay);
                var traffic = _instance.ActiveTab.GetTraffic();
                var data = new Dictionary<string, string>();
                i++;
                _logger.Send(i.ToString());

                foreach (var t in traffic)
                {
                    if (!t.Url.Contains(url) || t.Method == "OPTIONS")
                        continue;
                    _logger.Send(t.Url);
                    data.Add("Method", t.Method);
                    _logger.Send(t.Method);
                    data.Add("ResultCode", t.ResultCode.ToString());
                    data.Add("Url", t.Url);
                    data.Add("ResponseContentType", t.ResponseContentType);
                    data.Add("RequestHeaders", t.RequestHeaders);
                    _logger.Send(t.RequestHeaders);
                    data.Add("RequestCookies", t.RequestCookies);
                    data.Add("RequestBody", t.RequestBody);
                    data.Add("ResponseHeaders", t.ResponseHeaders);
                    data.Add("ResponseCookies", t.ResponseCookies);
                    data.Add("ResponseBody", t.ResponseBody == null ? "" : Encoding.UTF8.GetString(t.ResponseBody, 0, t.ResponseBody.Length));

                    if (data.TryGetValue(parametr, out var param))
                    {
                        _logger.Send($"[{parametr}] is [{param}]");
                        if (!string.IsNullOrEmpty(param))
                        {
                            if (parse) _project.Json.FromString(param);
                            return param;
                        }

                        break;
                    }

                }
                _logger.Send($"[{url}] not found in traffic");
            }
        }
        
        public Dictionary<string, string> Get(string url, bool reload = false,int deadline = 10)
        {
            _project.Deadline();
            _instance.UseTrafficMonitoring = true;
            if (reload) _instance.ActiveTab.MainDocument.EvaluateScript("location.reload(true)");

            get:
            _project.Deadline(deadline);
            Thread.Sleep(1000);
            var traffic = _instance.ActiveTab.GetTraffic();
            var data = new Dictionary<string, string>();
            foreach (var t in traffic)
            {
                if (t.Url.Contains(url))
                {
                    var Method = t.Method;

                    if (Method == "OPTIONS") continue;

                    var ResultCode = t.ResultCode.ToString();
                    var Url = t.Url;
                    var ResponseContentType = t.ResponseContentType;
                    var RequestHeaders = t.RequestHeaders;
                    var RequestCookies = t.RequestCookies;
                    var RequestBody = t.RequestBody;
                    var ResponseHeaders = t.ResponseHeaders;
                    var ResponseCookies = t.ResponseCookies;
                    var ResponseBody = t.ResponseBody == null ? "" : Encoding.UTF8.GetString(t.ResponseBody, 0, t.ResponseBody.Length);


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
            return data;
        }
        public string GetHeader(string url, string headerToGet = "Authorization", bool reload = false)
        {
            var Headers = new Traffic(_project, _instance, _showLog).Get(url, "RequestHeaders");
            var headers = new Dictionary<string, string>();
            foreach (string header in Headers.Split('\n'))
            {
                var key = header.Trim().Split(':')[0].ToLower();
                var value = header.Trim().Split(':')[1];
                headers.Add(key, value);
            }
            return headers[headerToGet.ToLower()];
        }
        public string GetParam(string url, string parametr, bool reload = false, int deadline = 10)
        {
            _project.Deadline();
            _instance.UseTrafficMonitoring = true;
            if (reload) _instance.ActiveTab.MainDocument.EvaluateScript("location.reload(true)");

            get:
            _project.Deadline(deadline);
            Thread.Sleep(1000);
            var traffic = _instance.ActiveTab.GetTraffic();
            var data = new Dictionary<string, string>();
            string param;
            foreach (var t in traffic)
            {
                if (t.Url.Contains(url))
                {
                    var Method = t.Method;

                    if (Method == "OPTIONS") continue;

                    var ResultCode = t.ResultCode.ToString();
                    var Url = t.Url;
                    var ResponseContentType = t.ResponseContentType;
                    var RequestHeaders = t.RequestHeaders;
                    var RequestCookies = t.RequestCookies;
                    var RequestBody = t.RequestBody;
                    var ResponseHeaders = t.ResponseHeaders;
                    var ResponseCookies = t.ResponseCookies;
                    var ResponseBody = t.ResponseBody == null ? "" : Encoding.UTF8.GetString(t.ResponseBody, 0, t.ResponseBody.Length);


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
                if (data.Count == 0) continue;
                else data.TryGetValue(parametr, out param);
                if (string.IsNullOrEmpty(param)) continue;
                else return param;
            }
            goto get;
        }
        
    }

}