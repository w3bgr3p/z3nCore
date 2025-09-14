using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Text;
using System.Globalization;
using System.Runtime.CompilerServices;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Diagnostics;

namespace z3nCore
{
    public static class Starter
    {
        private static string Quote(string name)
        {
            return $"\"{name.Replace("\"", "\"\"")}\"";
        }
        private static bool IsValidRange(string range)
        {
            if (string.IsNullOrEmpty(range)) return false;
            return Regex.IsMatch(range, @"^[\d\s,\-]+$");
        }
        public static bool ChooseSingleAcc(this IZennoPosterProjectModel project, bool oldest = false)
        {
            var listAccounts = project.Lists["accs"];
            string pathProfiles = project.Var("profiles_folder");

            
            if (listAccounts.Count == 0)
            {
                project.Variables["noAccsToDo"].Value = "True";
                project.SendToLog($"♻ noAccoutsAvaliable", LogType.Info, true, LogColor.Turquoise);
                project.Variables["acc0"].Value = "";
                return false;
            }

            int randomAccount = oldest ? 0 : new Random().Next(0, listAccounts.Count) ;
            string acc0 = listAccounts[randomAccount];
            project.Var("acc0", acc0);
            listAccounts.RemoveAt(randomAccount);
            project.L0g($"`working with: [acc{acc0}] accs left: [{listAccounts.Count}]");
            return true;
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
                project.SendWarningToLog($"file {filePath} not found. Last version can be downloaded by link \nhttps://raw.githubusercontent.com/w3bgrep/z3nFarm/master/DbBuilder.zp");
            }
            
        }
        
        public static List<string> ToDoQueries(this IZennoPosterProjectModel project, string toDo = null, string defaultRange = null, string defaultDoFail = null)
        {
            string tableName = project.Variables["projectTable"].Value;

            var nowIso = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            if (string.IsNullOrEmpty(toDo)) 
                toDo = project.Variables["cfgToDo"].Value;
            
            var toDoItems = new List<string>();

            foreach (string task in toDo.Split(','))
            { 
                toDoItems.Add(task.Trim());
            }
            
            
            string customTask  = project.Var("cfgCustomTask");
            if (!string.IsNullOrEmpty(customTask))
                toDoItems.Add(customTask);
            

            var allQueries = new List<(string TaskId, string Query)>();

            foreach (string taskId in toDoItems)
            {
                string trimmedTaskId = taskId.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedTaskId))
                {
                    string range = defaultRange ?? project.Variables["range"].Value;
                    string doFail = defaultDoFail ?? project.Variables["doFail"].Value;
                    project.ClmnAdd(trimmedTaskId,tableName);
                    string failCondition = (doFail != "True" ? "AND status NOT LIKE '%fail%'" : "");
                    string query = $@"SELECT {Quote("id")} 
                              FROM {Quote(tableName)} 
                              WHERE {Quote("id")} in ({range}) {failCondition} 
                              AND {Quote("status")} NOT LIKE '%skip%' 
                              AND ({Quote(trimmedTaskId)} < '{nowIso}' OR {Quote(trimmedTaskId)} = '')";
                    allQueries.Add((trimmedTaskId, query));
                }
            }

            return allQueries
                .OrderBy(x =>
                {
                    if (string.IsNullOrEmpty(x.TaskId))
                        return DateTime.MinValue;

                    if (DateTime.TryParseExact(
                            x.TaskId,
                            "yyyy-MM-ddTHH:mm:ss.fffZ",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                            out DateTime parsed))
                    {
                        return parsed;
                    }

                    // fallback: если парсинг не удался
                    return DateTime.MinValue;
                })
                .Select(x => x.Query)
                .ToList();
        }

        
        public static bool RunProject(this IZennoPosterProjectModel project, List<string> additionalVars = null, bool add = true )
        {
            string pathZp = project.Var("projectScript");
            var vars =  new List<string> {
                "acc0",
                "accRnd",
                "cfgChains",
                "cfgRefCode",
                "captchaModule",
                "cfgDelay",
                "cfgLog",
                "cfgPin",
                "cfgToDo",
                "DBmode",
                "DBpstgrPass",
                "DBpstgrUser",
                "DBsqltPath",
                "failReport",
                "humanNear",          
                "instancePort", 
                "ip",
                "run",
                "lastQuery",
                "lastErr",
                "pathCookies",
                "projectName",
                "projectTable", 
                "projectScript",
                "proxy",          
                "requiredSocial",
                "requiredWallets",
                "toDo",
                "varSessionId",
                "wkMode",
            };

            if (additionalVars != null)
            {
                if (add)
                {
                    foreach (var varName in additionalVars)
                        if (!vars.Contains(varName)) vars.Add(varName);
                }
                else
                {
                    vars = additionalVars;
                }

            }

            project.L0g($"running {pathZp}" );
            
            var mapVars = new List<Tuple<string, string>>();
            if (vars != null)
                foreach (var v in vars)
                    mapVars.Add(new Tuple<string, string>(v, v)); 
            return project.ExecuteProject(pathZp, mapVars, true, true, true); 
        }
    }
}