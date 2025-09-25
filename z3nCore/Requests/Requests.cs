using System;
using System.Threading;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.Enums.Http;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
   
    public static class Requests
    {
        private static readonly object LockObject = new object();

        public static string GET(
            this IZennoPosterProjectModel project,
            string url,            
            string proxy = "",
            string[] headers = null,
            bool log = false,
            bool parseJson = false,
            int deadline = 15,
            bool throwOnFail = false)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var logger = new Logger(project, log, classEmoji: "↑↓");
            string debugProxy = proxy;

            try
            {
                string response;

                lock (LockObject)
                {
                    string proxyString = ParseProxy(project, proxy, logger);
                    response = ZennoPoster.HTTP.Request(
                        ZennoLab.InterfacesLibrary.Enums.Http.HttpMethod.GET,
                        url,
                        "application/json",
                        "",
                        proxyString,
                        "UTF-8",
                        ResponceType.BodyOnly,
                        deadline * 1000,
                        "",
                        project.Profile.UserAgent,
                        true,
                        5,
                        headers,
                        "",
                        false,
                        false,
                        project.Profile.CookieContainer);
                    if (parseJson) ParseJson(project, response, logger);
                    if (log) logger.Send($"response: [{response}]");
                }


                return response.Trim();
            }
            catch (Exception e)
            {
                logger.Send($"!W RequestErr: [{e.Message}] url:[{url}] (proxy: [{debugProxy})]");
                if (throwOnFail) throw;
                return string.Empty;
            }
        }

        public static string POST(
            this IZennoPosterProjectModel project,
            string url,
            string body,            
            string proxy = "",
            string[] headers = null,
            bool log = false,
            bool parseJson = false,
            int deadline = 15,
            bool throwOnFail = false)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var logger = new Logger(project, log, classEmoji: "↑↓");
            string debugProxy = proxy;

            try
            {
                string response;
                lock (LockObject)
                {
                    string proxyString = ParseProxy(project, proxy, logger);
                    response = ZennoPoster.HTTP.Request(
                        ZennoLab.InterfacesLibrary.Enums.Http.HttpMethod.POST,
                        url,
                        body,
                        "application/json",
                        proxyString,
                        "UTF-8",
                        ResponceType.BodyOnly,
                        deadline * 1000,
                        "",
                        project.Profile.UserAgent,
                        true,
                        5,
                        headers,
                        "",
                        false,
                        false,
                        project.Profile.CookieContainer);
                }

                //if (log) logger.Send($"body sent: [{body}]");
                if (parseJson) ParseJson(project, response, logger);
                if (log) logger.Send($"response: [{response}]");
                return response.Trim();
            }
            catch (Exception e)
            {
                logger.Send($"!W RequestErr: [{e.Message}] url:[{url}] (proxy: [{debugProxy})]");
                if (throwOnFail) throw;
                return string.Empty;
            }
        }

        private static string ParseProxy(IZennoPosterProjectModel project, string proxyString, Logger logger = null)
        {
            if (string.IsNullOrEmpty(proxyString)) return "";

            if (proxyString == "+")
            {
                string projectProxy = project.Var("proxy");
                if (!string.IsNullOrEmpty(projectProxy))
                    proxyString = projectProxy;
                else
                {
                    proxyString = project.SqlGet("proxy", "_instance");
                    logger?.Send($"Proxy retrieved from SQL: [{proxyString}]");
                }
            }

            try
            {
                if (proxyString.Contains("//"))
                {
                    proxyString = proxyString.Split('/')[2];
                }

                if (proxyString.Contains("@")) // Proxy with authorization (login:pass@proxy:port)
                {
                    string[] parts = proxyString.Split('@');
                    string credentials = parts[0];
                    string proxyHost = parts[1];
                    string[] creds = credentials.Split(':');
                    return $"http://{creds[0]}:{creds[1]}@{proxyHost}";
                }
                else // Proxy without authorization (proxy:port)
                {
                    return $"http://{proxyString}";
                }
            }
            catch (Exception e)
            {
                logger?.Send($"Proxy parsing error: [{e.Message}] [{proxyString}]");
                return "";
            }
        }

        private static void ParseJson(IZennoPosterProjectModel project, string json, Logger logger = null)
        {
            try
            {
                project.Json.FromString(json);
            }
            catch (Exception ex)
            {
                logger?.Send($"[!W JSON parsing error: {ex.Message}] [{json}]");
                 
            }
        }

        public static void SetProxy(this IZennoPosterProjectModel project,  Instance instance, string proxyString = null)
        {

            string proxy = ParseProxy(project, proxyString);

            if (string.IsNullOrEmpty(proxy)) throw new Exception("!W EMPTY Proxy");
            long uTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string ipLocal = project.GET($"http://api.ipify.org/");

            while (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - uTime < 60)
            {
                instance.SetProxy(proxy, true, true, true, true); Thread.Sleep(2000);
                string ipProxy = project.GET($"http://api.ipify.org/", proxy);
                project.log($"local:[{ipLocal}]?proxyfied:[{ipProxy}]");
                project.Variables["ip"].Value = ipProxy;
                project.Variables["proxy"].Value = proxy;
                if (ipLocal != ipProxy) return;
            }
            project.log("!W badProxy");
            throw new Exception("!W badProxy");
        }
    }
}



