using System;
using System.Linq;

namespace Stef.DatabaseQuery.Business.Enumerations
{
    public enum ScriptType
    {
        Unknown,
        Query,
        NonQuery,
        Script,
        Commit,
        Rollback
    }
}
