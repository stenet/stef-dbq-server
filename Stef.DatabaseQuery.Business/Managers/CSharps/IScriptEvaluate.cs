using System;
using System.Linq;

namespace Stef.DatabaseQuery.Business.Managers.CSharps
{
    public interface IScriptEvaluate
    {
        object Evaluate(params object[] args);
    }
}
