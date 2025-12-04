using Dapper;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    #region DbHelpers - Вспомогательные методы
    internal static class DbHelpers
    {
        internal static string SchemaName = "public";
        internal const char RawSeparator = '·';
        internal const char ColumnSeparator = '¦';
        
        internal static string UnQuote(string name)
        {
            return name.Replace("\"","");
        }
        
        internal static string Quote(string name)
        {
            return $"\"{name.Replace("\"", "\"\"")}\"";
        }
        
        internal static string QuoteColumns(this string updateString)
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
        
        internal static string QuoteSelectColumns(this string columnString)
        {
            return string.Join(", ", 
                columnString.Split(',')
                    .Select(col => $"\"{col.Trim()}\""));
        }
        
        internal static readonly Regex ValidNamePattern = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);
        
        internal static string ValidateName(string name, string paramName)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"{paramName} cannot be null or empty");

            if (!ValidNamePattern.IsMatch(name))
                throw new ArgumentException($"Invalid {paramName}: {name}. Only alphanumeric characters and underscores are allowed.");

            return name;
        }
        
        internal static bool IsValidRange(string range)
        {
            if (string.IsNullOrEmpty(range)) return false;
            return Regex.IsMatch(range, @"^[\d\s,\-]+$");
        }

        internal static string TableName(this IZennoPosterProjectModel project, string tableName)
        {
            if (string.IsNullOrEmpty(tableName)) 
                return project.ProjectTable();
            return tableName;
        }
    }
    #endregion

    #region DbGet - Методы получения данных
    public static class Get
    {
        public static string DbGet(this IZennoPosterProjectModel project, string toGet, string tableName = null, bool log = false, bool thrw = false, string key = "id", string acc = null, string where = "")
        {
            return project.SqlGet(toGet, tableName, log, thrw, key, acc, where);
        }
        
        public static Dictionary<string, string> DbGetColumns(this IZennoPosterProjectModel project, string toGet, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            return  project.SqlGetDicFromLine(toGet, tableName, log, thrw, key, id, where);
        }
        
        public static string[] DbGetLine(this IZennoPosterProjectModel project, string toGet, string tableName = null,  bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            return project.SqlGetArrFromLine(toGet, tableName, log, thrw, key, id, where);
        }
        
        public static List<string> DbGetLines(this IZennoPosterProjectModel project, string toGet, string tableName = null,  bool log = false, bool thrw = false, string key = "id", object id = null, string where = "", string toList = null)
        {
            var list =  project.SqlGetListFromLines(toGet, tableName, log, thrw, key, id, where);
            if (!string.IsNullOrEmpty(toList)) project.ListSync(toList,list);
            return list;
        }
        
        public static Dictionary<string, string> DbToVars(this IZennoPosterProjectModel project, string toGet, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            var data = project.SqlGetDicFromLine(toGet, tableName, log, thrw, key, id, where);
            project.VarsFromDict(data);
            return data;
        }
        
        public static string DbGetRandom(this IZennoPosterProjectModel project, string toGet, string tableName = null, bool log = false, bool acc = false, bool thrw = false, int range = 0, bool single = true, bool invert = false)
        {
            if (range == 0)
            {
                var rng = project.Range();
                range = int.Parse(rng[rng.Count-1]);
            }
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

            return project.DbQ(query, log: log, thrw: thrw);
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

            var resp = project.SqlGet(chainType, "_wallets");
            string decoded = !string.IsNullOrEmpty(project.Var("cfgPin")) ? SAFU.Decode(project, resp) : resp;
            return decoded;
        }
    }
    #endregion

    #region DbUpdate - Методы обновления данных
    public static class DbUpdate
    {
        public static void JsonToDb(this IZennoPosterProjectModel project, string json, string tableName = null, bool log = false, bool thrw = false)
        {
            tableName =  project.TableName(tableName);
            
            var dataDic = json.JsonToDic(true);
            project.DicToDb(dataDic,tableName,log,thrw);
        }
        
        public static void DicToDb(this IZennoPosterProjectModel project, Dictionary<string,string> dataDic, string tableName = null, bool log = false, bool thrw = false)
        {
            if (string.IsNullOrWhiteSpace(tableName)) tableName = project.Var("projectTable");
            
            if (dataDic.ContainsKey("id"))
            {
                dataDic["_id"] = dataDic["id"];
                dataDic.Remove("id");
            }
            
            var columns = new List<string>();
            var updString = new StringBuilder();
            
            foreach(var p in dataDic)
            {
                columns.Add(p.Key);
            }
            project.ClmnAdd(project.TblForProject(columns), tableName);
            
            foreach(var p in dataDic)
            {
                updString.Append($"{p.Key} = '{p.Value.Replace("'","")}',");
            }
            project.DbUpd(updString.ToString().Trim(','), tableName, log, thrw);
        }
    
        public static void DbUpd(this IZennoPosterProjectModel project, string toUpd, string tableName = null, bool log = false, bool thrw = false, string key = "id", object acc = null, string where = "")
        {
            try { project.Var("lastQuery", toUpd); } catch (Exception ex){ project.SendWarningToLog(ex.Message, true); }
            project.SqlUpd(toUpd, tableName, log, thrw, key, acc, where);
        }

        public static void DbDone(this IZennoPosterProjectModel project, string task = "daily", int cooldownMin = 0, string tableName = null, bool log = false, bool thrw = false, string key = "id", object acc = null, string where = "")
        {
            var cd = (cooldownMin == 0) ? Time.Cd() : Time.Cd(cooldownMin);
            project.DbUpd($"{task} = '{cd}'", tableName, log, thrw);
        }

        public static void DbSettings(this IZennoPosterProjectModel project, bool set = true, bool log = false)
        {
            var dbConfig = new Dictionary<string, string>();
            var resp = project.DbQ($"SELECT {DbHelpers.Quote("id")}, {DbHelpers.Quote("value")} FROM {DbHelpers.Quote("_settings")}", log);
            foreach (string varData in resp.Split(DbHelpers.RawSeparator))
            {
                if (string.IsNullOrEmpty(varData)) continue;

                var parts = varData.Split(DbHelpers.ColumnSeparator);
                if (parts.Length >= 2)
                {
                    string varName = parts[0];
                    string varValue = string.Join($"{DbHelpers.ColumnSeparator}", parts.Skip(1)).Trim(); 

                    dbConfig.Add(varName, varValue);
                    if (set)
                    {
                        try { project.Var(varName, varValue); }
                        catch (Exception e) { e.Throw(project, throwEx: false); }
                    }
                }
            }
        }
    }
    #endregion

    #region DbLine - Операции со строками таблицы
    public static class DbLine
    {
        public static void DbClearLine(this IZennoPosterProjectModel project, int id, string tableName = null, bool log = false, bool thrw = false)
        {
            tableName = project.TableName(tableName);
            var quotedTable = DbHelpers.Quote(tableName);
            
            var columns = project.TblColumns(tableName, log);
            var columnsToClean = columns.Where(col => col.ToLower() != "id").ToList();
            
            if (columnsToClean.Count == 0)
            {
                if (log) project.SendInfoToLog($"No columns to clear in table {tableName} (only 'id' column exists)", true);
                return;
            }
            
            var setClause = string.Join(", ", columnsToClean.Select(col => $"{DbHelpers.Quote(col)} = ''"));
            var updateQuery = $"UPDATE {quotedTable} SET {setClause} WHERE {DbHelpers.Quote("id")} = {id}";
            
            project.DbQ(updateQuery, log, thrw: thrw);
            
            if (log) project.SendInfoToLog($"Cleared {columnsToClean.Count} columns in row with id={id} in table {tableName}", true);
        }
        
        public static void DbSwapLines(this IZennoPosterProjectModel project, int id1, int id2, string tableName = null, bool log = false, bool thrw = false)
        {
            tableName = project.TableName(tableName);
            var quotedTable = DbHelpers.Quote(tableName);
            
            var columns = project.TblColumns(tableName, log);
            var columnsToSwap = columns.Where(col => col.ToLower() != "id").ToList();
            
            if (columnsToSwap.Count == 0)
            {
                if (log) project.SendInfoToLog($"No columns to swap in table {tableName} (only 'id' column exists)", true);
                return;
            }
            
            var columnsString = string.Join(", ", columnsToSwap);
            var data1 = project.DbGetColumns(columnsString, tableName, log, thrw, "id", id1);
            var data2 = project.DbGetColumns(columnsString, tableName, log, thrw, "id", id2);
            
            if (data1 == null || data1.Count == 0)
            {
                var msg = $"Row with id={id1} not found in table {tableName}";
                if (log) project.SendWarningToLog(msg, true);
                if (thrw) throw new Exception(msg);
                return;
            }
            
            if (data2 == null || data2.Count == 0)
            {
                var msg = $"Row with id={id2} not found in table {tableName}";
                if (log) project.SendWarningToLog(msg, true);
                if (thrw) throw new Exception(msg);
                return;
            }
            
            var setClause1 = string.Join(", ", data2.Select(kvp => $"{DbHelpers.Quote(kvp.Key)} = '{kvp.Value.Replace("'", "''")}'"));
            var setClause2 = string.Join(", ", data1.Select(kvp => $"{DbHelpers.Quote(kvp.Key)} = '{kvp.Value.Replace("'", "''")}'"));
            
            var updateQuery1 = $"UPDATE {quotedTable} SET {setClause1} WHERE {DbHelpers.Quote("id")} = {id1}";
            var updateQuery2 = $"UPDATE {quotedTable} SET {setClause2} WHERE {DbHelpers.Quote("id")} = {id2}";
            
            project.DbQ(updateQuery1, log, thrw: thrw);
            project.DbQ(updateQuery2, log, thrw: thrw);
            
            if (log) project.SendInfoToLog($"Swapped data between rows id={id1} and id={id2} in table {tableName}", true);
        }
    }
    #endregion

    #region DbSql - Низкоуровневые SQL операции
    public static class DbSql
    {
        public static string SqlGet(this IZennoPosterProjectModel project, string toGet, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            if (string.IsNullOrWhiteSpace(toGet))
                throw new ArgumentException("Column names cannot be null or empty", nameof(toGet));

            toGet = DbHelpers.QuoteSelectColumns(toGet.Trim().TrimEnd(','));
            if (string.IsNullOrEmpty(tableName)) 
                tableName = project.Variables["projectTable"].Value;
            
            if (id is null) id = project.Variables["acc0"].Value;

            string query;
            if (string.IsNullOrEmpty(where))
            {
                if (string.IsNullOrEmpty(id.ToString())) 
                    throw new ArgumentException("variable \"acc0\" is null or empty", nameof(id));
                query = $"SELECT {toGet} from {DbHelpers.Quote(tableName)} WHERE {DbHelpers.Quote(key)} = {id}";
            }
            else
            {
                query = $@"SELECT {toGet} from {DbHelpers.Quote(tableName)} WHERE {where};";
            }

            return project.DbQ(query, log: log, thrw: thrw);
        }
        
        public static Dictionary<string, string> SqlGetDicFromLine(this IZennoPosterProjectModel project, string toGet, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "", bool set = false)
        {
            string result = project.SqlGet(toGet, tableName, log, thrw, key, id, where);
    
            if (string.IsNullOrWhiteSpace(result))
                return new Dictionary<string, string>();
           
            var columns = toGet.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim().Trim('`', '"', '[', ']'))
                .ToList();
            var values = result.Split(DbHelpers.ColumnSeparator);
            var dictionary = new Dictionary<string, string>();
    
            for (int i = 0; i < columns.Count && i < values.Length; i++)
            {
                dictionary[columns[i]] = values[i];
            }
            if (set) project.VarsFromDict(dictionary);
            return dictionary;
        }
        
        public static string[] SqlGetArrFromLine(this IZennoPosterProjectModel project, string toGet, string tableName = null,  bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            return project.SqlGet(toGet, tableName, log, thrw, key, id, where).Split(DbHelpers.ColumnSeparator);
        }
        
        public static List<string> SqlGetListFromLines(this IZennoPosterProjectModel project, string toGet, string tableName = null,  bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            return project.SqlGet(toGet, tableName, log, thrw, key, id, where).Split(DbHelpers.RawSeparator).ToList();
        }
        
        public static string SqlUpd(this IZennoPosterProjectModel project, string toUpd, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {          
            var parameters = new DynamicParameters();
            if (string.IsNullOrEmpty(tableName)) tableName = project.Var("projectTable");
            if (string.IsNullOrEmpty(tableName)) throw new Exception("TableName is null");
            
            toUpd = DbHelpers.QuoteColumns(toUpd);
            tableName = DbHelpers.Quote(tableName);
            
            if (id is null)
                id = project.Variables["acc0"].Value;
            
            string query;
            if (string.IsNullOrEmpty(where))
            {
                if (string.IsNullOrEmpty(id.ToString()))
                    throw new ArgumentException("variable \"acc0\" is null or empty", nameof(id));
                query = $"UPDATE {tableName} SET {toUpd} WHERE {DbHelpers.Quote(key)} = {id}";
            }
            else
            {
                query = $"UPDATE {tableName} SET {toUpd} WHERE {where}";
            }
            return project.DbQ(query, log:log, thrw: thrw);
        }
    }
    #endregion

    #region DbTable - Операции с таблицами
    public static class DbTable
    {
        public static void TblAdd(this IZennoPosterProjectModel project,  Dictionary<string, string> tableStructure, string tblName, bool log = false)
        {
            if (project.TblExist(tblName, log:log)) return;

            tblName = DbHelpers.Quote(tblName);

            bool _pstgr = project.Var("DBmode") == "PostgreSQL";

            string query;
            if (_pstgr)
                query = ($@" CREATE TABLE {tblName} ( {string.Join(", ", tableStructure.Select(kvp => $"\"{kvp.Key}\" {kvp.Value.Replace("AUTOINCREMENT", "SERIAL")}"))} );");
            else
                query = ($"CREATE TABLE {tblName} (" + string.Join(", ", tableStructure.Select(kvp => $"{DbHelpers.Quote(kvp.Key)} {kvp.Value}")) + ");");
            project.DbQ(query, log: log);
        }
        
        public static bool TblExist(this IZennoPosterProjectModel project, string tblName, bool log = false)
        {
            tblName = DbHelpers.UnQuote(tblName);
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

        public static List<string> TblList(this IZennoPosterProjectModel project, bool log = false)
        {
            string query = project.Var("DBmode") == "PostgreSQL"
                ? @"SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE' ORDER BY table_name;"
                : @"SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name;";
            
            var result = project.DbQ(query, log: log)
                .Split(DbHelpers.RawSeparator)
                .Select(s => s.Trim())
                .ToList();
            return result;
        }
        
        public static List<string> TblColumns(this IZennoPosterProjectModel project, string tblName, bool log = false)
        {
            var result = new List<string>();
            string query = project.Var("DBmode") == "PostgreSQL"
                ? $@"SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '{DbHelpers.UnQuote(tblName)}';"
                : $"SELECT name FROM pragma_table_info('{DbHelpers.UnQuote(tblName)}');";

            result = project.DbQ(query, log: log)
                .Split(DbHelpers.RawSeparator)
                .Select(s => s.Trim())
                .ToList();
            return result;
        }
        
        public static Dictionary<string, string> TblForProject(this IZennoPosterProjectModel project, string[] projectColumns , string defaultType = "TEXT DEFAULT ''")
        {
            var projectColumnsList = projectColumns.ToList();
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
        
        public static void PrepareProjectTable(this IZennoPosterProjectModel project, string[] projectColumns, string tblName = null, bool log = false, bool prune = false, bool rearrange = false)
        {
            var projectColumnsList = projectColumns.ToList();
            project.PrepareProjectTable(projectColumnsList, tblName, log, prune, rearrange);
        }

        public static void PrepareProjectTable(this IZennoPosterProjectModel project, List<string> projectColumns = null, string tblName = null, bool log = false, bool prune = false, bool rearrange = false)
        {
            var tableStructure = project.TblForProject(projectColumns);
            if (string.IsNullOrEmpty(tblName)) tblName = project.Var("projectTable");
            project.TblAdd(tableStructure, tblName, log: log);
            project.ClmnAdd(tableStructure, tblName, log: log);
            project.AddRange(tblName,log:log);
            if (prune) project.ClmnPrune(tableStructure,tblName, log: log);
            if (rearrange) project.ClmnRearrange(tableStructure,tblName, log: log);
        }
        
        private static int TableCopy(this IZennoPosterProjectModel project, string sourceTable, string destinationTable, string sqLitePath = null, string pgHost = null, string pgPort = null, string pgDbName = null, string pgUser = null, string pgPass = null, bool thrw = false)
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
                    if (thrw) throw;
                    return 0;
                }
            }
        }
    }
    #endregion

    #region DbColumn - Операции со столбцами
    public static class DbColumn
    {
        public static bool ClmnExist(this IZennoPosterProjectModel project, string clmnName, string tblName, bool log = false)
        {
            bool _pstgr = project.Var("DBmode") == "PostgreSQL";
            string query;

            if (_pstgr)
                query = $@"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = '{DbHelpers.SchemaName}' AND table_name = '{DbHelpers.UnQuote(tblName)}' AND lower(column_name) = lower('{DbHelpers.UnQuote(clmnName)}');";
            else
                query = $"SELECT COUNT(*) FROM pragma_table_info('{DbHelpers.UnQuote(tblName)}') WHERE name='{DbHelpers.UnQuote(clmnName)}';";
            string resp = project.DbQ(query, log);

            if (resp == "0" || resp == string.Empty) return false;
            else return true;
        }
        
        public static void ClmnAdd(this IZennoPosterProjectModel project, string clmnName, string tblName = null,  bool log = false, string defaultValue = "TEXT DEFAULT ''")
        {
            tblName =  project.TableName(tblName);
            var current = project.TblColumns(tblName, log: log);
            if (!current.Contains(clmnName))
            {
                clmnName = DbHelpers.Quote(clmnName);
                project.DbQ($@"ALTER TABLE {DbHelpers.Quote(tblName)} ADD COLUMN {clmnName} {defaultValue};", log: log);
            }
        }
        
        public static void ClmnAdd(this IZennoPosterProjectModel project, string[] columns, string tblName,  bool log = false, string defaultValue = "TEXT DEFAULT ''")
        {
            foreach (var column in columns)
                project.ClmnAdd(column, tblName, log:log);
        }
        
        public static void ClmnAdd(this IZennoPosterProjectModel project, Dictionary<string, string> tableStructure, string tblName = null,  bool log = false)
        {
            tblName =  project.TableName(tblName);
            var current = project.TblColumns(tblName,log:log);
            foreach (var column in tableStructure)
            {
                var keyWd = column.Key.Trim();
                if (!current.Contains(keyWd))
                {
                    keyWd = DbHelpers.Quote(keyWd);
                    project.DbQ($@"ALTER TABLE {DbHelpers.Quote(tblName)} ADD COLUMN {keyWd} {column.Value};", log: log);
                }
            }
        }
        
        public static List<string> ClmnList(this IZennoPosterProjectModel project, string tableName = null, bool log = false)
        {
            tableName =  project.TableName(tableName);
            string dbMode = project.Var("DBmode");
            string Q = (dbMode == "PostgreSQL") ?
                $@"SELECT column_name FROM information_schema.columns WHERE table_schema = '{DbHelpers.SchemaName}' AND table_name = '{tableName}'" :
                $@"SELECT name FROM pragma_table_info('{tableName}')";
            return project.DbQ(Q, log: log).Split(DbHelpers.RawSeparator).ToList();
        }
        
        public static void ClmnDrop(this IZennoPosterProjectModel project, string clmnName, string tblName = null,  bool log = false)
        {
            tblName =  project.TableName(tblName);

            var current = project.TblColumns(tblName, log: log);
            bool _pstgr = project.Var("DBmode") == "PostgreSQL";

            if (current.Contains(clmnName))
            {
                clmnName = DbHelpers.Quote(clmnName);
                string cascade = (_pstgr) ? " CASCADE" : null;
                project.DbQ($@"ALTER TABLE {DbHelpers.Quote(tblName)} DROP COLUMN {clmnName}{cascade};", log: log);
            }
        }
        
        public static void ClmnDrop(this IZennoPosterProjectModel project, Dictionary<string, string> tableStructure, string tblName = null,  bool log = false)
        {
            tblName =  project.TableName(tblName);
            var current = project.TblColumns(tblName, log: log);
            foreach (var column in tableStructure)
            {
                if (!current.Contains(column.Key))
                {
                    string clmnName = DbHelpers.Quote(column.Key);
                    string cascade = project.Var("DBmode") == "PostgreSQL" ? " CASCADE" : null;
                    project.DbQ($@"ALTER TABLE {DbHelpers.Quote(tblName)} DROP COLUMN {clmnName}{cascade};", log: log);
                }
            }
        }
        
        public static void ClmnPrune(this IZennoPosterProjectModel project, string tblName = null, bool log = false)
        {
            tblName =  project.TableName(tblName);
            var current = project.TblColumns(tblName, log: log);
    
            foreach (var column in current)
            {
                if (column.ToLower() == "id") continue;
                string quotedColumn = DbHelpers.Quote(column);
                string quotedTable = DbHelpers.Quote(tblName);
                
                string countQuery = $"SELECT COUNT(*) FROM {quotedTable} WHERE {quotedColumn} != '' AND {quotedColumn} IS NOT NULL";
                string result = project.DbQ(countQuery, log: log);
        
                int count = int.Parse(result);
        
                if (count == 0)
                {
                    project.ClmnDrop(column, tblName, log: log);
                }
            }
        }
        
        public static void ClmnPrune(this IZennoPosterProjectModel project, Dictionary<string, string> tableStructure, string tblName = null,  bool log = false)
        {
            tblName =  project.TableName(tblName);
            var current = project.TblColumns(tblName, log: log);
            foreach (var column in current)
            {
                if (!tableStructure.ContainsKey(column))
                {
                    project.ClmnDrop(column,tblName, log: log);
                }
            }
        }
        
        public static void ClmnRearrange(this IZennoPosterProjectModel project, Dictionary<string, string> tableStructure, string tblName = null, bool log = false)
        {
            tblName =  project.TableName(tblName);
            DbHelpers.ValidateName(tblName, "table name");
            
            bool _pstgr = project.Var("DBmode") == "PostgreSQL";
            string quotedTable = DbHelpers.Quote(tblName);
            string tempTable = DbHelpers.Quote($"{tblName}_temp_{DateTime.Now.Ticks}");
            
            try
            {
                var currentColumns = project.TblColumns(tblName, log: log);
                var idType = GetIdType(project, tblName, _pstgr, log);
                var newTableStructure = BuildNewStructure(project, tableStructure, currentColumns, idType, tblName, _pstgr, log);
                
                CreateTempTable(project, tempTable, newTableStructure, _pstgr, log);
                CopyDataToTemp(project, quotedTable, tempTable, newTableStructure, log);
                DropOldTable(project, quotedTable, log);
                RenameTempTable(project, tempTable, quotedTable, tblName, _pstgr, log);
                
                if (log)
                {
                    project.SendInfoToLog($"Table {tblName} rearranged successfully. New column order: {string.Join(", ", newTableStructure.Keys)}", true);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    project.DbQ($"DROP TABLE IF EXISTS {tempTable}", log: false, unSafe: true);
                }
                catch { }
                
                throw new Exception($"Failed to rearrange table {tblName}: {ex.Message}", ex);
            }
        }
        
        public static void ClmnRearrange(this IZennoPosterProjectModel project, List<string> projectColumns, string tblName = null, bool log = false)
        {
            tblName =  project.TableName(tblName);
            var tableStructure = project.TblForProject(projectColumns);
            DbHelpers.ValidateName(tblName, "table name");
            
            bool _pstgr = project.Var("DBmode") == "PostgreSQL";
            string quotedTable = DbHelpers.Quote(tblName);
            string tempTable = DbHelpers.Quote($"{tblName}_temp_{DateTime.Now.Ticks}");
            
            try
            {
                var currentColumns = project.TblColumns(tblName, log: log);
                var idType = GetIdType(project, tblName, _pstgr, log);
                var newTableStructure = BuildNewStructure(project, tableStructure, currentColumns, idType, tblName, _pstgr, log);
                
                CreateTempTable(project, tempTable, newTableStructure, _pstgr, log);
                CopyDataToTemp(project, quotedTable, tempTable, newTableStructure, log);
                DropOldTable(project, quotedTable, log);
                RenameTempTable(project, tempTable, quotedTable, tblName, _pstgr, log);
                
                if (log)
                {
                    project.SendInfoToLog($"Table {tblName} rearranged successfully. New column order: {string.Join(", ", newTableStructure.Keys)}", true);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    project.DbQ($"DROP TABLE IF EXISTS {tempTable}", log: false, unSafe: true);
                }
                catch { }
                
                throw new Exception($"Failed to rearrange table {tblName}: {ex.Message}", ex);
            }
        }

        #region _ClmnRearrange_private
        private static string GetIdType(IZennoPosterProjectModel project, string tblName, bool _pstgr, bool log)
        {
            string idType = "INTEGER PRIMARY KEY";
            
            if (_pstgr)
            {
                string getIdTypeQuery = $@"
                    SELECT data_type, is_identity 
                    FROM information_schema.columns 
                    WHERE table_schema = '{DbHelpers.SchemaName}' 
                    AND table_name = '{DbHelpers.UnQuote(tblName)}' 
                    AND column_name = 'id'";
                
                var idInfo = project.DbQ(getIdTypeQuery, log: log);
                if (!string.IsNullOrEmpty(idInfo))
                {
                    if (idInfo.Contains("character") || idInfo.Contains("text"))
                        idType = "TEXT PRIMARY KEY";
                    else if (idInfo.Contains("integer"))
                        idType = "SERIAL PRIMARY KEY";
                }
            }
            else
            {
                string getIdTypeQuery = $"SELECT type FROM pragma_table_info('{DbHelpers.UnQuote(tblName)}') WHERE name='id'";
                var sqliteIdType = project.DbQ(getIdTypeQuery, log: log);
                if (!string.IsNullOrEmpty(sqliteIdType) && sqliteIdType.ToUpper().Contains("TEXT"))
                    idType = "TEXT PRIMARY KEY";
            }
            
            return idType;
        }
        
        private static Dictionary<string, string> BuildNewStructure(IZennoPosterProjectModel project, Dictionary<string, string> tableStructure, List<string> currentColumns, string idType, string tblName, bool _pstgr, bool log)
        {
            var newTableStructure = new Dictionary<string, string>();
            newTableStructure.Add("id", idType);
            
            foreach (var col in tableStructure)
            {
                if (col.Key.ToLower() != "id" && currentColumns.Contains(col.Key))
                {
                    newTableStructure.Add(col.Key, col.Value);
                }
            }
            
            foreach (var col in currentColumns)
            {
                if (col.ToLower() != "id" && !newTableStructure.ContainsKey(col))
                {
                    string colType = GetColumnType(project, tblName, col, _pstgr, log);
                    newTableStructure.Add(col, colType);
                }
            }
            
            return newTableStructure;
        }
        
        private static string GetColumnType(IZennoPosterProjectModel project, string tblName, string col, bool _pstgr, bool log)
        {
            string colType = "TEXT DEFAULT ''";
            
            if (_pstgr)
            {
                string getTypeQuery = $@"
                    SELECT data_type, character_maximum_length, column_default
                    FROM information_schema.columns 
                    WHERE table_schema = '{DbHelpers.SchemaName}' 
                    AND table_name = '{DbHelpers.UnQuote(tblName)}' 
                    AND column_name = '{col}'";
                
                var typeInfo = project.DbQ(getTypeQuery, log: log);
                if (!string.IsNullOrEmpty(typeInfo))
                {
                    if (typeInfo.Contains("integer")) colType = "INTEGER";
                    else if (typeInfo.Contains("text") || typeInfo.Contains("character")) colType = "TEXT";
                    else if (typeInfo.Contains("timestamp")) colType = "TIMESTAMP";
                    else if (typeInfo.Contains("boolean")) colType = "BOOLEAN";
                    
                    if (typeInfo.Contains("''::")) colType += " DEFAULT ''";
                }
            }
            else
            {
                string getTypeQuery = $"SELECT type FROM pragma_table_info('{DbHelpers.UnQuote(tblName)}') WHERE name='{col}'";
                var sqliteType = project.DbQ(getTypeQuery, log: log);
                if (!string.IsNullOrEmpty(sqliteType))
                    colType = sqliteType;
            }
            
            return colType;
        }
        
        private static void CreateTempTable(IZennoPosterProjectModel project, string tempTable, Dictionary<string, string> newTableStructure, bool _pstgr, bool log)
        {
            string createTempTableQuery;
            if (_pstgr)
            {
                createTempTableQuery = $@"CREATE TABLE {tempTable} ( 
                    {string.Join(", ", newTableStructure.Select(kvp => $"{DbHelpers.Quote(kvp.Key)} {kvp.Value.Replace("AUTOINCREMENT", "SERIAL")}"))} )";
            }
            else
            {
                createTempTableQuery = $@"CREATE TABLE {tempTable} ( 
                    {string.Join(", ", newTableStructure.Select(kvp => $"{DbHelpers.Quote(kvp.Key)} {kvp.Value}"))} )";
            }
            
            project.DbQ(createTempTableQuery, log: log);
        }
        
        private static void CopyDataToTemp(IZennoPosterProjectModel project, string quotedTable, string tempTable, Dictionary<string, string> newTableStructure, bool log)
        {
            var columnsList = string.Join(", ", newTableStructure.Keys.Select(k => DbHelpers.Quote(k)));
            string copyDataQuery = $@"
                INSERT INTO {tempTable} ({columnsList})
                SELECT {columnsList}
                FROM {quotedTable}";
            
            project.DbQ(copyDataQuery, log: log);
        }
        
        private static void DropOldTable(IZennoPosterProjectModel project, string quotedTable, bool log)
        {
            string dropOldTableQuery = $"DROP TABLE {quotedTable}";
            project.DbQ(dropOldTableQuery, log: log, unSafe: true);
        }
        
        private static void RenameTempTable(IZennoPosterProjectModel project, string tempTable, string quotedTable, string tblName, bool _pstgr, bool log)
        {
            string renameTableQuery;
            if (_pstgr)
            {
                renameTableQuery = $"ALTER TABLE {tempTable} RENAME TO {quotedTable}";
            }
            else
            {
                renameTableQuery = $"ALTER TABLE {tempTable} RENAME TO {DbHelpers.UnQuote(tblName)}";
            }
            
            project.DbQ(renameTableQuery, log: log);
        }
        #endregion
    }
    #endregion

    #region DbRange - Операции с диапазонами
    public static class DbRange
    {
        public static void AddRange(this IZennoPosterProjectModel project, string tblName, int range = 0, bool log = false)
        {
            tblName = DbHelpers.Quote(tblName);
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

            int current = int.Parse(project.DbQ($@"SELECT COALESCE(MAX({DbHelpers.Quote("id")}), 0) FROM {tblName};"));
            
            for (int currentAcc0 = current + 1; currentAcc0 <= range; currentAcc0++)
            {
                project.DbQ($@"INSERT INTO {tblName} ({DbHelpers.Quote("id")}) VALUES ({currentAcc0}) ON CONFLICT DO NOTHING;", log: log);
            }
        }
    }
    #endregion

    #region DbMigration - Миграция данных
    public static class DbMigration
    {
        public static void MigrateTable(this IZennoPosterProjectModel project, string source, string dest)
        {
            DbHelpers.ValidateName(source, "source table");
            DbHelpers.ValidateName(dest, "destination table");
            project.SendInfoToLog($"{source} -> {dest}", true);
            project.TableCopy(source, dest);
            try { project.DbQ($"ALTER TABLE {DbHelpers.Quote(dest)} RENAME COLUMN {DbHelpers.Quote("acc0")} to {DbHelpers.Quote("id")}"); } catch { }
            try { project.DbQ($"ALTER TABLE {DbHelpers.Quote(dest)} RENAME COLUMN {DbHelpers.Quote("key")} to {DbHelpers.Quote("id")}"); } catch { }
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
        
        private static int TableCopy(this IZennoPosterProjectModel project, string sourceTable, string destinationTable, string sqLitePath = null, string pgHost = null, string pgPort = null, string pgDbName = null, string pgUser = null, string pgPass = null, bool thrw = false)
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
                    if (thrw) throw;
                    return 0;
                }
            }
        }
    }
    #endregion

    #region DbCore - Ядро работы с БД
    public static class DbCore
    {
        public static string DbQ(this IZennoPosterProjectModel project, string query, bool log = false, string sqLitePath = null, string pgHost = null, string pgPort = null, string pgDbName = null, string pgUser = null, string pgPass = null, bool thrw = false, bool unSafe = false)
        {
            if (string.IsNullOrEmpty(sqLitePath)) sqLitePath = project.Var("DBsqltPath");
            if (string.IsNullOrEmpty(pgHost)) pgHost = project.GVar("sqlPgHost");
            if (string.IsNullOrEmpty(pgPort)) pgPort = project.GVar("sqlPgPort");
            if (string.IsNullOrEmpty(pgDbName)) project.GVar("sqlPgName");
            if (string.IsNullOrEmpty(pgUser)) project.GVar("sqlPgUser");
            if (string.IsNullOrEmpty(pgPass)) pgPass = project.Var("DBpstgrPass");
            
            string dbMode = project.Var("DBmode");

            if (string.IsNullOrEmpty(sqLitePath)) sqLitePath = project.Var("DBsqltPath");
            if (string.IsNullOrEmpty(pgHost)) pgHost = "localhost";
            if (string.IsNullOrEmpty(pgPort)) pgPort = "5432";
            if (string.IsNullOrEmpty(pgDbName)) pgDbName = "postgres";
            if (string.IsNullOrEmpty(pgUser)) pgUser = "postgres";
            if (string.IsNullOrEmpty(pgPass)) pgPass = project.Var("DBpstgrPass");

            string result = string.Empty;
            try
            {
                using (var db = dbMode == "PostgreSQL"
                    ? new dSql($"Host={pgHost};Port={pgPort};Database={pgDbName};Username={pgUser};Password={pgPass};Pooling=true;Connection Idle Lifetime=10;")
                    : new dSql(sqLitePath, null))
                {
                    if (Regex.IsMatch(query.TrimStart(), @"^\s*SELECT\b", RegexOptions.IgnoreCase))
                        result = db.DbReadAsync(query, "¦","·").GetAwaiter().GetResult();
                    else
                        result = db.DbWriteAsync(query).GetAwaiter().GetResult().ToString();
                }
            }
            catch (Exception ex)
            {
                project.SendWarningToLog(ex.Message + $"\n [{query}]");
                if (thrw) throw ex.Throw();
                return string.Empty;
            }
 
            string toLog = (query.Contains("SELECT")) ? $"[{query}]\n[{result}]" : $"[{query}] - [{result}]";
            new Logger(project, log: log, classEmoji: dbMode == "PostgreSQL" ? "🐘" : "SQLite").Send(toLog);
            return result;
        }
    }
    #endregion
}