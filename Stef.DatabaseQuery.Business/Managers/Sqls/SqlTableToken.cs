using System;
using System.Linq;

namespace Stef.DatabaseQuery.Business.Managers.Sqls
{
    public class SqlTableToken
    {
        public SqlTableToken(string tableName, string alias, bool isValid)
        {
            TableName = tableName;
            Alias = alias;
            IsValid = isValid;
        }

        public string TableName { get; private set; }
        public string Alias { get; private set; }
        public bool IsValid { get; private set; }
    }
}
