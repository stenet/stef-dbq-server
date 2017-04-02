using Stef.DatabaseQuery.Business.Managers.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Stef.DatabaseQuery.Business.Managers
{
    public class ScriptManager
    {
        private static object _SyncLock = new object();
        private static ScriptManager _Instance;

        private ScriptManager()
        {
        }

        public static ScriptManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_SyncLock)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new ScriptManager();
                        }
                    }
                }

                return _Instance;
            }
        }

        public List<ScriptToken> GetScriptTokens(string script)
        {
            var matches = Regex.Matches(script, ";", RegexOptions.Multiline)
                .OfType<Match>()
                .Where(c =>
                    Utils.IsOuterSegment(script, c.Index))
                .ToList();

            List<ScriptToken> resultList = new List<ScriptToken>();

            if (matches.Count == 0)
            {
                resultList.Add(new ScriptToken(script));
            }
            else
            {
                for (int i = 0; i < matches.Count; i++)
                {
                    var match = matches[i];

                    if (i == 0 && match.Index > 0)
                    {
                        resultList.Add(new ScriptToken(script.Substring(0, match.Index), 0, match.Index));
                    }

                    int start = match.Index + 1;
                    if (i + 1 == matches.Count)
                    {
                        resultList.Add(new ScriptToken(script.Substring(start), start, script.Length));
                    }
                    else
                    {
                        resultList.Add(new ScriptToken(script.Substring(start, matches[i + 1].Index - start), start, matches[i + 1].Index));
                    }
                }
            }

            return resultList;
        }
        public ScriptToken GetScriptToken(string script, int line, int column)
        {
            var scriptTokens = GetScriptTokens(script);

            var index = 0;
            for (int i = 0; i < line; i++)
            {
                index = script.IndexOf("\n", index + 1);
            }

            index += column;

            return scriptTokens
                .FirstOrDefault(c =>
                    c.Start <= index
                    && c.End >= index);
        }
    }
}
