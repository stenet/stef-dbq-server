using Newtonsoft.Json.Linq;
using Stef.DatabaseQuery.Business.Enumerations;
using Stef.DatabaseQuery.Business.Managers.ChangeData;
using Stef.DatabaseQuery.Business.Managers.CSharps;
using Stef.DatabaseQuery.Business.Managers.Sqls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Stef.DatabaseQuery.Business.Managers
{
    public class ChangeDataManager
    {
        private static object _Sync = new object();
        private static ChangeDataManager _Instance;

        public ChangeDataManager()
        {
        }

        public static ChangeDataManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_Sync)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new ChangeDataManager();
                        }
                    }
                }

                return _Instance;
            }
        }

        public object ChangeData(JArray parameterData, List<SqlColumnToken> parameterColumns, JArray changeData)
        {
            var runningDic = new Dictionary<JToken, int>();
            var changeDataConnectionDic = new Dictionary<int, ChangeDataConnection>();
            var scriptDic = new Dictionary<JToken, ScriptEngine>();

            foreach (JObject item in parameterData)
            {
                foreach (JObject changeDataItem in changeData)
                {
                    var type = (string)changeDataItem["type"];
                    var internalFieldName = (string)changeDataItem["internalFieldName"];

                    if (string.IsNullOrEmpty(internalFieldName))
                        continue;

                    switch (type)
                    {
                        case "sql":
                            var script = (string)changeDataItem["script"];
                            if (string.IsNullOrEmpty(script))
                                continue;

                            var databaseId = (int?)changeDataItem["databaseId"];
                            if (databaseId == null)
                                continue;

                            var databaseInfo = DatabaseManager.Instance.GetDatabaseInfo(databaseId.Value);

                            ChangeDataConnection changeDataConnection;

                            if (!changeDataConnectionDic.TryGetValue(databaseId.Value, out changeDataConnection))
                            {
                                var connection = databaseInfo.CreateConnection();
                                changeDataConnection = new ChangeDataConnection(connection);
                                changeDataConnectionDic.Add(databaseId.Value, changeDataConnection);
                            }

                            var scriptToken = ScriptManager.Instance.GetScriptToken(script, 0, 0);

                            switch (scriptToken.ScriptType)
                            {
                                case ScriptType.Query:
                                    var sqlSelectToken = new SqlSelectToken(databaseInfo, script);

                                    var result = SqlManager.Instance.ExecuteQuery(
                                        databaseInfo,
                                        changeDataConnection.Connection,
                                        changeDataConnection.Transaction,
                                        sqlSelectToken,
                                        1,
                                        parameterData: item,
                                        parameterColumns: parameterColumns);

                                    var data = (JArray)result["data"];
                                    if (data.Count == 0)
                                        continue;

                                    var obj = (JObject)data[0];
                                    if (obj.Properties().Count() == 0)
                                        continue;

                                    item[internalFieldName] = ((JProperty)obj.First).Value;
                                    SetChanged(item, internalFieldName);
                                    break;
                                case ScriptType.NonQuery:
                                    changeDataConnection.Changes += SqlManager.Instance.ExecuteNonQuery(
                                        databaseInfo,
                                        changeDataConnection.Connection,
                                        changeDataConnection.Transaction,
                                        script,
                                        parameterData: item,
                                        parameterColumns: parameterColumns);
                                    break;
                                default:
                                    break;
                            }

                            break;
                        case "running":
                            var runningToken = changeDataItem["running"];
                            if (runningToken == null || runningToken.Type != JTokenType.Integer)
                                continue;

                            int lastRunning;
                            if (!runningDic.TryGetValue(changeDataItem, out lastRunning))
                            {
                                lastRunning = (int)runningToken;
                            }

                            item[internalFieldName] = lastRunning;
                            SetChanged(item, internalFieldName);

                            runningDic[changeDataItem] = lastRunning + 1;
                            break;
                        case "text":
                            item[internalFieldName] = changeDataItem["text"];
                            SetChanged(item, internalFieldName);
                            break;
                        case "code":
                            var code = (string)changeDataItem["script"];
                            if (string.IsNullOrEmpty(code))
                                continue;

                            ScriptEngine scriptEngine;
                            if (!scriptDic.TryGetValue(changeDataItem, out scriptEngine))
                            {
                                var builder = new ScriptBuilder(
                                    ExecutionType.Evaluate,
                                    new List<string>(),
                                    new List<ScriptParameter>()
                                    {
                                        new ScriptParameter(typeof(ScriptExecutionContext), "eval")
                                    },
                                    code);

                                scriptEngine = ScriptEngine.GetScriptEngine(builder);
                                scriptDic.Add(changeDataItem, scriptEngine);
                            }

                            var context = new ScriptExecutionContext(item, parameterColumns);
                            var value = scriptEngine.Evaluate(context);
                            item[internalFieldName] = value == null ? null : JToken.FromObject(value);
                            SetChanged(item, internalFieldName);
                            break;
                        default:
                            break;
                    }
                }
            }

            var transactionList = new List<IDbTransaction>();
            var changedRows = 0;

            foreach (var changeDataConnection in changeDataConnectionDic)
            {
                if (changeDataConnection.Value.Changes == 0)
                {
                    changeDataConnection.Value.Transaction.Rollback();
                    changeDataConnection.Value.Connection.Dispose();
                }
                else
                {
                    transactionList.Add(changeDataConnection.Value.Transaction);
                    changedRows += changeDataConnection.Value.Changes;
                }
            }

            return new
            {
                data = parameterData,
                transactionId = transactionList.Any() ? (object)TransactionManager.Instance.KeepTransaction(transactionList, parameterData) : null,
                changedRows = changedRows
            };
        }
        private void SetChanged(JObject data, string internalFieldName)
        {
            var changes = data["_changed"];
            if (changes == null)
            {
                changes = new JObject();
                data["_changed"] = changes;
            }

            changes[internalFieldName] = true;

            var state = data["_state"];
            if (state == null || (int)state == 0)
                data["_state"] = 1;
        }
    }
}
