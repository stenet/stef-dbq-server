using System;
using System.CodeDom.Compiler;
using System.Linq;

namespace Stef.DatabaseQuery.Business.Managers.CSharps
{
    public class CompileErrorInfo
    {
        private CompilerError _CompilerError;

        public CompileErrorInfo(CompilerError compilerError)
        {
            _CompilerError = compilerError;

            ErrorText = _CompilerError.ErrorText;
            IsWarning = _CompilerError.IsWarning;
        }

        public string ErrorText { get; private set; }
        public bool IsWarning { get; private set; }

        public override string ToString()
        {
            return $"{(IsWarning ? "W" : "E")}, Line-No: {_CompilerError.Line:00000}, {ErrorText}";
        }
    }
}
