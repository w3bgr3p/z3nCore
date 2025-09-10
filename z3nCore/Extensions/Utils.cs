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
            
            project.NullVars();
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
        public static void _SAFU(this IZennoPosterProjectModel project)
        {
            string tempFilePath = project.Path + "_SAFU.zp";
            var mapVars = new List<Tuple<string, string>>();
            mapVars.Add(new Tuple<string, string>("acc0", "acc0"));
            mapVars.Add(new Tuple<string, string>("cfgPin", "cfgPin"));
            mapVars.Add(new Tuple<string, string>("DBpstgrPass", "DBpstgrPass"));
            try { project.ExecuteProject(tempFilePath, mapVars, true, true, true); }
            catch (Exception ex) { project.SendWarningToLog(ex.Message); }
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

        public static void OldCode(this IZennoPosterProjectModel project, string someName = "undefined")
        {
            try
            {
                if (project == null) return;

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"! using obsolete code. Switch to new call \"{someName}\" ASAP \n");

                try
                {
                    var trace = new System.Diagnostics.StackTrace(1, true); // пропускаем сам метод

                    for (int i = 0; i < trace.FrameCount; i++)
                    {
                        var f = trace.GetFrame(i);
                        var m = f?.GetMethod();
                        if (m == null || m.DeclaringType == null) continue;

                        var typeName = m.DeclaringType.FullName;
                        if (string.IsNullOrEmpty(typeName)) continue;

                        // фильтрация системных и зенно-методов
                        if (typeName.StartsWith("System.") || typeName.StartsWith("ZennoLab.")) continue;

                        sb.AppendLine($"{typeName}.{m.Name}");
                    }

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

        public static void BuildNewDatabase (this IZennoPosterProjectModel project)
        {
            if (project.Var("cfgBuldDb") != "True") return;

            string filePath = Path.Combine(project.Path, "DbBuilder.zp");
            if (File.Exists(filePath))
            {
                project.Var("projectScript",filePath);
                project.Var("wkMode","Build");
                project.Var("cfgAccRange", project.Var("rangeEnd"));
                
                var vars =  new List<string> {
                    "cfgLog",
                    "cfgPin",
                    "cfgAccRange",
                    "DBmode",
                    "DBpstgrPass",
                    "DBpstgrUser",
                    "DBsqltPath",
                    "debug",
                    "lastQuery",
                    "wkMode",
                };
                project.RunZp(vars);
            }
            else
            {
                project.SendWarningToLog($"file {filePath} not found. Last version can be downloaded by link \nhttps://raw.githubusercontent.com/w3bgrep/z3nFarm/master/DbBuilder.zp", false);
            }
            
        }

        public static void PrepareInstance(this IZennoPosterProjectModel project, Instance instance)
        {
            new Init(project, instance).PrepareInstance();
        }


    }
    public static class Vars
    {
        private static readonly object LockObject = new object();


        public static string Var(this IZennoPosterProjectModel project, string Var)
        {
            string value = string.Empty;
            try
            {
                value = project.Variables[Var].Value;
            }
            catch (Exception e)
            {
                project.SendInfoToLog(e.Message);
            }
            if (value == string.Empty)
            { }

            return value;
        }
        public static string Var(this IZennoPosterProjectModel project, string var, object value)
        {
            if (value == null ) return string.Empty;
            try
            {
                project.Variables[var].Value = value.ToString();
            }
            catch (Exception e)
            {
                project.SendInfoToLog(e.Message);
            }
            return string.Empty;
        }

        public static string VarRnd(this IZennoPosterProjectModel project, string Var)
        {
            string value = string.Empty;
            try
            {
                value = project.Variables[Var].Value;
            }
            catch (Exception e)
            {
                project.SendInfoToLog(e.Message);
            }
            if (value == string.Empty) project.L0g($"no Value from [{Var}] `w");

            if (value.Contains("-"))
            {
                var min = int.Parse(value.Split('-')[0].Trim());
                var max = int.Parse(value.Split('-')[1].Trim());
                return new Random().Next(min, max).ToString();
            }
            return value.Trim();
        }
        public static int VarCounter(this IZennoPosterProjectModel project, string varName, int input)
        {
            project.Variables[$"{varName}"].Value = (int.Parse(project.Variables[$"{varName}"].Value) + input).ToString();
            return int.Parse(project.Variables[varName].Value);
        }
        public static decimal VarsMath(this IZennoPosterProjectModel project, string varA, char operation, string varB, string varRslt = "a_")
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            decimal a = decimal.Parse(project.Var(varA));
            decimal b = decimal.Parse(project.Var(varB));
            decimal result;
            switch (operation)
            {
                case '+':

                    result = a + b;
                    break;
                case '-':
                    result = a - b;
                    break;
                case '*':
                    result = a * b;
                    break;
                case '/':
                    result = a / b;
                    break;
                default:
                    throw new Exception($"unsuppoted operation {operation}");
            }
            try { project.Var(varRslt, $"{result}"); } catch { }
            return result;
        }

        public static void acc0w(this IZennoPosterProjectModel project, object acc0)
        {
            project.Variables["acc0"].Value = acc0?.ToString() ?? string.Empty;
        }
        public static void NullVars(this IZennoPosterProjectModel project)
        {
            project.GlobalNull();
            project.Var("acc0", "");
        }
        public static bool ChooseSingleAcc(this IZennoPosterProjectModel project, bool oldest = false)
        {
            var listAccounts = project.Lists["accs"];
            string pathProfiles = project.Var("profiles_folder");

        check:
            if (listAccounts.Count == 0)
            {
                project.Variables["noAccsToDo"].Value = "True";
                project.SendToLog($"♻ noAccoutsAvaliable", LogType.Info, true, LogColor.Turquoise);
                project.Variables["acc0"].Value = "";
                return false;
                throw new Exception($"TimeToChill");
            }

            int randomAccount = oldest ? 0 : new Random().Next(0, listAccounts.Count) ;
            string acc0 = listAccounts[randomAccount];
            project.Var("acc0", acc0);
            listAccounts.RemoveAt(randomAccount);
            if (!project.GlobalSet(set:false))
                goto check;
            project.Var("pathProfileFolder", $"{pathProfiles}accounts\\profilesFolder\\{acc0}");
            project.Var("pathCookies", $"{pathProfiles}accounts\\cookies\\{acc0}.json");
            project.L0g($"`working with: [acc{acc0}] accs left: [{listAccounts.Count}]");
            return true;
        }

        public static string Invite(this IZennoPosterProjectModel project, string invite = null, bool log = false)
        {
            if (string.IsNullOrEmpty(invite)) invite = project.Variables["cfgRefCode"].Value;
            
            string tableName = project.Variables["projectTable"].Value;
            if (string.IsNullOrEmpty(invite)) invite =
                    project.SqlGet("refcode", tableName, where: @"TRIM(refcode) != '' ORDER BY RANDOM() LIMIT 1;");                  
            return invite;
        }

        public static bool RndBool(this int truePercent)
        {
            return new Random().NextDouble() * 100 < truePercent;
        }



        #region GlobalVars

        public static bool GlobalSet(this IZennoPosterProjectModel project, bool log = false , bool set = true, bool clean = false )
        {
            string nameSpase = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyTitleAttribute>()
            ?.Title ?? "Unknown";
            clean = (project.Variables["cleanGlobal"].Value == "True");


            var cleaned = new List<int>();
            var notDeclared = new List<int>();
            var busyAccounts = new List<string>();


            lock (LockObject)
            {
                try
                {

                    for (int i = 1; i <= int.Parse(project.Variables["rangeEnd"].Value); i++)
                    {
                        string threadKey = $"acc{i}";
                        try
                        {
                            var globalVar = project.GlobalVariables[nameSpase, threadKey];
                            if (globalVar != null)
                            {
                                if (!string.IsNullOrEmpty(globalVar.Value))
                                    busyAccounts.Add($"{i}:{globalVar.Value}");
                                if (clean)
                                {
                                    globalVar.Value = string.Empty;
                                    cleaned.Add(i);
                                }
                            }
                            else notDeclared.Add(i);
                        }
                        catch { notDeclared.Add(i); }
                    }

                    if (clean)
                        project.L0g($"!W cleanGlobal is [on] Cleaned: {string.Join(",", cleaned)}");
                    else
                        project.L0g($"buzy Accounts: [{string.Join(" | ", busyAccounts)}]");

                    int currentThread = int.Parse(project.Variables["acc0"].Value);
                    string currentThreadKey = $"acc{currentThread}";
                    if (!busyAccounts.Any(x => x.StartsWith($"{currentThread}:"))) //
                    {
                        if (!set) return true;
                        try
                        {
                            project.GlobalVariables.SetVariable(nameSpase, currentThreadKey, project.Variables["projectName"].Value);
                        }
                        catch
                        {
                            project.GlobalVariables[nameSpase, currentThreadKey].Value = project.Variables["projectName"].Value;
                        }
                        if (log) project.L0g($"{currentThreadKey} bound to {project.Variables["projectName"].Value}");
                        return true;
                    }
                    else
                    {
                        if (log) project.L0g($"{currentThreadKey} is already busy!");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    if (log) project.L0g($"⚙  {ex.Message}");
                    throw;
                }
            }
        }
        public static void GlobalGet(this IZennoPosterProjectModel project)
        {
            string nameSpase = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyTitleAttribute>()
            ?.Title ?? "Unknown";
            var cleaned = new List<int>();
            var notDeclared = new List<int>();
            var busyAccounts = new List<string>();

            try
            {

                for (int i = int.Parse(project.Variables["rangeStart"].Value); i <= int.Parse(project.Variables["rangeEnd"].Value); i++)
                {
                    string threadKey = $"acc{i}";
                    try
                    {
                        var globalVar = project.GlobalVariables[nameSpase, threadKey];
                        if (globalVar != null)
                        {
                            if (!string.IsNullOrEmpty(globalVar.Value)) busyAccounts.Add(i.ToString());
                            if (project.Variables["cleanGlobal"].Value == "True")
                            {
                                globalVar.Value = string.Empty;
                                cleaned.Add(i);
                            }
                        }
                        else notDeclared.Add(i);
                    }
                    catch { notDeclared.Add(i); }
                }
                if (project.Variables["cleanGlobal"].Value == "True") project.L0g($"GlobalVars cleaned: {string.Join(",", cleaned)}");
                else project.Variables["busyAccounts"].Value = string.Join(",", busyAccounts);
            }
            catch (Exception ex) { project.L0g($"⚙  {ex.Message}"); }

        }
        public static void GlobalNull(this IZennoPosterProjectModel project)
        {
            string nameSpase = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyTitleAttribute>()
            ?.Title ?? "Unknown";
            try
            {
                project.GlobalVariables[nameSpase, $"acc{project.Variables["acc0"].Value}"].Value = "";
                project.Var("acc0", "");
            }
            catch { }

        }

        #endregion

    }
}
