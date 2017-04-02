using Stef.DatabaseQuery.Business.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Stef.DatabaseQuery.Business.Managers.CSharps
{
    public class ScriptEngine
    {
        private static object _SyncObject = new object();
        private static Dictionary<int, ScriptEngine> _ScriptEngineDic;

        static ScriptEngine()
        {
            _ScriptEngineDic = new Dictionary<int, ScriptEngine>();
        }
        public static ScriptEngine GetScriptEngine(ScriptBuilder scriptBuilder)
        {
            ScriptEngine scriptEngine;

            lock (_SyncObject)
            {
                if (!_ScriptEngineDic.TryGetValue(scriptBuilder.HashCode, out scriptEngine))
                {
                    scriptEngine = new ScriptEngine(scriptBuilder);

                    //nicht schön, aber im Debug-Modus klappt es irgendwie mit dem Lock nicht
                    _ScriptEngineDic[scriptBuilder.HashCode] = scriptEngine;
                }
            }

            return scriptEngine;
        }

        private Assembly _Assembly;
        private IScriptEvaluate _ScriptEvaluate;
        private IScriptExecute _ScriptExecute;

        private ScriptEngine(ScriptBuilder scriptBuilder)
        {
            var classCode = scriptBuilder.GetClassCode();

            try
            {
                Script = classCode;

                _Assembly = CSharpManager.Instance.CreateRuntimeAssembly(classCode, true);
            }
            catch (RuntimeCompilationException ex)
            {
                var errorMsg = string.Join(Environment.NewLine, ex.CompileErrorInfoList);
                throw new InvalidOperationException(errorMsg, ex);
            }

            var type = _Assembly.GetType($"{ScriptBuilder.Namespace}.{scriptBuilder.ClassName}");

            switch (scriptBuilder.ScriptType)
            {
                case ExecutionType.Evaluate:
                    _ScriptEvaluate = (IScriptEvaluate)Activator.CreateInstance(type);
                    break;
                case ExecutionType.Execute:
                    _ScriptExecute = (IScriptExecute)Activator.CreateInstance(type);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public string Script { get; private set; }

        public object Evaluate(params object[] args)
        {
            if (_ScriptEvaluate == null)
                throw new InvalidOperationException();

            return _ScriptEvaluate.Evaluate(args);
        }
        public void Execute(params object[] args)
        {
            if (_ScriptExecute == null)
                throw new InvalidOperationException();

            _ScriptExecute.Execute(args);
        }
    }
}