
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
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

    public class Sql : IDisposable
    {
        private readonly IDbConnection _connection;
        private bool _disposed = false;

        public Sql(string dbPath, string dbPass)
        {
            Debug.WriteLine(dbPath);
            _connection = new OdbcConnection($"Driver={{SQLite3 ODBC Driver}};Database={dbPath}");
            _connection.Open();
        }

        public Sql(string hostname, string port, string database, string user, string password)
        {
            _connection = new NpgsqlConnection($"Host={hostname};Port={port};Database={database};Username={user};Password={password};Pooling=true;Connection Idle Lifetime=100;");
            _connection.Open();
        }

        public Sql(string connectionstring)
        {
            _connection = new NpgsqlConnection(connectionstring);
            _connection.Open();
        }

        public Sql(IDbConnection connection)
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
                if (_connection is OdbcConnection)
                    return DatabaseType.SQLite;
                if (_connection is NpgsqlConnection)
                    return DatabaseType.PostgreSQL;
                return DatabaseType.Unknown;
            }
        }

        private void EnsureConnection()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Sql));

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
                catch { }
                finally
                {
                    _connection?.Dispose();
                    _disposed = true;
                }
            }
        }

        public IDbDataParameter CreateParameter(string name, object value)
        {
            if (_connection is OdbcConnection)
            {
                return new OdbcParameter(name, value ?? DBNull.Value);
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

        public async Task<string> DbReadAsync(string sql, string columnSeparator = "|", string rawSepararor = "\r\n")
        {
            EnsureConnection();
            var result = new List<string>();

            if (_connection is OdbcConnection odbcConn)
            {
                using (var cmd = new OdbcCommand(sql, odbcConn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                            row.Add(reader[i]?.ToString() ?? "");
                        result.Add(string.Join(columnSeparator, row));
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
                        result.Add(string.Join(columnSeparator, row));
                    }
                }
            }
            else
            {
                throw new NotSupportedException("Unsupported connection type");
            }
            return string.Join(rawSepararor, result);
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
                if (_connection is OdbcConnection odbcConn)
                {
                    using (var cmd = new OdbcCommand(sql, odbcConn))
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
        public static async Task<int> MigrateAllTablesAsync(Sql sourceDb, Sql destinationDb)
        {
            string columnSeparator = "|";
            string rowSeparator = "░";
            
            if (sourceDb == null) throw new ArgumentNullException(nameof(sourceDb));
            if (destinationDb == null) throw new ArgumentNullException(nameof(destinationDb));
            if (sourceDb.ConnectionType == destinationDb.ConnectionType) throw new ArgumentException("Source and destination must be different database types.");

            // Получить список несистемных таблиц из источника
            string tablesQuery;
            List<string> tables = new List<string>();
            if (sourceDb.ConnectionType == DatabaseType.PostgreSQL)
            {
                tablesQuery = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE' AND table_name NOT LIKE 'pg_%';";
                string tablesResult = await sourceDb.DbReadAsync(tablesQuery, columnSeparator, rowSeparator);
                tables = tablesResult.Split(new[] { rowSeparator }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else if (sourceDb.ConnectionType == DatabaseType.SQLite)
            {
                tablesQuery = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';";
                string tablesResult = await sourceDb.DbReadAsync(tablesQuery, columnSeparator, rowSeparator);
                tables = tablesResult.Split(new[] { rowSeparator }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else
            {
                throw new NotSupportedException("Unsupported source database type.");
            }

            int totalRows = 0;
            foreach (var tableName in tables)
            {
                try
                {
                    // Получить схему таблицы и создать таблицу в целевой базе
                    string createTableQuery = "";
                    string columnsDef;
                    List<string> columnNames;
                    bool tableExists = false;

                    // Проверить, существует ли таблица в целевой базе
                    if (destinationDb.ConnectionType == DatabaseType.SQLite)
                    {
                        string checkTableQuery = $"SELECT name FROM sqlite_master WHERE type = 'table' AND name = '{tableName}';";
                        string checkResult = await destinationDb.DbReadAsync(checkTableQuery, columnSeparator, rowSeparator);
                        if (!string.IsNullOrEmpty(checkResult))
                        {
                            System.Diagnostics.Debug.WriteLine($"Table '{tableName}' already exists in destination database. Skipping creation.");
                            tableExists = true;
                        }
                    }
                    else if (destinationDb.ConnectionType == DatabaseType.PostgreSQL)
                    {
                        string checkTableQuery = $"SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '{tableName}';";
                        string checkResult = await destinationDb.DbReadAsync(checkTableQuery, columnSeparator, rowSeparator);
                        if (!string.IsNullOrEmpty(checkResult))
                        {
                            System.Diagnostics.Debug.WriteLine($"Table '{tableName}' already exists in destination database. Skipping creation.");
                            tableExists = true;
                        }
                    }

                    if (!tableExists)
                    {
                        if (sourceDb.ConnectionType == DatabaseType.PostgreSQL)
                        {
                            // Получить схему таблицы из PostgreSQL
                            string schemaQuery = $"SELECT column_name, data_type, is_nullable, column_default FROM information_schema.columns WHERE table_name = '{tableName}' AND table_schema = 'public';";
                            columnsDef = await sourceDb.DbReadAsync(schemaQuery, columnSeparator, rowSeparator);
                            var columnsData = columnsDef.Split(new[] { rowSeparator }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(row => row.Split(new[] { columnSeparator }, StringSplitOptions.None))
                                .ToList();

                            // Проверка на дубликаты имен столбцов с учетом нечувствительности к регистру
                            var columnNamesLower = columnsData.Select(parts => parts[0].ToLower()).ToList();
                            if (columnNamesLower.Distinct().Count() != columnNamesLower.Count)
                            {
                                var duplicates = columnNamesLower.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key);
                                var duplicateColumns = columnsData.Where(parts => duplicates.Contains(parts[0].ToLower())).Select(parts => parts[0]);
                                System.Diagnostics.Debug.WriteLine($"Duplicate column names (case-insensitive) found in table '{tableName}': {string.Join(", ", duplicateColumns)}. Skipping table.");
                                throw new InvalidOperationException($"Duplicate column names (case-insensitive) found in table '{tableName}': {string.Join(", ", duplicateColumns)}");
                            }

                            var columns = columnsData.Select(parts =>
                            {
                                string dataType = parts[1];
                                // Приведение типов PostgreSQL к SQLite
                                if (dataType == "bigint") dataType = "INTEGER";
                                if (dataType == "text") dataType = "TEXT";
                                
                                string defaultValue = parts[3];
                                if (!string.IsNullOrEmpty(defaultValue))
                                {
                                    // Удаляем PostgreSQL-специфичные конструкции
                                    defaultValue = defaultValue.Replace("::text", "").Replace("::bigint", "");
                                    // Удаляем кавычки для числовых значений
                                    if (long.TryParse(defaultValue.Trim('\''), out _))
                                    {
                                        defaultValue = defaultValue.Trim('\'');
                                    }
                                    // Для строковых значений сохраняем или добавляем кавычки
                                    else if (!defaultValue.StartsWith("'"))
                                    {
                                        defaultValue = $"'{defaultValue}'";
                                    }
                                }
                                return $"\"{parts[0]}\" {dataType} {(parts[2] == "NO" ? "NOT NULL" : "")} {(defaultValue != "" ? $"DEFAULT {defaultValue}" : "")}";
                            }).ToList();

                            // Получить первичный ключ
                            string primaryKeyConstraint = "";
                            string pkQuery = $"SELECT pg_get_constraintdef(pg_constraint.oid) FROM pg_constraint JOIN pg_class ON pg_constraint.conrelid = pg_class.oid JOIN pg_namespace ON pg_class.relnamespace = pg_namespace.oid WHERE pg_class.relname = '{tableName}' AND pg_constraint.contype = 'p' AND pg_namespace.nspname = 'public';";
                            string pkResult = await sourceDb.DbReadAsync(pkQuery, columnSeparator, rowSeparator);
                            if (!string.IsNullOrEmpty(pkResult))
                            {
                                // Очистка PostgreSQL-специфичных конструкций
                                pkResult = pkResult.Replace("::bigint", "").Replace("::text", "");
                                primaryKeyConstraint = $", {pkResult}";
                            }

                            createTableQuery = $"CREATE TABLE \"{tableName}\" ({string.Join(", ", columns)}{primaryKeyConstraint});";
                            await destinationDb.DbWriteAsync(createTableQuery);
                        }
                        else if (sourceDb.ConnectionType == DatabaseType.SQLite)
                        {
                            // Получить схему таблицы из SQLite
                            string schemaQuery = $"PRAGMA table_info(\"{tableName}\");";
                            columnsDef = await sourceDb.DbReadAsync(schemaQuery, columnSeparator, rowSeparator);
                            var columnsData = columnsDef.Split(new[] { rowSeparator }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(row => row.Split(new[] { columnSeparator }, StringSplitOptions.None))
                                .ToList();

                            // Проверка на дубликаты имен столбцов с учетом нечувствительности к регистру
                            var columnNamesLower = columnsData.Select(parts => parts[1].ToLower()).ToList();
                            if (columnNamesLower.Distinct().Count() != columnNamesLower.Count)
                            {
                                var duplicates = columnNamesLower.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key);
                                var duplicateColumns = columnsData.Where(parts => duplicates.Contains(parts[1].ToLower())).Select(parts => parts[1]);
                                System.Diagnostics.Debug.WriteLine($"Duplicate column names (case-insensitive) found in table '{tableName}': {string.Join(", ", duplicateColumns)}. Skipping table.");
                                throw new InvalidOperationException($"Duplicate column names (case-insensitive) found in table '{tableName}': {string.Join(", ", duplicateColumns)}");
                            }

                            var columns = columnsData.Select(parts => $"\"{parts[1]}\" {parts[2]} {(parts[3] == "1" ? "NOT NULL" : "")} {(parts[4] != "" ? $"DEFAULT {parts[4]}" : "")}")
                                .ToList();

                            // Получить первичный ключ
                            string primaryKeyConstraint = "";
                            string pkResult = await sourceDb.DbReadAsync(schemaQuery, columnSeparator, rowSeparator);
                            var pkColumns = pkResult.Split(new[] { rowSeparator }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(row => row.Split(new[] { columnSeparator }, StringSplitOptions.None))
                                .Where(parts => parts[5] == "1")
                                .Select(parts => $"\"{parts[1]}\"")
                                .ToList();
                            if (pkColumns.Any())
                            {
                                primaryKeyConstraint = $", PRIMARY KEY ({string.Join(", ", pkColumns)})";
                            }

                            createTableQuery = $"CREATE TABLE \"{tableName}\" ({string.Join(", ", columns)}{primaryKeyConstraint});";
                            await destinationDb.DbWriteAsync(createTableQuery);
                        }
                        else
                        {
                            throw new NotSupportedException("Unsupported database type.");
                        }
                    }

                    // Получить список столбцов для точного соответствия
                    if (sourceDb.ConnectionType == DatabaseType.PostgreSQL)
                    {
                        string columnQuery = $"SELECT column_name FROM information_schema.columns WHERE table_name = '{tableName}' AND table_schema = 'public';";
                        string columnResult = await sourceDb.DbReadAsync(columnQuery, columnSeparator, rowSeparator);
                        columnNames = columnResult.Split(new[] { rowSeparator }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    }
                    else
                    {
                        string columnQuery = $"PRAGMA table_info(\"{tableName}\");";
                        string columnResult = await sourceDb.DbReadAsync(columnQuery, columnSeparator, rowSeparator);
                        columnNames = columnResult.Split(new[] { rowSeparator }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(row => row.Split(new[] { columnSeparator }, StringSplitOptions.None)[1])
                            .ToList();
                    }

                    // Проверка на дубликаты имен столбцов для INSERT
                    var columnNamesLowerForInsert = columnNames.Select(c => c.ToLower()).ToList();
                    if (columnNamesLowerForInsert.Distinct().Count() != columnNamesLowerForInsert.Count)
                    {
                        var duplicates = columnNamesLowerForInsert.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key);
                        var duplicateColumns = columnNames.Where(c => duplicates.Contains(c.ToLower()));
                        System.Diagnostics.Debug.WriteLine($"Duplicate column names (case-insensitive) found in table '{tableName}' for INSERT: {string.Join(", ", duplicateColumns)}. Skipping table.");
                        throw new InvalidOperationException($"Duplicate column names (case-insensitive) found in table '{tableName}' for INSERT: {string.Join(", ", duplicateColumns)}");
                    }

                    // Копировать данные, явно указывая столбцы
                    string selectQuery = $"SELECT {string.Join(", ", columnNames.Select(c => $"\"{c}\""))} FROM \"{tableName}\";";
                    string dataResult = await sourceDb.DbReadAsync(selectQuery, columnSeparator, rowSeparator);

                    if (!string.IsNullOrEmpty(dataResult))
                    {
                        var rows = dataResult.Split(new[] { rowSeparator }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var row in rows)
                        {
                            var values = row.Split(new[] { columnSeparator }, StringSplitOptions.None)
                                .Take(columnNames.Count) // Ограничить количеством столбцов
                                .Select(v => string.IsNullOrEmpty(v) ? "NULL" : $"'{v.Replace("'", "''")}'")
                                .ToArray();
                            string insertQuery = $"INSERT INTO \"{tableName}\" ({string.Join(", ", columnNames.Select(c => $"\"{c}\""))}) VALUES ({string.Join(", ", values)});";
                            try
                            {
                                await destinationDb.DbWriteAsync(insertQuery);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error inserting row into table '{tableName}': {ex.Message} : Prev row: [{row}] : Prev result: [{dataResult}] : [{insertQuery}]");
                                throw new Exception($"Error inserting row into table '{tableName}': {ex.Message} : Prev row: [{row}] : Prev result: [{dataResult}] : [{insertQuery}]");
                            }
                        }
                        totalRows += rows.Length;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to migrate table '{tableName}': {ex.Message}");
                    continue; // Продолжить со следующей таблицей
                }
            }
            return totalRows;
        }
        
        public async Task<int> Upd(string toUpd, object id, string tableName = null, string where = null, bool last = false)
        {
            //var parameters = new DynamicParameters();
            var parameters = new List<IDbDataParameter>();
            if (tableName == null) throw new Exception("TableName is null");

            toUpd = QuoteName(toUpd, true);
            tableName = QuoteName(tableName);

            if (last)
            {
                toUpd += ", last = @lastTime";
                //parameters.Add("lastTime", DateTime.UtcNow.ToString("MM-ddTHH:mm"));
                parameters.Add(CreateParameter("@lastTime", DateTime.UtcNow.ToString("MM-ddTHH:mm")));
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
                //return await _connection.ExecuteAsync(query, parameters, commandType: System.Data.CommandType.Text);
                return await DbWriteAsync(query, parameters.ToArray());
            }
            catch (Exception ex)
            {
                string formattedQuery = query;
                foreach (var param in parameters)
                {
                    var value = param.Value?.ToString() ?? "NULL";
                    formattedQuery = formattedQuery.Replace(param.ParameterName, $"'{value}'");
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
            var parameters = new List<IDbDataParameter>();
            if (tableName == null) throw new Exception("TableName is null");

            toGet = QuoteName(toGet, true);
            tableName = QuoteName(tableName);



            string query;
            if (string.IsNullOrEmpty(where))
            {
                query = $"SELECT {toGet} FROM {tableName} WHERE id = @id";
                parameters.Add(CreateParameter("@id", id));
            }
            else
            {
                query = $"SELECT {toGet} FROM {tableName} WHERE {where}";
            }

            try
            {
                EnsureConnection();
                if (_connection is OdbcConnection odbcConn)
                {
                    using (var cmd = new OdbcCommand(query, odbcConn))
                    {
                        foreach (var param in parameters)
                            cmd.Parameters.Add(param);
                        var result = await cmd.ExecuteScalarAsync();
                        return result?.ToString();
                    }
                }
                else if (_connection is NpgsqlConnection npgsqlConn)
                {
                    using (var cmd = new NpgsqlCommand(query, npgsqlConn))
                    {
                        foreach (var param in parameters)
                            cmd.Parameters.Add(param);
                        var result = await cmd.ExecuteScalarAsync();
                        return result?.ToString();
                    }
                }
                else
                {
                    throw new NotSupportedException("Unsupported connection type");
                }
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

            EnsureConnection();
            int current = 0;
    
            if (_connection is OdbcConnection odbcConn)
            {
                using (var cmd = new OdbcCommand(query, odbcConn))
                {
                    var result = await cmd.ExecuteScalarAsync();
                    current = Convert.ToInt32(result);
                }
            }
            else if (_connection is NpgsqlConnection npgsqlConn)
            {
                using (var cmd = new NpgsqlCommand(query, npgsqlConn))
                {
                    var result = await cmd.ExecuteScalarAsync();
                    current = Convert.ToInt32(result);
                }
            }

            for (int currentId = current + 1; currentId <= range; currentId++)
            {
                await DbWriteAsync($@"INSERT INTO {tableName} (id) VALUES ({currentId}) ON CONFLICT DO NOTHING;");
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