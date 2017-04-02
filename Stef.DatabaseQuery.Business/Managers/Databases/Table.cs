using System;
using System.Linq;
using Newtonsoft.Json;

namespace Stef.DatabaseQuery.Business.Managers.Databases
{
    public class Table
    {
        public Table(string tableName, bool isView, string primaryKeyColumn)
        {
            TableName = tableName;
            IsView = isView;
            PrimaryKeyColumn = primaryKeyColumn;
        }

        [JsonProperty("tableName")]
        public string TableName { get; private set; }
        [JsonProperty("isView")]
        public bool IsView { get; private set; }
        [JsonProperty("primaryKeyColumn")]
        public string PrimaryKeyColumn { get; private set; }
    }
}
