using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using ZennoLab.InterfacesLibrary.Enums.Log;
using ZennoLab.InterfacesLibrary.ProjectModel;

namespace z3nCore
{
    
    public class PostgresDB : IDisposable
    {

        private NpgsqlConnection _conn;

        public void Dispose()
        {
            _conn?.Close();
            _conn?.Dispose();
        }
        public PostgresDB(string host, string database, string user, string password)
        {
            var (hostname, port) = ParseHostPort(host, 5432);
            var cs = $"Host={hostname};Port={port};Database={database};Username={user};Password={password};";
            _conn = new NpgsqlConnection(cs);

        }
        private (string host, int port) ParseHostPort(string input, int defaultPort)
        {
            var parts = input.Split(':');
            return parts.Length == 2
                ? (parts[0], int.Parse(parts[1]))
                : (input, defaultPort);
        }
        public void Open()
        {
            try
            {
                _conn.Open();
            }
            catch (Exception ex)
            {
                _conn?.Dispose();
                _conn = null;
                throw new Exception($"DB connection failed: {ex.Message}");
            }
        }

        private void EnsureConnection()
        {
            if (_conn.State != System.Data.ConnectionState.Open)
                throw new InvalidOperationException("Connection is not opened");
        }

        public string DbRead(string sql, string separator = "|")
        {
            EnsureConnection();
            var result = new List<string>();

            using (var cmd = new NpgsqlCommand(sql, _conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var row = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        row.Add(reader[i].ToString());

                    result.Add(string.Join(separator, row));
                }
            }
            return string.Join("\r\n", result);
        }
        public int DbWrite(string sql, params NpgsqlParameter[] parameters)
        {
            EnsureConnection();

            using (var cmd = new NpgsqlCommand(sql, _conn))
            {
                try
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteNonQuery();
                }
                catch (NpgsqlException ex)
                {
                    throw new Exception($"SQL Error: {ex.Message} | Query: [{sql}]");
                }
            }
        }

        public static string Raw( string query, bool throwOnEx = false, string host = "localhost:5432", string dbName = "postgres", string dbUser = "postgres", string dbPswd = "")
        {
            if (string.IsNullOrEmpty(dbPswd)) throw new Exception("PostgreSQL password isNull");
            var db = new PostgresDB(host, dbName, dbUser, dbPswd);
            try
            {
                db.Open();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
            try
            {
                var response = "";
                if (query.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                    response = db.DbRead(query);
                else
                    response = db.DbWrite(query).ToString();
                return response;
            }
            catch (Exception ex)
            {  
                return $"{ex.Message}\n[{query}]";
            }
            finally
            {
                db?.Dispose();
            }
        }


        public static string DbQueryPostgre(IZennoPosterProjectModel project, string query, bool throwOnEx = false, string host = "localhost:5432", string dbName = "postgres", string dbUser = "postgres", string dbPswd = "")
        {
            if (string.IsNullOrEmpty(dbName)) dbName = project.Variables["DBpstgrName"].Value;
            if (string.IsNullOrEmpty(dbUser)) dbUser = project.Variables["DBpstgrUser"].Value;

            if (string.IsNullOrEmpty(dbPswd))
            {
                dbPswd = project.Variables["DBpstgrPass"].Value;
                if (string.IsNullOrEmpty(dbPswd)) throw new Exception("PostgreSQL password isNull");
            }

            var db = new PostgresDB(host, dbName, dbUser, dbPswd);
            try
            {
                db.Open();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
            try
            {
                var response = "";
                if (query.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                    response = db.DbRead(query);
                else
                    response = db.DbWrite(query).ToString();
 
                return response;
            }
            catch (Exception ex)
            {
                project.SendWarningToLog(ex.Message);
                return string.Empty;
            }
            finally
            {
                db?.Dispose();
            }
        }
        public static void MkTablePostgre(IZennoPosterProjectModel project, Dictionary<string, string> tableStructure, string tableName = "", bool strictMode = false, bool insertData = true, string host = null, string dbName = "postgres", string dbUser = "postgres", string dbPswd = "", string schemaName = "projects", bool log = false)
        {

            if (string.IsNullOrEmpty(tableName))
                tableName = project.Variables["projectTable"].Value;

            if (string.IsNullOrEmpty(dbPswd))
            {
                dbPswd = project.Variables["DBpstgrPass"].Value ?? throw new Exception("PostgreSQL password is null");
            }

            if (log)
                using (var db = new PostgresDB(host, dbName, dbUser, dbPswd))
                {
                    try
                    {
                        db.Open();
                        CheckAndCreateTable(db, schemaName, tableName, tableStructure, project, log: log);
                        ManageColumns(db, schemaName, tableName, tableStructure, strictMode, project, log: log);

                        if (tableStructure.ContainsKey("acc0") && !string.IsNullOrEmpty(project.Variables["rangeEnd"].Value))
                        {
                            InsertInitialData(db, schemaName, tableName, project.Variables["rangeEnd"].Value, project, log: log);
                        }

                    }
                    catch (Exception ex)
                    {
                        project.SendToLog($"Ошибка выполнения: {ex.Message}", LogType.Warning);
                        throw;
                    }
                }
        }
        private static void CheckAndCreateTable(PostgresDB db, string schemaName, string tableName, Dictionary<string, string> tableStructure, IZennoPosterProjectModel project, bool log = false)
        {
            if (tableName.Contains("."))
            {
                schemaName = tableName.Split('.')[0];
                tableName = tableName.Split('.')[1];
            }
            string query = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{schemaName}' AND table_name = '{tableName}';";

            string tableExists = db.DbRead(query)?.Trim() ?? "0";

            if (tableExists == "0")
            {
                string createQuery = $@" CREATE TABLE {schemaName}.{tableName} ( {string.Join(", ", tableStructure.Select(kvp => $"\"{kvp.Key}\" {kvp.Value.Replace("AUTOINCREMENT", "SERIAL")}"))});";
                db.DbWrite(query);
            }
        }
        private static void ManageColumns(PostgresDB db, string schemaName, string tableName, Dictionary<string, string> tableStructure, bool strictMode, IZennoPosterProjectModel project, bool log = false)
        {
            if (tableName.Contains("."))
            {
                schemaName = tableName.Split('.')[0];
                tableName = tableName.Split('.')[1];
            }

            string query = ""; 
            query = $@"SELECT column_name FROM information_schema.columns WHERE table_schema = '{schemaName}' AND table_name = '{tableName}';";


            var currentColumns = db.DbRead(query).Split('\n').Select(c => c.Split('|')[0]).ToList();

            if (strictMode)
            {
                var columnsToRemove = currentColumns.Where(col => !tableStructure.ContainsKey(col)).ToList();
                foreach (var column in columnsToRemove)
                {
                    query = $@"ALTER TABLE {schemaName}.{tableName} DROP COLUMN {column} CASCADE;";
                    db.DbWrite(query);
                }
            }

            foreach (var column in tableStructure)
            {
                query = $@"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = '{schemaName}' AND table_name = '{tableName}' AND lower(column_name) = lower('{column.Key}');";

                string columnExists = db.DbRead(query);

                if (columnExists == "0")
                {
                    query = $@"ALTER TABLE {schemaName}.{tableName} ADD COLUMN {column.Key} {column.Value};";
                    db.DbWrite(query);
                }
            }
        }
        private static void InsertInitialData(PostgresDB db, string schemaName, string tableName, string cfgRangeEnd, IZennoPosterProjectModel project, bool log = false)
        {
            if (tableName.Contains("."))
            {
                schemaName = tableName.Split('.')[0];
                tableName = tableName.Split('.')[1];
            }
            if (!int.TryParse(cfgRangeEnd, out int rangeEnd) || rangeEnd <= 0)
                throw new ArgumentException("cfgRangeEnd must be a positive integer");

            string maxAcc0Query = $@"SELECT COALESCE(MAX(acc0), 0) FROM {schemaName}.{tableName};";

            string maxAcc0Str = db.DbRead(maxAcc0Query)?.Trim() ?? "0";
            if (!int.TryParse(maxAcc0Str, out int maxAcc0))
            {
                maxAcc0 = 1;
                project.SendWarningToLog($"Failed to parse max acc0, defaulting to 1");
            }

            if (maxAcc0 < rangeEnd)
            {
                for (int currentAcc0 = maxAcc0 + 1; currentAcc0 <= rangeEnd; currentAcc0++)
                {
                    string insertQuery = $@"INSERT INTO {schemaName}.{tableName} (acc0) VALUES ({currentAcc0}) ON CONFLICT DO NOTHING;";

                    db.DbWrite(insertQuery);
                }
            }
            else
            {
                if (log)
                {
                    project.SendInfoToLog($"No new data to insert. Max acc0 ({maxAcc0}) is >= rangeEnd ({rangeEnd})");
                }
            }
        }
   
    
    }
}

