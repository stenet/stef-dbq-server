using System;
using System.Collections.Generic;
using System.Linq;
using Stef.DatabaseQuery.Business.Enumerations;
using Stef.DatabaseQuery.Business.Managers.Databases;

namespace Stef.DatabaseQuery.Business.Managers.Sqls
{
    public class SqlToken
    {
        public SqlToken(DatabaseInfo databaseInfo, ScriptType scriptType, string script, int start, int end)
        {
            DatabaseInfo = databaseInfo;

            ScriptType = scriptType;
            Script = script;

            Start = start;
            End = end;

            ParameterList = Utils.GetParameters(script);
        }

        public DatabaseInfo DatabaseInfo { get; private set; }
        public string ProviderKey { get; private set; }
        public string Script { get; private set; }
        public ScriptType ScriptType { get; private set; }

        public IEnumerable<string> ParameterList { get; private set; }

        public int Start { get; private set; }
        public int End { get; private set; }
        public int Length
        {
            get
            {
                return End - Start;
            }
        }
    }
}
