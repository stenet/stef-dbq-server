using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stef.DatabaseQuery.Business;
using Stef.DatabaseQuery.Business.Enumerations;
using Stef.DatabaseQuery.Business.Managers;
using Stef.DatabaseQuery.Business.Managers.Sqls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Stef.DatabaseQuery.Controllers
{
    public class SqlController : ApiController
    {
        [HttpPost]
        public object Execute(JObject data)
        {
            try
            {
                return ExecuteInternal(data);
            }
            catch (Exception ex)
            {
                return new
                {
                    exception = ex.Message
                };
            }
        }
        private object ExecuteInternal(JObject data)
        {
            var tableData = TableData.FromJObject(data);
            var databaseInfo = DatabaseManager.Instance.GetDatabaseInfo(tableData.DatabaseId);

            if (string.IsNullOrEmpty(tableData.Script) && !string.IsNullOrEmpty(tableData.TableName))
            {
                tableData.Script = SqlManager.Instance.CreateSelect(databaseInfo, tableData.TableName, tableData.Alias);

                if (!string.IsNullOrEmpty(tableData.ColumnName) && tableData.Value != null)
                {
                    tableData.Script += string.Concat(" WHERE ", tableData.ColumnName, " = ");

                    if (tableData.Value is string)
                        tableData.Script += string.Concat("'", tableData.Value, "'");
                    else if (tableData.Value is DateTime)
                        tableData.Script += string.Concat("'", ((DateTime)tableData.Value).ToString("dd.MM.yyyy"), "'");
                    else
                        tableData.Script += tableData.Value.ToString();
                }
            }

            var scriptToken = ScriptManager.Instance.GetScriptToken(tableData.Script, 0, 0);

            switch (scriptToken.ScriptType)
            {
                case ScriptType.Query:
                    {
                        var sqlSelectToken = new SqlSelectToken(databaseInfo, tableData.Script);

                        using (var connection = databaseInfo.CreateConnection())
                        {
                            JObject result;
                            if (tableData.Data != null && tableData.ReferencedTableData != null)
                            {
                                JArray resultList = new JArray();

                                foreach (JObject item in tableData.Data)
                                {
                                    var queryResult = (JArray)SqlManager.Instance.ExecuteQuery(
                                        databaseInfo,
                                        connection,
                                        null,
                                        sqlSelectToken,
                                        int.MaxValue,
                                        item,
                                        tableData.ReferencedTableData.Columns)["data"];

                                    queryResult.ToList().ForEach(c => resultList.Add(c));
                                }

                                result = new JObject();
                                result["data"] = resultList;
                                result["hasMoreRows"] = false;
                            }
                            else
                            {
                                result = SqlManager.Instance.ExecuteQuery(databaseInfo, connection, null, sqlSelectToken, tableData.Rows);
                            }

                            return new
                            {
                                tableData = new
                                {
                                    tableId = tableData.TableId,
                                    databaseId = tableData.DatabaseId,
                                    rows = tableData.Rows,
                                    script = tableData.Script,
                                    columns = sqlSelectToken.ColumnList,
                                    tables = string.Join(", ", sqlSelectToken.TableList.Select(c => c.TableName)),
                                    changeData = tableData.ChangeData,
                                    referencedTableData = tableData.ReferencedTableData
                                },
                                columnsSave = sqlSelectToken.ColumnSaveList,
                                result = result
                            };
                        }
                    }
                case ScriptType.NonQuery:
                    {
                        var connection = databaseInfo.CreateConnection();
                        var transaction = connection.BeginTransaction();

                        var changedRows = SqlManager.Instance.ExecuteNonQuery(databaseInfo, connection, transaction, tableData.Script);

                        if (changedRows > 0)
                        {
                            return new
                            {
                                changedRows = changedRows,
                                transactionId = TransactionManager.Instance.KeepTransaction(transaction, null)
                            };
                        }
                        else
                        {
                            return null;
                        }
                    }
                default:
                    return null;
            }
        }

        [HttpPost]
        public object ExecuteChanges(JObject data)
        {
            try
            {
                return ExecuteChangesInternal(data);
            }
            catch (Exception ex)
            {
                return new
                {
                    exception = ex.Message
                };
            }
        }
        private object ExecuteChangesInternal(JObject data)
        {
            var tableData = TableData.FromJObject(data);
            return ChangeDataManager.Instance.ChangeData(tableData.Data, tableData.Columns, tableData.ChangeData);
        }

        public object Save(JObject data)
        {
            var tableData = TableData.FromJObject(data);
            var databaseInfo = DatabaseManager.Instance.GetDatabaseInfo(tableData.DatabaseId);
            var scriptToken = ScriptManager.Instance.GetScriptToken(tableData.Script, 0, 0);

            if (scriptToken.ScriptType != ScriptType.Query)
                throw new InvalidOperationException();

            var sqlSelectToken = new SqlSelectToken(databaseInfo, tableData.Script);
            var connection = databaseInfo.CreateConnection();
            var transaction = connection.BeginTransaction();

            var changedRows = SqlManager.Instance.SaveData(databaseInfo, connection, transaction, sqlSelectToken, tableData.Data);

            if (changedRows > 0)
            {
                return new
                {
                    changedRows = changedRows,
                    transactionId = TransactionManager.Instance.KeepTransaction(transaction, null)
                };
            }
            else
            {
                return null;
            }
        }
        [HttpPost]
        public object Commit(JObject data)
        {
            var transactionId = (Guid)data["transactionId"];
            return TransactionManager.Instance.CommitTransaction(transactionId);
        }
        [HttpPost]
        public object Rollback(JObject data)
        {
            var transactionId = (Guid)data["transactionId"];
            return TransactionManager.Instance.RollbackTransaction(transactionId);
        }

        [HttpPost]
        public object SelectSql(JObject data)
        {
            var tableId = (long)data["tableId"];
            var databaseId = (int)data["databaseId"];
            var tableName = (string)data["tableName"];
            var alias = (string)data["alias"];

            var databaseInfo = DatabaseManager.Instance.GetDatabaseInfo(databaseId);
            var script = SqlManager.Instance.CreateSelect(databaseInfo, tableName, alias);

            var sqlSelectToken = new SqlSelectToken(databaseInfo, script);

            return new
            {
                tableData = new
                {
                    tableId = tableId,
                    databaseId = databaseId,
                    script = script,
                    tables = string.Join(", ", sqlSelectToken.TableList.Select(c => c.TableName))
                },
                columns = sqlSelectToken.ColumnList
            };
        }
        [HttpPost]
        public object FormatSql(JObject data)
        {
            var script = (string)data["script"];

            return new
            {
                script = Utils.GetFormattedStatement(script, SqlFormatRule.AllColumnsInOneLine)
            };
        }

        [HttpPost]
        public object TableInfo(JObject data)
        {
            var databaseId = (int)data["databaseId"];
            var tableName = (string)data["tableName"];

            var databaseInfo = DatabaseManager.Instance.GetDatabaseInfo(databaseId);

            return new
            {
                table = databaseInfo.SchemaInfo.GetTable(tableName),
                columns = databaseInfo.SchemaInfo.GetColumns(tableName)
            };
        }

        private class TableData
        {
            public static TableData FromJObject(JObject obj)
            {
                return JsonConvert.DeserializeObject<TableData>(obj.ToString());
            }

            [JsonProperty("tableId")]
            public long TableId { get; private set; }
            [JsonProperty("databaseId")]
            public int DatabaseId { get; private set; }
            [JsonProperty("tableName")]
            public string TableName { get; private set; }
            [JsonProperty("columnName")]
            public string ColumnName { get; private set; }
            [JsonProperty("value")]
            public object Value { get; private set; }
            [JsonProperty("rows")]
            public int Rows { get; private set; }
            [JsonProperty("alias")]
            public string Alias { get; private set; }
            [JsonProperty("columns")]
            public List<SqlColumnToken> Columns { get; private set; }
            [JsonProperty("data")]
            public JArray Data { get; private set; }
            [JsonProperty("changeData")]
            public JArray ChangeData { get; private set; }
            [JsonProperty("referencedTableData")]
            public TableData ReferencedTableData { get; set; }
            [JsonProperty("script")]
            public string Script { get; set; }
        }
    }
}
