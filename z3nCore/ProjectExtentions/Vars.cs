using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using ZennoLab.InterfacesLibrary.ProjectModel;




namespace z3nCore
{
    public static class Vars
    {
        private static readonly object LockObject = new object();
        
        public static string Var(this IZennoPosterProjectModel project, string var)
        {
            string value = string.Empty;
            try
            {
                value = project.Variables[var].Value;
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

        public static string VarRnd(this IZennoPosterProjectModel project, string var)
        {
            string value = string.Empty;
            try
            {
                value = project.Variables[var].Value;
            }
            catch (Exception e)
            {
                project.SendInfoToLog(e.Message);
            }
            if (value == string.Empty) project.log($"no Value from [{var}] `w");

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
        public static decimal VarsMath(this IZennoPosterProjectModel project, string varA, string operation, string varB, string resultVar = null)
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
                    throw new Exception($"unsupported operation {operation}");
            }
            if (string.IsNullOrEmpty(resultVar)) 
                try { project.Var(resultVar, $"{result}"); } catch { }
            return result;
        }
        
    }

    public static class Constantes
    {
        private static readonly object LockObject = new object();
        public static string ProjectName(this IZennoPosterProjectModel project)
        {
            var path = project.Var("projectScript");
            if (string.IsNullOrEmpty(path)) throw new Exception("projectScript no defined");
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
        public static string SetSessionId(this IZennoPosterProjectModel project)
        {
            string sessionId =  Time.Now("utcToId");
            project.Var("varSessionId", sessionId);
            return sessionId;
        }
        public static string CaptchaModule(this IZennoPosterProjectModel project)
        {
           
            string localModule = project.Var("captchaModule");
            string globalVar = $"{project.ProjectName()}_" + "captcha";

            if (!string.IsNullOrEmpty(localModule))
                project.GVar(globalVar,localModule);

            else localModule = project.GVar(globalVar);

            if (string.IsNullOrEmpty(localModule))
                throw new Exception ("captchaModule not set");
            project.Var("captchaModule",localModule);
            return localModule;
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
    }




    public static class GVars
    {
        private static readonly object LockObject = new object();

        public static string GVar(this IZennoPosterProjectModel project, string var)
        {
            string nameSpase = project.ExecuteMacro("{-Environment.CurrentUser-}");
            //string globalVar = $"{project.ProjectName()}_" + var;
            
            string value = string.Empty;
            lock (LockObject)
            {
                try
                {
                    value = project.GlobalVariables[nameSpase, var].Value;
                }
                catch //(Exception e)
                {
                    //project.SendInfoToLog(e.Message);
                }
            }
            return value;
        }
        public static string GVar(this IZennoPosterProjectModel project, string var, object value)
        {
            string nameSpase = project.ExecuteMacro("{-Environment.CurrentUser-}");
            //string globalVar = $"{project.ProjectName()}_" + var;
            lock (LockObject)
            {
                try
                {
                    project.GlobalVariables[nameSpase, var].Value = value.ToString();
                }
                catch
                {
                    try
                    {
                        project.GlobalVariables.SetVariable(nameSpase, var, value.ToString());
                    }
                    catch //(Exception e)
                    {
                        //project.SendWarningToLog(e.Message);
                    }

                }
            }
            return string.Empty;
        }
        public static string DemandGVarFromUi(this IZennoPosterProjectModel project, string var, string message = null)
        {
            var value = project.GVar(var);
            if (!string.IsNullOrEmpty(value)) 
                return value;
            
            if (string.IsNullOrEmpty(message)) 
                message = $"input string to set global variable [{var}]";
            while (true)
            {
                if (string.IsNullOrEmpty(value)) 
                    value = F0rms.InputBox(message,700,100);
                else break;
            }
            project.GVar(var,value);
            return value;
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
                        project.log($"busy Accounts: [{string.Join(" | ", busyAccounts)}]");
                    }
                    
                    return busyAccounts;
                }
                catch (Exception ex)
                {
                    if (log) project.log($"⚙ GGet: {ex.Message}");
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
                            if (log) project.log($"{currentThreadKey} is already busy!");
                            return false;
                        }
                    }
                    
                    try
                    {
                        project.GlobalVariables.SetVariable(nameSpase, currentThreadKey, valueToSet);
                    }
                    catch (Exception ex)
                    {
                        if (log) project.SendWarningToLog(ex.Message, true);
                        project.GlobalVariables[nameSpase, currentThreadKey].Value = valueToSet;
                    }
                    
                    if (log) 
                    {
                        string forceText = force ? " (forced)" : "";
                        project.log($"{currentThreadKey} bound to {valueToSet}{forceText}");
                    }
                    
                    return true;
                }
                catch (Exception ex)
                {
                    if (log) project.log($"⚙ GSet: {ex.Message}");
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
                        project.log($"Cleaned accounts: {string.Join(",", cleaned)}");
                    }
                    
                    return cleaned;
                }
                catch (Exception ex)
                {
                    if (log) project.log($"⚙ GClean: {ex.Message}");
                    throw;
                }
            }
        }
        
    }

}