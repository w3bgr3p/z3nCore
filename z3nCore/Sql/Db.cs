﻿using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ZennoLab.InterfacesLibrary.ProjectModel;
using System.Globalization;

namespace z3nCore
{
    public static class Db
    {
        private static string _schemaName = "public";

        private static string UnQuote( string name)
        {
            return name.Replace("\"","");
        }
        private static string Quote(string name)
        {
            return $"\"{name.Replace("\"", "\"\"")}\"";
        }
        private static string QuoteColumns(this string updateString)
        {
            var parts = updateString.Split(',').Select(p => p.Trim()).ToList();
            var result = new List<string>();

            foreach (var part in parts)
            {
                int equalsIndex = part.IndexOf('=');
                if (equalsIndex > 0)
                {
                    string columnName = part.Substring(0, equalsIndex).Trim();
                    string valuePart = part.Substring(equalsIndex).Trim();

                    result.Add($"\"{columnName}\" {valuePart}");
                }
                else
                {
                    result.Add(part);
                }
            }
            return string.Join(", ", result);
        }
        private static string QuoteSelectColumns(this string columnString)
        {
            return string.Join(", ", 
                columnString.Split(',')
                    .Select(col => $"\"{col.Trim()}\""));
        }
        private static readonly Regex ValidNamePattern = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);
        private static string ValidateName(string name, string paramName)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"{paramName} cannot be null or empty");

            if (!ValidNamePattern.IsMatch(name))
                throw new ArgumentException($"Invalid {paramName}: {name}. Only alphanumeric characters and underscores are allowed.");

            return name;
        }
        private static bool IsValidRange(string range)
        {
            if (string.IsNullOrEmpty(range)) return false;
            return Regex.IsMatch(range, @"^[\d\s,\-]+$");
        }

        public static string DbGet(this IZennoPosterProjectModel project, string toGet, string tableName = null, bool log = false, bool throwOnEx = false, string key = "id", string acc = null, string where = "")
        {
            return project.SqlGet(toGet, tableName, log, throwOnEx, key, acc, where);
        }
        public static void DbUpd(this IZennoPosterProjectModel project, string toUpd, string tableName = null, bool log = false, bool throwOnEx = false, bool last = true, string key = "id", object acc = null, string where = "")
        {
            if (toUpd.Contains("relax")) log = true;
            try { project.Var("lastQuery", toUpd); } catch (Exception Ex){ project.SendWarningToLog(Ex.Message, true); }
            project.SqlUpd(toUpd, tableName, log, throwOnEx, last, key, acc, where);

        }

        public static void DbStatus(this IZennoPosterProjectModel project, string status, string tableName = null)
        {
             project.DbUpd($"status = '{status}'", tableName);
             return;
        }


        public static string Ref(this IZennoPosterProjectModel project, string refCode = null, bool log = false, string limit = null)
        {
            project.ObsoleteCode("project.RndInvite");
            return project.RndInvite(limit, log);
        }
        public static string Invite(this IZennoPosterProjectModel project,  object limit = null, bool log = false)
        {
            project.ObsoleteCode("project.RndInvite");
            return project.RndInvite(limit, log);
        }

        public static void DbSettings(this IZennoPosterProjectModel project, bool set = true, bool log = false)
        {
            var dbConfig = new Dictionary<string, string>();
            var resp = project.DbQ($"SELECT {Quote("id")}, {Quote("value")} FROM {Quote("_settings")}", log);
            foreach (string varData in resp.Split('\n'))
            {
                if (string.IsNullOrEmpty(varData)) continue;

                var parts = varData.Split('|');
                if (parts.Length >= 2)
                {
                    string varName = parts[0];
                    string varValue = string.Join("|", parts.Skip(1)).Trim(); 

                    dbConfig.Add(varName, varValue);
                    if (set)
                    {
                        try { project.Var(varName, varValue); }
                        catch (Exception e) { e.Throw(project, throwEx: false); }
                    }
                }
            }

        }
        
        public static void MigrateTable(this IZennoPosterProjectModel project, string source, string dest)
        {
            ValidateName(source, "source table");
            ValidateName(dest, "destination table");
            project.SendInfoToLog($"{source} -> {dest}", true);
            project.TableCopy(source, dest);
            try { project.DbQ($"ALTER TABLE {Quote(dest)} RENAME COLUMN {Quote("acc0")} to {Quote("id")}"); } catch { }
            try { project.DbQ($"ALTER TABLE {Quote(dest)} RENAME COLUMN {Quote("key")} to {Quote("id")}"); } catch { }
        }

        public static string DbGetRandom(this IZennoPosterProjectModel project, string toGet, string tableName = null, bool log = false, bool acc = false, bool throwOnEx = false, int range = 0, bool single = true, bool invert = false)
        {
            if (range == 0) range = project.Range();
            if (string.IsNullOrEmpty(tableName)) tableName = project.ProjectTable();;

            string acc0 = string.Empty;
            if (acc) acc0 = "id, ";
            string query = $@"
                SELECT {acc0}{toGet.Trim().TrimEnd(',')} 
                from {tableName} 
                WHERE TRIM({toGet}) != ''
	            AND id < {range}
                ORDER BY RANDOM()";

            if (single) query += " LIMIT 1;";
            if (invert) query = query.Replace("!=", "=");

            return project.DbQ(query, log: log, throwOnEx: throwOnEx);
        }
        public static string DbKey(this IZennoPosterProjectModel project, string chainType = "evm")
        {

            chainType = chainType.ToLower().Trim();
            switch (chainType)
            {
                case "evm":
                    chainType = "secp256k1";
                    break;
                case "sol":
                    chainType = "base58";
                    break;
                case "seed":
                    chainType = "bip39";
                    break;
                default:
                    throw new Exception("unexpected input. Use (evm|sol|seed|pkFromSeed)");
            }

            var resp = project.DbGet(chainType, "_wallets");
            string decoded = !string.IsNullOrEmpty(project.Var("cfgPin")) ? SAFU.Decode(project, resp) : resp;
            //if (!string.IsNullOrEmpty(project.Var("cfgPin")))
            //return SAFU.Decode(project, resp);
            return decoded;

        }
        public static string SqlGet(this IZennoPosterProjectModel project, string toGet, string tableName = null, bool log = false, bool throwOnEx = false, string key = "id", object id = null, string where = "")
        {

            if (string.IsNullOrWhiteSpace(toGet))
                throw new ArgumentException("Column names cannot be null or empty", nameof(toGet));

            toGet = QuoteSelectColumns(toGet.Trim().TrimEnd(','));
            if (string.IsNullOrEmpty(tableName)) 
                tableName = project.Variables["projectTable"].Value;
            
            if (id is null) id = project.Variables["acc0"].Value;

            string query;
            if (string.IsNullOrEmpty(where))
            {
                if (string.IsNullOrEmpty(id.ToString())) 
                    throw new ArgumentException("variable \"acc0\" is null or empty", nameof(id));
                query = $"SELECT {toGet} from {Quote(tableName)} WHERE {Quote(key)} = {id}";
            }
            else
            {
                query = $@"SELECT {toGet} from {Quote(tableName)} WHERE {where};";
            }

            return project.DbQ(query, log: log, throwOnEx: throwOnEx);
        }
        private static string SqlUpd(this IZennoPosterProjectModel project, string toUpd, string tableName = null, bool log = false, bool throwOnEx = false, bool last = true, string key = "id", object id = null, string where = "")
        {          
            var parameters = new DynamicParameters();
            if (string.IsNullOrEmpty(tableName)) tableName = project.Var("projectTable");
            if (string.IsNullOrEmpty(tableName)) throw new Exception("TableName is null");
            
            toUpd = QuoteColumns(toUpd);
            tableName = Quote(tableName);

            if (last)
            { 
                if (last) toUpd = toUpd + $", last = '{DateTime.UtcNow.ToString("MM-ddTHH:mm")}'";
            }

            if (id is null)
                id = project.Variables["acc0"].Value;
            
            string query;
            if (string.IsNullOrEmpty(where))
            {
                if (string.IsNullOrEmpty(id.ToString()))
                    throw new ArgumentException("variable \"acc0\" is null or empty", nameof(id));
                query = $"UPDATE {tableName} SET {toUpd} WHERE {Quote(key)} = {id}";
            }
            else
            {
                query = $"UPDATE {tableName} SET {toUpd} WHERE {where}";
            }
            return project.DbQ(query, log:log);
        }

        //Tables
        public static void TblAdd(this IZennoPosterProjectModel project,  Dictionary<string, string> tableStructure, string tblName, bool log = false)
        {
            
            if (project.TblExist(tblName, log:log)) return;

            tblName = Quote(tblName);

            bool _pstgr = project.Var("DBmode") == "PostgreSQL";

            string query;
            if (_pstgr)
                query = ($@" CREATE TABLE {tblName} ( {string.Join(", ", tableStructure.Select(kvp => $"\"{kvp.Key}\" {kvp.Value.Replace("AUTOINCREMENT", "SERIAL")}"))} );");
            else
                query = ($"CREATE TABLE {tblName} (" + string.Join(", ", tableStructure.Select(kvp => $"{Quote(kvp.Key)} {kvp.Value}")) + ");");
            project.DbQ(query, log: log);

        }
        public static bool TblExist(this IZennoPosterProjectModel project, string tblName, bool log = false)
        {
            tblName = UnQuote(tblName);
            bool _pstgr = project.Var("DBmode") == "PostgreSQL";
            string query;

            if (_pstgr) 
                query = ($"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '{tblName}';");
            else 
                query = ($"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tblName}';");

            string resp = project.DbQ(query, log);

            if (resp == "0" || resp == string.Empty) return false;
            else return true;
        }

        public static List<string> TblColumns(this IZennoPosterProjectModel project, string tblName, bool log = false)
        {
            var result = new List<string>();
            string query = project.Var("DBmode") == "PostgreSQL"
                ? $@"SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '{UnQuote(tblName)}';"
                : $"SELECT name FROM pragma_table_info('{UnQuote(tblName)}');";

            result = project.DbQ(query, log: log)
                .Split('\n')
                .Select(s => s.Trim())
                .ToList();
            return result;
        }

        public static Dictionary<string, string> TblForProject(this IZennoPosterProjectModel project, string[] projectColumns , string defaultType = "TEXT DEFAULT ''")
        {
            var projectColumnsList =projectColumns.ToList();
            return TblForProject(project, projectColumnsList, defaultType);
        }

        public static Dictionary<string, string> TblForProject(this IZennoPosterProjectModel project, List<string> projectColumns = null,  string defaultType = "TEXT DEFAULT ''")
        {
            string cfgToDo = project.Variables["cfgToDo"].Value;
            var tableStructure = new Dictionary<string, string>
            {
                { "id", "INTEGER PRIMARY KEY" },
                { "status", defaultType },
                { "last", defaultType }
            };

            if (projectColumns != null)
            {
                foreach (string column in projectColumns)
                {
                    string trimmed = column.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !tableStructure.ContainsKey(trimmed))
                    {
                        tableStructure.Add(trimmed, defaultType);
                    }
                }

            }

            if (!string.IsNullOrEmpty(cfgToDo))
            {
                string[] toDoItems = (cfgToDo ?? "").Split(',');
                foreach (string taskId in toDoItems)
                {
                    string trimmedTaskId = taskId.Trim();
                    if (!string.IsNullOrEmpty(trimmedTaskId) && !tableStructure.ContainsKey(trimmedTaskId))
                    {
                        tableStructure.Add(trimmedTaskId, defaultType);
                    }
                }
            }
            return tableStructure;
        }
        public static void TblPrepareDefault(this IZennoPosterProjectModel project, bool log = false)
        {
            var tableStructure = project.TblForProject();
            var tblName = project.Var("projectTable");

            project.TblAdd(tableStructure, tblName, log: log);
            project.ClmnAdd(tableStructure, tblName, log: log);
            project.AddRange(tblName,log:log);
        }
        //Columns
        public static bool ClmnExist(this IZennoPosterProjectModel project, string clmnName, string tblName, bool log = false)
        {

            bool _pstgr = project.Var("DBmode") == "PostgreSQL";
            string query;

            if (_pstgr)
                query = $@"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = '{_schemaName}' AND table_name = '{UnQuote(tblName)}' AND lower(column_name) = lower('{UnQuote(clmnName)}');";
            else
                query = $"SELECT COUNT(*) FROM pragma_table_info('{UnQuote(tblName)}') WHERE name='{UnQuote(clmnName)}';";
            string resp = project.DbQ(query, log);

            if (resp == "0" || resp == string.Empty) return false;
            else return true;

        }

        public static void ClmnAdd(this IZennoPosterProjectModel project, string clmnName, string tblName,  bool log = false, string defaultValue = "TEXT DEFAULT ''")
        {

            var current = project.TblColumns(tblName, log: log);
            if (!current.Contains(clmnName))
            {
                clmnName = Quote(clmnName);
                project.DbQ($@"ALTER TABLE {Quote(tblName)} ADD COLUMN {clmnName} {defaultValue};", log: log);
            }
        }
        public static void ClmnAdd(this IZennoPosterProjectModel project, string[] columns, string tblName,  bool log = false, string defaultValue = "TEXT DEFAULT ''")
        {
            foreach (var column in columns)
                project.ClmnAdd(column, tblName, log:log);

        }
        public static void ClmnAdd(this IZennoPosterProjectModel project, Dictionary<string, string> tableStructure, string tblName,  bool log = false)
        {

            var current = project.TblColumns(tblName,log:log);
            foreach (var column in tableStructure)
            {
                var keyWd = column.Key.Trim();
                if (!current.Contains(keyWd))
                {
                    keyWd =Quote(keyWd);
                    project.DbQ($@"ALTER TABLE {Quote(tblName)} ADD COLUMN {keyWd} {column.Value};", log: log);
                }
            }
        }
        public static List<string> ClmnList(this IZennoPosterProjectModel project, string tableName, bool log = false)
        {
            string dbMode = project.Var("DBmode");
            string Q = (dbMode == "PostgreSQL") ?
                $@"SELECT column_name FROM information_schema.columns WHERE table_schema = '{_schemaName}' AND table_name = '{tableName}'" :
                $@"SELECT name FROM pragma_table_info('{tableName}')";
            return project.DbQ(Q, log: log).Split('\n').ToList();
        }
        public static void ClmnDrop(this IZennoPosterProjectModel project, string clmnName, string tblName,  bool log = false)
        {
            var current = project.TblColumns(tblName, log: log);
            bool _pstgr = project.Var("DBmode") == "PostgreSQL";

            if (current.Contains(clmnName))
            {
                clmnName = Quote(clmnName);
                string cascade = (_pstgr) ? " CASCADE" : null;
                project.DbQ($@"ALTER TABLE {Quote(tblName)} DROP COLUMN {clmnName}{cascade};", log: log);
            }
        }
        public static void ClmnDrop(this IZennoPosterProjectModel project, Dictionary<string, string> tableStructure, string tblName,  bool log = false)
        {
            var current = project.TblColumns(tblName, log: log);
            foreach (var column in tableStructure)
            {
                if (!current.Contains(column.Key))
                {
                    string clmnName = Quote(column.Key);
                    string cascade = project.Var("DBmode") == "PostgreSQL" ? " CASCADE" : null;
                    project.DbQ($@"ALTER TABLE {Quote(tblName)} DROP COLUMN {clmnName}{cascade};", log: log);
                }
            }
        }
 
        public static void ClmnPrune(this IZennoPosterProjectModel project, Dictionary<string, string> tableStructure, string tblName,  bool log = false)
        {
            var current = project.TblColumns(tblName, log: log);
            foreach (var column in current)
            {
                if (!tableStructure.ContainsKey(column))
                {
                    project.ClmnDrop(column,tblName, log: log);
                }
            }
        }

        //Range
        public static void AddRange(this IZennoPosterProjectModel project, string tblName, int range = 0, bool log = false)
        {
            tblName = Quote(tblName);
            if (range == 0)
                try
                {
                    range = int.Parse(project.Variables["rangeEnd"].Value);
                }
                catch
                {
                    project.SendWarningToLog("var  rangeEnd is empty or 0, used default \"10\"", true);                  
                    range = 10;
                }

            int current = int.Parse(project.DbQ($@"SELECT COALESCE(MAX({Quote("id")}), 0) FROM {tblName};"));
            
            for (int currentAcc0 = current + 1; currentAcc0 <= range; currentAcc0++)
            {
                project.DbQ($@"INSERT INTO {tblName} ({Quote("id")}) VALUES ({currentAcc0}) ON CONFLICT DO NOTHING;", log: log);
            }

        }

        private static int TableCopy(this IZennoPosterProjectModel project, string sourceTable, string destinationTable, string sqLitePath = null, string pgHost = null, string pgPort = null, string pgDbName = null, string pgUser = null, string pgPass = null, bool throwOnEx = false)
        {
            if (string.IsNullOrEmpty(sourceTable)) throw new ArgumentNullException(nameof(sourceTable));
            if (string.IsNullOrEmpty(destinationTable)) throw new ArgumentNullException(nameof(destinationTable));
            if (string.IsNullOrEmpty(sqLitePath)) sqLitePath = project.Var("DBsqltPath");
            if (string.IsNullOrEmpty(pgHost)) pgHost = "localhost";
            if (string.IsNullOrEmpty(pgPort)) pgPort = "5432";
            if (string.IsNullOrEmpty(pgDbName)) pgDbName = "postgres";
            if (string.IsNullOrEmpty(pgUser)) pgUser = "postgres";
            if (string.IsNullOrEmpty(pgPass)) pgPass = project.Var("DBpstgrPass");
            string dbMode = project.Var("DBmode");

            using (var db = dbMode == "PostgreSQL"
                       ? new dSql($"Host={pgHost};Port={pgPort};Database={pgDbName};Username={pgUser};Password={pgPass};Pooling=true;Connection Idle Lifetime=10;")
                       : new dSql(sqLitePath, null))
            {
                try
                {
                    return db.CopyTableAsync(sourceTable, destinationTable).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    project.SendWarningToLog(ex.Message, true);
                    if (throwOnEx) throw;
                    return 0;
                }
            }
        }
        public static void MigrateAllTables(this IZennoPosterProjectModel project)
        {
            string dbMode = project.Var("DBmode");
            if (dbMode != "PostgreSQL" && dbMode != "SQLite") throw new ArgumentException("DBmode must be 'PostgreSQL' or 'SQLite'");

            string direction = dbMode == "PostgreSQL" ? "toSQLite" : "toPostgreSQL";

            string sqLitePath = project.Var("DBsqltPath");
            string pgHost = "localhost";
            string pgPort = "5432";
            string pgDbName = "postgres";
            string pgUser = "postgres";
            string pgPass = project.Var("DBpstgrPass");

            string pgConnection = $"Host={pgHost};Port={pgPort};Database={pgDbName};Username={pgUser};Password={pgPass};Pooling=true;Connection Idle Lifetime=10;";

            project.SendInfoToLog($"Migrating all tables from {dbMode} to {(direction == "toSQLite" ? "SQLite" : "PostgreSQL")}", true);

            using (var sourceDb = dbMode == "PostgreSQL" ? new dSql(pgConnection) : new dSql(sqLitePath, null))
            using (var destinationDb = dbMode == "PostgreSQL" ? new dSql(sqLitePath, null) : new dSql(pgConnection))
            {
                try
                {
                    int rowsMigrated = dSql.MigrateAllTablesAsync(sourceDb, destinationDb).GetAwaiter().GetResult();
                    project.SendInfoToLog($"Successfully migrated {rowsMigrated} rows", true);
                }
                catch (Exception ex)
                {
                    project.SendWarningToLog($"Error during migration: {ex.Message}", true);
                }
            }
        }

    }

    public static class DbCore
    {
        public static string DbQ(this IZennoPosterProjectModel project, string query, bool log = false, string sqLitePath = null, string pgHost = null, string pgPort = null, string pgDbName = null, string pgUser = null, string pgPass = null, bool throwOnEx = false)
        {
            string dbMode = project.Var("DBmode");

            if (string.IsNullOrEmpty(sqLitePath)) sqLitePath = project.Var("DBsqltPath");
            if (string.IsNullOrEmpty(pgHost)) pgHost = "localhost";
            if (string.IsNullOrEmpty(pgPort)) pgPort = "5432";
            if (string.IsNullOrEmpty(pgDbName)) pgDbName = "postgres";
            if (string.IsNullOrEmpty(pgUser)) pgUser = "postgres";
            if (string.IsNullOrEmpty(pgPass)) pgPass = project.Var("DBpstgrPass");

            //_logger.Send(query);
            string result = string.Empty;
            try
            {
                using (var db = dbMode == "PostgreSQL"
                    ? new dSql($"Host={pgHost};Port={pgPort};Database={pgDbName};Username={pgUser};Password={pgPass};Pooling=true;Connection Idle Lifetime=10;")
                    : new dSql(sqLitePath, null))
                    
                {

                    if (Regex.IsMatch(query, @"^\s*(DROP|DELETE)\b", RegexOptions.IgnoreCase) && !query.Contains("WHERE"))
                        throw new InvalidOperationException("Unsafe query detected: DROP/DELETE without WHERE");


                    if (Regex.IsMatch(query.TrimStart(), @"^\s*SELECT\b", RegexOptions.IgnoreCase))
                        result = db.DbReadAsync(query).GetAwaiter().GetResult();
                    else
                        result = db.DbWriteAsync(query).GetAwaiter().GetResult().ToString();
                }
            }
            catch (Exception ex)
            {
                project.SendWarningToLog(ex.Message + $"\n [{query}]");
                if (throwOnEx) throw ex.Throw();
                return string.Empty;
            }
 
            //var _logger = new Logger(project, log: log, classEmoji: dbMode == "PostgreSQL" ? "🐘" : "SQLite");
            string toLog = (query.Contains("SELECT")) ? $"[{query}]\n[{result}]" : $"[{query}] - [{result}]";
            new Logger(project, log: log, classEmoji: dbMode == "PostgreSQL" ? "🐘" : "SQLite").Send(toLog);
            return result;

        }
    }
}
