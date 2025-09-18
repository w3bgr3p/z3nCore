using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using ZennoLab.InterfacesLibrary.ProjectModel;




namespace z3nCore
{
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
        public static decimal VarsMath(this IZennoPosterProjectModel project, string varA, string operation, string varB, string varRslt = null)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            decimal a = decimal.Parse(project.Var(varA));
            decimal b = decimal.Parse(project.Var(varB));
            decimal result;
            switch (operation)
            {
                case "+":
                    result = a + b;
                    break;
                case "-":
                    result = a - b;
                    break;
                case "*":
                    result = a * b;
                    break;
                case "/":
                    result = a / b;
                    break;
                default:
                    throw new Exception($"unsuppoted operation {operation}");
            }
            if (string.IsNullOrEmpty(varRslt)) 
                try { project.Var(varRslt, $"{result}"); } catch { }
            return result;
        }
        /*
        public static string ProjectName(this IZennoPosterProjectModel project)
        {
            string name = ProjectName(project.Variables["projectScript"].Value);
            project.Var("projectName", name);
            return name;
        }
        private static string ProjectName(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath)) throw new ArgumentNullException(nameof(projectPath));
            return System.IO.Path.GetFileName(projectPath).Split('.')[0];
        }
        public static string ProjectTable(this IZennoPosterProjectModel project)
        {
            string table  = "__" + ProjectName(project);
            project.Var("projectTable", table);
            return table;
        }
        public static string SessionId(this IZennoPosterProjectModel project)
        {
            string sessionId =  Time.Now("utcToId");
            project.Var("sessionId", sessionId);
            return sessionId;
        }
        */
    }

    public static class Constantas
    {
        private static readonly object LockObject = new object();
        public static string ProjectName(this IZennoPosterProjectModel project)
        {
            string name = ProjectName(project.Variables["projectScript"].Value);
            project.Var("projectName", name);
            return name;
        }
        private static string ProjectName(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath)) throw new ArgumentNullException(nameof(projectPath));
            return System.IO.Path.GetFileName(projectPath).Split('.')[0];
        }
        public static string ProjectTable(this IZennoPosterProjectModel project)
        {
            string table  = "__" + ProjectName(project);
            project.Var("projectTable", table);
            return table;
        }
        public static string SessionId(this IZennoPosterProjectModel project)
        {
            string sessionId =  Time.Now("utcToId");
            project.Var("sessionId", sessionId);
            return sessionId;
        }
        public static string CaptchaModule(this IZennoPosterProjectModel project)
        {
            string nameSpase = project.ExecuteMacro("{-Environment.CurrentUser-}");
            string localModule = project.Var("captchaModule");

            if (!string.IsNullOrEmpty(localModule))
                project.GVar("captcha",localModule);

            else localModule = project.GVar("capcha");

            if (string.IsNullOrEmpty(localModule))
                throw new Exception ("captchModule not set");
            return localModule;
        }
        
    }




    public static class GVars
    {
        private static readonly object LockObject = new object();

        public static string GVar(this IZennoPosterProjectModel project, string var)
        {
            string nameSpase = project.ExecuteMacro("{-Environment.CurrentUser-}");
            string globalVar = $"_{project.ProjectName()}_" + var;
            
            string value = string.Empty;
            lock (LockObject)
            {
                try
                {
                    value = project.GlobalVariables[nameSpase, globalVar].Value;
                }
                catch (Exception e)
                {
                    project.SendInfoToLog(e.Message);
                }
            }

            return value;
        }
        public static string GVar(this IZennoPosterProjectModel project, string var, object value)
        {

            string nameSpase = project.ExecuteMacro("{-Environment.CurrentUser-}");
            string globalVar = $"_{project.ProjectName()}_" + var;
            lock (LockObject)
            {
                try
                {
                    project.GlobalVariables[nameSpase, globalVar].Value = value.ToString();
                }
                catch
                {
                    try
                    {
                        project.GlobalVariables.SetVariable(nameSpase, globalVar, value.ToString());
                    }
                    catch (Exception e)
                    {
                        project.SendWarningToLog(e.Message);
                    }

                }
            }
            return string.Empty;
        }


        
        public static List<string> GGetBusyList(this IZennoPosterProjectModel project, bool log = false)
        {
            string nameSpase = project.ExecuteMacro("{-Environment.CurrentUser-}");
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
                            if (globalVar != null && !string.IsNullOrEmpty(globalVar.Value))
                            {
                                busyAccounts.Add($"{i}:{globalVar.Value}");
                            }
                        }
                        catch { }
                    }
                    
                    if (log)
                    {
                        project.L0g($"buzy Accounts: [{string.Join(" | ", busyAccounts)}]");
                    }
                    
                    return busyAccounts;
                }
                catch (Exception ex)
                {
                    if (log) project.L0g($"⚙ GGet: {ex.Message}");
                    throw;
                }
            }
        }

        public static bool GSetAcc(this IZennoPosterProjectModel project, string input = null, bool force = false, bool log = false)
        {
            string nameSpase = project.ExecuteMacro("{-Environment.CurrentUser-}");
            
            lock (LockObject)
            {
                try
                {
                    int currentThread = int.Parse(project.Variables["acc0"].Value);
                    string currentThreadKey = $"acc{currentThread}";
                    
                    string valueToSet = input ?? project.Variables["projectName"].Value;
                    
                    if (!force)
                    {
                        var busyAccounts = project.GGetBusyList(false);
                        if (busyAccounts.Any(x => x.StartsWith($"{currentThread}:")))
                        {
                            if (log) project.L0g($"{currentThreadKey} is already busy!");
                            return false;
                        }
                    }
                    
                    try
                    {
                        project.GlobalVariables.SetVariable(nameSpase, currentThreadKey, valueToSet);
                    }
                    catch
                    {
                        project.GlobalVariables[nameSpase, currentThreadKey].Value = valueToSet;
                    }
                    
                    if (log) 
                    {
                        string forceText = force ? " (forced)" : "";
                        project.L0g($"{currentThreadKey} bound to {valueToSet}{forceText}");
                    }
                    
                    return true;
                }
                catch (Exception ex)
                {
                    if (log) project.L0g($"⚙ GSet: {ex.Message}");
                    throw;
                }
            }
        }

        public static List<int> GClean(this IZennoPosterProjectModel project, bool log = false)
        {
            string nameSpase = project.ExecuteMacro("{-Environment.CurrentUser-}");
            var cleaned = new List<int>();
            
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
                                globalVar.Value = string.Empty;
                                cleaned.Add(i);
                            }
                        }
                        catch { }
                    }
                    
                    if (log)
                    {
                        project.L0g($"Cleaned accounts: {string.Join(",", cleaned)}");
                    }
                    
                    return cleaned;
                }
                catch (Exception ex)
                {
                    if (log) project.L0g($"⚙ GClean: {ex.Message}");
                    throw;
                }
            }
        }
        
    }

}