using Dapper;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using z3nCore;

namespace Core
{
    public class Db
    {
        #region PRIVATE FIELDS

        private readonly Variables _variables;
        private readonly Logger _logger;
        private readonly string _dbMode;
        private readonly string _dbPath;
        private readonly string _pgHost;
        private readonly string _pgPort;
        private readonly string _pgDbName;
        private readonly string _pgUser;
        private readonly string _pgPass;

        private static string _schemaName = "public";
        private const char _rawSeparator = '·';
        private const char _columnSeparator = '¦';

        #endregion

        #region CONSTRUCTOR

        public Db(Variables variables, Logger logger,
            string dbMode = "SQLite",
            string dbPath = null,
            string pgHost = "localhost",
            string pgPort = "5432",
            string pgDbName = "postgres",
            string pgUser = "postgres",
            string pgPass = null)
        {
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _dbMode = dbMode;
            _dbPath = dbPath;
            _pgHost = pgHost;
            _pgPort = pgPort;
            _pgDbName = pgDbName;
            _pgUser = pgUser;
            _pgPass = pgPass;

            // Сохраняем настройки в переменных для совместимости
            _variables.Set("DBmode", _dbMode);
            if (!string.IsNullOrEmpty(_dbPath))
                _variables.Set("DBsqltPath", _dbPath);
            if (!string.IsNullOrEmpty(_pgPass))
                _variables.Set("DBpstgrPass", _pgPass);
        }

        #endregion

        #region PRIVATE METHODS

        private static string UnQuote(string name)
        {
            return name.Replace("\"", "");
        }

        private static string Quote(string name)
        {
            return $"\"{name.Replace("\"", "\"\"")}\"";
        }

        private static string QuoteColumns(string updateString)
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

        private static string QuoteSelectColumns(string columnString)
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

        private string TableName(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return _variables.Get("projectTable");
            return tableName;
        }

        #endregion

        #region GET PUBLIC

        public string DbGet(string toGet, string tableName = null, bool log = false, bool thrw = false, string key = "id", string acc = null, string where = "")
        {
            return SqlGet(toGet, tableName, log, thrw, key, acc, where);
        }

        public Dictionary<string, string> DbGetColumns(string toGet, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            return SqlGetDicFromLine(toGet, tableName, log, thrw, key, id, where);
        }

        public string[] DbGetLine(string toGet, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            return SqlGetArrFromLine(toGet, tableName, log, thrw, key, id, where);
        }

        public List<string> DbGetLines(string toGet, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "", string toList = null)
        {
            var list = SqlGetListFromLines(toGet, tableName, log, thrw, key, id, where);
            return list;
        }

        public Dictionary<string, string> DbToVars(string toGet, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            var data = SqlGetDicFromLine(toGet, tableName, log, thrw, key, id, where);
            _variables.FromDict(data);
            return data;
        }

        #endregion

        #region JSON/DICT TO DB

        public void JsonToDb(string json, string tableName = null, bool log = false, bool thrw = false)
        {
            if (string.IsNullOrWhiteSpace(tableName)) tableName = _variables.Get("projectTable");

            // Используем метод из z3nCore.JsonExtensions если есть, или парсим вручную
            // Для простоты предполагаем, что json уже в виде Dictionary
            throw new NotImplementedException("JsonToDb requires JSON parsing implementation");
        }

        public void DicToDb(Dictionary<string, string> dataDic, string tableName = null, bool log = false, bool thrw = false)
        {
            if (string.IsNullOrWhiteSpace(tableName)) tableName = _variables.Get("projectTable");

            if (dataDic.ContainsKey("id"))
            {
                dataDic["_id"] = dataDic["id"];
                dataDic.Remove("id");
            }

            var columns = new List<string>();
            var updString = new StringBuilder();

            foreach (var p in dataDic)
            {
                columns.Add(p.Key);
            }
            ClmnAdd(TblForProject(columns), tableName);

            foreach (var p in dataDic)
            {
                updString.Append($"{p.Key} = '{p.Value.Replace("'", "")}',");
            }
            DbUpd(updString.ToString().Trim(','), tableName);
        }

        #endregion

        #region UPDATE

        public void DbUpd(string toUpd, string tableName = null, bool log = false, bool thrw = false, string key = "id", object acc = null, string where = "")
        {
            try { _variables.Set("lastQuery", toUpd); } catch (Exception ex) { _logger.Warn(ex.Message, show: true); }
            SqlUpd(toUpd, tableName, log, thrw, key, acc, where);
        }

        public void DbSettings(bool set = true, bool log = false)
        {
            var dbConfig = new Dictionary<string, string>();
            var resp = DbQ($"SELECT {Quote("id")}, {Quote("value")} FROM {Quote("_settings")}", log);
            foreach (string varData in resp.Split(_rawSeparator))
            {
                if (string.IsNullOrEmpty(varData)) continue;

                var parts = varData.Split(_columnSeparator);
                if (parts.Length >= 2)
                {
                    string varName = parts[0];
                    string varValue = string.Join($"{_columnSeparator}", parts.Skip(1)).Trim();

                    dbConfig.Add(varName, varValue);
                    if (set)
                    {
                        try { _variables.Set(varName, varValue); }
                        catch (Exception e) { _logger.Warn(e.Message, show: false); }
                    }
                }
            }
        }

        #endregion

        #region MIGRATE

        public void MigrateTable(string source, string dest)
        {
            ValidateName(source, "source table");
            ValidateName(dest, "destination table");
            _logger.Send($"{source} -> {dest}", show: true);
            TableCopy(source, dest);
            try { DbQ($"ALTER TABLE {Quote(dest)} RENAME COLUMN {Quote("acc0")} to {Quote("id")}"); } catch { }
            try { DbQ($"ALTER TABLE {Quote(dest)} RENAME COLUMN {Quote("key")} to {Quote("id")}"); } catch { }
        }

        public void MigrateAllTables()
        {
            string dbMode = _variables.Get("DBmode");
            if (dbMode != "PostgreSQL" && dbMode != "SQLite") throw new ArgumentException("DBmode must be 'PostgreSQL' or 'SQLite'");

            string direction = dbMode == "PostgreSQL" ? "toSQLite" : "toPostgreSQL";

            string sqLitePath = _variables.Get("DBsqltPath");
            string pgHost = "localhost";
            string pgPort = "5432";
            string pgDbName = "postgres";
            string pgUser = "postgres";
            string pgPass = _variables.Get("DBpstgrPass");

            string pgConnection = $"Host={pgHost};Port={pgPort};Database={pgDbName};Username={pgUser};Password={pgPass};Pooling=true;Connection Idle Lifetime=10;";

            _logger.Send($"Migrating all tables from {dbMode} to {(direction == "toSQLite" ? "SQLite" : "PostgreSQL")}", show: true);

            using (var sourceDb = dbMode == "PostgreSQL" ? new dSql(pgConnection) : new dSql(sqLitePath, null))
            using (var destinationDb = dbMode == "PostgreSQL" ? new dSql(sqLitePath, null) : new dSql(pgConnection))
            {
                try
                {
                    int rowsMigrated = dSql.MigrateAllTablesAsync(sourceDb, destinationDb).GetAwaiter().GetResult();
                    _logger.Send($"Successfully migrated {rowsMigrated} rows", show: true);
                }
                catch (Exception ex)
                {
                    _logger.Warn($"Error during migration: {ex.Message}", show: true);
                }
            }
        }

        #endregion

        #region RANDOM

        public string DbGetRandom(string toGet, string tableName = null, bool log = false, bool acc = false, bool thrw = false, int range = 0, bool single = true, bool invert = false)
        {
            if (range == 0)
            {
                var rng = _variables.Range();
                range = int.Parse(rng[rng.Count - 1]);
            }
            if (string.IsNullOrEmpty(tableName)) tableName = _variables.Get("projectTable");

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

            return DbQ(query, log: log, thrw: thrw);
        }

        public string DbKey(string chainType = "evm")
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

            var resp = SqlGet(chainType, "_wallets");
            // SAFU decoding would need to be implemented or referenced
            return resp;
        }

        #endregion

        #region GET SQL

        private string SqlGet(string toGet, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            if (string.IsNullOrWhiteSpace(toGet))
                throw new ArgumentException("Column names cannot be null or empty", nameof(toGet));

            toGet = QuoteSelectColumns(toGet.Trim().TrimEnd(','));
            if (string.IsNullOrEmpty(tableName))
                tableName = _variables.Get("projectTable");

            if (id is null) id = _variables.Get("acc0");

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

            return DbQ(query, log: log, thrw: thrw);
        }

        private Dictionary<string, string> SqlGetDicFromLine(string toGet, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "", bool set = false)
        {
            string result = SqlGet(toGet, tableName, log, thrw, key, id, where);

            if (string.IsNullOrWhiteSpace(result))
                return new Dictionary<string, string>();

            var columns = toGet.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim().Trim('`', '"', '[', ']'))
                .ToList();
            var values = result.Split(_columnSeparator);
            var dictionary = new Dictionary<string, string>();

            for (int i = 0; i < columns.Count && i < values.Length; i++)
            {
                dictionary[columns[i]] = values[i];
            }
            if (set) _variables.FromDict(dictionary);
            return dictionary;
        }

        private string[] SqlGetArrFromLine(string toGet, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            return SqlGet(toGet, tableName, log, thrw, key, id, where).Split(_columnSeparator);
        }

        private List<string> SqlGetListFromLines(string toGet, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            return SqlGet(toGet, tableName, log, thrw, key, id, where).Split(_rawSeparator).ToList();
        }

        private string SqlUpd(string toUpd, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            var parameters = new DynamicParameters();
            if (string.IsNullOrEmpty(tableName)) tableName = _variables.Get("projectTable");
            if (string.IsNullOrEmpty(tableName)) throw new Exception("TableName is null");

            toUpd = QuoteColumns(toUpd);
            tableName = Quote(tableName);

            if (id is null)
                id = _variables.Get("acc0");

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
            return DbQ(query, log: log, thrw: thrw);
        }

        #endregion

        #region TABLES

        public void TblAdd(Dictionary<string, string> tableStructure, string tblName, bool log = false)
        {
            if (TblExist(tblName, log: log)) return;

            tblName = Quote(tblName);

            bool _pstgr = _variables.Get("DBmode") == "PostgreSQL";

            string query;
            if (_pstgr)
                query = ($@" CREATE TABLE {tblName} ( {string.Join(", ", tableStructure.Select(kvp => $"\"{kvp.Key}\" {kvp.Value.Replace("AUTOINCREMENT", "SERIAL")}"))} );");
            else
                query = ($"CREATE TABLE {tblName} (" + string.Join(", ", tableStructure.Select(kvp => $"{Quote(kvp.Key)} {kvp.Value}")) + ");");
            DbQ(query, log: log);
        }

        public bool TblExist(string tblName, bool log = false)
        {
            tblName = UnQuote(tblName);
            bool _pstgr = _variables.Get("DBmode") == "PostgreSQL";
            string query;

            if (_pstgr)
                query = ($"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '{tblName}';");
            else
                query = ($"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tblName}';");

            string resp = DbQ(query, log);

            if (resp == "0" || resp == string.Empty) return false;
            else return true;
        }

        public List<string> TblList(bool log = false)
        {
            string query = _variables.Get("DBmode") == "PostgreSQL"
                ? @"SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE' ORDER BY table_name;"
                : @"SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name;";

            var result = DbQ(query, log: log)
                .Split(_rawSeparator)
                .Select(s => s.Trim())
                .ToList();
            return result;
        }

        public List<string> TblColumns(string tblName, bool log = false)
        {
            var result = new List<string>();
            string query = _variables.Get("DBmode") == "PostgreSQL"
                ? $@"SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '{UnQuote(tblName)}';"
                : $"SELECT name FROM pragma_table_info('{UnQuote(tblName)}');";

            result = DbQ(query, log: log)
                .Split(_rawSeparator)
                .Select(s => s.Trim())
                .ToList();
            return result;
        }

        public Dictionary<string, string> TblForProject(string[] projectColumns, string defaultType = "TEXT DEFAULT ''")
        {
            var projectColumnsList = projectColumns.ToList();
            return TblForProject(projectColumnsList, defaultType);
        }

        public Dictionary<string, string> TblForProject(List<string> projectColumns = null, string defaultType = "TEXT DEFAULT ''")
        {
            string cfgToDo = _variables.Get("cfgToDo");
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

        public void TblPrepareDefault(bool log = false)
        {
            var tableStructure = TblForProject();
            var tblName = _variables.Get("projectTable");

            TblAdd(tableStructure, tblName, log: log);
            ClmnAdd(tableStructure, tblName, log: log);
            AddRange(tblName, log: log);
        }

        public void PrepareProjectTable(string[] projectColumns, string tblName = null, bool log = false, bool prune = false, bool rearrange = false)
        {
            var projectColumnsList = projectColumns.ToList();
            PrepareProjectTable(projectColumnsList, tblName, log, prune, rearrange);
        }

        public void PrepareProjectTable(List<string> projectColumns = null, string tblName = null, bool log = false, bool prune = false, bool rearrange = false)
        {
            var tableStructure = TblForProject(projectColumns);
            if (string.IsNullOrEmpty(tblName)) tblName = _variables.Get("projectTable");
            TblAdd(tableStructure, tblName, log: log);
            ClmnAdd(tableStructure, tblName, log: log);
            AddRange(tblName, log: log);
            if (prune) ClmnPrune(tableStructure, tblName, log: log);
            if (rearrange) ClmnRearrange(tableStructure, tblName, log: log);
        }

        #endregion

        #region COLUMNS

        public bool ClmnExist(string clmnName, string tblName, bool log = false)
        {
            bool _pstgr = _variables.Get("DBmode") == "PostgreSQL";
            string query;

            if (_pstgr)
                query = $@"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = '{_schemaName}' AND table_name = '{UnQuote(tblName)}' AND lower(column_name) = lower('{UnQuote(clmnName)}');";
            else
                query = $"SELECT COUNT(*) FROM pragma_table_info('{UnQuote(tblName)}') WHERE name='{UnQuote(clmnName)}';";
            string resp = DbQ(query, log);

            if (resp == "0" || resp == string.Empty) return false;
            else return true;
        }

        public void ClmnAdd(string clmnName, string tblName = null, bool log = false, string defaultValue = "TEXT DEFAULT ''")
        {
            tblName = TableName(tblName);
            var current = TblColumns(tblName, log: log);
            if (!current.Contains(clmnName))
            {
                clmnName = Quote(clmnName);
                DbQ($@"ALTER TABLE {Quote(tblName)} ADD COLUMN {clmnName} {defaultValue};", log: log);
            }
        }

        public void ClmnAdd(string[] columns, string tblName, bool log = false, string defaultValue = "TEXT DEFAULT ''")
        {
            foreach (var column in columns)
                ClmnAdd(column, tblName, log: log);
        }

        public void ClmnAdd(Dictionary<string, string> tableStructure, string tblName, bool log = false)
        {
            var current = TblColumns(tblName, log: log);
            foreach (var column in tableStructure)
            {
                var keyWd = column.Key.Trim();
                if (!current.Contains(keyWd))
                {
                    keyWd = Quote(keyWd);
                    DbQ($@"ALTER TABLE {Quote(tblName)} ADD COLUMN {keyWd} {column.Value};", log: log);
                }
            }
        }

        public List<string> ClmnList(string tableName, bool log = false)
        {
            string dbMode = _variables.Get("DBmode");
            string Q = (dbMode == "PostgreSQL") ?
                $@"SELECT column_name FROM information_schema.columns WHERE table_schema = '{_schemaName}' AND table_name = '{tableName}'" :
                $@"SELECT name FROM pragma_table_info('{tableName}')";
            return DbQ(Q, log: log).Split(_rawSeparator).ToList();
        }

        public void ClmnDrop(string clmnName, string tblName, bool log = false)
        {
            var current = TblColumns(tblName, log: log);
            bool _pstgr = _variables.Get("DBmode") == "PostgreSQL";

            if (current.Contains(clmnName))
            {
                clmnName = Quote(clmnName);
                string cascade = (_pstgr) ? " CASCADE" : null;
                DbQ($@"ALTER TABLE {Quote(tblName)} DROP COLUMN {clmnName}{cascade};", log: log);
            }
        }

        public void ClmnDrop(Dictionary<string, string> tableStructure, string tblName, bool log = false)
        {
            var current = TblColumns(tblName, log: log);
            foreach (var column in tableStructure)
            {
                if (!current.Contains(column.Key))
                {
                    string clmnName = Quote(column.Key);
                    string cascade = _variables.Get("DBmode") == "PostgreSQL" ? " CASCADE" : null;
                    DbQ($@"ALTER TABLE {Quote(tblName)} DROP COLUMN {clmnName}{cascade};", log: log);
                }
            }
        }

        public void ClmnPrune(Dictionary<string, string> tableStructure, string tblName, bool log = false)
        {
            var current = TblColumns(tblName, log: log);
            foreach (var column in current)
            {
                if (!tableStructure.ContainsKey(column))
                {
                    ClmnDrop(column, tblName, log: log);
                }
            }
        }

        public void ClmnRearrange(Dictionary<string, string> tableStructure, string tblName, bool log = false)
        {
            ValidateName(tblName, "table name");

            bool _pstgr = _variables.Get("DBmode") == "PostgreSQL";
            string quotedTable = Quote(tblName);
            string tempTable = Quote($"{tblName}_temp_{DateTime.Now.Ticks}");

            try
            {
                var currentColumns = TblColumns(tblName, log: log);
                var idType = GetIdType(tblName, _pstgr, log);
                var newTableStructure = BuildNewStructure(tableStructure, currentColumns, idType, tblName, _pstgr, log);

                CreateTempTable(tempTable, newTableStructure, _pstgr, log);
                CopyDataToTemp(quotedTable, tempTable, newTableStructure, log);
                DropOldTable(quotedTable, log);
                RenameTempTable(tempTable, quotedTable, tblName, _pstgr, log);

                if (log)
                {
                    _logger.Send($"Table {tblName} rearranged successfully. New column order: {string.Join(", ", newTableStructure.Keys)}", show: true);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    DbQ($"DROP TABLE IF EXISTS {tempTable}", log: false, unSafe: true);
                }
                catch { }

                throw new Exception($"Failed to rearrange table {tblName}: {ex.Message}", ex);
            }
        }

        #region _ClmnRearrange_private

        private string GetIdType(string tblName, bool _pstgr, bool log)
        {
            string idType = "INTEGER PRIMARY KEY";

            if (_pstgr)
            {
                string getIdTypeQuery = $@"
                    SELECT data_type, is_identity
                    FROM information_schema.columns
                    WHERE table_schema = '{_schemaName}'
                    AND table_name = '{UnQuote(tblName)}'
                    AND column_name = 'id'";

                var idInfo = DbQ(getIdTypeQuery, log: log);
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
                string getIdTypeQuery = $"SELECT type FROM pragma_table_info('{UnQuote(tblName)}') WHERE name='id'";
                var sqliteIdType = DbQ(getIdTypeQuery, log: log);
                if (!string.IsNullOrEmpty(sqliteIdType) && sqliteIdType.ToUpper().Contains("TEXT"))
                    idType = "TEXT PRIMARY KEY";
            }

            return idType;
        }

        private Dictionary<string, string> BuildNewStructure(Dictionary<string, string> tableStructure, List<string> currentColumns, string idType, string tblName, bool _pstgr, bool log)
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
                    string colType = GetColumnType(tblName, col, _pstgr, log);
                    newTableStructure.Add(col, colType);
                }
            }

            return newTableStructure;
        }

        private string GetColumnType(string tblName, string col, bool _pstgr, bool log)
        {
            string colType = "TEXT DEFAULT ''";

            if (_pstgr)
            {
                string getTypeQuery = $@"
                    SELECT data_type, character_maximum_length, column_default
                    FROM information_schema.columns
                    WHERE table_schema = '{_schemaName}'
                    AND table_name = '{UnQuote(tblName)}'
                    AND column_name = '{col}'";

                var typeInfo = DbQ(getTypeQuery, log: log);
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
                string getTypeQuery = $"SELECT type FROM pragma_table_info('{UnQuote(tblName)}') WHERE name='{col}'";
                var sqliteType = DbQ(getTypeQuery, log: log);
                if (!string.IsNullOrEmpty(sqliteType))
                    colType = sqliteType;
            }

            return colType;
        }

        private void CreateTempTable(string tempTable, Dictionary<string, string> newTableStructure, bool _pstgr, bool log)
        {
            string createTempTableQuery;
            if (_pstgr)
            {
                createTempTableQuery = $@"CREATE TABLE {tempTable} (
                    {string.Join(", ", newTableStructure.Select(kvp => $"{Quote(kvp.Key)} {kvp.Value.Replace("AUTOINCREMENT", "SERIAL")}"))} )";
            }
            else
            {
                createTempTableQuery = $@"CREATE TABLE {tempTable} (
                    {string.Join(", ", newTableStructure.Select(kvp => $"{Quote(kvp.Key)} {kvp.Value}"))} )";
            }

            DbQ(createTempTableQuery, log: log);
        }

        private void CopyDataToTemp(string quotedTable, string tempTable, Dictionary<string, string> newTableStructure, bool log)
        {
            var columnsList = string.Join(", ", newTableStructure.Keys.Select(k => Quote(k)));
            string copyDataQuery = $@"
                INSERT INTO {tempTable} ({columnsList})
                SELECT {columnsList}
                FROM {quotedTable}";

            DbQ(copyDataQuery, log: log);
        }

        private void DropOldTable(string quotedTable, bool log)
        {
            string dropOldTableQuery = $"DROP TABLE {quotedTable}";
            DbQ(dropOldTableQuery, log: log, unSafe: true);
        }

        private void RenameTempTable(string tempTable, string quotedTable, string tblName, bool _pstgr, bool log)
        {
            string renameTableQuery;
            if (_pstgr)
            {
                renameTableQuery = $"ALTER TABLE {tempTable} RENAME TO {quotedTable}";
            }
            else
            {
                renameTableQuery = $"ALTER TABLE {tempTable} RENAME TO {UnQuote(tblName)}";
            }

            DbQ(renameTableQuery, log: log);
        }

        #endregion

        #endregion

        #region RANGE

        public void AddRange(string tblName, int range = 0, bool log = false)
        {
            tblName = Quote(tblName);
            if (range == 0)
                try
                {
                    range = int.Parse(_variables.Get("rangeEnd"));
                }
                catch
                {
                    _logger.Warn("var  rangeEnd is empty or 0, used default \"10\"", show: true);
                    range = 10;
                }

            int current = int.Parse(DbQ($@"SELECT COALESCE(MAX({Quote("id")}), 0) FROM {tblName};"));

            for (int currentAcc0 = current + 1; currentAcc0 <= range; currentAcc0++)
            {
                DbQ($@"INSERT INTO {tblName} ({Quote("id")}) VALUES ({currentAcc0}) ON CONFLICT DO NOTHING;", log: log);
            }
        }

        #endregion

        #region TABLE COPY

        private int TableCopy(string sourceTable, string destinationTable, string sqLitePath = null, string pgHost = null, string pgPort = null, string pgDbName = null, string pgUser = null, string pgPass = null, bool thrw = false)
        {
            if (string.IsNullOrEmpty(sourceTable)) throw new ArgumentNullException(nameof(sourceTable));
            if (string.IsNullOrEmpty(destinationTable)) throw new ArgumentNullException(nameof(destinationTable));
            if (string.IsNullOrEmpty(sqLitePath)) sqLitePath = _variables.Get("DBsqltPath");
            if (string.IsNullOrEmpty(pgHost)) pgHost = "localhost";
            if (string.IsNullOrEmpty(pgPort)) pgPort = "5432";
            if (string.IsNullOrEmpty(pgDbName)) pgDbName = "postgres";
            if (string.IsNullOrEmpty(pgUser)) pgUser = "postgres";
            if (string.IsNullOrEmpty(pgPass)) pgPass = _variables.Get("DBpstgrPass");
            string dbMode = _variables.Get("DBmode");

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
                    _logger.Warn(ex.Message, show: true);
                    if (thrw) throw;
                    return 0;
                }
            }
        }

        #endregion

        #region DbQ - CORE QUERY METHOD

        public string DbQ(string query, bool log = false, string sqLitePath = null, string pgHost = null, string pgPort = null, string pgDbName = null, string pgUser = null, string pgPass = null, bool thrw = false, bool unSafe = false)
        {
            if (string.IsNullOrEmpty(sqLitePath)) sqLitePath = _dbPath ?? _variables.Get("DBsqltPath");
            if (string.IsNullOrEmpty(pgHost)) pgHost = _pgHost;
            if (string.IsNullOrEmpty(pgPort)) pgPort = _pgPort;
            if (string.IsNullOrEmpty(pgDbName)) pgDbName = _pgDbName;
            if (string.IsNullOrEmpty(pgUser)) pgUser = _pgUser;
            if (string.IsNullOrEmpty(pgPass)) pgPass = _pgPass ?? _variables.Get("DBpstgrPass");

            string dbMode = _dbMode;

            string result = string.Empty;
            try
            {
                using (var db = dbMode == "PostgreSQL"
                    ? new dSql($"Host={pgHost};Port={pgPort};Database={pgDbName};Username={pgUser};Password={pgPass};Pooling=true;Connection Idle Lifetime=10;")
                    : new dSql(sqLitePath, null))
                {
                    if (Regex.IsMatch(query.TrimStart(), @"^\s*SELECT\b", RegexOptions.IgnoreCase))
                        result = db.DbReadAsync(query, "¦", "·").GetAwaiter().GetResult();
                    else
                        result = db.DbWriteAsync(query).GetAwaiter().GetResult().ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex.Message + $"\n [{query}]");
                if (thrw) throw;
                return string.Empty;
            }

            string toLog = (query.Contains("SELECT")) ? $"[{query}]\n[{result}]" : $"[{query}] - [{result}]";
            _logger.Send(toLog, show: log);
            return result;
        }

        #endregion
    }
}
