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
        
        public static void NullVars(this IZennoPosterProjectModel project)
        {
            project.GlobalNull();
            project.Var("acc0", "");
        }
        public static string Invite(this IZennoPosterProjectModel project, string invite = null, bool log = false)
        {
            if (string.IsNullOrEmpty(invite)) invite = project.Variables["cfgRefCode"].Value;
            
            string tableName = project.Variables["projectTable"].Value;
            if (string.IsNullOrEmpty(invite)) invite =
                    project.SqlGet("refcode", tableName, where: @"TRIM(refcode) != '' ORDER BY RANDOM() LIMIT 1;");                  
            return invite;
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