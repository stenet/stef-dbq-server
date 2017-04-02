using System;
using System.Linq;
using Newtonsoft.Json;

namespace Stef.DatabaseQuery.Business.Managers.Sqls
{
    public class SqlColumnToken
    {
        public SqlColumnToken(string columnName, string alias, string titleAlias, string internalFieldName, Type type, string relatedTableName, string relatedColumnName, bool isValid)
        {
            ColumnName = columnName;
            Alias = alias;
            TitleAlias = titleAlias;
            InternalFieldName = internalFieldName;
            Type = type;
            RelatedTableName = relatedTableName;
            RelatedColumnName = relatedColumnName;
            IsValid = isValid;

            Caption = ToString();
        }

        [JsonProperty("columnName")]
        public string ColumnName { get; private set; }
        [JsonProperty("alias")]
        public string Alias { get; private set; }
        [JsonProperty("titleAlias")]
        public string TitleAlias { get; private set; }
        [JsonProperty("caption")]
        public string Caption { get; private set; }
        [JsonProperty("relatedTableName")]
        public string RelatedTableName { get; private set; }
        [JsonProperty("relatedColumnName")]
        public string RelatedColumnName { get; private set; }
        [JsonIgnore]
        public Type Type { get; private set; }

        [JsonProperty("internalFieldName")]
        public string InternalFieldName { get; set; }

        [JsonProperty("isValid")]
        public bool IsValid { get; private set; }

        public override string ToString()
        {
            if (TitleAlias != null)
                return TitleAlias;
            else if (Alias != null)
                return $"{Alias}.{ColumnName}";
            else
                return ColumnName;
        }
    }
}
