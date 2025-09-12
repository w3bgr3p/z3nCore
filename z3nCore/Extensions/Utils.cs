using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.ProjectModel;
using Newtonsoft.Json.Linq;

namespace z3nCore
{
    public static class Utils
    {
        
        public static void L0g(this IZennoPosterProjectModel project, string toLog, [CallerMemberName] string callerName = "", bool show = true, bool thr0w = false, bool toZp = true)
        {
            new Logger(project).Send(toLog, show: show, thr0w: thr0w, toZp: toZp);
        }
        public static string[] GetErr(this IZennoPosterProjectModel project, Instance instance, bool log = false)
        {
            var error = project?.GetLastError();
            if (error == null) return null;

            string actionId = error.ActionId.ToString() ?? "noActionId";
            string actionComment = error.ActionComment ??"noActionComment";

            string type = "";
            string msg = "";
            string stackTrace = "";
            string innerMsg = "";

            try {
	            var exception = error.Exception;
	            var typeEx = exception.GetType();
	            type = typeEx?.Name ?? "Unknown";
	            msg = exception.Message ?? string.Empty;
	            stackTrace = exception.StackTrace ?? string.Empty;
	            stackTrace = stackTrace.Split(new[] { 'в' }, StringSplitOptions.None).Skip(1).FirstOrDefault()?.Trim() ?? string.Empty;
	            innerMsg = exception.InnerException?.Message ?? string.Empty;
            }
            catch (Exception ex)
            {
	            project.SendWarningToLog(ex.Message);
            }
            
            string toLog = $"{actionId}: {actionComment}\n{type}\n{msg}\n{stackTrace}\n{innerMsg}".Trim();
            if (log) project?.SendWarningToLog(toLog);

            string failReport = $"⛔️\\#fail  \\#{project?.Var("projectScript")?.EscapeMarkdown() ?? "Unknown"} \\#{project?.Variables?["acc0"]?.Value ?? "Unknown"} \n" +
                               $"Error: `{actionId.EscapeMarkdown()}` \n" +
                               $"Type: `{type.EscapeMarkdown()}` \n" +
                               $"Msg: `{msg.EscapeMarkdown()}` \n" +
                               $"Trace: `{stackTrace.EscapeMarkdown()}` \n";
            if (!string.IsNullOrEmpty(innerMsg))
                failReport += $"Inner: `{innerMsg.EscapeMarkdown()}` \n";


            var browser = string.Empty;
            try { browser = instance.BrowserType.ToString(); } catch { }
            if (browser == "Chromium") failReport += $"Page: `{instance.ActiveTab?.URL?.EscapeMarkdown() ?? "Unknown"}`";



            if (project?.Variables?["failReport"] != null)
                project.Variables["failReport"].Value = failReport;

            try
            {
                string path = $"{project?.Path ?? ""}.failed\\{project?.Variables?["projectName"]?.Value ?? "Unknown"}\\{project?.Name ?? "Unknown"} • {project?.Variables?["acc0"]?.Value ?? "Unknown"} • [{project?.LastExecutedActionId ?? "Unknown"} - {actionId}].jpg";
                project?.SendInfoToLog(path);
	            ZennoPoster.ImageProcessingResizeFromScreenshot(instance?.Port ?? 0, path, 50, 50, "percent", true, false);
            }
            catch (Exception e)
            {
                if (log) project?.SendInfoToLog(e.Message ?? "Error during screenshot processing");
            }

            return new string[] { actionId, type, msg, stackTrace, innerMsg };
        }
        public static int Range(this IZennoPosterProjectModel project, string accRange = null, string output = null, bool log = false)
        {
            if (string.IsNullOrEmpty(accRange)) accRange = project.Variables["cfgAccRange"].Value;
            if (string.IsNullOrEmpty(accRange)) throw new Exception("range is not provided by input or project setting [cfgAccRange]");
            int rangeS, rangeE;
            string range;

            if (accRange.Contains(","))
            {
                range = accRange;
                var rangeParts = accRange.Split(',').Select(int.Parse).ToArray();
                rangeS = rangeParts.Min();
                rangeE = rangeParts.Max();
            }
            else if (accRange.Contains("-"))
            {
                var rangeParts = accRange.Split('-').Select(int.Parse).ToArray();
                rangeS = rangeParts[0];
                rangeE = rangeParts[1];
                range = string.Join(",", Enumerable.Range(rangeS, rangeE - rangeS + 1));
            }
            else
            {
                rangeE = int.Parse(accRange);
                rangeS = int.Parse(accRange);
                range = accRange;
            }
            project.Variables["rangeStart"].Value = $"{rangeS}";
            project.Variables["rangeEnd"].Value = $"{rangeE}";
            project.Variables["range"].Value = range;
            return rangeE;
            //project.L0g($"{rangeS}-{rangeE}\n{range}");
        }

        public static void Clean(this IZennoPosterProjectModel project, Instance instance)
        {
            bool releaseResouses = true;
            try { releaseResouses = project.Var("forceReleaseResouses") == "True"; } catch { }
            
            if (instance.BrowserType.ToString() == "Chromium" && releaseResouses)
            {
                try { instance.Launch(ZennoLab.InterfacesLibrary.Enums.Browser.BrowserType.Firefox45, false); } catch { }
                try { instance.ClearCookie(); instance.ClearCache(); } catch { }
            }

            if (!string.IsNullOrEmpty(project.Var("accRnd")))
                new FS(project).RmRf(project.Var("pathProfileFolder"));
        }
        public static void Finish(this IZennoPosterProjectModel project, Instance instance)
        {
            try
            {
                if (!string.IsNullOrEmpty(project.Var("acc0")))
                new Logger(project).SendToTelegram();
            }
            catch (Exception ex)
            {
                project.L0g(ex.Message);
            }
            var browser = string.Empty;
            try { browser = instance.BrowserType.ToString(); } catch { }
            if (browser == "Chromium" && !string.IsNullOrEmpty(project.Var("acc0")) && string.IsNullOrEmpty(project.Var("accRnd")))
                new Cookies(project, instance).Save("all", project.Var("pathCookies"));
            
            project.GSet("");
            project.Var("acc0", "");
        }

        public static string ReplaceCreds(this IZennoPosterProjectModel project, string social)
        {
            return new  FS(project).GetNewCreds(social);   
        }
        public static void WaitTx(this IZennoPosterProjectModel project, string rpc = null, string hash = null, int deadline = 60, string proxy = "", bool log = false)
        {
            //new W3b(project, log: log).WaitTx(rpc, hash, deadline);
            return;
        }

        public static string GetExtVer(string securePrefsPath, string extId)
        {
            string json = File.ReadAllText(securePrefsPath);
            JObject jObj = JObject.Parse(json);
            JObject settings = (JObject)jObj["extensions"]?["settings"];

            if (settings == null)
            {
                throw new Exception("Секция extensions.settings не найдена");
            }

            JObject extData = (JObject)settings[extId];
            if (extData == null)
            {
                throw new Exception($"Расширение с ID {extId} не найдено");
            }

            string version = (string)extData["manifest"]?["version"];
            if (string.IsNullOrEmpty(version))
            {
                throw new Exception($"Версия для расширения {extId} не найдена");
            }
            return version;
        }

        public static void ObsoleteCode(this IZennoPosterProjectModel project, string newName = "unknown")
        {
            try
            {
                if (project == null) return;

                var sb = new System.Text.StringBuilder();

                try
                {
                    var trace = new System.Diagnostics.StackTrace(1, true); // пропускаем сам метод
                    var oldName ="";
                    for (int i = 0; i < trace.FrameCount; i++)
                    {
                        var f = trace.GetFrame(i);
                        var m = f?.GetMethod();
                        if (m == null || m.DeclaringType == null) continue;

                        var typeName = m.DeclaringType.FullName;
                        if (string.IsNullOrEmpty(typeName)) continue;

                        // фильтрация системных и зенно-методов
                        if (typeName.StartsWith("System.") || typeName.StartsWith("ZennoLab.")) continue;
                        oldName = $"{typeName}.{m.Name}";
                        sb.AppendLine($"{typeName}.{m.Name}");
                    }
                    sb.AppendLine($"![OBSOLETE CODE. UPDATE REQUIRED]. Obsolete: [{oldName}] New: [{newName}]");
                    project.SendToLog(sb.ToString(), LogType.Warning, true, LogColor.Default);
                }
                catch (Exception ex)
                {
                    try
                    {
                        project.SendToLog($"!E WarnObsolete logging failed: {ex.Message}", LogType.Error, true, LogColor.Red);
                    }
                    catch { }
                }
            }
            catch { }
        }
        public static void RunZp(this IZennoPosterProjectModel project, List<string> vars = null)
        {

            string tempFilePath = project.Var("projectScript");
            var mapVars = new List<Tuple<string, string>>();

            if (vars != null)
                foreach (var v in vars)
                    try 
                    {
                        mapVars.Add(new Tuple<string, string>(v, v)); 
                    }
                    catch (Exception ex)
                    {
                        project.SendWarningToLog(ex.Message, true);
                        throw;
                    }

            try 
            { 
                project.ExecuteProject(tempFilePath, mapVars, true, true, true); 
            }
            catch (Exception ex) 
            { 
                project.SendWarningToLog(ex.Message, true);
                throw;
            }
            return;
        }
        
        
    }

}
