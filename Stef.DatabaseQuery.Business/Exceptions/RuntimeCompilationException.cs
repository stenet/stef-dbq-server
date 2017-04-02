using Stef.DatabaseQuery.Business.Managers.CSharps;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Stef.DatabaseQuery.Business.Exceptions
{
    [Serializable]
    public sealed class RuntimeCompilationException : Exception
    {
        public IEnumerable<CompileErrorInfo> CompileErrorInfoList { get; private set; }
        public IEnumerable<string> ReferencedAssemblyList { get; private set; }

        public RuntimeCompilationException(
            string message,
            IEnumerable<CompileErrorInfo> compileErrorInfoList,
            IEnumerable<string> referencedAssemblyList)
            : base(message)
        {
            CompileErrorInfoList = compileErrorInfoList;
            ReferencedAssemblyList = referencedAssemblyList;
        }
    }
}
