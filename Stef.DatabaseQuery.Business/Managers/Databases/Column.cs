using System;
using System.Linq;
using Newtonsoft.Json;

namespace Stef.DatabaseQuery.Business.Managers.Databases
{
    public class Column
    {
        public Column(string tableName, string columnName, Type type, string typeName, int maxLength, bool isNullable)
        {
            TableName = tableName;
            ColumnName = columnName;
            Type = type;
            TypeName = typeName;
            MaxLength = maxLength;
            IsNullable = isNullable;
        }

        [JsonProperty("tableName")]
        public string TableName { get; private set; }
        [JsonProperty("columnName")]
        public string ColumnName { get; private set; }
        [JsonProperty("type")]
        public Type Type { get; private set; }
        [JsonProperty("typeName")]
        public string TypeName { get; private set; }
        [JsonProperty("maxLength")]
        public int MaxLength { get; private set; }
        [JsonProperty("isNullable")]
        public bool IsNullable { get; private set; }

        [JsonProperty("relatedTableName")]
        public string RelatedTableName { get; set; }
        [JsonProperty("relatedColumnName")]
        public string RelatedColumnName { get; set; }
    }
}
