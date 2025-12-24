using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    public class Db
    {
        private readonly string _dbMode;
        private readonly string _sqLitePath;
        private readonly string _pgHost;
        private readonly string _pgPort;
        private readonly string _pgDbName;
        private readonly string _pgUser;
        private readonly string _pgPass;
        private readonly string _defaultTable;
        private readonly IZennoPosterProjectModel _project;
        
        private const char RawSeparator = '·';
        private const char ColumnSeparator = '¦';
        private const string SchemaName = "public";

        public Db(
            string dbMode = "SQLite",
            string sqLitePath = null,
            string pgHost = "localhost",
            string pgPort = "5432",
            string pgDbName = "postgres",
            string pgUser = "postgres",
            string pgPass = "",
            string defaultTable = null)
        {
            _dbMode = dbMode;
            _sqLitePath = sqLitePath;
            _pgHost = pgHost;
            _pgPort = pgPort;
            _pgDbName = pgDbName;
            _pgUser = pgUser;
            _pgPass = pgPass;
            _defaultTable = defaultTable;
        }
        
        public Db(
            IZennoPosterProjectModel project,
            string dbMode = null,
            string sqLitePath = null,
            string pgHost = null,
            string pgPort = null,
            string pgDbName = null,
            string pgUser = null,
            string pgPass = null,
            string defaultTable = null)
        {
            _project = project;
            _dbMode = dbMode ?? project.Var("DBmode");
            _sqLitePath = sqLitePath ?? project.Var("DBsqltPath");
            _pgHost = pgHost ?? project.GVar("sqlPgHost");
            _pgPort = pgPort ?? project.GVar("sqlPgPort");
            _pgDbName = pgDbName ?? project.GVar("sqlPgName");
            _pgUser = pgUser ?? project.GVar("sqlPgUser");
            _pgPass = pgPass ?? project.Var("DBpstgrPass");
            _defaultTable = defaultTable ?? project.ProjectTable();
        }

        private void Log(object toLog)
        {
            string toSend = $"{toLog}";
            if (_project!=null) _project.SendInfoToLog(toSend);
            else Console.WriteLine(toSend);

        }

        #region Core Query
        public string Query(string query, bool log = false, bool thrw = false, bool unSafe = false)
        {
            string result = string.Empty;
            int maxRetries = 10;
            int delay = 100;
            Random rnd = new Random();

            using (var db = _dbMode == "PostgreSQL"
                       ? new Sql($"Host={_pgHost};Port={_pgPort};Database={_pgDbName};Username={_pgUser};Password={_pgPass};Pooling=true;Connection Idle Lifetime=10;")
                       : new Sql(_sqLitePath, null))
            {
                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        if (Regex.IsMatch(query.TrimStart(), @"^\s*SELECT\b", RegexOptions.IgnoreCase))
                            result = db.DbReadAsync(query, ColumnSeparator.ToString(), RawSeparator.ToString()).GetAwaiter().GetResult();
                        else
                            result = db.DbWriteAsync(query).GetAwaiter().GetResult().ToString();

                        break;
                    }
                    catch (Exception ex)
                    {
                        if (_dbMode == "SQLite" && ex.Message.Contains("locked") && i < maxRetries - 1)
                        {
                            delay = 50 * (1 << i) + rnd.Next(10, 50);
                            Thread.Sleep(delay);
                            continue;
                        }

                        if (log) Log($"Database Error: {ex.Message}\n[{query}]");
                        if (thrw) throw;
                        return string.Empty;
                    }
                }
            }

            if (log)
            {
                string toLog = query.Contains("SELECT") ? $"[{query}]\n[{result}]" : $"[{query}] - [{result}]";
                Log($"[{(_dbMode == "PostgreSQL" ? "🐘" : "SQLite")}] {toLog}");
            }
            
            return result;
        }
        #endregion

        #region Get Methods
        public string Get(string columns, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            if (string.IsNullOrWhiteSpace(columns))
                throw new ArgumentException("Column names cannot be null or empty", nameof(columns));

            columns = QuoteSelectColumns(columns.Trim().TrimEnd(','));
            tableName = tableName ?? _defaultTable;
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("Table name must be provided");

            string query;
            if (string.IsNullOrEmpty(where))
            {
                if (id == null || string.IsNullOrEmpty(id.ToString()))
                    throw new ArgumentException("ID must be provided when where clause is empty");
                query = $"SELECT {columns} FROM {Quote(tableName)} WHERE {Quote(key)} = {id}";
            }
            else
            {
                query = $"SELECT {columns} FROM {Quote(tableName)} WHERE {where}";
            }

            return Query(query, log, thrw);
        }

        public Dictionary<string, string> GetColumns(string columns, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            string result = Get(columns, tableName, log, thrw, key, id, where);

            if (string.IsNullOrWhiteSpace(result))
                return new Dictionary<string, string>();

            var columnList = columns.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim().Trim('`', '"', '[', ']'))
                .ToList();
            var values = result.Split(ColumnSeparator);
            var dictionary = new Dictionary<string, string>();

            for (int i = 0; i < columnList.Count && i < values.Length; i++)
            {
                dictionary[columnList[i]] = values[i];
            }

            return dictionary;
        }

        public string[] GetLine(string columns, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            return Get(columns, tableName, log, thrw, key, id, where).Split(ColumnSeparator);
        }

        public List<string> GetLines(string columns, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            return Get(columns, tableName, log, thrw, key, id, where).Split(RawSeparator).ToList();
        }

        public string GetRandom(string column, string tableName = null, bool log = false, bool thrw = false, int maxId = 0, bool includeId = false, bool single = true, bool invertEmpty = false)
        {
            tableName = tableName ?? _defaultTable;
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("Table name must be provided");

            string idColumn = includeId ? "id, " : "";
            string emptyCondition = invertEmpty ? "=" : "!=";
            
            string query = $@"
                SELECT {idColumn}{column.Trim().TrimEnd(',')} 
                FROM {Quote(tableName)} 
                WHERE TRIM({column}) {emptyCondition} ''";
            
            if (maxId > 0)
                query += $" AND id < {maxId}";
            
            query += " ORDER BY RANDOM()";
            
            if (single)
                query += " LIMIT 1";

            return Query(query, log, thrw);
        }
        #endregion

        #region Update Methods
        public void Upd(string setClause, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            tableName = tableName ?? _defaultTable;
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("Table name must be provided");

            setClause = QuoteColumns(setClause);
            string quotedTable = Quote(tableName);

            string query;
            if (string.IsNullOrEmpty(where))
            {
                if (id == null || string.IsNullOrEmpty(id.ToString()))
                    throw new ArgumentException("ID or where clause must be provided");
                query = $"UPDATE {quotedTable} SET {setClause} WHERE {Quote(key)} = {id}";
            }
            else
            {
                query = $"UPDATE {quotedTable} SET {setClause} WHERE {where}";
            }

            Query(query, log, thrw);
        }
        public void UpdFromDict(Dictionary<string, string> data, string tableName = null, bool log = false, bool thrw = false, string where = "")
        {
            tableName = tableName ?? _defaultTable;
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("Table name must be provided");

            if (data.ContainsKey("id"))
            {
                data["_id"] = data["id"];
                data.Remove("id");
            }

            var columns = data.Keys.ToList();
            AddColumns(columns, tableName, log);

            var updString = new StringBuilder();
            foreach (var kvp in data)
            {
                updString.Append($"{kvp.Key} = '{kvp.Value.Replace("'", "")}',");
            }

            Upd(updString.ToString().Trim(','), tableName, log, thrw, where: where);
        }
        public void InsertDic(Dictionary<string, string> data, string tableName = null, bool log = false, bool thrw = false)
        {
            tableName = tableName ?? _defaultTable;
    
            var columns = string.Join(", ", data.Keys.Select(k => Quote(k)));
            var values = string.Join(", ", data.Values.Select(v => $"'{v.Replace("'", "''")}'"));
    
            string query = $"INSERT INTO {Quote(tableName)} ({columns}) VALUES ({values})";
    
            if (_dbMode == "PostgreSQL")
                query += " ON CONFLICT DO NOTHING";
    
            Query(query, log, thrw);
        }
        public void SetDone(string taskColumn = "daily", int cooldownMin = 0, string tableName = null, bool log = false, bool thrw = false, string key = "id", object id = null, string where = "")
        {
            string cooldown = cooldownMin == 0 ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : DateTime.Now.AddMinutes(cooldownMin).ToString("yyyy-MM-dd HH:mm:ss");
            Upd($"{taskColumn} = '{cooldown}'", tableName, log, thrw, key, id, where);
        }
        
        #endregion

        #region JSON Methods
        public void JsonToDb(string json, string tableName = null, bool log = false, bool thrw = false, string where = "")
        {
            tableName = tableName ?? _defaultTable;
            var structure = ExtractJsonStructure(json);
            var dataDic = JsonToDictionary(json);
            dataDic["_json_structure"] = structure;

            UpdFromDict(dataDic, tableName, log, thrw, where);
        }

        public string DbToJson(string tableName = null, bool log = false, bool thrw = false, object id = null)
        {
            tableName = tableName ?? _defaultTable;
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("Table name must be provided");

            var columns = GetTableColumns(tableName, log);
            if (!columns.Contains("_json_structure"))
            {
                if (log) Log("ERROR: _json_structure column not found");
                return "{}";
            }

            var columnsString = string.Join(",", columns);
            var allColumns = GetColumns(columnsString, tableName, log, thrw, id: id);

            if (!allColumns.ContainsKey("_json_structure"))
            {
                if (log) Log("ERROR: _json_structure not in result");
                return "{}";
            }

            var structureJson = allColumns["_json_structure"];
            Dictionary<string, string> structure;
            
            try
            {
                structure = JsonConvert.DeserializeObject<Dictionary<string, string>>(structureJson);
            }
            catch (Exception ex)
            {
                if (log) Log($"Structure parse error: {ex.Message}");
                return "{}";
            }

            var keysToRemove = allColumns.Keys.Where(k => k.StartsWith("_") || k == "id").ToList();
            foreach (var key in keysToRemove)
            {
                allColumns.Remove(key);
            }

            return BuildJson(allColumns, structure, log);
        }

        private string ExtractJsonStructure(string json)
        {
            var structure = new Dictionary<string, string>();
            var jObject = JObject.Parse(json);

            MapStructure(jObject, "");
            return JsonConvert.SerializeObject(structure);

            void MapStructure(JToken token, string prefix)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        if (!string.IsNullOrEmpty(prefix))
                            structure[prefix] = "object";

                        foreach (var property in token.Children<JProperty>())
                        {
                            var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}_{property.Name}";
                            MapStructure(property.Value, key);
                        }
                        break;

                    case JTokenType.Array:
                        structure[prefix] = "array";
                        var index = 0;
                        foreach (var item in token.Children())
                        {
                            MapStructure(item, $"{prefix}_{index}");
                            index++;
                        }
                        break;

                    default:
                        structure[prefix] = token.Type.ToString().ToLower();
                        break;
                }
            }
        }

        private Dictionary<string, string> JsonToDictionary(string json)
        {
            var result = new Dictionary<string, string>();
            var jObject = JObject.Parse(json);
            FlattenJson(jObject, "", result);
            return result;

            void FlattenJson(JToken token, string prefix, Dictionary<string, string> dict)
            {
                if (token is JObject obj)
                {
                    foreach (var prop in obj.Properties())
                    {
                        var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}_{prop.Name}";
                        FlattenJson(prop.Value, key, dict);
                    }
                }
                else if (token is JArray arr)
                {
                    for (int i = 0; i < arr.Count; i++)
                    {
                        FlattenJson(arr[i], $"{prefix}_{i}", dict);
                    }
                }
                else
                {
                    dict[prefix] = token.ToString();
                }
            }
        }

        private string BuildJson(Dictionary<string, string> data, Dictionary<string, string> structure, bool log)
        {
            var root = new JObject();

            foreach (var kvp in data)
            {
                if (!structure.ContainsKey(kvp.Key))
                    continue;

                var type = structure[kvp.Key];
                if (type == "object" || type == "array")
                    continue;

                var path = kvp.Key.Split('_');
                JToken current = root;

                var containers = new List<(int segmentCount, string path, bool isArray)>();

                for (int i = 1; i < path.Length; i++)
                {
                    var testPath = string.Join("_", path.Take(i));
                    if (structure.ContainsKey(testPath))
                    {
                        var testType = structure[testPath];
                        if (testType == "object" || testType == "array")
                        {
                            containers.Add((i, testPath, testType == "array"));
                        }
                    }
                }

                int lastContainerDepth = 0;
                foreach (var (segmentCount, containerPath, isArray) in containers)
                {
                    var containerSegments = containerPath.Split('_');
                    var containerKey = string.Join("_", containerSegments.Skip(lastContainerDepth));

                    if (current is JObject jObj)
                    {
                        if (jObj[containerKey] == null)
                        {
                            jObj[containerKey] = isArray ? (JToken)new JArray() : (JToken)new JObject();
                        }
                        current = jObj[containerKey];
                    }
                    else if (current is JArray jArr && int.TryParse(containerKey, out int idx))
                    {
                        while (jArr.Count <= idx)
                        {
                            jArr.Add(isArray ? (JToken)new JArray() : (JToken)new JObject());
                        }
                        current = jArr[idx];
                    }

                    lastContainerDepth = segmentCount;
                }

                var finalKey = string.Join("_", path.Skip(lastContainerDepth));
                var value = kvp.Value;

                JToken token;
                if (type != "string" && IsJsonString(value))
                {
                    try
                    {
                        token = JToken.Parse(value);
                    }
                    catch
                    {
                        token = CreateTypedToken(value, kvp.Key, structure);
                    }
                }
                else
                {
                    token = CreateTypedToken(value, kvp.Key, structure);
                }

                if (current is JObject jObj2)
                {
                    jObj2[finalKey] = token;
                }
                else if (current is JArray jArr2)
                {
                    jArr2.Add(token);
                }
            }

            return root.ToString(Formatting.None);
        }

        private bool IsJsonString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var trimmed = value.Trim();
            return (trimmed.StartsWith("{") && trimmed.EndsWith("}")) ||
                   (trimmed.StartsWith("[") && trimmed.EndsWith("]"));
        }

        private JToken CreateTypedToken(string value, string fullKey, Dictionary<string, string> structure)
        {
            if (structure.ContainsKey(fullKey))
            {
                var type = structure[fullKey];

                switch (type)
                {
                    case "integer":
                        if (int.TryParse(value, out int intVal))
                            return new JValue(intVal);
                        break;
                    case "float":
                        if (double.TryParse(value, out double dblVal))
                            return new JValue(dblVal);
                        break;
                    case "boolean":
                        if (bool.TryParse(value, out bool boolVal))
                            return new JValue(boolVal);
                        break;
                    case "null":
                        return JValue.CreateNull();
                }
            }

            return new JValue(value);
        }
        #endregion
        
        #region Table Preparation Methods
        public void PrepareTable(Dictionary<string, string> tableStructure, string tableName = null, bool log = false, bool prune = false, bool rearrange = false)
        {
            tableName = tableName ?? _defaultTable;
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("Table name must be provided");

            CreateTable(tableStructure, tableName, log);
            AddColumns(tableStructure, tableName, log);
            
            if (prune) 
                PruneColumns(tableStructure, tableName, log);
            
            if (rearrange) 
                RearrangeColumns(tableStructure, tableName, log);
        }

        public void PrepareTable(List<string> columns, string tableName = null, string defaultType = "TEXT DEFAULT ''", bool log = false, bool prune = false, bool rearrange = false)
        {
            var tableStructure = new Dictionary<string, string>
            {
                { "id", "INTEGER PRIMARY KEY AUTOINCREMENT" }
            };

            foreach (var column in columns)
            {
                string trimmed = column.Trim();
                if (!string.IsNullOrEmpty(trimmed) && trimmed.ToLower() != "id" && !tableStructure.ContainsKey(trimmed))
                {
                    tableStructure.Add(trimmed, defaultType);
                }
            }

            PrepareTable(tableStructure, tableName, log, prune, rearrange);
        }
        #endregion

        #region Column Rearrange Method
        public void RearrangeColumns(Dictionary<string, string> tableStructure, string tableName = null, bool log = false)
        {
            tableName = tableName ?? _defaultTable;
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("Table name must be provided");

            ValidateName(tableName, "table name");

            string quotedTable = Quote(tableName);
            string tempTable = Quote($"{tableName}_temp_{DateTime.Now.Ticks}");

            try
            {
                var currentColumns = GetTableColumns(tableName, log);
                var idType = GetIdType(tableName, log);
                var newTableStructure = BuildNewStructure(tableStructure, currentColumns, idType, tableName, log);

                CreateTempTable(tempTable, newTableStructure, log);
                CopyDataToTemp(quotedTable, tempTable, newTableStructure, log);
                DropOldTable(quotedTable, log);
                RenameTempTable(tempTable, quotedTable, tableName, log);

                if (log)
                {
                    Log($"Table {tableName} rearranged successfully. New column order: {string.Join(", ", newTableStructure.Keys)}");
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Query($"DROP TABLE IF EXISTS {tempTable}", log: false);
                }
                catch { }

                throw new Exception($"Failed to rearrange table {tableName}: {ex.Message}", ex);
            }
            finally
            {
                try
                {
                    var tempExists = _dbMode == "PostgreSQL"
                        ? Query($"SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_schema = '{SchemaName}' AND table_name = '{UnQuote(tempTable)}')", log: false)
                        : Query($"SELECT name FROM sqlite_master WHERE type='table' AND name='{UnQuote(tempTable)}'", log: false);

                    if (!string.IsNullOrEmpty(tempExists) && tempExists != "0" && tempExists.ToLower() != "false")
                    {
                        Query($"DROP TABLE {tempTable}", log: false);
                    }
                }
                catch { }
            }
        }

        private string GetIdType(string tableName, bool log)
        {
            string idType = "INTEGER PRIMARY KEY";

            if (_dbMode == "PostgreSQL")
            {
                string getIdTypeQuery = $@"
                    SELECT data_type, is_identity 
                    FROM information_schema.columns 
                    WHERE table_schema = '{SchemaName}' 
                    AND table_name = '{UnQuote(tableName)}' 
                    AND column_name = 'id'";

                var idInfo = Query(getIdTypeQuery, log);
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
                string getIdTypeQuery = $"SELECT type FROM pragma_table_info('{UnQuote(tableName)}') WHERE name='id'";
                var sqliteIdType = Query(getIdTypeQuery, log);
                if (!string.IsNullOrEmpty(sqliteIdType) && sqliteIdType.ToUpper().Contains("TEXT"))
                    idType = "TEXT PRIMARY KEY";
            }

            return idType;
        }

        private Dictionary<string, string> BuildNewStructure(Dictionary<string, string> tableStructure, List<string> currentColumns, string idType, string tableName, bool log)
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
                    string colType = GetColumnType(tableName, col, log);
                    newTableStructure.Add(col, colType);
                }
            }

            return newTableStructure;
        }

        private string GetColumnType(string tableName, string col, bool log)
        {
            string colType = "TEXT DEFAULT ''";

            if (_dbMode == "PostgreSQL")
            {
                string getTypeQuery = $@"
                    SELECT data_type, character_maximum_length, column_default
                    FROM information_schema.columns 
                    WHERE table_schema = '{SchemaName}' 
                    AND table_name = '{UnQuote(tableName)}' 
                    AND column_name = '{col}'";

                var typeInfo = Query(getTypeQuery, log);
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
                string getTypeQuery = $"SELECT type FROM pragma_table_info('{UnQuote(tableName)}') WHERE name='{col}'";
                var sqliteType = Query(getTypeQuery, log);
                if (!string.IsNullOrEmpty(sqliteType))
                    colType = sqliteType;
            }

            return colType;
        }

        private void CreateTempTable(string tempTable, Dictionary<string, string> newTableStructure, bool log)
        {
            string createTempTableQuery;
            if (_dbMode == "PostgreSQL")
            {
                createTempTableQuery = $@"CREATE TABLE {tempTable} ( 
                    {string.Join(", ", newTableStructure.Select(kvp => $"{Quote(kvp.Key)} {kvp.Value.Replace("AUTOINCREMENT", "SERIAL")}"))} )";
            }
            else
            {
                createTempTableQuery = $@"CREATE TABLE {tempTable} ( 
                    {string.Join(", ", newTableStructure.Select(kvp => $"{Quote(kvp.Key)} {kvp.Value}"))} )";
            }

            Query(createTempTableQuery, log);
        }

        private void CopyDataToTemp(string quotedTable, string tempTable, Dictionary<string, string> newTableStructure, bool log)
        {
            var columnsList = string.Join(", ", newTableStructure.Keys.Select(k => Quote(k)));
            string copyDataQuery = $@"
                INSERT INTO {tempTable} ({columnsList})
                SELECT {columnsList}
                FROM {quotedTable}";

            Query(copyDataQuery, log);
        }

        private void DropOldTable(string quotedTable, bool log)
        {
            string dropOldTableQuery = $"DROP TABLE {quotedTable}";
            Query(dropOldTableQuery, log);
        }

        private void RenameTempTable(string tempTable, string quotedTable, string tableName, bool log)
        {
            string renameTableQuery;
            if (_dbMode == "PostgreSQL")
            {
                renameTableQuery = $"ALTER TABLE {tempTable} RENAME TO {quotedTable}";
            }
            else
            {
                renameTableQuery = $"ALTER TABLE {tempTable} RENAME TO {UnQuote(tableName)}";
            }

            Query(renameTableQuery, log);
        }
        #endregion

        #region Prune Columns Method
        public void PruneColumns(Dictionary<string, string> tableStructure, string tableName = null, bool log = false)
        {
            tableName = tableName ?? _defaultTable;
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("Table name must be provided");

            var currentColumns = GetTableColumns(tableName, log);

            foreach (var column in currentColumns)
            {
                if (!tableStructure.ContainsKey(column) && column.ToLower() != "id")
                {
                    DropColumn(column, tableName, log);
                }
            }
        }
        #endregion

        #region Table Methods
        public void CreateTable(Dictionary<string, string> tableStructure, string tableName, bool log = false)
        {
            if (TableExists(tableName, log))
                return;

            string quotedTable = Quote(tableName);
            string query;

            if (_dbMode == "PostgreSQL")
            {
                query = $"CREATE TABLE {quotedTable} ( {string.Join(", ", tableStructure.Select(kvp => $"\"{kvp.Key}\" {kvp.Value.Replace("AUTOINCREMENT", "SERIAL")}"))} )";
            }
            else
            {
                query = $"CREATE TABLE {quotedTable} ({string.Join(", ", tableStructure.Select(kvp => $"{Quote(kvp.Key)} {kvp.Value}"))})";
            }

            Query(query, log);
        }
        public bool TableExists(string tableName, bool log = false)
        {
            tableName = UnQuote(tableName);
            string query;

            if (_dbMode == "PostgreSQL")
            {
                query = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{SchemaName}' AND table_name = '{tableName}'";
            }
            else
            {
                query = $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tableName}'";
            }

            string resp = Query(query, log);
            return resp != "0" && !string.IsNullOrEmpty(resp);
        }

        public List<string> GetTables(bool log = false)
        {
            string query = _dbMode == "PostgreSQL"
                ? $"SELECT table_name FROM information_schema.tables WHERE table_schema = '{SchemaName}' AND table_type = 'BASE TABLE' ORDER BY table_name"
                : "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";

            return Query(query, log)
                .Split(RawSeparator)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        public List<string> GetTableColumns(string tableName, bool log = false)
        {
            string query = _dbMode == "PostgreSQL"
                ? $"SELECT column_name FROM information_schema.columns WHERE table_schema = '{SchemaName}' AND table_name = '{UnQuote(tableName)}'"
                : $"SELECT name FROM pragma_table_info('{UnQuote(tableName)}')";

            return Query(query, log)
                .Split(RawSeparator)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }
        #endregion

        #region Column Methods
        public bool ColumnExists(string columnName, string tableName, bool log = false)
        {
            string query;

            if (_dbMode == "PostgreSQL")
            {
                query = $"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = '{SchemaName}' AND table_name = '{UnQuote(tableName)}' AND LOWER(column_name) = LOWER('{UnQuote(columnName)}')";
            }
            else
            {
                query = $"SELECT COUNT(*) FROM pragma_table_info('{UnQuote(tableName)}') WHERE name='{UnQuote(columnName)}'";
            }

            string resp = Query(query, log);
            return resp != "0" && !string.IsNullOrEmpty(resp);
        }

        public void AddColumn(string columnName, string tableName = null, bool log = false, string defaultValue = "TEXT DEFAULT ''")
        {
            tableName = tableName ?? _defaultTable;
            var current = GetTableColumns(tableName, log);
            
            if (!current.Contains(columnName))
            {
                string quotedColumn = Quote(columnName);
                string quotedTable = Quote(tableName);
                Query($"ALTER TABLE {quotedTable} ADD COLUMN {quotedColumn} {defaultValue}", log);
            }
        }
        public void AddColumns(List<string> columns, string tableName = null, bool log = false, string defaultValue = "TEXT DEFAULT ''")
        {
            foreach (var column in columns)
            {
                AddColumn(column, tableName, log, defaultValue);
            }
        }

        public void AddColumns(Dictionary<string, string> tableStructure, string tableName = null, bool log = false)
        {
            tableName = tableName ?? _defaultTable;
            var current = GetTableColumns(tableName, log);
            
            foreach (var column in tableStructure)
            {
                var keyWd = column.Key.Trim();
                if (!current.Contains(keyWd))
                {
                    string quotedColumn = Quote(keyWd);
                    string quotedTable = Quote(tableName);
                    Query($"ALTER TABLE {quotedTable} ADD COLUMN {quotedColumn} {column.Value}", log);
                }
            }
        }

        public void DropColumn(string columnName, string tableName = null, bool log = false)
        {
            tableName = tableName ?? _defaultTable;
            var current = GetTableColumns(tableName, log);

            if (current.Contains(columnName))
            {
                string quotedColumn = Quote(columnName);
                string quotedTable = Quote(tableName);
                string cascade = _dbMode == "PostgreSQL" ? " CASCADE" : "";
                Query($"ALTER TABLE {quotedTable} DROP COLUMN {quotedColumn}{cascade}", log);
            }
        }

        public void PruneEmptyColumns(string tableName = null, bool log = false)
        {
            tableName = tableName ?? _defaultTable;
            var current = GetTableColumns(tableName, log);

            foreach (var column in current)
            {
                if (column.ToLower() == "id") continue;
                
                string quotedColumn = Quote(column);
                string quotedTable = Quote(tableName);
                string countQuery = $"SELECT COUNT(*) FROM {quotedTable} WHERE {quotedColumn} != '' AND {quotedColumn} IS NOT NULL";
                string result = Query(countQuery, log);

                if (int.TryParse(result, out int count) && count == 0)
                {
                    DropColumn(column, tableName, log);
                }
            }
        }
        #endregion

        #region Range Methods
        public void AddRange(string tableName, int range, bool log = false)
        {
            string quotedTable = Quote(tableName);
            string currentQuery = $"SELECT COALESCE(MAX(CAST({Quote("id")} AS INTEGER)), 0) FROM {quotedTable}";
            int current = int.Parse(Query(currentQuery));

            if (current >= range) return;

            var values = new List<string>();
            for (int i = current + 1; i <= range; i++)
            {
                values.Add($"('{i}')");
            }

            if (values.Count > 0)
            {
                const int batchSize = 500;
                for (int i = 0; i < values.Count; i += batchSize)
                {
                    var batch = values.Skip(i).Take(batchSize);
                    var batchValues = string.Join(", ", batch);
                    Query($"INSERT INTO {quotedTable} ({Quote("id")}) VALUES {batchValues} ON CONFLICT DO NOTHING", log);
                }
            }
        }
        #endregion

        #region Line Operations
        public void ClearLine(int id, string tableName = null, bool log = false, bool thrw = false)
        {
            tableName = tableName ?? _defaultTable;
            string quotedTable = Quote(tableName);

            var columns = GetTableColumns(tableName, log);
            var columnsToClean = columns.Where(col => col.ToLower() != "id").ToList();

            if (columnsToClean.Count == 0)
            {
                if (log) Log($"No columns to clear in table {tableName}");
                return;
            }

            var setClause = string.Join(", ", columnsToClean.Select(col => $"{Quote(col)} = ''"));
            var updateQuery = $"UPDATE {quotedTable} SET {setClause} WHERE {Quote("id")} = {id}";

            Query(updateQuery, log, thrw);

            if (log) Log($"Cleared {columnsToClean.Count} columns in row id={id}");
        }

        public void SwapLines(int id1, int id2, string tableName = null, bool log = false, bool thrw = false)
        {
            tableName = tableName ?? _defaultTable;
            string quotedTable = Quote(tableName);

            var columns = GetTableColumns(tableName, log);
            var columnsToSwap = columns.Where(col => col.ToLower() != "id").ToList();

            if (columnsToSwap.Count == 0)
            {
                if (log) Log($"No columns to swap in table {tableName}");
                return;
            }

            var columnsString = string.Join(", ", columnsToSwap);
            var data1 = GetColumns(columnsString, tableName, log, thrw, "id", id1);
            var data2 = GetColumns(columnsString, tableName, log, thrw, "id", id2);

            if (data1 == null || data1.Count == 0)
            {
                if (log) Log($"Row id={id1} not found");
                if (thrw) throw new Exception($"Row id={id1} not found");
                return;
            }

            if (data2 == null || data2.Count == 0)
            {
                if (log) Log($"Row id={id2} not found");
                if (thrw) throw new Exception($"Row id={id2} not found");
                return;
            }

            var setClause1 = string.Join(", ", data2.Select(kvp => $"{Quote(kvp.Key)} = '{kvp.Value.Replace("'", "''")}'"));
            var setClause2 = string.Join(", ", data1.Select(kvp => $"{Quote(kvp.Key)} = '{kvp.Value.Replace("'", "''")}'"));

            Query($"UPDATE {quotedTable} SET {setClause1} WHERE {Quote("id")} = {id1}", log, thrw);
            Query($"UPDATE {quotedTable} SET {setClause2} WHERE {Quote("id")} = {id2}", log, thrw);

            if (log) Log($"Swapped data between id={id1} and id={id2}");
        }
        #endregion

        #region Helper Methods
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
        #endregion
    }
}