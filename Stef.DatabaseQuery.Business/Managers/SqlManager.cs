using Newtonsoft.Json.Linq;
using Stef.DatabaseQuery.Business.Enumerations;
using Stef.DatabaseQuery.Business.Managers.Databases;
using Stef.DatabaseQuery.Business.Managers.Sqls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Stef.DatabaseQuery.Business.Managers
{
    public class SqlManager
    {
        private static object _Sync = new object();
        private static SqlManager _Instance;

        public SqlManager()
        {
        }

        public static SqlManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_Sync)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new SqlManager();
                        }
                    }
                }

                return _Instance;
            }
        }

        public string CreateSelect(DatabaseInfo databaseInfo, string tableName, string alias = null)
        {
            Func<string, string> getColumnName = (column) =>
            {
                if (string.IsNullOrEmpty(alias))
                    return column;
                else
                    return string.Concat(alias, ".", column);
            };

            var sb = new StringBuilder();
            sb.Append("SELECT ");

            sb.Append(string.Join(
                ", ",
                databaseInfo
                    .SchemaInfo
                    .GetColumns(tableName)
                    .Select(c => getColumnName(databaseInfo.Provider.GetSafeColumnName(c.ColumnName)))));

            sb.Append(" FROM ");
            sb.Append(tableName);

            if (!string.IsNullOrEmpty(alias))
            {
                sb.Append(" ");
                sb.Append(alias);
            }

            return Utils.GetFormattedStatement(sb.ToString(), Enumerations.SqlFormatRule.AllColumnsInOneLine);
        }

        public JObject ExecuteQuery(DatabaseInfo databaseInfo, IDbConnection connection, IDbTransaction transaction, SqlSelectToken sqlSelectToken, int rows, JObject parameterData = null, List<SqlColumnToken> parameterColumns = null)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandTimeout = 600;
                command.Transaction = transaction;
                PrepareCommand(databaseInfo, command, sqlSelectToken.Script, parameterData, parameterColumns);

                var data = new JArray();
                var hasMoreRows = false;

                if (rows < 0)
                    rows = int.MaxValue - 1;
                if (rows == int.MaxValue)
                    rows = int.MaxValue - 1;

                try
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var rowCount = 0;
                        var fieldCount = reader.FieldCount;

                        if (fieldCount != sqlSelectToken.Columns.Count)
                        {
                            sqlSelectToken.Columns.Clear();
                            var dataTable = reader.GetSchemaTable();

                            UpdateSqlSelectTokenColumns(sqlSelectToken, dataTable);
                        }

                        while (reader.Read())
                        {
                            rowCount++;

                            if (rowCount >= rows + 1)
                            {
                                hasMoreRows = true;
                                break;
                            }

                            var obj = new JObject();
                            data.Add(obj);

                            for (var i = 0; i < fieldCount; i++)
                            {
                                var column = sqlSelectToken.Columns[i];
                                var value = databaseInfo.Provider.ConvertFromStorageType(reader.GetValue(i), column.Type);

                                if (value == null)
                                    obj[column.InternalFieldName] = null;
                                else
                                    obj[column.InternalFieldName] = JToken.FromObject(value);
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        string.Concat(ex.Message, Environment.NewLine, Environment.NewLine, command.CommandText),
                        ex);
                }

                var result = new JObject();
                result["data"] = data;
                result["hasMoreRows"] = hasMoreRows;

                return result;
            }
        }
        private void UpdateSqlSelectTokenColumns(SqlSelectToken sqlSelectToken, DataTable dataTable)
        {
            var index = 0;
            foreach (DataRow dataRow in dataTable.Rows)
            {
                var columnName = (string)dataRow["ColumnName"];
                var column = sqlSelectToken.GetColumnInfo(null, columnName);

                sqlSelectToken.Columns.Add(
                    new SqlColumnToken(
                        columnName,
                        null,
                        null,
                        string.Concat("f", index),
                        (Type)dataRow["DataType"],
                        column?.RelatedTableName,
                        column?.RelatedColumnName,
                        true));

                index++;
            }

            sqlSelectToken.RefreshCanUpdate();
        }

        public int ExecuteNonQuery(DatabaseInfo databaseInfo, IDbConnection connection, IDbTransaction transaction, string nonQuery, JObject parameterData = null, List<SqlColumnToken> parameterColumns = null)
        {
            using (var command = connection.CreateCommand())
            {
                try
                {
                    command.CommandTimeout = 600;
                    command.Transaction = transaction;
                    PrepareCommand(databaseInfo, command, nonQuery, parameterData, parameterColumns);

                    return command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        string.Concat(ex.Message, Environment.NewLine, Environment.NewLine, command.CommandText),
                        ex);
                }
            }
        }

        public int SaveData(DatabaseInfo databaseInfo, IDbConnection connection, IDbTransaction transaction, SqlSelectToken sqlSelectToken, JArray data)
        {
            var changedRows = 0;

            foreach (JObject item in data)
            {
                var stateToken = item["_state"];
                if (stateToken.Type != JTokenType.Integer)
                    continue;

                var state = (RowState)(int)stateToken;
                switch (state)
                {
                    case RowState.Modified:
                        changedRows += UpdateRow(databaseInfo, connection, transaction, sqlSelectToken, item);
                        break;
                    case RowState.New:
                        changedRows += InsertRow(databaseInfo, connection, transaction, sqlSelectToken, item);
                        break;
                    case RowState.Deleted:
                        changedRows += DeleteRow(databaseInfo, connection, transaction, sqlSelectToken, item);
                        break;
                    default:
                        break;
                }
            }

            return changedRows;
        }
        private int InsertRow(DatabaseInfo databaseInfo, IDbConnection connection, IDbTransaction transaction, SqlSelectToken sqlSelectToken, JObject data)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;

            var columnBuilder = new StringBuilder();
            var parameterBuilder = new StringBuilder();

            var index = 0;
            foreach (var column in sqlSelectToken.Columns.Union(sqlSelectToken.ColumnSaveItems))
            {
                var parameterValue = data[column.InternalFieldName].ToObject();

                if ((parameterValue == null || parameterValue == DBNull.Value) && sqlSelectToken.Columns.Contains(column))
                    continue;

                if (index > 0)
                {
                    columnBuilder.Append(", ");
                    parameterBuilder.Append(", ");
                }

                columnBuilder.Append(column.ColumnName);

                var parameterName = $"{databaseInfo.Provider.ParameterPrefix}p{index}";
                parameterBuilder.Append(parameterName);

                var parameter = command.CreateParameter();
                parameter.ParameterName = parameterName;
                parameter.Value = databaseInfo.Provider.ConvertToStorageType(parameterValue, column.Type);

                command.Parameters.Add(parameter);

                index++;
            }

            command.CommandText = $"INSERT INTO {sqlSelectToken.TableSave.TableName} ({columnBuilder.ToString()}) VALUES ({parameterBuilder.ToString()})";

            try
            {
                return command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(command.CommandText, ex);
            }
        }
        private int UpdateRow(DatabaseInfo databaseInfo, IDbConnection connection, IDbTransaction transaction, SqlSelectToken sqlSelectToken, JObject data)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;

            var setBuilder = new StringBuilder();
            var whereBuilder = new StringBuilder();

            var parameterIndex = 0;
            var setIndex = 0;
            var whereIndex = 0;

            foreach (var column in sqlSelectToken.ColumnSaveItems)
            {
                if (data["_changed"][column.InternalFieldName]?.Type != JTokenType.Boolean)
                    continue;

                if (setIndex > 0)
                {
                    setBuilder.Append(", ");
                }

                setBuilder.Append(column.ColumnName);
                setBuilder.Append(" = ");

                var parameterName = $"{databaseInfo.Provider.ParameterPrefix}p{parameterIndex}";
                setBuilder.Append(parameterName);

                var parameter = command.CreateParameter();
                parameter.ParameterName = parameterName;

                var value = data[column.InternalFieldName].ToObject();
                parameter.Value = databaseInfo.Provider.ConvertToStorageType(value, column.Type);

                command.Parameters.Add(parameter);

                parameterIndex++;
                setIndex++;
            }

            if (parameterIndex == 0)
                return 0;

            foreach (var column in sqlSelectToken.ColumnKeys)
            {
                if (whereIndex > 0)
                    whereBuilder.Append(" AND ");

                whereBuilder.Append(column.ColumnName);
                whereBuilder.Append(" = ");

                var parameterName = $"{databaseInfo.Provider.ParameterPrefix}p{parameterIndex}";
                whereBuilder.Append(parameterName);

                var parameter = command.CreateParameter();
                parameter.ParameterName = parameterName;

                var value = data[column.InternalFieldName].ToObject();
                parameter.Value = databaseInfo.Provider.ConvertToStorageType(value, column.Type);

                command.Parameters.Add(parameter);

                parameterIndex++;
                whereIndex++;
            }

            command.CommandText = $"UPDATE {sqlSelectToken.TableSave.TableName} SET {setBuilder.ToString()} WHERE {whereBuilder.ToString()}";

            try
            {
                return command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(command.CommandText, ex);
            }
        }
        private int DeleteRow(DatabaseInfo databaseInfo, IDbConnection connection, IDbTransaction transaction, SqlSelectToken sqlSelectToken, JObject data)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;

            var whereBuilder = new StringBuilder();

            var parameterIndex = 0;
            var whereIndex = 0;

            foreach (var column in sqlSelectToken.ColumnKeys)
            {
                if (whereIndex > 0)
                    whereBuilder.Append(" AND ");

                whereBuilder.Append(column.ColumnName);
                whereBuilder.Append(" = ");

                var parameterName = $"{databaseInfo.Provider.ParameterPrefix}p{parameterIndex}";
                whereBuilder.Append(parameterName);

                var parameter = command.CreateParameter();
                parameter.ParameterName = parameterName;

                var value = data[column.InternalFieldName].ToObject();
                parameter.Value = databaseInfo.Provider.ConvertToStorageType(value, column.Type);

                command.Parameters.Add(parameter);

                parameterIndex++;
                whereIndex++;
            }

            command.CommandText = $"DELETE FROM {sqlSelectToken.TableSave.TableName} WHERE {whereBuilder.ToString()}";

            try
            {
                return command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(command.CommandText, ex);
            }
        }

        private void PrepareCommand(DatabaseInfo databaseInfo, IDbCommand command, string script, JObject parameterData, List<SqlColumnToken> parameterColumns)
        {
            if (parameterData != null && parameterColumns != null)
            {
                var parameters = Utils.GetParameters(script);

                var index = 0;
                foreach (var parameter in parameters)
                {
                    var column = parameterColumns.FirstOrDefault(c => c.Caption == parameter);
                    if (column == null)
                        throw new InvalidOperationException($"Spalte {parameter} nicht vorhanden");

                    var parameterName = $"p{index}";
                    var value = databaseInfo.Provider.ConvertToStorageType(parameterData[column.InternalFieldName].ToObject(), column.Type);

                    var commandParameter = command.CreateParameter();
                    commandParameter.ParameterName = parameterName;
                    commandParameter.Value = value;

                    command.Parameters.Add(commandParameter);

                    script = script.Replace($"<#{parameter}#>", string.Concat(databaseInfo.Provider.ParameterPrefix, parameterName));
                }
            }

            command.CommandText = script;
        }
    }
}
