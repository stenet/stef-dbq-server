using Newtonsoft.Json.Linq;
using Stef.DatabaseQuery.Business.Managers.Sqls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Stef.DatabaseQuery.Business.Managers.CSharps
{
    public class ScriptExecutionContext
    {
        public ScriptExecutionContext(JObject data, List<SqlColumnToken> columns)
        {
            Data = data;
            Columns = columns;
        }

        public JObject Data { get; private set; }
        public List<SqlColumnToken> Columns { get; private set; }

        public object GetValue(string caption)
        {
            var column = Columns.FirstOrDefault(c => c.Caption == caption);
            if (column == null)
                return null;

            return Data[column.InternalFieldName].ToObject();
        }
    }
}
