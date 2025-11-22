using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ZennoLab.CommandCenter;
using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public static class SQLite
    {
        public static string lSQL(IZennoPosterProjectModel project, string query, bool log = false, bool ignoreErrors = false)
        {
            try
            {
                string connectionString = $"Dsn=SQLite3 Datasource; database={project.Variables["DBsqltPath"].Value};";
                var response = ZennoPoster.Db.ExecuteQuery(query, null, ZennoLab.InterfacesLibrary.Enums.Db.DbProvider.Odbc, connectionString, "|", "\r\n", true);
                if (log)
                {
                    if (query.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)) project.SendToLog($"[SQLite ▼ ]: [{Regex.Replace(query.Trim(), @"\s+", " ")}]\nRESULT: [{response.Replace('\n', '|')}]", LogType.Info, true, LogColor.Gray);
                    else
                    {
                        if (response != "0") project.SendToLog($"[SQLite ▲ ]: [{Regex.Replace(query.Trim(), @"\s+", " ")}]", LogType.Info, true, LogColor.Gray);
                        else project.SendToLog($"[SQLite ▲ ]: [{Regex.Replace(query.Trim(), @"\s+", " ")}]", LogType.Info, true, LogColor.Default);
                    }
                }
                return response;
            }
            catch (Exception ex)
            {
                //project.SendToLog($"{ex.Message}", LogType.Warning);

                project.log($"!W Err:[{ex.Message}]. debug:\n{query}");
                if (!ignoreErrors) throw ;
                return string.Empty;
            }
        }

        public static void lSQLMakeTable(IZennoPosterProjectModel project, Dictionary<string, string> tableStructure, string tableName = "", bool strictMode = false)
        {
            if (tableName == "") tableName = project.Variables["projectTable"].Value;
            string tableExists = lSQL(project, $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}';");
            if (tableExists.Trim() == "0")
            {
                string createTableQuery = $"CREATE TABLE {tableName} (";
                createTableQuery += string.Join(", ", tableStructure.Select(kvp => $"{kvp.Key} {kvp.Value}"));
                createTableQuery += ");";
                lSQL(project, createTableQuery);
            }
            else
            {
                string[] currentColumns = lSQL(project, $"SELECT name FROM pragma_table_info('{tableName}');").Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (strictMode)
                {
                    var columnsToRemove = currentColumns.Where(col => !tableStructure.ContainsKey(col)).ToList();
                    foreach (var column in columnsToRemove) lSQL(project, $"ALTER TABLE {tableName} DROP COLUMN {column};");
                }
                foreach (var column in tableStructure)
                {
                    string resp = lSQL(project, $"SELECT COUNT(*) FROM pragma_table_info('{tableName}') WHERE name='{column.Key}';");
                    if (resp.Trim() == "0") lSQL(project, $"ALTER TABLE {tableName} ADD COLUMN {column.Key} {column.Value};");
                }
            }

            if (tableStructure.ContainsKey("acc0") && !string.IsNullOrEmpty(project.Variables["rangeEnd"].Value))
            {
                for (int currentAcc0 = 1; currentAcc0 <= int.Parse(project.Variables["rangeEnd"].Value); currentAcc0++)
                { lSQL(project, $"INSERT OR IGNORE INTO {tableName} (acc0) VALUES ('{currentAcc0}');"); }
            }
        }

    }
}