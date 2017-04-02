using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stef.DatabaseQuery.Business.Managers.CSharps
{
    public class ScriptBuilder
    {
        public const string Namespace = "Stef.DatabaseQuery.Business.Managers.CSharps.Customs";
        private const string EvaluateClassNamePrefix = "Evaluate";
        private const string ExecuteClassNamePrefix = "Execute";

        public ScriptBuilder(ExecutionType scriptType, IEnumerable<string> usingList, IEnumerable<ScriptParameter> parameterList, string code)
        {
            ScriptType = scriptType;
            UsingList = usingList;
            ParameterList = parameterList;
            Code = code;

            HashCode = GetScriptHashCode();

            ClassName = GetScriptClassName();
        }

        public ExecutionType ScriptType { get; private set; }
        public IEnumerable<string> UsingList { get; private set; }
        public IEnumerable<ScriptParameter> ParameterList { get; private set; }
        public string Code { get; private set; }

        internal string ClassName { get; private set; }
        internal int HashCode { get; private set; }

        internal string GetClassCode()
        {
            var builder = new StringBuilder();

            builder.AppendLine("using System;");
            builder.AppendLine("using System.Linq;");
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine("using System.Collections;");

            UsingList
                .ToList()
                .ForEach(c => builder.AppendLine($"using {c};"));

            builder.AppendLine();

            builder.AppendLine($"namespace {Namespace}");
            builder.AppendLine("{");

            builder.AppendLine(GetClassSignature());
            builder.AppendLine("{");

            builder.AppendLine(GetMethodSignature());
            builder.AppendLine("{");

            builder.Append(GetParameterCasts());
            builder.AppendLine(Code);

            builder.AppendLine("}");

            builder.AppendLine("}");

            builder.AppendLine("}");

            return builder.ToString();
        }
        private string GetClassSignature()
        {
            switch (ScriptType)
            {
                case ExecutionType.Evaluate:
                    return $"public class {ClassName} : {typeof(IScriptEvaluate).Name}";
                case ExecutionType.Execute:
                    return $"public class {ClassName} : {typeof(IScriptExecute).Name}";
                default:
                    throw new NotImplementedException();
            }
        }
        private string GetMethodSignature()
        {
            switch (ScriptType)
            {
                case ExecutionType.Evaluate:
                    return "public object Evaluate(params object[] args)";
                case ExecutionType.Execute:
                    return "public void Execute(params object[] args)";
                default:
                    throw new NotImplementedException();
            }

        }
        private string GetParameterCasts()
        {
            var builder = new StringBuilder();

            var parameterIndex = 0;
            foreach (var parameter in ParameterList)
            {
                builder.AppendLine($"var {parameter.Name} = ({parameter.Type.FullName})args[{parameterIndex}];");
                parameterIndex++;
            }

            return builder.ToString();
        }

        private int GetScriptHashCode()
        {
            var builder = new StringBuilder();

            builder.AppendLine(ScriptType.ToString());
            builder.AppendLine(string.Join(";", UsingList));
            builder.AppendLine(string.Join(";", ParameterList));
            builder.AppendLine(Code);

            return Math.Abs(builder.ToString().GetHashCode());
        }
        private string GetScriptClassName()
        {
            switch (ScriptType)
            {
                case ExecutionType.Evaluate:
                    return $"{EvaluateClassNamePrefix}{HashCode}";
                case ExecutionType.Execute:
                    return $"{ExecuteClassNamePrefix}{HashCode}";
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
