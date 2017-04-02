using System;
using System.Linq;
using Stef.DatabaseQuery.Business.Enumerations;

namespace Stef.DatabaseQuery.Business.Managers.Scripts
{
    public class ScriptToken
    {
        public ScriptToken(string script)
        {
            ScriptType = Utils.GetScriptType(script);
            Script = script;

            Start = 0;
            End = script.Length;
        }
        public ScriptToken(string script, int start, int end)
            : this(script)
        {
            Start = start;
            End = end;
        }

        public ScriptType ScriptType { get; private set; }
        public string Script { get; private set; }

        public int Start { get; private set; }
        public int End { get; private set; }
    }
}
