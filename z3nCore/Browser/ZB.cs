using System;
using System.IO;
using System.Collections.Generic;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;


namespace z3nCore
{
    public static class ZennoBrowser
    {
        private static readonly object _dbLock = new object();
        public static Dictionary<string, string> ZBids(this IZennoPosterProjectModel project)
        {
            lock (_dbLock)
            {
                var modeBkp = project.Var("DBmode");
                var pathBkp = project.Var("DBsqltPath");

                try
                {
                    project.Var("DBmode", "SQLite");
                    string dbPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "ZennoLab", "ZP8", ".zp8", "ProfileManagement.db");

                    if (!File.Exists(dbPath))
                        throw new FileNotFoundException($"ZB db not found by path: {dbPath}");

                    project.Var("DBsqltPath", dbPath);

                    var current = project.DbGetLines("id, name", "ProfileInfos", where: "id = id");
                    var zbId_acc0 = new Dictionary<string, string>();

                    foreach (var line in current)
                    {
                        var parts = line.Split('¦');
                        if (parts.Length < 2) continue;

                        var id = parts[0].Trim();
                        var acc = parts[1].Trim();

                        if (acc == "template") continue;
                        zbId_acc0.Add(id, acc);
                    }

                    return zbId_acc0;
                }
                finally
                {
                    project.Var("DBsqltPath", pathBkp);
                    project.Var("DBmode", modeBkp);
                }
            }
        }
        
        public static bool ZB(this IZennoPosterProjectModel project, string toDo)
        {
            var path = Path.Combine(project.Path,".internal","ZB.zp");
            project.Var("toDo", toDo);
            var vars = new List<string>
            {
                "acc0", "cfgLog", "cfgPin",
                "DBmode", "DBpstgrPass", "DBpstgrUser", "DBsqltPath",
                "instancePort", "lastQuery", "cookies",
                "projectScript", "varSessionId", "toDo", 
            };
            var mapVars = new List<Tuple<string, string>>();
            foreach (var v in vars) mapVars.Add(new Tuple<string, string>(v, v));
            return project.ExecuteProject(path, mapVars, true, true, true);
        }
    }
    
        public static class ZbDbManager
    {
        public static string ZBDbGet(this IZennoPosterProjectModel project,string query, string tableName = "ProfileInfos", bool log = false)
        {
            var modeBkp = project.Var("DBmode");   
            var pathBkp = project.Var("DBsqltPath");   
            var acc0Bkp = project.Var("acc0");   
	
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ZennoLab", "ZP8", ".zp8", "ProfileManagement.db"
            );
    
            if (!File.Exists(dbPath))
            {
                throw new FileNotFoundException(
                    $"База данных не найдена по пути: {dbPath}"
                );
            }
            project.Var("DBmode", "SQLite");
            project.Var("acc0", 1);
            project.Var("DBsqltPath", dbPath);

            
            project.Var("acc0",project.Var("zb_id")); 
            string resp = project.DbGet(query,tableName, log:log );
            
            project.Var("DBmode",modeBkp);   
            project.Var("DBsqltPath",pathBkp);  
            project.Var("acc0",acc0Bkp); 
            return resp;
            
        }
        public static Dictionary<string,string> ZBIdDic(this IZennoPosterProjectModel project, string json, string folder = null)
        {
            var array = JArray.Parse(json);
    
            var filtered = string.IsNullOrEmpty(folder) 
                ? array 
                : array.Where(x => (string)x["FolderName"] == folder);
    
            var nameToId = filtered
                .GroupBy(x => (string)x["Name"])
                .ToDictionary(g => g.Key, g => (string)g.First()["Id"]);

            return nameToId;
        }
        
        public static List<string> ZBIdList(this IZennoPosterProjectModel project, string json, string folder = "Farm")
        {
            var dic = project.ZBIdDic(json, folder);
            var res = new List<string>();
            foreach(var p in dic){
                res.Add(p.Value);
            }

            return res;
        }
        
    }
    
}