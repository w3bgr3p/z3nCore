using Dapper;
using Microsoft.Data.Sqlite;
using Npgsql;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

using System.Linq;

using System.Threading.Tasks;


namespace z3nCore
{
    public enum DatabaseType
    {
        Unknown,
        SQLite,
        PostgreSQL
    }

    public class dSql : IDisposable
    {
        private readonly IDbConnection _connection;

        private bool _disposed = false;

        public dSql(string dbPath, string dbPass)
        {
            Debug.WriteLine(dbPath);
            _connection = new SqliteConnection($"Data Source={dbPath}");
            _connection.Open();
        }
        public dSql(string hostname, string port, string database, string user, string password)
        {
            _connection = new NpgsqlConnection($"Host={hostname};Port={port};Database={database};Username={user};Password={password};Pooling=true;Connection Idle Lifetime=100;");
            _connection.Open();
        }
        public dSql(string connectionstring)
        {
            _connection = new NpgsqlConnection(connectionstring);
            _connection.Open();
        }
        public dSql(IDbConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));

            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
        }



        public DatabaseType ConnectionType
        {
            get
            {
                if (_connection is SqliteConnection)
                    return DatabaseType.SQLite;
                if (_connection is NpgsqlConnection)
                    return DatabaseType.PostgreSQL;
                return DatabaseType.Unknown;
            }
        }

        private void EnsureConnection()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(dSql));

            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    _connection?.Close();
                }
                catch
                {
                    // Игнорируем ошибки при закрытии
                }
                finally
                {
                    _connection?.Dispose();
                    _disposed = true;
                }
            }
        }

        public IDbDataParameter CreateParameter(string name, object value)
        {
            if (_connection is SqliteConnection)
            {
                return new SqliteParameter(name, value ?? DBNull.Value);
            }
            else if (_connection is NpgsqlConnection)
            {
                return new NpgsqlParameter(name, value ?? DBNull.Value);
            }
            else
            {
                throw new NotSupportedException("Unsupported connection type");
            }
        }
        public IDbDataParameter[] CreateParameters(params (string name, object value)[] parameters)
        {
            var result = new IDbDataParameter[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                result[i] = CreateParameter(parameters[i].name, parameters[i].value);
            }
            return result;
        }

        public async Task<string> DbReadAsync(string sql, string separator = "|")
        {
            EnsureConnection();
            var result = new List<string>();

            if (_connection is SqliteConnection sqliteConn)
            {
                using (var cmd = new SqliteCommand(sql, sqliteConn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                            row.Add(reader[i]?.ToString() ?? "");
                        result.Add(string.Join(separator, row));
                    }
                }
            }
            else if (_connection is NpgsqlConnection npgsqlConn)
            {
                using (var cmd = new NpgsqlCommand(sql, npgsqlConn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                            row.Add(reader[i]?.ToString() ?? "");
                        result.Add(string.Join(separator, row));
                    }
                }
            }
            else
            {
                throw new NotSupportedException("Unsupported connection type");
            }

            return string.Join("\r\n", result);
        }
        public string DbRead(string sql, string separator = "|")
        {
            return DbReadAsync(sql, separator).GetAwaiter().GetResult();
        }

        public async Task<int> DbWriteAsync(string sql, params IDbDataParameter[] parameters)
        {
            EnsureConnection();

            try
            {
                if (_connection is SqliteConnection sqliteConn)
                {
                    using (var cmd = new SqliteCommand(sql, sqliteConn))
                    {
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                                cmd.Parameters.Add(param);
                        }
                        return await cmd.ExecuteNonQueryAsync();
                    }
                }
                else if (_connection is NpgsqlConnection npgsqlConn)
                {
                    using (var cmd = new NpgsqlCommand(sql, npgsqlConn))
                    {
                        if (parameters != null)
                        {
                            foreach (var param in parameters)
                                cmd.Parameters.Add(param);
                        }
                        return await cmd.ExecuteNonQueryAsync();
                    }
                }
                else
                {
                    throw new NotSupportedException("Unsupported connection type");
                }
            }
            catch (Exception ex)
            {
                string formattedQuery = sql;
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        var value = param.Value?.ToString() ?? "NULL";
                        formattedQuery = formattedQuery.Replace(param.ParameterName, $"'{value}'");
                    }
                }

                Debug.WriteLine($"Error: {ex.Message}");
                Debug.WriteLine($"Executed query: {formattedQuery}");
                throw new Exception($"{ex.Message} : [{sql}]");
            }
        }
        public int DbWrite(string sql, params IDbDataParameter[] parameters)
        {
            return DbWriteAsync(sql, parameters).GetAwaiter().GetResult();
        }


        public async Task<int> CopyTableAsync(string sourceTable, string destinationTable)
        {
            if (string.IsNullOrEmpty(sourceTable)) throw new ArgumentNullException(nameof(sourceTable));
            if (string.IsNullOrEmpty(destinationTable)) throw new ArgumentNullException(nameof(destinationTable));

            string sourceTableName = sourceTable;
            string destinationTableName = destinationTable;
            string sourceSchema = "public";
            string destinationSchema = "public";

            if (ConnectionType == DatabaseType.PostgreSQL)
            {
                if (sourceTable.Contains("."))
                {
                    var parts = sourceTable.Split('.');
                    if (parts.Length != 2) throw new ArgumentException("Invalid source table format. Expected 'schema.table'.");
                    sourceSchema = parts[0];
                    sourceTableName = parts[1];
                }
                if (destinationTable.Contains("."))
                {
                    var parts = destinationTable.Split('.');
                    if (parts.Length != 2) throw new ArgumentException("Invalid destination table format. Expected 'schema.table'.");
                    destinationSchema = parts[0];
                    destinationTableName = parts[1];
                }
            }
            else if (sourceTable.Contains(".") || destinationTable.Contains("."))
            {
                throw new ArgumentException("Schemas are not supported in SQLite.");
            }

            sourceTableName = QuoteName(sourceTableName);
            destinationTableName = QuoteName(destinationTableName);
            sourceSchema = QuoteName(sourceSchema);
            destinationSchema = QuoteName(destinationSchema);

            string fullSourceTable = ConnectionType == DatabaseType.PostgreSQL ? $"{sourceSchema}.{sourceTableName}" : sourceTableName;
            string fullDestinationTable = ConnectionType == DatabaseType.PostgreSQL ? $"{destinationSchema}.{destinationTableName}" : destinationTableName;

            string createTableQuery;
            string primaryKeyConstraint = "";
            if (ConnectionType == DatabaseType.PostgreSQL)
            {
                string schemaQuery = $@"
                    SELECT column_name, data_type, is_nullable, column_default
                    FROM information_schema.columns
                    WHERE table_name = '{sourceTableName.Replace("\"", "")}' AND table_schema = '{sourceSchema.Replace("\"", "")}';";
                //string columnsDef = await DbReadAsync(schemaQuery);
                string columnsDef = null;
                try
                {
                    columnsDef = await DbReadAsync(schemaQuery);
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.Message} : [{schemaQuery}]");
                }


                if (string.IsNullOrEmpty(columnsDef))
                    throw new Exception($"Source table {fullSourceTable} does not exist");

                var columns = columnsDef.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(row => row.Split('|'))
                    .Select(parts => $"\"{parts[0]}\" {parts[1]} {(parts[2] == "NO" ? "NOT NULL" : "")} {(parts[3] != "" ? $"DEFAULT {parts[3]}" : "")}")
                    .ToList();

                //string pkQuery = $@"
                //    SELECT pg_constraint.conname, pg_get_constraintdef(pg_constraint.oid)
                //    FROM pg_constraint
                //    JOIN pg_class ON pg_constraint.conrelid = pg_class.oid
                //    WHERE pg_class.relname = '{sourceTableName.Replace("\"", "")}' AND pg_constraint.contype = 'p' AND pg_namespace.nspname = '{sourceSchema.Replace("\"", "")}'
                //    JOIN pg_namespace ON pg_class.relnamespace = pg_namespace.oid;";
                string pkQuery = $@"
                    SELECT pg_constraint.conname, pg_get_constraintdef(pg_constraint.oid)
                    FROM pg_constraint
                    JOIN pg_class ON pg_constraint.conrelid = pg_class.oid
                    JOIN pg_namespace ON pg_class.relnamespace = pg_namespace.oid
                    WHERE pg_class.relname = '{sourceTableName.Replace("\"", "")}' AND pg_constraint.contype = 'p' AND pg_namespace.nspname = '{sourceSchema.Replace("\"", "")}';";

                string pkResult = null;
                try
                {
                    pkResult = await DbReadAsync(pkQuery);
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.Message} : [{pkQuery}]");
                }
                if (!string.IsNullOrEmpty(pkResult))
                {
                    var pkParts = pkResult.Split('|');
                    primaryKeyConstraint = $", CONSTRAINT \"{destinationTableName.Replace("\"", "")}_pkey\" {pkParts[1]}";
                    //primaryKeyConstraint = $", CONSTRAINT \"{pkParts[0]}\" {pkParts[1]}";
                }

                createTableQuery = $"CREATE TABLE {fullDestinationTable} ({string.Join(", ", columns)}{primaryKeyConstraint});";
            }
            else
            {
                string schemaQuery = $"PRAGMA table_info({sourceTableName});";
                string columnsDef = null;
                try
                {
                    columnsDef = await DbReadAsync(schemaQuery);
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.Message} : [{schemaQuery}]");
                }
                if (string.IsNullOrEmpty(columnsDef))
                    throw new Exception($"Source table {sourceTableName} does not exist");

                var columns = columnsDef.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(row => row.Split('|'))
                    .Select(parts => $"\"{parts[1]}\" {parts[2]} {(parts[3] == "1" ? "NOT NULL" : "")} {(parts[4] != "" ? $"DEFAULT {parts[4]}" : "")}")
                    .ToList();

                string pkQuery = $"PRAGMA table_info({sourceTableName});";
                string pkResult = await DbReadAsync(pkQuery);
                var pkColumns = pkResult.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(row => row.Split('|'))
                    .Where(parts => parts[5] == "1")
                    .Select(parts => $"\"{parts[1]}\"")
                    .ToList();
                if (pkColumns.Any())
                {
                    primaryKeyConstraint = $", PRIMARY KEY ({string.Join(", ", pkColumns)})";
                }

                createTableQuery = $"CREATE TABLE {destinationTableName} ({string.Join(", ", columns)}{primaryKeyConstraint});";
            }

            try
            {
                await DbWriteAsync(createTableQuery);
            }
            catch (Exception ex) 
            {
                throw new Exception($"{ex.Message} : [{createTableQuery}]");
            }

            string copyQuery = $"INSERT INTO {fullDestinationTable} SELECT * FROM {fullSourceTable};";
            try
            {
                int rowsAffected = await DbWriteAsync(copyQuery);
                return rowsAffected;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message} : [{copyQuery}]");
            }
            
        }

        
        public static async Task<int> MigrateAllTablesAsync(dSql sourceDb, dSql destinationDb)
        {
            if (sourceDb == null) throw new ArgumentNullException(nameof(sourceDb));
            if (destinationDb == null) throw new ArgumentNullException(nameof(destinationDb));
            if (sourceDb.ConnectionType == destinationDb.ConnectionType) throw new ArgumentException("Source and destination must be different database types.");

            // Получить список несистемных таблиц из источника
            string tablesQuery;
            List<string> tables = new List<string>();
            if (sourceDb.ConnectionType == DatabaseType.PostgreSQL)
            {
                tablesQuery = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE' AND table_name NOT LIKE 'pg_%';";
                string tablesResult = await sourceDb.DbReadAsync(tablesQuery);
                tables = tablesResult.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else if (sourceDb.ConnectionType == DatabaseType.SQLite)
            {
                tablesQuery = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';";
                string tablesResult = await sourceDb.DbReadAsync(tablesQuery);
                tables = tablesResult.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else
            {
                throw new NotSupportedException("Unsupported source database type.");
            }

            int totalRows = 0;
            foreach (var tableName in tables)
            {
                // Получить схему таблицы из источника
                string createTableQuery = await GetCreateTableQueryAsync(sourceDb, tableName);

                // Создать таблицу в destination
                await destinationDb.DbWriteAsync(createTableQuery);

                // Копировать данные
                string selectQuery = $"SELECT * FROM \"{tableName}\";";
                string dataResult = await sourceDb.DbReadAsync(selectQuery, separator: "|");

                if (!string.IsNullOrEmpty(dataResult))
                {
                    var rows = dataResult.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var row in rows)
                    {
                        var values = row.Split('|').Select(v => string.IsNullOrEmpty(v) ? "NULL" : $"'{v.Replace("'", "''")}'").ToArray();
                        string insertQuery = $"INSERT INTO \"{tableName}\" VALUES ({string.Join(", ", values)});";
                        await destinationDb.DbWriteAsync(insertQuery);
                    }
                    totalRows += rows.Length;
                }
            }
            return totalRows;
        }
        private static async Task<string> GetCreateTableQueryAsync(dSql db, string tableName)
        {
            string columnsDef;
            string primaryKeyConstraint = "";
            if (db.ConnectionType == DatabaseType.PostgreSQL)
            {
                string schemaQuery = $"SELECT column_name, data_type, is_nullable, column_default FROM information_schema.columns WHERE table_name = '{tableName}' AND table_schema = 'public';";
                columnsDef = await db.DbReadAsync(schemaQuery);
                var columns = columnsDef.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(row => row.Split('|'))
                    .Select(parts => $"\"{parts[0]}\" {parts[1]} {(parts[2] == "NO" ? "NOT NULL" : "")} {(parts[3] != "" ? $"DEFAULT {parts[3]}" : "")}")
                    .ToList();

                string pkQuery = $"SELECT pg_get_constraintdef(pg_constraint.oid) FROM pg_constraint JOIN pg_class ON pg_constraint.conrelid = pg_class.oid JOIN pg_namespace ON pg_class.relnamespace = pg_namespace.oid WHERE pg_class.relname = '{tableName}' AND pg_constraint.contype = 'p' AND pg_namespace.nspname = 'public';";
                string pkResult = await db.DbReadAsync(pkQuery);
                if (!string.IsNullOrEmpty(pkResult))
                {
                    primaryKeyConstraint = $", {pkResult}";
                }
                return $"CREATE TABLE \"{tableName}\" ({string.Join(", ", columns)}{primaryKeyConstraint});";
            }
            else if (db.ConnectionType == DatabaseType.SQLite)
            {
                string schemaQuery = $"PRAGMA table_info(\"{tableName}\");";
                columnsDef = await db.DbReadAsync(schemaQuery);
                var columns = columnsDef.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(row => row.Split('|'))
                    .Select(parts => $"\"{parts[1]}\" {parts[2]} {(parts[3] == "1" ? "NOT NULL" : "")} {(parts[4] != "" ? $"DEFAULT {parts[4]}" : "")}")
                    .ToList();

                string pkResult = await db.DbReadAsync(schemaQuery);
                var pkColumns = pkResult.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(row => row.Split('|'))
                    .Where(parts => parts[5] == "1")
                    .Select(parts => $"\"{parts[1]}\"")
                    .ToList();
                if (pkColumns.Any())
                {
                    primaryKeyConstraint = $", PRIMARY KEY ({string.Join(", ", pkColumns)})";
                }
                return $"CREATE TABLE \"{tableName}\" ({string.Join(", ", columns)}{primaryKeyConstraint});";
            }
            throw new NotSupportedException("Unsupported database type.");
        }
        
        
        
        public async Task<int> Upd(string toUpd, object id, string tableName = null, string where = null, bool last = false)
        {
            var parameters = new DynamicParameters();
            if (tableName == null) throw new Exception("TableName is null");

            toUpd = QuoteName(toUpd, true);
            tableName = QuoteName(tableName);

            if (last)
            {
                toUpd += ", last = @lastTime";
                parameters.Add("lastTime", DateTime.UtcNow.ToString("MM-ddTHH:mm"));
            }

            string query;
            if (string.IsNullOrEmpty(where))
            {
                query = $"UPDATE {tableName} SET {toUpd} WHERE id = {id}";
            }
            else
            {
                query = $"UPDATE {tableName} SET {toUpd} WHERE {where}";
            }

            try
            {
                return await _connection.ExecuteAsync(query, parameters, commandType: System.Data.CommandType.Text);
            }
            catch (Exception ex)
            {
                string formattedQuery = query;
                foreach (var param in parameters.ParameterNames)
                {
                    var value = parameters.Get<dynamic>(param)?.ToString() ?? "NULL";
                    formattedQuery = formattedQuery.Replace($"@{param}", $"'{value}'");
                }
                throw new Exception ($"{ex.Message} : [{formattedQuery}]");
            }
        }
        public async Task Upd(List<string> toWrite, string tableName = null, string where = null, bool last = false)
        {
            int id = 0;
            foreach (var item in toWrite)
            {
                await Upd(item, id, tableName, where, last);
                id++;
            }
        }


        public async Task<string> Get(string toGet, string id, string tableName = null, string where = null)
        {
            var parameters = new DynamicParameters();
            if (tableName == null) throw new Exception("TableName is null");

            toGet = QuoteName(toGet, true);
            tableName = QuoteName(tableName);



            string query;
            if (string.IsNullOrEmpty(where))
            {
                query = $"SELECT {toGet} FROM {tableName} WHERE id = @id";
                parameters.Add("id", id);
            }
            else
            {
                query = $"SELECT {toGet} FROM {tableName} WHERE {where}";
            }

            try
            {
                return await _connection.ExecuteScalarAsync<string>(query, parameters, commandType: System.Data.CommandType.Text);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }

        public async Task AddRange(int range, string tableName = null)
        {
            if (tableName == null) throw new Exception("TableName is null");

            string query = $@"SELECT COALESCE(MAX(id), 0) FROM {tableName};";

            int current = await _connection.ExecuteScalarAsync<int>(query, commandType: System.Data.CommandType.Text);

            for (int currentId = current + 1; currentId <= range; currentId++)
            {
                await _connection.ExecuteAsync($@"INSERT INTO {tableName} (id) VALUES ({currentId}) ON CONFLICT DO NOTHING;");
            }
        }

        private string QuoteName(string name, bool isColumnList = false)
        {
            if (isColumnList)
            {
                name = name.Trim().TrimEnd(',');
                var parts = name.Split(',').Select(p => p.Trim()).ToList();
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
                        result.Add($"\"{part}\"");
                    }
                }
                return string.Join(", ", result);
            }
            return $"\"{name.Replace("\"", "\"\"")}\"";
        }

    }



}
