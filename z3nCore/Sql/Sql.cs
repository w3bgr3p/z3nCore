
//using Global.IE;
using NBitcoin;
using Nethereum.ABI.CompilationMetadata;
using Nethereum.Contracts.QueryHandlers.MultiCall;
using Nethereum.Signer;
using OtpNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ZennoLab.InterfacesLibrary.ProjectModel;
using static NBitcoin.Scripting.OutputDescriptor;
using System.Diagnostics;

namespace z3nCore
{
    public class Sql
    {
        private readonly IZennoPosterProjectModel _project;
        protected readonly string _dbMode;
        private readonly bool _logShow;
        private readonly string _dbPass;

        private readonly Logger _logger;

        protected bool _pstgr = false;
        protected string _tableName = string.Empty;
        protected string _schemaName = "public";


        public Sql(IZennoPosterProjectModel project, bool log = false)
        {
            _project = project;
            _pstgr = _project.Variables["DBmode"].Value == "PostgreSQL" ? true : false;
            _dbMode = _project.Variables["DBmode"].Value;
            _dbPass = project.Variables["DBpstgrPass"].Value;
            if (_pstgr) _logger = new Logger(project, log: log, classEmoji: "🐘");
            else _logger = new Logger(project, log: log, classEmoji: "✒");
            _logShow = log;
        }
        public void Log(string query, string response = null, bool log = false)
        {
            if (!_logShow && !log) return;
            string dbMode = _project.Variables["DBmode"].Value;
            string toLog = null;


            if (query.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                toLog += $"( ▼ ): [{Regex.Replace(query.Trim(), @"\s+", " ")}]";
                if (!string.IsNullOrEmpty(response)) toLog += $"\n          [{response.Replace("\n", "|").Replace("\r", "")}]";
            }
            else
            {
                toLog += $"( ▲ ): [{Regex.Replace(query.Trim(), @"\s+", " ")}]";
            }
            _logger.Send(toLog);
            //_project.L0g(toLog);
        }


        private string QuoteColumnNames(string updateString)
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


        private string DbQ(string query, bool log = false, bool throwOnEx = false)
        {
            _project.ObsoleteCode("project.DbQ");
            
            string dbMode = _project.Variables["DBmode"].Value;
            string result = null;

            if (_dbMode == "SQLite")
            {
                try
                {
                    result = SQLite.lSQL(_project, query, log);
                }
                catch (Exception ex)
                {
                    Log($"!W Err:[{ex.Message}]. debug:\n{query}");
                    if (throwOnEx) throw;
                }
            }
            else if (_dbMode == "PostgreSQL")
            {
                try
                {
                    //result = PostgresDB.Raw(query, throwOnEx,dbPswd: _dbPass);
                   
                    result = PostgresDB.DbQueryPostgre(_project, query, throwOnEx);
                }
                catch (Exception ex)
                {
                    _logger.Send($"!W Err:[{ex.Message}]. debug:\n{query}");
                    if (throwOnEx) throw;
                }
            }
            else throw new Exception($"unknown DBmode: {_dbMode}");
            Log(query, result, log: log);
            return result;
        }

        public void MkTable(Dictionary<string, string> tableStructure, string tableName = null, bool strictMode = false, bool insertData = false, string host = "localhost:5432", string dbName = "postgres", string dbUser = "postgres", string dbPswd = "", string schemaName = "projects", bool log = false)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                if (_project.Variables["makeTable"].Value != "True") return;
                else tableName = $"{_project.Variables["projectName"].Value.ToLower()}";
                if (_dbMode != "PostgreSQL") tableName = $"_{tableName}";
            }
            if (_dbMode == "SQLite")
            {
                SQLite.lSQLMakeTable(_project, tableStructure, tableName, strictMode);
            }
            else if (_dbMode == "PostgreSQL")
            {
                PostgresDB.MkTablePostgre(_project, tableStructure, tableName, strictMode, insertData, host, dbName, dbUser, dbPswd, schemaName, log: log);
            }
            else throw new Exception($"unknown DBmode: {_dbMode}");
            return;
        }
        
        public void Write(Dictionary<string, string> toWrite, string tableName = null, bool log = false, bool throwOnEx = false, bool last = true)
        {
            if (string.IsNullOrEmpty(tableName)) tableName = _project.Variables["projectTable"].Value;

            foreach (KeyValuePair<string, string> pair in toWrite)
            {
                string key = pair.Key.Replace("'", "''");
                string value = pair.Value.Replace("'", "''");
                string query = $@"INSERT INTO {tableName} (key, value) VALUES ('{key}', '{value}')
	                  ON CONFLICT (key) DO UPDATE SET value = EXCLUDED.value;";
                DbQ(query, log: log);
            }

        }
        public void UpdTxt(string toUpd, string tableName, string key, bool log = false, bool throwOnEx = false)
        {
            toUpd = toUpd.Trim().TrimEnd(',');
            DbQ($@"INSERT INTO {tableName} (key) VALUES ('{key}') ON CONFLICT DO NOTHING;");
            DbQ($@"UPDATE {tableName} SET {toUpd} WHERE key = '{key}';", log: log, throwOnEx: throwOnEx);

        }

        public void Upd(string toUpd, string tableName = null, bool log = false, bool throwOnEx = false, bool last = true, string key = "id", object acc = null, string where = "")
        {
            _project.ObsoleteCode("project.DbUpd");
            string[] keywords = { "blockchain", "browser", "cex_deps", "native", "profile", "settings" };
            if (keywords.Any(keyword => _tableName.Contains(keyword))) last = false;

            toUpd = toUpd.Trim().TrimEnd(',');
            if (last) toUpd = toUpd + $", last = '{DateTime.UtcNow.ToString("MM-ddTHH:mm")}'";

            toUpd = QuoteColumnNames(toUpd.Trim().TrimEnd(','));

            if (string.IsNullOrEmpty(where))
            {
                if (acc is null)
                    acc = _project.Variables["acc0"].Value;
                if (key == "id")
                {
                    DbQ($@"UPDATE {tableName} SET {toUpd} WHERE id = {acc};", log: log, throwOnEx: throwOnEx);
                    return;
                }
                else
                {
                    DbQ($@"UPDATE {tableName} SET {toUpd} WHERE key = '{key}';", log: log, throwOnEx: throwOnEx);
                    return;
                }
            }
            else
            {
                DbQ($@"UPDATE {tableName} SET {toUpd} WHERE {where};", log: log, throwOnEx: throwOnEx);
                return;
            }

        }
        public void Upd(Dictionary<string, string> toWrite, string tableName = null, bool log = false, bool throwOnEx = false, bool last = true, string key = "id", object acc = null, string where = "")
        {
            string toUpd = string.Empty;

            foreach (KeyValuePair<string, string> pair in toWrite)
            {

                toUpd += $"{pair.Key} = '{pair.Value}', ";

            }
            Upd(toUpd, tableName, log, throwOnEx, last, key, acc, where);
        }

        public void Upd(List<string> toWrite, string columnName, string tableName = null, bool log = false, bool throwOnEx = false, bool last = true, bool byKey = false)
        {
            if (string.IsNullOrEmpty(tableName)) tableName = _project.Variables["projectTable"].Value;


            int dicSize = toWrite.Count;
            AddRange(tableName, dicSize);

            int key = 1;
            foreach (string valToWrite in toWrite)
            {

                Upd($"{columnName} = '{valToWrite.Replace("'", "''")}'", tableName, last: last, acc: key);
                key++;
            }
        }

        public string Get(string toGet, string tableName = null, bool log = false, bool throwOnEx = false, string key = "id", string acc = null, string where = "")
        {
            _project.ObsoleteCode("project.DbGet");
            if (string.IsNullOrWhiteSpace(toGet))
                throw new ArgumentException("Column names cannot be null or empty", nameof(toGet));


            toGet = QuoteColumnNames(toGet.Trim().TrimEnd(','));
            if (string.IsNullOrEmpty(tableName)) tableName = _project.Variables["projectTable"].Value;


            if (string.IsNullOrEmpty(where))
            {
                if (string.IsNullOrEmpty(acc))
                    acc = _project.Variables["acc0"].Value;
                if (key == "id")
                    return DbQ($@"SELECT {toGet} from {tableName} WHERE id = {acc};", log: log, throwOnEx: throwOnEx);
                else
                    return DbQ($@"SELECT {toGet} from {tableName} WHERE key = '{key}';", log: log, throwOnEx: throwOnEx);
            }
            else
                return DbQ($@"SELECT {toGet} from {tableName} WHERE {where};", log: log, throwOnEx: throwOnEx);
        }

        public string GetRandom(string toGet, string tableName = null, bool log = false, bool acc = false, bool throwOnEx = false, int range = 0, bool single = true, bool invert = false)
        {
            if (range == 0)
            {
                var rng = _project.Range();
                range = int.Parse(rng[rng.Count-1]);
            }
            if (string.IsNullOrEmpty(tableName)) tableName = _project.Variables["projectTable"].Value;

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

            return DbQ(query, log: log, throwOnEx: throwOnEx);
        }


        public string GetColumns(string tableName, bool log = false)
        {
            string Q;
            if (_pstgr) Q = $@"SELECT column_name FROM information_schema.columns WHERE table_schema = '{_schemaName}' AND table_name = '{tableName}'";
            else Q = $@"SELECT name FROM pragma_table_info('{tableName}')";
            return DbQ(Q, log: log).Replace("\n", ", ").Trim(',').Trim();
        }

        public List<string> GetColumnList(string tableName, bool log = false)
        {

            string Q;
            if (_pstgr) Q = $@"SELECT column_name FROM information_schema.columns WHERE table_schema = '{_schemaName}' AND table_name = '{tableName}'";
            else Q = $@"SELECT name FROM pragma_table_info('{tableName}')";

            return DbQ(Q, log: log).Split('\n').ToList();
        }

        public void CreateShemas(string[] schemas)
        {
            if (!_pstgr) return;
            foreach (string name in schemas) DbQ($"CREATE SCHEMA IF NOT EXISTS {name};");
        }



        public bool TblExist(string tblName)
        {         
            string resp = null;
            if (_pstgr) resp = DbQ($"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '{tblName}';");
            else resp = DbQ($"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='{tblName}';");
            if (resp == "0" || resp == string.Empty) return false;
            else return true;
        }
        
        
        public void TblAdd(string tblName, Dictionary<string, string> tableStructure)
        {

            if (TblExist(tblName)) return;
            if (_pstgr) DbQ($@" CREATE TABLE {_schemaName}.{tblName} ( {string.Join(", ", tableStructure.Select(kvp => $"\"{kvp.Key}\" {kvp.Value.Replace("AUTOINCREMENT", "SERIAL")}"))} );");
            else DbQ($"CREATE TABLE {tblName} (" + string.Join(", ", tableStructure.Select(kvp => $"{kvp.Key} {kvp.Value}")) + ");");
        }
        public List<string> TblColumns(string tblName)
        {
            var result = new List<string>();
            string query = _dbMode == "PostgreSQL"
                ? $@"SELECT column_name FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '{tblName}';"
                : $"SELECT name FROM pragma_table_info('{tblName}');";

            result = DbQ(query, log: _logShow)
                .Split('\n')
                .Select(s => s.Trim())
                .ToList();
            return result;
        }
        public Dictionary<string, string> TblMapForProject(string[] staticColumns, string dynamicToDo = null, string defaultType = "TEXT DEFAULT ''")
        {
            if (string.IsNullOrEmpty(dynamicToDo)) dynamicToDo = _project.Variables["cfgToDo"].Value;


            var tableStructure = new Dictionary<string, string>
            {
                { "id", "INTEGER PRIMARY KEY" }
            };
            foreach (string name in staticColumns)
            {
                if (!tableStructure.ContainsKey(name))
                {
                    tableStructure.Add(name, defaultType);
                }
            }
            if (!string.IsNullOrEmpty(dynamicToDo))
            {
                string[] toDoItems = (dynamicToDo ?? "").Split(',');
                foreach (string taskId in toDoItems)
                {
                    string trimmedTaskId = taskId.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmedTaskId) && !tableStructure.ContainsKey(trimmedTaskId))
                    {
                        tableStructure.Add(trimmedTaskId, defaultType);
                    }
                }
            }
            return tableStructure;
        }



        public bool ClmnExist(string tblName, string clmnName)
        {
            
            string resp = null;
            if (_pstgr)
                resp = DbQ($@"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = '{_schemaName}' AND table_name = '{_tableName}' AND lower(column_name) = lower('{clmnName}');", log: _logShow)?.Trim();
            else
                resp = DbQ($"SELECT COUNT(*) FROM pragma_table_info('{tblName}') WHERE name='{clmnName}';", log: _logShow);
            if (resp == "0" || resp == string.Empty) return false;
            else return true;

        }
        public void ClmnAdd(string tblName, string clmnName, string defaultValue = "TEXT DEFAULT ''")
        {
            
            var current = TblColumns(tblName);
            if (!current.Contains(clmnName))
            {
                clmnName = QuoteColumnNames(clmnName);
                DbQ($@"ALTER TABLE {tblName} ADD COLUMN {clmnName} {defaultValue};", log: _logShow);
            }
        }
        public void ClmnAdd(string tblName, Dictionary<string, string> tableStructure)
        {

            var current = TblColumns(tblName);
            _logger.Send(string.Join(",", current));
            foreach (var column in tableStructure)
            {
                var keyWd = column.Key.Trim();
                if (!current.Contains(keyWd))
                {
                    _logger.Send($"CLMNADD [{keyWd}] not in  [{string.Join(",", current)}] ");
                    DbQ($@"ALTER TABLE {tblName} ADD COLUMN {keyWd} {column.Value};", log: _logShow);
                }
            }
        }
        public void ClmnDrop(string tblName, string clmnName)
        {
            var current = TblColumns(tblName);

            if (current.Contains(clmnName))
            {
                string cascade = (_pstgr) ? " CASCADE" : null;
                DbQ($@"ALTER TABLE {tblName} DROP COLUMN {clmnName}{cascade};", log: _logShow);
            }
        }
        public void ClmnDrop(string tblName, Dictionary<string, string> tableStructure)
        {

            var current = TblColumns(tblName);

            foreach (var column in tableStructure)
            {
                if (!current.Contains(column.Key))
                {
                    string cascade = _dbMode == "PostgreSQL" ? " CASCADE" : null;
                    DbQ($@"ALTER TABLE {tblName} DROP COLUMN {column.Key}{cascade};", log: _logShow);
                }
            }
        }
        public void ClmnPrune(string tblName, Dictionary<string, string> tableStructure)
        {


            var current = TblColumns(tblName);

            foreach (var column in current)
            {
                if (!tableStructure.ContainsKey(column))
                {
                    _logger.Send($"[{column}] not in tableStructure keys, dropping");
                    string cascade = _pstgr ? " CASCADE" : "";
                    DbQ($@"ALTER TABLE {tblName} DROP COLUMN {column}{cascade};", log: _logShow);
                }
            }
        }
        public void AddRange(string tblName, int range = 0)
        {
            if (range == 0)
                try
                {
                    range = int.Parse(_project.Variables["rangeEnd"].Value);
                }
                catch
                {
                    _logger.Send("var  rangeEnd is empty or 0, fallback to 100");
                    range = 100;
                }


            int current = int.Parse(DbQ($@"SELECT COALESCE(MAX(id), 0) FROM {tblName};"));
            _logger.Send(current.ToString());
            _logger.Send(range.ToString());
            for (int currentAcc0 = current + 1; currentAcc0 <= range; currentAcc0++)
            {
                DbQ($@"INSERT INTO {tblName} (id) VALUES ({currentAcc0}) ON CONFLICT DO NOTHING;");
            }

        }


        public string Proxy()
        {

            var resp = _project.SqlGet("proxy", "_instance");
            _project.Variables["proxy"].Value = resp;
            return resp;
        }
        public string Bio()
        {
            var resp = DbQ($@"SELECT nickname, bio FROM _profile WHERE id = {_project.Variables["acc0"].Value};");
            string[] respData = resp.Split('|');
            _project.Variables["accNICKNAME"].Value = respData[0].Trim();
            _project.Variables["accBIO"].Value = respData[1].Trim();
            return resp;
        }
        public Dictionary<string, string> Settings(bool set = true)
        {
            var dbConfig = new Dictionary<string, string>();


            var resp = DbQ($"SELECT key, value FROM _settings");
            foreach (string varData in resp.Split('\n'))
            {
                string varName = varData.Split('|')[0];
                string varValue = varData.Split('|')[1].Trim();
                dbConfig.Add(varName, varValue);

                if (set)
                {
                    try { _project.Var(varName, varValue); }
                    catch (Exception e) { _logger.Send(e.Message); }
                }
            }
            return dbConfig;

        }

        public string Ref(string refCode = null, bool log = false)
        {
            if (string.IsNullOrEmpty(refCode)) refCode = _project.Variables["cfgRefCode"].Value;

            if (string.IsNullOrEmpty(refCode) || refCode == "_") refCode = DbQ($@"SELECT refcode FROM {_project.Variables["projectTable"].Value}
			WHERE refcode != '_' 
			AND TRIM(refcode) != ''
			ORDER BY RANDOM()
			LIMIT 1;", log);
            return refCode;
        }

        public Dictionary<string, string> GetAddresses(string chains = null)
        {
            var addrss = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(chains)) chains = GetColumns("_addresses");
            string[] tikers = chains.Replace(" ", "").Split(',');
            string[] addresses = _project.SqlGet(chains, "_addresses").Replace(" ", "").Split('|');


            for (int i = 0; i < tikers.Length; i++)
            {
                var tiker = tikers[i].Trim();
                var address = addresses[i].Trim();
                addrss.Add(tiker, address);
            }

            return addrss;

        }
        public List<string> MkToDoQueries(string toDo = null, string defaultRange = null, string defaultDoFail = null)
        {
            string tableName = _project.Variables["projectTable"].Value;

            var nowIso = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            if (string.IsNullOrEmpty(toDo)) toDo = _project.Variables["cfgToDo"].Value;

            string[] toDoItems = (toDo ?? "").Split(',');

            var allQueries = new List<string>();

            foreach (string taskId in toDoItems)
            {
                string trimmedTaskId = taskId.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedTaskId))
                {

                    string range = defaultRange ?? _project.Variables["range"].Value;
                    string doFail = defaultDoFail ?? _project.Variables["doFail"].Value;
                    string failCondition = (doFail != "True" ? "AND status NOT LIKE '%fail%'" : "");
                    string query = $@"SELECT id FROM {tableName} WHERE id in ({range}) {failCondition} AND status NOT LIKE '%skip%' 
                AND ({trimmedTaskId} < '{nowIso}' OR {trimmedTaskId} = '_')";
                    allQueries.Add(query);
                }
            }

            return allQueries;
        }
        public void FilterAccList(List<string> dbQueries, bool log = false)
        {
            if (!string.IsNullOrEmpty(_project.Variables["acc0Forced"].Value))
            {
                _project.Lists["accs"].Clear();
                _project.Lists["accs"].Add(_project.Variables["acc0Forced"].Value);
                _logger.Send($@"manual mode on with {_project.Variables["acc0Forced"].Value}");
                return;
            }

            var allAccounts = new HashSet<string>();
            foreach (var query in dbQueries)
            {
                try
                {
                    var accsByQuery = DbQ(query).Trim();
                    if (!string.IsNullOrWhiteSpace(accsByQuery))
                    {
                        var accounts = accsByQuery.Split('\n').Select(x => x.Trim().TrimStart(','));
                        allAccounts.UnionWith(accounts);
                    }
                }
                catch
                {

                    _logger.Send($"{query}");
                }
            }

            if (allAccounts.Count == 0)
            {
                _project.Variables["noAccsToDo"].Value = "True";
                _logger.Send($"♻ noAccountsAvailable by queries [{string.Join(" | ", dbQueries)}]");
                return;
            }
            _logger.Send($"Initial availableAccounts: [{string.Join(", ", allAccounts)}]");

            if (!string.IsNullOrEmpty(_project.Variables["requiredSocial"].Value))
            {
                string[] demanded = _project.Variables["requiredSocial"].Value.Split(',');
                _logger.Send($"Filtering by socials: [{string.Join(", ", demanded)}]");

                foreach (string social in demanded)
                {
                    string tableName = ($"_{social.Trim().ToLower()}");
                    var notOK = _project.SqlGet($"id", _tableName, where: "status NOT LIKE '%ok%'", log: log)

                        .Split('\n')
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrEmpty(x));
                    allAccounts.ExceptWith(notOK);
                    _logger.Send($"After {social} filter: [{string.Join("|", allAccounts)}]");
                }
            }
            _project.Lists["accs"].Clear();
            _project.Lists["accs"].AddRange(allAccounts);
            _logger.Send($"final list [{string.Join("|", _project.Lists["accs"])}]");
        }

        public string Address(string chainType = "evm")
        {
            chainType = chainType.ToLower().Trim();

            var resp = _project.SqlGet(chainType, "_addresses");

            try
            {
                chainType = chainType.ToUpper();
                _project.Variables[$"address{chainType}"].Value = resp;
            }
            catch { }
            return resp;

        }
        public string Key(string chainType = "evm")
        {

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

            var resp = _project.SqlGet(chainType, "_wallets");
            if (!string.IsNullOrEmpty(_project.Var("cfgPin")))
                return SAFU.Decode(_project, resp);
            else return resp;

        }

        public string[] columnsDefault = {
                        "status",
                        "last",
                        };

        public string[] columnsSocial = {
                        "status",
                        "last",
                        "cookie",
                        "login",
                        "pass",
                        "otpsecret",
                        "email",
                        "recovery",
                        };
    }

}