using System;
using System.Linq;

namespace Stef.DatabaseQuery.Business.Managers.CSharps
{
    public interface IScriptExecute
    {
        void Execute(params object[] args);
    }
}
